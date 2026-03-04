using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AuraWellness.Application.Interfaces.External;
using AuraWellness.Application.Interfaces.Services;

namespace AuraWellness.API.Middleware;

/// <summary>
/// Handles a single bidirectional WebSocket session for a chat workspace.
/// - Read loop : receives { "type": "send_message", "content": "..." } from the browser
///               and forwards them to the chat service via REST/gRPC.
/// - Write loop: consumes the gRPC StreamMessages server-streaming RPC and pushes
///               each new message to the browser as a JSON text frame.
/// Both loops run concurrently; when either exits the other is cancelled.
/// </summary>
public class ChatWebSocketHandler(IChatAccessService chatAccessService, IChatServiceClient chatClient)
{
    private static readonly JsonSerializerOptions CamelCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task HandleAsync(HttpContext context, Guid buId, WebSocket ws)
    {
        var companyId  = Guid.Parse(context.User.FindFirstValue("companyId")!);
        var personId   = Guid.Parse(context.User.FindFirstValue("personId")!);
        var firstName  = context.User.FindFirstValue("firstName") ?? "";
        var lastName   = context.User.FindFirstValue("lastName")  ?? "";
        var senderName = $"{firstName} {lastName}".Trim();

        // Validate workspace existence + caller access before streaming anything.
        var workspace = await chatAccessService.GetWorkspaceAsync(buId, companyId);
        if (workspace is null)
        {
            await ws.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Workspace not found",
                CancellationToken.None);
            return;
        }

        var member = workspace.Members.FirstOrDefault(m => m.PersonId == personId);
        if (member is null || !member.HasAccess)
        {
            await ws.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Access denied",
                CancellationToken.None);
            return;
        }

        // Linked CTS so that when either loop finishes it cancels the other.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        // Serialise WebSocket sends so ReadLoop and WriteLoop never overlap.
        using var sendLock = new SemaphoreSlim(1, 1);

        // ReadLoop ends only when the browser closes the socket → drives session lifetime.
        // WriteLoop may die on transient gRPC errors without killing the session.
        var readTask  = ReadLoopAsync(ws, buId, workspace.WorkspaceId, personId, senderName, companyId, sendLock, cts.Token);
        var writeTask = WriteLoopAsync(ws, workspace.WorkspaceId, sendLock, cts.Token);

        await Task.WhenAny(readTask, writeTask);
        await cts.CancelAsync();

        if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
        {
            try
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing",
                    CancellationToken.None);
            }
            catch
            {
                // Best-effort close — ignore if the socket is already torn down.
            }
        }
    }

    /// <summary>Receives frames from the browser and calls SendMessage for each valid payload.
    /// Immediately echoes the saved message back to the sender so it appears without waiting
    /// for the Redis pub/sub round-trip.</summary>
    private async Task ReadLoopAsync(
        WebSocket ws,
        Guid buId,
        Guid workspaceId,
        Guid personId,
        string senderName,
        Guid companyId,
        SemaphoreSlim sendLock,
        CancellationToken ct)
    {
        var buffer = new byte[8 * 1024];

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await ws.ReceiveAsync(buffer, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (WebSocketException)         { break; }

            if (result.MessageType == WebSocketMessageType.Close) break;
            if (result.MessageType != WebSocketMessageType.Text)  continue;

            try
            {
                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var incoming = JsonSerializer.Deserialize<WsIncomingMessage>(text,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (incoming?.Type == "send_message" && !string.IsNullOrWhiteSpace(incoming.Content))
                {
                    var saved = await chatAccessService.SendMessageAsync(buId, personId, senderName,
                        incoming.Content, companyId, ct);

                    // Echo the saved message back to the sender immediately.
                    // This is the shape the browser expects ("id", not "messageId").
                    // WriteLoop will also broadcast it via pub/sub; the frontend deduplicates by id.
                    if (saved is not null)
                    {
                        var echo = new
                        {
                            Id         = saved.MessageId,
                            PersonId   = saved.PersonId,
                            SenderName = saved.SenderName,
                            Content    = saved.Content,
                            CreatedAt  = saved.CreatedAt,
                        };
                        await SendJsonAsync(ws, echo, sendLock, ct);
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch
            {
                // Ignore malformed frames or transient service errors.
            }
        }
    }

    /// <summary>Pulls messages from the gRPC streaming RPC and pushes them to the browser.
    /// gRPC errors are caught so they never kill the whole WebSocket session — the sender's
    /// own messages are still echoed immediately by ReadLoop.</summary>
    private async Task WriteLoopAsync(WebSocket ws, Guid workspaceId, SemaphoreSlim sendLock, CancellationToken ct)
    {
        try
        {
            await foreach (var msg in chatClient.StreamMessagesAsync(workspaceId, ct))
            {
                if (ws.State != WebSocketState.Open) break;
                await SendJsonAsync(ws, msg, sendLock, ct);
            }
        }
        catch (OperationCanceledException) { /* expected on shutdown */ }
        catch
        {
            // Swallow gRPC / network errors so the WebSocket session stays alive.
            // The sender still sees their own messages via the ReadLoop echo.
        }
    }

    /// <summary>Serialises <paramref name="payload"/> as camelCase JSON and sends it,
    /// using <paramref name="sendLock"/> to prevent overlapping sends.</summary>
    private async Task SendJsonAsync(WebSocket ws, object payload, SemaphoreSlim sendLock, CancellationToken ct)
    {
        var json  = JsonSerializer.Serialize(payload, CamelCase);
        var bytes = Encoding.UTF8.GetBytes(json);
        await sendLock.WaitAsync(ct);
        try
        {
            if (ws.State == WebSocketState.Open)
                await ws.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct);
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException)         { }
        finally
        {
            sendLock.Release();
        }
    }

    // ---------------- inner DTO ----------------

    private sealed record WsIncomingMessage(string? Type, string? Content);
}

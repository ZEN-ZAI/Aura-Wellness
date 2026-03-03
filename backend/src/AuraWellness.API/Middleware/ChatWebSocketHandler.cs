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

        var readTask  = ReadLoopAsync(ws, buId, workspace.WorkspaceId, personId, senderName, companyId, cts.Token);
        var writeTask = WriteLoopAsync(ws, workspace.WorkspaceId, cts.Token);

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

    /// <summary>Receives frames from the browser and calls SendMessage for each valid payload.</summary>
    private async Task ReadLoopAsync(
        WebSocket ws,
        Guid buId,
        Guid workspaceId,
        Guid personId,
        string senderName,
        Guid companyId,
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
                    await chatAccessService.SendMessageAsync(buId, personId, senderName,
                        incoming.Content, companyId, ct);
                }
            }
            catch
            {
                // Ignore malformed frames or transient service errors.
            }
        }
    }

    /// <summary>Pulls messages from the gRPC streaming RPC and pushes them to the browser.</summary>
    private async Task WriteLoopAsync(WebSocket ws, Guid workspaceId, CancellationToken ct)
    {
        try
        {
            await foreach (var msg in chatClient.StreamMessagesAsync(workspaceId, ct))
            {
                if (ws.State != WebSocketState.Open) break;

                var json  = JsonSerializer.Serialize(msg, CamelCase);
                var bytes = Encoding.UTF8.GetBytes(json);

                try
                {
                    await ws.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct);
                }
                catch (OperationCanceledException) { break; }
                catch (WebSocketException)         { break; }
            }
        }
        catch (OperationCanceledException) { /* expected on shutdown */ }
    }

    // ---------------- inner DTO ----------------

    private sealed record WsIncomingMessage(string? Type, string? Content);
}

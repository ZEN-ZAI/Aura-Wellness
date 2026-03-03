using System.Security.Claims;
using AuraWellness.API.Middleware;
using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraWellness.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController(IChatAccessService chatAccessService, ChatWebSocketHandler wsHandler) : ControllerBase
{
    [HttpGet("workspace/{buId:guid}")]
    public async Task<IActionResult> GetWorkspace(Guid buId, CancellationToken ct)
    {
        var companyId = GetCompanyId();
        var result = await chatAccessService.GetWorkspaceAsync(buId, companyId, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPut("workspace/{buId:guid}/members/{personId:guid}/access")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> UpdateAccess(Guid buId, Guid personId, [FromBody] UpdateChatAccessRequest request, CancellationToken ct)
    {
        var companyId = GetCompanyId();
        await chatAccessService.UpdateAccessAsync(buId, personId, request.HasAccess, companyId, ct);
        return NoContent();
    }

    [HttpGet("workspace/{buId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid buId, [FromQuery] int limit = 50, [FromQuery] DateTime? before = null, CancellationToken ct = default)
    {
        var companyId = GetCompanyId();
        var personId = GetPersonId();
        try
        {
            var result = await chatAccessService.GetMessagesAsync(buId, personId, companyId, limit, before, ct);
            if (result is null) return NotFound();
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
    }

    [HttpPost("workspace/{buId:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid buId, [FromBody] SendChatMessageRequest request, CancellationToken ct)
    {
        var companyId = GetCompanyId();
        var personId = GetPersonId();
        var firstName = User.FindFirstValue("firstName") ?? "";
        var lastName = User.FindFirstValue("lastName") ?? "";
        var senderName = $"{firstName} {lastName}".Trim();

        try
        {
            var result = await chatAccessService.SendMessageAsync(buId, personId, senderName, request.Content, companyId, ct);
            if (result is null) return NotFound();
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// WebSocket endpoint — bidirectional real-time chat.
    /// The browser sends  { "type": "send_message", "content": "..." } frames.
    /// The server pushes  ChatMessageServiceDto  JSON frames for every new message.
    /// </summary>
    [HttpGet("workspace/{buId:guid}/ws")]
    public async Task ChatWebSocket(Guid buId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await wsHandler.HandleAsync(HttpContext, buId, ws);
    }

    private Guid GetCompanyId() => Guid.Parse(User.FindFirstValue("companyId")!);
    private Guid GetPersonId() => Guid.Parse(User.FindFirstValue("personId")!);
}

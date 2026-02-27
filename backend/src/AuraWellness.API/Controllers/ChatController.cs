using System.Security.Claims;
using AuraWellness.Application.DTOs;
using AuraWellness.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraWellness.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController(IChatAccessService chatAccessService) : ControllerBase
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

    private Guid GetCompanyId() => Guid.Parse(User.FindFirstValue("companyId")!);
}

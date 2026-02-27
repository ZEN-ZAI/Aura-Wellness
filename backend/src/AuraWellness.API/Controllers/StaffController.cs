using System.Security.Claims;
using AuraWellness.Application.DTOs;
using AuraWellness.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraWellness.API.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize]
public class StaffController(IStaffService staffService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var companyId = GetCompanyId();
        var result = await staffService.GetAllAsync(companyId, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequest request, CancellationToken ct)
    {
        var companyId = GetCompanyId();
        var result = await staffService.CreateAsync(companyId, request, ct);
        return CreatedAtAction(nameof(GetAll), result);
    }

    [HttpPut("{personId:guid}/role")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> UpdateRole(Guid personId, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var companyId = GetCompanyId();
        var buId = GetBuId();
        await staffService.UpdateRoleAsync(personId, buId, companyId, request.Role, ct);
        return NoContent();
    }

    private Guid GetCompanyId() => Guid.Parse(User.FindFirstValue("companyId")!);
    private Guid GetBuId() => Guid.Parse(User.FindFirstValue("buId")!);
}

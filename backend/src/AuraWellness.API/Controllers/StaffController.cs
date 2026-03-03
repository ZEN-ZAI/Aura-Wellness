using System.Security.Claims;
using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.Services;
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

    [HttpGet("persons")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetPersons(CancellationToken ct)
    {
        var companyId = GetCompanyId();
        var result = await staffService.GetPersonsAsync(companyId, ct);
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

    [HttpPost("enroll")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Enroll([FromBody] EnrollExistingStaffRequest request, CancellationToken ct)
    {
        var companyId = GetCompanyId();
        try
        {
            var result = await staffService.EnrollExistingAsync(companyId, request, ct);
            return CreatedAtAction(nameof(GetAll), result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{personId:guid}/role")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> UpdateRole(Guid personId, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var companyId = GetCompanyId();
        await staffService.UpdateRoleAsync(personId, request.BuId, companyId, request.Role, ct);
        return NoContent();
    }

    private Guid GetCompanyId() => Guid.Parse(User.FindFirstValue("companyId")!);
}

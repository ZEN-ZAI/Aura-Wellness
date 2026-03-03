using System.Security.Claims;
using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraWellness.API.Controllers;

[ApiController]
[Route("api/business-units")]
[Authorize]
public class BusinessUnitsController(IBusinessUnitService buService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var companyId = GetCompanyId();
        var result = await buService.GetAllAsync(companyId, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Create([FromBody] CreateBuRequest request, CancellationToken ct)
    {
        var companyId = GetCompanyId();
        var personId = GetPersonId();
        var result = await buService.CreateAsync(companyId, personId, request, ct);
        return CreatedAtAction(nameof(GetAll), result);
    }

    private Guid GetCompanyId() => Guid.Parse(User.FindFirstValue("companyId")!);
    private Guid GetPersonId() => Guid.Parse(User.FindFirstValue("personId")!);
}

using AuraWellness.Application.DTOs;
using AuraWellness.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuraWellness.API.Controllers;

[ApiController]
[Route("api/companies")]
public class CompaniesController(IOnboardingService onboardingService) : ControllerBase
{
    [HttpPost("onboard")]
    public async Task<IActionResult> Onboard([FromBody] OnboardRequest request, CancellationToken ct)
    {
        var result = await onboardingService.OnboardAsync(request, ct);
        return CreatedAtAction(nameof(Onboard), result);
    }
}

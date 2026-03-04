using AuraWellness.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraWellness.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Owner")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpPost("reset-db")]
    public async Task<IActionResult> ResetDb(CancellationToken ct)
    {
        await adminService.ResetDatabaseAsync(ct);
        return NoContent();
    }

    [HttpPost("reset-redis")]
    public async Task<IActionResult> ResetRedis(CancellationToken ct)
    {
        await adminService.ResetRedisAsync(ct);
        return NoContent();
    }
}

using System.Security.Claims;
using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraWellness.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        // Validate email
        if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
        {
            return BadRequest(new { error = "Valid email required." });
        }

        var result = await authService.LoginAsync(request, ct);

        if (result is List<BuChoiceResponse> choices)
            return Ok(new { requiresBuSelection = true, choices });

        return Ok(result);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Ok(new { claims, isInRoleOwner = User.IsInRole("Owner") });
    }

    [HttpPut("password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var personId = Guid.Parse(User.FindFirstValue("personId")!);
        var buId = Guid.Parse(User.FindFirstValue("buId")!);
        await authService.ChangePasswordAsync(personId, buId, request.CurrentPassword, request.NewPassword, ct);
        return NoContent();
    }
}

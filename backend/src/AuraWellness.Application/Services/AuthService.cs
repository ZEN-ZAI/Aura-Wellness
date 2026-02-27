using AuraWellness.Application.DTOs;
using AuraWellness.Domain.Interfaces;

namespace AuraWellness.Application.Services;

public interface IAuthService
{
    Task<object> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public class AuthService(
    IBuStaffProfileRepository profileRepo,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtService) : IAuthService
{
    public async Task<object> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var profiles = await profileRepo.GetByEmailMultipleAsync(request.Email, ct);
        if (profiles.Count == 0)
            throw new UnauthorizedAccessException("Invalid credentials.");

        // If multiple BUs have this email and no BuId specified, return choices
        if (profiles.Count > 1 && request.BuId is null)
        {
            return profiles.Select(p => new BuChoiceResponse(p.BuId, p.BusinessUnit.Name)).ToList();
        }

        var profile = request.BuId.HasValue
            ? profiles.FirstOrDefault(p => p.BuId == request.BuId.Value)
            : profiles[0];

        if (profile is null)
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!passwordHasher.Verify(request.Password, profile.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var token = jwtService.GenerateToken(profile);
        return new LoginResponse(
            token,
            profile.PersonId,
            profile.BuId,
            profile.Person.CompanyId,
            profile.Role.ToString(),
            profile.Person.FirstName,
            profile.Person.LastName);
    }
}

using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.Services;
using AuraWellness.Domain.Interfaces;

namespace AuraWellness.Application.Services;

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

        // Verify the password is correct for at least one profile before any BU selection.
        // This prevents leaking BU membership info on invalid credentials.
        var validProfiles = profiles
            .Where(p => passwordHasher.Verify(request.Password, p.PasswordHash))
            .ToList();

        if (validProfiles.Count == 0)
            throw new UnauthorizedAccessException("Invalid credentials.");

        // If multiple BUs have a valid profile and no BuId specified, return choices
        if (validProfiles.Count > 1 && request.BuId is null)
        {
            return validProfiles.Select(p => new BuChoiceResponse(p.BuId, p.BusinessUnit.Name)).ToList();
        }

        var profile = request.BuId.HasValue
            ? validProfiles.FirstOrDefault(p => p.BuId == request.BuId.Value)
            : validProfiles[0];

        if (profile is null)
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

    public async Task ChangePasswordAsync(Guid personId, Guid buId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var profile = await profileRepo.GetByPersonAndBuAsync(personId, buId, ct)
            ?? throw new UnauthorizedAccessException("Profile not found.");

        if (!passwordHasher.Verify(currentPassword, profile.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        profile.UpdatePassword(passwordHasher.Hash(newPassword));
        await profileRepo.SaveChangesAsync(ct);
    }
}

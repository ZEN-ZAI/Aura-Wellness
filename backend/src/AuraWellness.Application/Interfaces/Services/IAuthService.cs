using AuraWellness.Application.DTOs;

namespace AuraWellness.Application.Interfaces.Services;

public interface IAuthService
{
    Task<object> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid personId, Guid buId, string currentPassword, string newPassword, CancellationToken ct = default);
}

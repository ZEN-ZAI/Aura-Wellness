using AuraWellness.Application.DTOs;

namespace AuraWellness.Application.Interfaces.Services;

public interface IOnboardingService
{
    Task<OnboardResponse> OnboardAsync(OnboardRequest request, CancellationToken ct = default);
}

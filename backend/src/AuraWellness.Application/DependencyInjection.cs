using AuraWellness.Application.Interfaces.Services;
using AuraWellness.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AuraWellness.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IBusinessUnitService, BusinessUnitService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IChatAccessService, ChatAccessService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}

namespace AuraWellness.Application.Interfaces.Services;

public interface IAdminService
{
    Task ResetDatabaseAsync(CancellationToken ct = default);
    Task ResetRedisAsync(CancellationToken ct = default);
}

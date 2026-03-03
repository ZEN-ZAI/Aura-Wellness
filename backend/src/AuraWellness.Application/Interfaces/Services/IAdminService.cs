namespace AuraWellness.Application.Interfaces.Services;

public interface IAdminService
{
    Task ResetDatabaseAsync(CancellationToken ct = default);
}

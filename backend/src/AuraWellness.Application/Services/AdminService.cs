using AuraWellness.Application.Interfaces.External;
using AuraWellness.Application.Interfaces.Services;

namespace AuraWellness.Application.Services;

public class AdminService(IDatabaseResetter dbResetter) : IAdminService
{
    public Task ResetDatabaseAsync(CancellationToken ct = default) =>
        dbResetter.ResetToSeedAsync(ct);
}

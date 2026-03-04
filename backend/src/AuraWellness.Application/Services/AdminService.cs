using AuraWellness.Application.Interfaces.External;
using AuraWellness.Application.Interfaces.Services;

namespace AuraWellness.Application.Services;

public class AdminService(IDatabaseResetter dbResetter, IRedisResetter redisResetter) : IAdminService
{
    public Task ResetDatabaseAsync(CancellationToken ct = default) =>
        dbResetter.ResetToSeedAsync(ct);

    public Task ResetRedisAsync(CancellationToken ct = default) =>
        redisResetter.ResetAsync(ct);
}

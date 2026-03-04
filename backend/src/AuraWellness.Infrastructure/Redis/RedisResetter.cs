using AuraWellness.Application.Interfaces.External;
using StackExchange.Redis;

namespace AuraWellness.Infrastructure.Redis;

public class RedisResetter(IConnectionMultiplexer redis) : IRedisResetter
{
    public async Task ResetAsync(CancellationToken ct = default)
    {
        var endpoints = redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = redis.GetServer(endpoint);
            await server.FlushAllDatabasesAsync();
        }
    }
}

namespace AuraWellness.Application.Interfaces.External;

public interface IRedisResetter
{
    Task ResetAsync(CancellationToken ct = default);
}

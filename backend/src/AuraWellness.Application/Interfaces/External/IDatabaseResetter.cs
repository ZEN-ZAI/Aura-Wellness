namespace AuraWellness.Application.Interfaces.External;

/// <summary>
/// Low-level contract: wipes all application data and restores the initial seed.
/// Implemented in the Infrastructure layer; injected into the Application layer.
/// </summary>
public interface IDatabaseResetter
{
    Task ResetToSeedAsync(CancellationToken ct = default);
}

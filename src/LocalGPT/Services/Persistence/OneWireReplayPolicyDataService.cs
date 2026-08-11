using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Provides one wire replay policy data service operations.
/// </summary>
public sealed class OneWireReplayPolicyDataService(
    ILogger<OneWireReplayPolicyDataService> logger) : IOneWireReplayPolicyDataService
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly OneWireReplayPolicySnapshot snapshot = new()
    {
        Retention = TimeSpan.FromMinutes(15),
        AllowedFutureSkew = TimeSpan.FromMinutes(2),
        CleanupInterval = 64,
        MaximumTrackedMessages = 4096
    };

    /// <summary>
    /// Gets snapshot.
    /// </summary>
    public OneWireReplayPolicySnapshot GetSnapshot()
    {
        try
        {
            logger.LogTrace($"Returned the configured 1-Wire replay policy.");
            return snapshot;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not return the configured 1-Wire replay policy.");
            throw;
        }
    }
}

using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Coordinates one wire replay policy behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
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
    /// Retrieves snapshot as part of the one wire replay policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The one wire replay policy snapshot produced by the operation.</returns>
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

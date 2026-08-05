using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Provides the persisted first-run guide, local model discovery summary, installer profiles and council quick starts.
/// </summary>
[DocumentationUpdated("2.1.20")]
public interface IFirstRunOnboardingService
{
    /// <summary>
    /// Builds the current onboarding status and optionally probes supported loopback AI providers.
    /// </summary>
    /// <param name="refreshConnectivity">When true, runs a bounded loopback provider discovery before returning.</param>
    /// <param name="cancellationToken">Cancels the asynchronous status operation.</param>
    /// <returns>A task that completes with the current onboarding status.</returns>
    Task<FirstRunOnboardingStatus> GetStatusAsync(bool refreshConnectivity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists that the current user completed or dismissed the first-run guide.
    /// </summary>
    /// <param name="userConfirmed">Must be true to record completion.</param>
    /// <param name="cancellationToken">Cancels the asynchronous persistence operation.</param>
    /// <returns>A task that completes when the completion flag has been stored.</returns>
    Task CompleteAsync(bool userConfirmed, CancellationToken cancellationToken = default);
}

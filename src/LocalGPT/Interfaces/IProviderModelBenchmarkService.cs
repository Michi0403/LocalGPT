using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for provider model benchmark behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IProviderModelBenchmarkService
{
    /// <summary>
    /// Performs run as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The provider model benchmark report produced by the operation.</returns>
    Task<ProviderModelBenchmarkReport> RunAsync(
        ProviderModelBenchmarkRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies recommendations as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="report">Report value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="presetName">Preset name value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="makeDefault">Value indicating whether make default should apply to this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<CouncilModelPreset>> ApplyRecommendationsAsync(
        ProviderModelBenchmarkReport report,
        string presetName,
        bool makeDefault,
        bool userConfirmed,
        CancellationToken cancellationToken = default);
}

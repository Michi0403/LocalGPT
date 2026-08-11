using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the provider model benchmark service contract.
/// </summary>
public interface IProviderModelBenchmarkService
{
    /// <summary>
    /// Runs the run async operation.
    /// </summary>
    Task<ProviderModelBenchmarkReport> RunAsync(
        ProviderModelBenchmarkRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies recommendations async.
    /// </summary>
    Task<IReadOnlyList<CouncilModelPreset>> ApplyRecommendationsAsync(
        ProviderModelBenchmarkReport report,
        string presetName,
        bool makeDefault,
        bool userConfirmed,
        CancellationToken cancellationToken = default);
}

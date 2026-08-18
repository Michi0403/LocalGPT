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

    /// <summary>Returns recent durable benchmark evidence archives without loading their potentially large task payloads.</summary>
    /// <param name="maxCount">Maximum number of report archives to return, newest first.</param>
    /// <param name="cancellationToken">Cancellation token for local evidence enumeration.</param>
    /// <returns>Recent durable benchmark evidence descriptors.</returns>
    Task<IReadOnlyList<ProviderModelBenchmarkStoredEvidence>> GetStoredEvidenceAsync(
        int maxCount = 20,
        CancellationToken cancellationToken = default);

    /// <summary>Loads one durable benchmark report archive by run identifier.</summary>
    /// <param name="runId">Benchmark run identifier.</param>
    /// <param name="cancellationToken">Cancellation token for local evidence loading.</param>
    /// <returns>The stored benchmark report, or null when no report archive exists.</returns>
    Task<ProviderModelBenchmarkReport?> LoadStoredEvidenceAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    /// <summary>Loads the full-fidelity durable evidence for one measured task.</summary>
    /// <param name="artifactId">LocalGPT-owned relative artifact identifier retained by the task result.</param>
    /// <param name="cancellationToken">Cancellation token for local evidence loading.</param>
    /// <returns>The full task evidence archive, or null when the artifact does not exist or is invalid.</returns>
    Task<ProviderModelBenchmarkTaskEvidenceArchive?> LoadTaskEvidenceAsync(
        string artifactId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores successful measured recommendations as a hardware-spooler performance profile without changing Council membership.
    /// </summary>
    /// <param name="report">Completed benchmark report whose provider-qualified recommendations should be persisted.</param>
    /// <param name="presetName">User-visible profile name.</param>
    /// <param name="userConfirmed">Whether the user explicitly approved the benchmark/profile operation.</param>
    /// <param name="cancellationToken">Cancellation token for persistence.</param>
    /// <returns>The durable performance profile created or updated for the benchmark run.</returns>
    Task<HardwarePerformancePreset> SavePerformancePresetAsync(
        ProviderModelBenchmarkReport report,
        string presetName,
        bool userConfirmed,
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

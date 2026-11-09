using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Configuration steward: diagnoses configuration and data with deterministic checks and turns findings into
/// Human Collaboration approval requests with selectable options. It never writes without an approved option.
/// </summary>
public interface IConfigurationStewardService
{
    /// <summary>Runs all (or one category of) configuration health checks.</summary>
    /// <param name="category">Optional category filter; empty runs every check.</param>
    /// <param name="cancellationToken">Cancels the diagnosis.</param>
    /// <returns>Findings with fingerprints, most severe first.</returns>
    Task<IReadOnlyList<ConfigurationFinding>> DiagnoseAsync(string? category = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests or completes the repair of one finding. The first call queues an approval request with the finding's
    /// options; a later call after approval applies the selected option once with compare-and-set semantics.
    /// </summary>
    /// <param name="findingKey">Finding key returned by <see cref="DiagnoseAsync"/>.</param>
    /// <param name="requestedBy">Actor shown in the approval request.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The repair outcome.</returns>
    Task<ConfigurationRepairOutcome> RequestRepairAsync(string findingKey, string requestedBy, CancellationToken cancellationToken = default);
}

using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// One deterministic, read-only configuration or data check of the configuration steward. Checks never write; they
/// describe what they observed, what it was compared with and which exact compare-and-set options could repair it.
/// </summary>
public interface IConfigurationHealthCheck
{
    /// <summary>Gets the stable check key.</summary>
    /// <value>Check key.</value>
    string CheckKey { get; }

    /// <summary>Gets the category used for filtering (council, runtime-values, teams, tools).</summary>
    /// <value>Category.</value>
    string Category { get; }

    /// <summary>Inspects the current data and returns findings.</summary>
    /// <param name="policy">Current steward thresholds.</param>
    /// <param name="cancellationToken">Cancels the inspection.</param>
    /// <returns>Findings without fingerprints; the steward computes them.</returns>
    Task<IReadOnlyList<ConfigurationFinding>> InspectAsync(ConfigurationStewardPolicy policy, CancellationToken cancellationToken = default);
}

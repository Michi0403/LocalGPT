using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Applies one human-approved repair option with compare-and-set semantics inside a single transaction.</summary>
public interface IConfigurationRepairApplier
{
    /// <summary>Applies all changes of the option, or none when any stored value differs from the observed one.</summary>
    /// <param name="finding">Finding whose option was approved.</param>
    /// <param name="option">Approved option.</param>
    /// <param name="cancellationToken">Cancels the transaction.</param>
    /// <returns>Applied or state-changed outcome.</returns>
    Task<ConfigurationRepairOutcome> ApplyAsync(ConfigurationFinding finding, ConfigurationRepairOption option, CancellationToken cancellationToken = default);
}

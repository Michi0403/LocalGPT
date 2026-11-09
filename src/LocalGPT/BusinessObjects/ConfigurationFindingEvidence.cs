namespace LocalGPT.BusinessObjects;

/// <summary>
/// One observed fact behind a configuration finding: where it was read, what was stored and what it was compared with.
/// Stored data is evidence, not truth; the reference explains what the value was checked against.
/// </summary>
public sealed class ConfigurationFindingEvidence
{
    /// <summary>Gets or sets where the value was read, for example <c>SystemVariables.DefaultCouncilResourceLoadPercent</c>.</summary>
    /// <value>Source location of the observed value.</value>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets the observed value; sensitive or large values are masked or summarized.</summary>
    /// <value>Observed value as read.</value>
    public string ObservedValue { get; set; } = string.Empty;

    /// <summary>Gets or sets what the value was compared with (definition default, measured count, documented rule).</summary>
    /// <value>Reference used for the comparison.</value>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Gets or sets a short explanation of what this evidence does and does not prove.</summary>
    /// <value>Explanatory note.</value>
    public string Note { get; set; } = string.Empty;
}

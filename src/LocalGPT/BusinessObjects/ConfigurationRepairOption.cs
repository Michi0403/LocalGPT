namespace LocalGPT.BusinessObjects;

/// <summary>One selectable answer of a configuration repair request; an option without changes keeps the current state.</summary>
public sealed class ConfigurationRepairOption
{
    /// <summary>Gets or sets the stable option key within its finding.</summary>
    /// <value>Option key.</value>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the short label shown as a selectable answer in the approval card.</summary>
    /// <value>Option label.</value>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets what choosing the option does and what it costs.</summary>
    /// <value>Option description.</value>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the exact changes applied when the option is approved; empty keeps the current state.</summary>
    /// <value>Compare-and-set changes.</value>
    public List<ConfigurationRepairChange> Changes { get; set; } = [];
}

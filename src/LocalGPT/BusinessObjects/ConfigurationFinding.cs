namespace LocalGPT.BusinessObjects;

/// <summary>
/// A deterministic configuration or data observation produced by a configuration health check, with its evidence,
/// the limits of that evidence and the selectable repair options a human may approve.
/// </summary>
public sealed class ConfigurationFinding
{
    /// <summary>Gets or sets the stable finding key used to request a repair.</summary>
    /// <value>Finding key.</value>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the key of the health check that produced the finding.</summary>
    /// <value>Health check key.</value>
    public string CheckKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the finding category (council, runtime-values, teams, tools).</summary>
    /// <value>Category.</value>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the severity: Info, Warning or Error.</summary>
    /// <value>Severity.</value>
    public string Severity { get; set; } = "Info";

    /// <summary>Gets or sets the short title.</summary>
    /// <value>Title.</value>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the explanation of the observed effect.</summary>
    /// <value>Summary.</value>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gets or sets the observed facts behind the finding.</summary>
    /// <value>Evidence records.</value>
    public List<ConfigurationFindingEvidence> Evidence { get; set; } = [];

    /// <summary>Gets or sets the selectable repair options; empty for report-only findings.</summary>
    /// <value>Repair options.</value>
    public List<ConfigurationRepairOption> Options { get; set; } = [];

    /// <summary>Gets or sets the recommended option key, preselected in the approval card.</summary>
    /// <value>Recommended option key.</value>
    public string RecommendedOptionKey { get; set; } = string.Empty;

    /// <summary>Gets or sets what the evidence cannot prove (intent, reasons outside LocalGPT).</summary>
    /// <value>Verification note.</value>
    public string VerificationNote { get; set; } = string.Empty;

    /// <summary>Gets or sets the fingerprint of key, evidence and options; a changed fingerprint requires a new decision.</summary>
    /// <value>Lower-case SHA-256 fingerprint.</value>
    public string Fingerprint { get; set; } = string.Empty;
}

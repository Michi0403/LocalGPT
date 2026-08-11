namespace LocalGPT.BusinessObjects;

/// <summary>Current dependency-injected function and organic-skill directory persisted for AI context.</summary>
public sealed class RuntimeCapabilityDirectorySnapshot
{
    /// <summary>
    /// Gets or sets synchronized at UTC.
    /// </summary>
    public DateTime SynchronizedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets functions.
    /// </summary>
    public IReadOnlyList<DxaichatFunctionInfo> Functions { get; set; } = [];
    /// <summary>
    /// Gets or sets catalog entries.
    /// </summary>
    public IReadOnlyList<DxAiFunctionCatalogEntry> CatalogEntries { get; set; } = [];
    /// <summary>
    /// Gets or sets skills.
    /// </summary>
    public IReadOnlyList<OneWireSkillDescriptor> Skills { get; set; } = [];
    /// <summary>
    /// Gets or sets warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}

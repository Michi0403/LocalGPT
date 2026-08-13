namespace LocalGPT.BusinessObjects;

/// <summary>Current dependency-injected function and organic-skill directory persisted for AI context.</summary>
public sealed class RuntimeCapabilityDirectorySnapshot
{
    /// <summary>
    /// Gets or sets the synchronized at UTC associated with this runtime capability directory snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The synchronized at UTC value exposed by <see cref="RuntimeCapabilityDirectorySnapshot"/>.</value>
    public DateTime SynchronizedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the functions collection maintained or exposed by this runtime capability directory snapshot instance for downstream processing.
    /// </summary>
    /// <value>The functions value exposed by <see cref="RuntimeCapabilityDirectorySnapshot"/>.</value>
    public IReadOnlyList<DxaichatFunctionInfo> Functions { get; set; } = [];
    /// <summary>
    /// Gets or sets the catalog entries collection maintained or exposed by this runtime capability directory snapshot instance for downstream processing.
    /// </summary>
    /// <value>The catalog entries value exposed by <see cref="RuntimeCapabilityDirectorySnapshot"/>.</value>
    public IReadOnlyList<DxAiFunctionCatalogEntry> CatalogEntries { get; set; } = [];
    /// <summary>
    /// Gets or sets the skills collection maintained or exposed by this runtime capability directory snapshot instance for downstream processing.
    /// </summary>
    /// <value>The skills value exposed by <see cref="RuntimeCapabilityDirectorySnapshot"/>.</value>
    public IReadOnlyList<OneWireSkillDescriptor> Skills { get; set; } = [];
    /// <summary>
    /// Gets or sets the warnings collection maintained or exposed by this runtime capability directory snapshot instance for downstream processing.
    /// </summary>
    /// <value>The warnings value exposed by <see cref="RuntimeCapabilityDirectorySnapshot"/>.</value>
    public List<string> Warnings { get; set; } = [];
}

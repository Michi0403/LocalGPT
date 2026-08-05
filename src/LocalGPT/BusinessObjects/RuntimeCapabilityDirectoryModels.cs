namespace LocalGPT.BusinessObjects;

/// <summary>Current dependency-injected function and organic-skill directory persisted for AI context.</summary>
public sealed class RuntimeCapabilityDirectorySnapshot
{
    public DateTime SynchronizedAtUtc { get; set; } = DateTime.UtcNow;
    public IReadOnlyList<DxaichatFunctionInfo> Functions { get; set; } = [];
    public IReadOnlyList<DxAiFunctionCatalogEntry> CatalogEntries { get; set; } = [];
    public IReadOnlyList<OneWireSkillDescriptor> Skills { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

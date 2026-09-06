namespace LocalGPT.BusinessObjects;

/// <summary>Describes the effective LocalGPT folder contract detected for the current host and user.</summary>
public sealed class LocalGptApplicationPathLayout
{
    public string Platform { get; init; } = string.Empty;
    public string UserDataRoot { get; init; } = string.Empty;
    public string ConfigurationFile { get; init; } = string.Empty;
    public string DatabaseFile { get; init; } = string.Empty;
    public string RuntimeDirectory { get; init; } = string.Empty;
    public string LogsDirectory { get; init; } = string.Empty;
    public string KnowledgeDirectory { get; init; } = string.Empty;
    public string PortableApplicationRoot { get; init; } = string.Empty;
    public IReadOnlyList<string> SystemWideDiscoveryRoots { get; init; } = Array.Empty<string>();
    public string LayoutReportFile { get; init; } = string.Empty;
    public bool FirstBootDetected { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
}

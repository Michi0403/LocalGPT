namespace LocalGPT.BusinessObjects;

/// <summary>Read-only file hierarchy node projected from the persisted source manifest.</summary>
public sealed class CodeGenerationDraftTreeNode
{
    /// <summary>Display-ready directory or file name.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Exact canonical manifest path, empty for a directory.</summary>
    public string Path { get; init; } = string.Empty;
    /// <summary>Nested directories and file entries; no source data.</summary>
    public List<CodeGenerationDraftTreeNode> Children { get; } = [];
    /// <summary>Exact file progress evidence; null for directory nodes.</summary>
    public CodeGenerationDraftPathProgress? File { get; init; }
    /// <summary>Icon class describing the persisted file evidence, without claiming a successful build.</summary>
    public string IconCssClass => File is null ? "oi oi-folder" : File.InspectionAccepted ? "oi oi-check" :
        !string.IsNullOrWhiteSpace(File.Sha256) ? "oi oi-file" : !string.IsNullOrWhiteSpace(File.Diagnostic) ? "oi oi-warning" :
        File.Attempt > 0 ? "oi oi-clock" : "oi oi-document";
}

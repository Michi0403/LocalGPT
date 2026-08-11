namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a remote knowledge import request.
/// </summary>
public sealed class RemoteKnowledgeImportRequest
{
    /// <summary>
    /// Gets or sets source URL.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source kind.
    /// </summary>
    public string SourceKind { get; set; } = "Auto";
    /// <summary>
    /// Gets or sets branch.
    /// </summary>
    public string Branch { get; set; } = "main";
    /// <summary>
    /// Gets or sets file include regex.
    /// </summary>
    public string FileIncludeRegex { get; set; } = @"(?i)\.(cs|razor|csproj|sln|json|xml|md|mdx|rst|adoc|txt|ps1|cmd|sh|py|js|ts|tsx|css|scss|html|htm|php|c|h|cpp|hpp|cc|cxx|ino|pde|cmake|kconfig|sdkconfig|toml|ini|cfg|csv|java|kt|go|rs|sql|yml|yaml)$|(^|/)(CMakeLists\.txt|platformio\.ini|library\.properties)$";
    /// <summary>
    /// Gets or sets max files.
    /// </summary>
    public int MaxFiles { get; set; }
    /// <summary>
    /// Gets or sets an optional caller-requested linked-page count; non-positive values use the database-backed MaxFiles policy.
    /// </summary>
    public int MaxLinkedPages { get; set; }
    /// <summary>
    /// Gets or sets save to knowledge.
    /// </summary>
    public bool SaveToKnowledge { get; set; } = true;
    /// <summary>
    /// Gets or sets preview only.
    /// </summary>
    public bool PreviewOnly { get; set; }
    /// <summary>
    /// Gets or sets role keys.
    /// </summary>
    public List<string> RoleKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets topics.
    /// </summary>
    public List<string> Topics { get; set; } = [];
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a remote knowledge import file.
/// </summary>
public sealed class RemoteKnowledgeImportFile
{
    /// <summary>
    /// Gets or sets relative path.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source URL.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets length.
    /// </summary>
    public long Length { get; set; }
    /// <summary>
    /// Gets or sets matches file policy.
    /// </summary>
    public bool MatchesFilePolicy { get; set; }
    /// <summary>
    /// Gets or sets imported.
    /// </summary>
    public bool Imported { get; set; }
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents a remote knowledge import result.
/// </summary>
public sealed class RemoteKnowledgeImportResult
{
    /// <summary>
    /// Gets or sets source URL.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source kind.
    /// </summary>
    public string SourceKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets cache root.
    /// </summary>
    public string CacheRoot { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets resolved revision.
    /// </summary>
    public string ResolvedRevision { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets downloaded file count.
    /// </summary>
    public int DownloadedFileCount { get; set; }
    /// <summary>
    /// Gets or sets matched file count.
    /// </summary>
    public int MatchedFileCount { get; set; }
    /// <summary>
    /// Gets or sets imported knowledge count.
    /// </summary>
    public int ImportedKnowledgeCount { get; set; }
    /// <summary>
    /// Gets or sets files.
    /// </summary>
    public List<RemoteKnowledgeImportFile> Files { get; set; } = [];
    /// <summary>
    /// Gets or sets learn base result.
    /// </summary>
    public LearnBaseImportResult? LearnBaseResult { get; set; }
    /// <summary>
    /// Gets or sets applied tags.
    /// </summary>
    public List<string> AppliedTags { get; set; } = [];
    /// <summary>
    /// Gets or sets warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}

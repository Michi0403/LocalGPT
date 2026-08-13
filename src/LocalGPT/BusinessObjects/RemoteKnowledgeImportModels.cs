namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents the input contract for remote knowledge import, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class RemoteKnowledgeImportRequest
{
    /// <summary>
    /// Gets or sets the source URL that identifies the network or application endpoint associated with this remote knowledge import state.
    /// </summary>
    /// <value>The source URL value exposed by <see cref="RemoteKnowledgeImportRequest"/>.</value>
    public string SourceUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source kind value that forms part of the remote knowledge import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source kind value exposed by <see cref="RemoteKnowledgeImportRequest"/>.</value>
    public string SourceKind { get; set; } = "Auto";
    /// <summary>
    /// Gets or sets the branch value that forms part of the remote knowledge import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The branch value exposed by <see cref="RemoteKnowledgeImportRequest"/>.</value>
    public string Branch { get; set; } = "main";
    /// <summary>
    /// Gets or sets the file include regex value that forms part of the remote knowledge import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The file include regex value exposed by <see cref="RemoteKnowledgeImportRequest"/>.</value>
    public string FileIncludeRegex { get; set; } = @"(?i)\.(cs|razor|csproj|sln|json|xml|md|mdx|rst|adoc|txt|ps1|cmd|sh|py|js|ts|tsx|css|scss|html|htm|php|c|h|cpp|hpp|cc|cxx|ino|pde|cmake|kconfig|sdkconfig|toml|ini|cfg|csv|java|kt|go|rs|sql|yml|yaml)$|(^|/)(CMakeLists\.txt|platformio\.ini|library\.properties)$";
    /// <summary>
    /// Gets or sets the max files value that forms part of the remote knowledge import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max files value exposed by <see cref="RemoteKnowledgeImportRequest"/>.</value>
    public int MaxFiles { get; set; }
    /// <summary>
    /// Gets or sets an optional caller-requested linked-page count; non-positive values use the database-backed MaxFiles policy.
    /// </summary>
    /// <value>The max linked pages value exposed by <see cref="RemoteKnowledgeImportRequest"/>.</value>
    public int MaxLinkedPages { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether save to knowledge applies to the remote knowledge import state.
    /// </summary>
    /// <value>The save to knowledge value exposed by <see cref="RemoteKnowledgeImportRequest"/>.</value>
    public bool SaveToKnowledge { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether preview only applies to the remote knowledge import state.
    /// </summary>
    /// <value>The preview only value exposed by <see cref="RemoteKnowledgeImportRequest"/>.</value>
    public bool PreviewOnly { get; set; }
    /// <summary>
    /// Gets or sets the role keys collection maintained or exposed by this remote knowledge import instance for downstream processing.
    /// </summary>
    /// <value>The role keys value exposed by <see cref="RemoteKnowledgeImportRequest"/>.</value>
    public List<string> RoleKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets the topics collection maintained or exposed by this remote knowledge import instance for downstream processing.
    /// </summary>
    /// <value>The topics value exposed by <see cref="RemoteKnowledgeImportRequest"/>.</value>
    public List<string> Topics { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the remote knowledge import state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="RemoteKnowledgeImportRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a remote knowledge import file application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class RemoteKnowledgeImportFile
{
    /// <summary>
    /// Gets or sets the relative path used by this remote knowledge import file instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="RemoteKnowledgeImportFile"/>.</value>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source URL that identifies the network or application endpoint associated with this remote knowledge import file state.
    /// </summary>
    /// <value>The source URL value exposed by <see cref="RemoteKnowledgeImportFile"/>.</value>
    public string SourceUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the length that quantifies the associated remote knowledge import file data.
    /// </summary>
    /// <value>The length value exposed by <see cref="RemoteKnowledgeImportFile"/>.</value>
    public long Length { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether matches file policy applies to the remote knowledge import file state.
    /// </summary>
    /// <value>The matches file policy value exposed by <see cref="RemoteKnowledgeImportFile"/>.</value>
    public bool MatchesFilePolicy { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether imported applies to the remote knowledge import file state.
    /// </summary>
    /// <value>The imported value exposed by <see cref="RemoteKnowledgeImportFile"/>.</value>
    public bool Imported { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the remote knowledge import file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="RemoteKnowledgeImportFile"/>.</value>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents the outcome of remote knowledge import, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class RemoteKnowledgeImportResult
{
    /// <summary>
    /// Gets or sets the source URL that identifies the network or application endpoint associated with this remote knowledge import state.
    /// </summary>
    /// <value>The source URL value exposed by <see cref="RemoteKnowledgeImportResult"/>.</value>
    public string SourceUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source kind value that forms part of the remote knowledge import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source kind value exposed by <see cref="RemoteKnowledgeImportResult"/>.</value>
    public string SourceKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the cache root value that forms part of the remote knowledge import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The cache root value exposed by <see cref="RemoteKnowledgeImportResult"/>.</value>
    public string CacheRoot { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the resolved revision value that forms part of the remote knowledge import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The resolved revision value exposed by <see cref="RemoteKnowledgeImportResult"/>.</value>
    public string ResolvedRevision { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the downloaded file count that quantifies the associated remote knowledge import data.
    /// </summary>
    /// <value>The downloaded file count value exposed by <see cref="RemoteKnowledgeImportResult"/>.</value>
    public int DownloadedFileCount { get; set; }
    /// <summary>
    /// Gets or sets the matched file count that quantifies the associated remote knowledge import data.
    /// </summary>
    /// <value>The matched file count value exposed by <see cref="RemoteKnowledgeImportResult"/>.</value>
    public int MatchedFileCount { get; set; }
    /// <summary>
    /// Gets or sets the imported knowledge count that quantifies the associated remote knowledge import data.
    /// </summary>
    /// <value>The imported knowledge count value exposed by <see cref="RemoteKnowledgeImportResult"/>.</value>
    public int ImportedKnowledgeCount { get; set; }
    /// <summary>
    /// Gets or sets the files collection maintained or exposed by this remote knowledge import instance for downstream processing.
    /// </summary>
    /// <value>The files value exposed by <see cref="RemoteKnowledgeImportResult"/>.</value>
    public List<RemoteKnowledgeImportFile> Files { get; set; } = [];
    /// <summary>
    /// Gets or sets the learn base result value that forms part of the remote knowledge import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The learn base result value exposed by <see cref="RemoteKnowledgeImportResult"/>.</value>
    public LearnBaseImportResult? LearnBaseResult { get; set; }
    /// <summary>
    /// Gets or sets the applied tags collection maintained or exposed by this remote knowledge import instance for downstream processing.
    /// </summary>
    /// <value>The applied tags value exposed by <see cref="RemoteKnowledgeImportResult"/>.</value>
    public List<string> AppliedTags { get; set; } = [];
    /// <summary>
    /// Gets or sets the warnings collection maintained or exposed by this remote knowledge import instance for downstream processing.
    /// </summary>
    /// <value>The warnings value exposed by <see cref="RemoteKnowledgeImportResult"/>.</value>
    public List<string> Warnings { get; set; } = [];
}

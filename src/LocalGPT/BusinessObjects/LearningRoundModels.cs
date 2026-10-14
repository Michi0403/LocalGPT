namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a learning round snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LearningRoundSnapshot
{
    /// <summary>
    /// Gets or sets the generated at UTC associated with this learning round snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The generated at UTC value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the conversation count that quantifies the associated learning round snapshot data.
    /// </summary>
    /// <value>The conversation count value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public int ConversationCount { get; set; }
    /// <summary>
    /// Gets or sets the message count that quantifies the associated learning round snapshot data.
    /// </summary>
    /// <value>The message count value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public int MessageCount { get; set; }
    /// <summary>
    /// Gets or sets the log count that quantifies the associated learning round snapshot data.
    /// </summary>
    /// <value>The log count value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public int LogCount { get; set; }
    /// <summary>
    /// Gets or sets the knowledge count that quantifies the associated learning round snapshot data.
    /// </summary>
    /// <value>The knowledge count value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public int KnowledgeCount { get; set; }
    /// <summary>
    /// Gets or sets the regex count that quantifies the associated learning round snapshot data.
    /// </summary>
    /// <value>The regex count value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public int RegexCount { get; set; }
    /// <summary>Gets or sets the number of database-first projects available to the learning round.</summary>
    /// <value>The project count value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public int ProjectCount { get; set; }
    /// <summary>
    /// Gets or sets the recent conversations collection maintained or exposed by this learning round snapshot instance for downstream processing.
    /// </summary>
    /// <value>The recent conversations value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public IReadOnlyList<object> RecentConversations { get; set; } = [];
    /// <summary>
    /// Gets or sets the recent messages collection maintained or exposed by this learning round snapshot instance for downstream processing.
    /// </summary>
    /// <value>The recent messages value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public IReadOnlyList<object> RecentMessages { get; set; } = [];
    /// <summary>
    /// Gets or sets the recent logs collection maintained or exposed by this learning round snapshot instance for downstream processing.
    /// </summary>
    /// <value>The recent logs value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public IReadOnlyList<object> RecentLogs { get; set; } = [];
    /// <summary>
    /// Gets or sets the recent knowledge collection maintained or exposed by this learning round snapshot instance for downstream processing.
    /// </summary>
    /// <value>The recent knowledge value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public IReadOnlyList<object> RecentKnowledge { get; set; } = [];
    /// <summary>
    /// Gets or sets the regex patterns collection maintained or exposed by this learning round snapshot instance for downstream processing.
    /// </summary>
    /// <value>The regex patterns value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public IReadOnlyList<object> RegexPatterns { get; set; } = [];
    /// <summary>Gets or sets bounded source-backed project/version/revision summaries for learning-round verification.</summary>
    /// <value>The projects value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public IReadOnlyList<object> Projects { get; set; } = [];
}

/// <summary>
/// Represents a learning fact input application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LearningFactInput
{
    /// <summary>
    /// Gets or sets the topic value that forms part of the learning fact input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The topic value exposed by <see cref="LearningFactInput"/>.</value>
    public string Topic { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the scope value that forms part of the learning fact input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scope value exposed by <see cref="LearningFactInput"/>.</value>
    public string Scope { get; set; } = "AI Council Learning";
    /// <summary>
    /// Gets or sets the content value that forms part of the learning fact input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content value exposed by <see cref="LearningFactInput"/>.</value>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the helpful sources value that forms part of the learning fact input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The helpful sources value exposed by <see cref="LearningFactInput"/>.</value>
    public string HelpfulSources { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the tags value that forms part of the learning fact input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The tags value exposed by <see cref="LearningFactInput"/>.</value>
    public string Tags { get; set; } = "learning-round;model-suggested";
    /// <summary>
    /// Gets or sets the confidence value that forms part of the learning fact input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The confidence value exposed by <see cref="LearningFactInput"/>.</value>
    public int Confidence { get; set; }
}

/// <summary>
/// Represents a learning regex input application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LearningRegexInput
{
    /// <summary>
    /// Gets or sets the name value that forms part of the learning regex input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="LearningRegexInput"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the pattern value that forms part of the learning regex input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The pattern value exposed by <see cref="LearningRegexInput"/>.</value>
    public string Pattern { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the flags value that forms part of the learning regex input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The flags value exposed by <see cref="LearningRegexInput"/>.</value>
    public string Flags { get; set; } = "c";

    /// <summary>Explains the concrete source-backed purpose of the suggested pattern.</summary>
    /// <value>The description value exposed by <see cref="LearningRegexInput"/>.</value>
    public string Description { get; set; } = string.Empty;

    /// <summary>Optional literal source excerpt against which the proposed pattern must match.</summary>
    /// <value>The sample text value exposed by <see cref="LearningRegexInput"/>.</value>
    public string SampleText { get; set; } = string.Empty;
}

/// <summary>
/// Represents the input contract for learning maintenance, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class LearningMaintenanceRequest
{
    /// <summary>Gets or sets whether source repositories in the chat upload workspace are synchronized into the database-first project structure.</summary>
    /// <value>True by default so a completed software learning round cannot silently omit project/version/file persistence.</value>
    public bool SynchronizeProjectStructure { get; set; } = true;
    /// <summary>Gets or sets the exact chat upload workspace to synchronize; empty selects the latest workspace.</summary>
    /// <value>The workspace name value exposed by <see cref="LearningMaintenanceRequest"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the facts collection maintained or exposed by this learning maintenance instance for downstream processing.
    /// </summary>
    /// <value>The facts value exposed by <see cref="LearningMaintenanceRequest"/>.</value>
    public List<LearningFactInput> Facts { get; set; } = [];
    /// <summary>
    /// Gets or sets the regex patterns collection maintained or exposed by this learning maintenance instance for downstream processing.
    /// </summary>
    /// <value>The regex patterns value exposed by <see cref="LearningMaintenanceRequest"/>.</value>
    public List<LearningRegexInput> RegexPatterns { get; set; } = [];
}

/// <summary>
/// Represents the outcome of learning maintenance, carrying the data and status produced by the corresponding application operation.
/// </summary>
/// <param name="FactsStored">Facts stored value supplied to the learning maintenance operation and used when producing its result.</param>
/// <param name="RegexPatternsStored">Regex patterns stored value supplied to the learning maintenance operation and used when producing its result.</param>
/// <param name="KnowledgeEntryIds">Guid dependency used by the learning maintenance workflow to provide the corresponding application capability.</param>
/// <param name="RegexNames">String dependency used by the learning maintenance workflow to provide the corresponding application capability.</param>
/// <param name="WorkspaceName">Workspace name value supplied to the learning maintenance operation and used when producing its result.</param>
/// <param name="ProjectsSynchronized">Learning project sync result dependency used by the learning maintenance workflow to provide the corresponding application capability.</param>
public sealed record LearningMaintenanceResult(
    int FactsStored,
    int RegexPatternsStored,
    IReadOnlyList<Guid> KnowledgeEntryIds,
    IReadOnlyList<string> RegexNames,
    string WorkspaceName,
    IReadOnlyList<LearningProjectSyncResult> ProjectsSynchronized);

/// <summary>One bounded file-checklist row; a scan or hash never constitutes a semantic review.</summary>
/// <param name="FileId">Identifier of the file to use for this operation.</param>
/// <param name="RelativePath">Relative path value supplied to the learning file checklist item operation and used when producing its result.</param>
/// <param name="ContentHash">Content hash value supplied to the learning file checklist item operation and used when producing its result.</param>
/// <param name="Status">Status value supplied to the learning file checklist item operation and used when producing its result.</param>
/// <param name="ReadCharacters">Read characters value supplied to the learning file checklist item operation and used when producing its result.</param>
/// <param name="ReadComplete">Value indicating whether read complete should apply to this operation.</param>
/// <param name="Evidence">Evidence value supplied to the learning file checklist item operation and used when producing its result.</param>
/// <param name="BlockReason">Block reason value supplied to the learning file checklist item operation and used when producing its result.</param>
/// <param name="UpdatedAtUtc">Updated at utc value supplied to the learning file checklist item operation and used when producing its result.</param>
public sealed record LearningFileChecklistItem(
    Guid FileId,
    string RelativePath,
    string ContentHash,
    string Status,
    long ReadCharacters,
    bool ReadComplete,
    string Evidence,
    string BlockReason,
    DateTime? UpdatedAtUtc);

/// <summary>Database-grounded progress counters and a bounded, resumable window of file tasks.</summary>
/// <param name="ProjectId">Identifier of the project to use for this operation.</param>
/// <param name="RevisionId">Identifier of the revision to use for this operation.</param>
/// <param name="Total">Total value supplied to the learning file checklist snapshot operation and used when producing its result.</param>
/// <param name="Pending">Pending value supplied to the learning file checklist snapshot operation and used when producing its result.</param>
/// <param name="Reading">Reading value supplied to the learning file checklist snapshot operation and used when producing its result.</param>
/// <param name="NeedsReview">Value indicating whether review should apply to this operation.</param>
/// <param name="Blocked">Blocked value supplied to the learning file checklist snapshot operation and used when producing its result.</param>
/// <param name="Completed">Completed value supplied to the learning file checklist snapshot operation and used when producing its result.</param>
/// <param name="SkippedWithReason">Skipped with reason value supplied to the learning file checklist snapshot operation and used when producing its result.</param>
/// <param name="Offset">Offset value supplied to the learning file checklist snapshot operation and used when producing its result.</param>
/// <param name="Files">Learning file checklist item dependency used by the learning file checklist snapshot workflow to provide the corresponding application capability.</param>
public sealed record LearningFileChecklistSnapshot(
    Guid ProjectId,
    Guid? RevisionId,
    int Total,
    int Pending,
    int Reading,
    int NeedsReview,
    int Blocked,
    int Completed,
    int SkippedWithReason,
    int Offset,
    IReadOnlyList<LearningFileChecklistItem> Files);

/// <summary>A single verified source chunk and its durable read-continuation cursor.</summary>
/// <param name="FileId">Identifier of the file to use for this operation.</param>
/// <param name="RelativePath">Relative path value supplied to the learning file read operation and used when producing its result.</param>
/// <param name="ContentHash">Content hash value supplied to the learning file read operation and used when producing its result.</param>
/// <param name="OffsetCharacters">Offset characters value supplied to the learning file read operation and used when producing its result.</param>
/// <param name="NextOffsetCharacters">Next offset characters value supplied to the learning file read operation and used when producing its result.</param>
/// <param name="HasMore">Value indicating whether more should apply to this operation.</param>
/// <param name="Content">Content value supplied to the learning file read operation and used when producing its result.</param>
/// <param name="Status">Status value supplied to the learning file read operation and used when producing its result.</param>
/// <param name="BlockReason">Block reason value supplied to the learning file read operation and used when producing its result.</param>
public sealed record LearningFileReadResult(
    Guid FileId,
    string RelativePath,
    string ContentHash,
    long OffsetCharacters,
    long NextOffsetCharacters,
    bool HasMore,
    string Content,
    string Status,
    string BlockReason);

/// <summary>Result of storing a model-suggested per-file review, never user-approved knowledge.</summary>
/// <param name="FileId">Identifier of the file to use for this operation.</param>
/// <param name="ContentHash">Content hash value supplied to the learning file review operation and used when producing its result.</param>
/// <param name="Status">Status value supplied to the learning file review operation and used when producing its result.</param>
/// <param name="ReadComplete">Value indicating whether read complete should apply to this operation.</param>
public sealed record LearningFileReviewResult(
    Guid FileId,
    string ContentHash,
    string Status,
    bool ReadComplete);

/// <summary>JSON arguments selecting one bounded checklist page or tracked source chunk.</summary>
public sealed class LearningFileCursorRequest
{
    /// <summary>Selected project, falling back to the active chat project if omitted.</summary>
    /// <value>The project identifier value exposed by <see cref="LearningFileCursorRequest"/>.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>Selected revision or the project's base files.</summary>
    /// <value>The revision identifier value exposed by <see cref="LearningFileCursorRequest"/>.</value>
    public Guid? RevisionId { get; set; }
    /// <summary>Optional specific file identity for continuation.</summary>
    /// <value>The file identifier value exposed by <see cref="LearningFileCursorRequest"/>.</value>
    public Guid? FileId { get; set; }
    /// <summary>Bounded checklist-page offset.</summary>
    /// <value>The offset value exposed by <see cref="LearningFileCursorRequest"/>.</value>
    public int Offset { get; set; }
    /// <summary>Bounded result size.</summary>
    /// <value>The take value exposed by <see cref="LearningFileCursorRequest"/>.</value>
    public int Take { get; set; } = 100;
    /// <summary>Maximum content characters in one read step.</summary>
    /// <value>The maximum characters value exposed by <see cref="LearningFileCursorRequest"/>.</value>
    public int MaximumCharacters { get; set; } = 6000;
}

/// <summary>Model-suggested, non-authoritative file review or explicit blocker.</summary>
public sealed class LearningFileReviewRequest
{
    /// <summary>Source-backed file identity.</summary>
    /// <value>The file identifier value exposed by <see cref="LearningFileReviewRequest"/>.</value>
    public Guid FileId { get; set; }
    /// <summary>Concise findings or reason for an unresolved blocker.</summary>
    /// <value>The evidence value exposed by <see cref="LearningFileReviewRequest"/>.</value>
    public string Evidence { get; set; } = string.Empty;
    /// <summary>True when no credible semantic review is possible.</summary>
    /// <value>The blocked value exposed by <see cref="LearningFileReviewRequest"/>.</value>
    public bool Blocked { get; set; }
}

/// <summary>Describes one repository that a learning round synchronized into LocalGPT's database-first project structure.</summary>
/// <param name="ProjectId">Stable project identifier.</param>
/// <param name="RevisionId">Source-backed revision identifier.</param>
/// <param name="ProjectName">Detected canonical project name.</param>
/// <param name="Version">Exact repository version read from project metadata.</param>
/// <param name="SdkVersion">Exact SDK version read from global.json when present.</param>
/// <param name="TargetFrameworks">Exact target frameworks declared by repository project files.</param>
/// <param name="WorkspaceName">Chat upload workspace that supplied the source repository.</param>
/// <param name="RepositoryRoot">Repository root inside the chat upload workspace.</param>
/// <param name="TrackedFileCount">Number of repository files persisted in the tracked-file structure.</param>
/// <param name="SourceSnapshotHash">SHA-256 hash binding the revision to the complete tracked source structure.</param>
public sealed record LearningProjectSyncResult(
    Guid ProjectId,
    Guid RevisionId,
    string ProjectName,
    string Version,
    string SdkVersion,
    IReadOnlyList<string> TargetFrameworks,
    string WorkspaceName,
    string RepositoryRoot,
    int TrackedFileCount,
    string SourceSnapshotHash);


/// <summary>Describes one user-requested refresh of canonical repository knowledge from a bounded public remote source.</summary>
/// <param name="SourceUrl">Public repository URL that supplied the refreshed evidence.</param>
/// <param name="ResolvedRevision">Branch or remote revision resolved by the importer.</param>
/// <param name="DownloadedFileCount">Number of files reported by the bounded remote importer.</param>
/// <param name="ProjectsSynchronized">Canonical projects synchronized from the downloaded source tree.</param>
public sealed record RepositoryKnowledgeRefreshResult(
    string SourceUrl,
    string ResolvedRevision,
    int DownloadedFileCount,
    IReadOnlyList<LearningProjectSyncResult> ProjectsSynchronized);

/// <summary>Reports review-required Knowledge and regex candidates staged from one synchronized source revision.</summary>
/// <param name="ProjectId">Identifier of the project to use for this operation.</param>
/// <param name="RevisionId">Identifier of the revision to use for this operation.</param>
/// <param name="ProjectName">Project name value supplied to the project repository learning operation and used when producing its result.</param>
/// <param name="AddedFiles">Added files value supplied to the project repository learning operation and used when producing its result.</param>
/// <param name="ChangedFiles">Changed files value supplied to the project repository learning operation and used when producing its result.</param>
/// <param name="RemovedFiles">Removed files value supplied to the project repository learning operation and used when producing its result.</param>
/// <param name="KnowledgeIds">Guid dependency used by the project repository learning workflow to provide the corresponding application capability.</param>
/// <param name="RegexNames">String dependency used by the project repository learning workflow to provide the corresponding application capability.</param>
public sealed record ProjectRepositoryLearningResult(
    Guid ProjectId,
    Guid RevisionId,
    string ProjectName,
    int AddedFiles,
    int ChangedFiles,
    int RemovedFiles,
    IReadOnlyList<Guid> KnowledgeIds,
    IReadOnlyList<string> RegexNames);

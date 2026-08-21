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

namespace LocalGPT.BusinessObjects;

/// <summary>Read-only navigation in one existing, scanned project and an exact revision.</summary>
public sealed class ProjectSourceNavigationRequest
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project source navigation instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectSourceNavigationRequest"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this project source navigation instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="ProjectSourceNavigationRequest"/>.</value>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets the stable focus tracked file identifier used to identify or correlate this project source navigation instance with related application state.
    /// </summary>
    /// <value>The focus tracked file identifier value exposed by <see cref="ProjectSourceNavigationRequest"/>.</value>
    public Guid? FocusTrackedFileId { get; set; }
    /// <summary>
    /// Gets or sets the query value that forms part of the project source navigation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The query value exposed by <see cref="ProjectSourceNavigationRequest"/>.</value>
    public string Query { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the take value that forms part of the project source navigation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The take value exposed by <see cref="ProjectSourceNavigationRequest"/>.</value>
    public int Take { get; set; } = 40;
}

/// <summary>A file identity and verified learning metadata, without loading or leaking its contents.</summary>
public sealed class ProjectSourceFileMatch
{
    /// <summary>
    /// Gets or sets the stable tracked file identifier used to identify or correlate this project source file match instance with related application state.
    /// </summary>
    /// <value>The tracked file identifier value exposed by <see cref="ProjectSourceFileMatch"/>.</value>
    public Guid TrackedFileId { get; set; }
    /// <summary>
    /// Gets or sets the relative path used by this project source file match instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="ProjectSourceFileMatch"/>.</value>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the content hash value that forms part of the project source file match state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content hash value exposed by <see cref="ProjectSourceFileMatch"/>.</value>
    public string ContentHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the file role value that forms part of the project source file match state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The file role value exposed by <see cref="ProjectSourceFileMatch"/>.</value>
    public string FileRole { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the extension value that forms part of the project source file match state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The extension value exposed by <see cref="ProjectSourceFileMatch"/>.</value>
    public string Extension { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the learning status value that forms part of the project source file match state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The learning status value exposed by <see cref="ProjectSourceFileMatch"/>.</value>
    public string LearningStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether learning read complete applies to the project source file match state.
    /// </summary>
    /// <value>The learning read complete value exposed by <see cref="ProjectSourceFileMatch"/>.</value>
    public bool LearningReadComplete { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether reviewed for current hash applies to the project source file match state.
    /// </summary>
    /// <value>The reviewed for current hash value exposed by <see cref="ProjectSourceFileMatch"/>.</value>
    public bool ReviewedForCurrentHash { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user approved applies to the project source file match state.
    /// </summary>
    /// <value>The user approved value exposed by <see cref="ProjectSourceFileMatch"/>.</value>
    public bool UserApproved { get; set; }
    /// <summary>
    /// Gets or sets the project file path used by this project source file match instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The project file path value exposed by <see cref="ProjectSourceFileMatch"/>.</value>
    public string ProjectFilePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the approved pattern names collection maintained or exposed by this project source file match instance for downstream processing.
    /// </summary>
    /// <value>The approved pattern names value exposed by <see cref="ProjectSourceFileMatch"/>.</value>
    public IReadOnlyList<string> ApprovedPatternNames { get; set; } = [];
}

/// <summary>Curator-approved regex provenance plus exact compiler-version evidence for navigation, not a generated compatibility claim.</summary>
public sealed class ProjectSourceNavigationResult
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project source navigation instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectSourceNavigationResult"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this project source navigation instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="ProjectSourceNavigationResult"/>.</value>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets the project name value that forms part of the project source navigation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project name value exposed by <see cref="ProjectSourceNavigationResult"/>.</value>
    public string ProjectName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the project type value that forms part of the project source navigation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project type value exposed by <see cref="ProjectSourceNavigationResult"/>.</value>
    public string ProjectType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the files collection maintained or exposed by this project source navigation instance for downstream processing.
    /// </summary>
    /// <value>The files value exposed by <see cref="ProjectSourceNavigationResult"/>.</value>
    public IReadOnlyList<ProjectSourceFileMatch> Files { get; set; } = [];
    /// <summary>
    /// Gets or sets the related patterns collection maintained or exposed by this project source navigation instance for downstream processing.
    /// </summary>
    /// <value>The related patterns value exposed by <see cref="ProjectSourceNavigationResult"/>.</value>
    public IReadOnlyList<ProjectSourceCuratedPattern> RelatedPatterns { get; set; } = [];
    /// <summary>
    /// Gets or sets the toolchains collection maintained or exposed by this project source navigation instance for downstream processing.
    /// </summary>
    /// <value>The toolchains value exposed by <see cref="ProjectSourceNavigationResult"/>.</value>
    public IReadOnlyList<ProjectSourceToolchainEvidence> Toolchains { get; set; } = [];
}

/// <summary>
/// Represents a project source curated pattern application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectSourceCuratedPattern
{
    /// <summary>
    /// Gets or sets the name value that forms part of the project source curated pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="ProjectSourceCuratedPattern"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the usage scope value that forms part of the project source curated pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The usage scope value exposed by <see cref="ProjectSourceCuratedPattern"/>.</value>
    public string UsageScope { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the provenance value that forms part of the project source curated pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The provenance value exposed by <see cref="ProjectSourceCuratedPattern"/>.</value>
    public string Provenance { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the review status value that forms part of the project source curated pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The review status value exposed by <see cref="ProjectSourceCuratedPattern"/>.</value>
    public string ReviewStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the independent approvals value that forms part of the project source curated pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The independent approvals value exposed by <see cref="ProjectSourceCuratedPattern"/>.</value>
    public int IndependentApprovals { get; set; }
    /// <summary>
    /// Gets or sets the required independent approvals value that forms part of the project source curated pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The required independent approvals value exposed by <see cref="ProjectSourceCuratedPattern"/>.</value>
    public int RequiredIndependentApprovals { get; set; }
    /// <summary>
    /// Gets or sets the matched tracked file identifiers collection maintained or exposed by this project source curated pattern instance for downstream processing.
    /// </summary>
    /// <value>The matched tracked file identifiers value exposed by <see cref="ProjectSourceCuratedPattern"/>.</value>
    public IReadOnlyList<Guid> MatchedTrackedFileIds { get; set; } = [];
}

/// <summary>
/// Represents a project source toolchain evidence application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectSourceToolchainEvidence
{
    /// <summary>
    /// Gets or sets the stable compiler installation identifier used to identify or correlate this project source toolchain evidence instance with related application state.
    /// </summary>
    /// <value>The compiler installation identifier value exposed by <see cref="ProjectSourceToolchainEvidence"/>.</value>
    public Guid CompilerInstallationId { get; set; }
    /// <summary>
    /// Gets or sets the language value that forms part of the project source toolchain evidence state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The language value exposed by <see cref="ProjectSourceToolchainEvidence"/>.</value>
    public string Language { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the version value that forms part of the project source toolchain evidence state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The version value exposed by <see cref="ProjectSourceToolchainEvidence"/>.</value>
    public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable knowledge profile key used to identify or correlate this project source toolchain evidence instance with related application state.
    /// </summary>
    /// <value>The knowledge profile key value exposed by <see cref="ProjectSourceToolchainEvidence"/>.</value>
    public string KnowledgeProfileKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable version knowledge entry identifier used to identify or correlate this project source toolchain evidence instance with related application state.
    /// </summary>
    /// <value>The version knowledge entry identifier value exposed by <see cref="ProjectSourceToolchainEvidence"/>.</value>
    public Guid? VersionKnowledgeEntryId { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether last validation succeeded applies to the project source toolchain evidence state.
    /// </summary>
    /// <value>The last validation succeeded value exposed by <see cref="ProjectSourceToolchainEvidence"/>.</value>
    public bool LastValidationSucceeded { get; set; }
    /// <summary>True only if the project workspace explicitly associates this compiler profile.</summary>
    /// <value>The project preferred value exposed by <see cref="ProjectSourceToolchainEvidence"/>.</value>
    public bool ProjectPreferred { get; set; }
    /// <summary>Detection does not establish compatibility with the project's target framework.</summary>
    /// <value>The target compatibility verified value exposed by <see cref="ProjectSourceToolchainEvidence"/>.</value>
    public bool TargetCompatibilityVerified { get; set; }
}

/// <summary>Publish only a user-approved tracked source revision as one executable plugin snapshot.</summary>
public sealed class PublishTrackedProjectScriptRequest
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this publish tracked project script instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="PublishTrackedProjectScriptRequest"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this publish tracked project script instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="PublishTrackedProjectScriptRequest"/>.</value>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets the stable tracked file identifier used to identify or correlate this publish tracked project script instance with related application state.
    /// </summary>
    /// <value>The tracked file identifier value exposed by <see cref="PublishTrackedProjectScriptRequest"/>.</value>
    public Guid TrackedFileId { get; set; }
    /// <summary>
    /// Gets or sets the plugin value that forms part of the publish tracked project script state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The plugin value exposed by <see cref="PublishTrackedProjectScriptRequest"/>.</value>
    public RuntimePluginDefinition Plugin { get; set; } = new();
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the publish tracked project script state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="PublishTrackedProjectScriptRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents the outcome of publish tracked project script, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class PublishTrackedProjectScriptResult
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this publish tracked project script instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="PublishTrackedProjectScriptResult"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable tracked file identifier used to identify or correlate this publish tracked project script instance with related application state.
    /// </summary>
    /// <value>The tracked file identifier value exposed by <see cref="PublishTrackedProjectScriptResult"/>.</value>
    public Guid TrackedFileId { get; set; }
    /// <summary>
    /// Gets or sets the stable plugin identifier used to identify or correlate this publish tracked project script instance with related application state.
    /// </summary>
    /// <value>The plugin identifier value exposed by <see cref="PublishTrackedProjectScriptResult"/>.</value>
    public Guid PluginId { get; set; }
    /// <summary>
    /// Gets or sets the stable project artifact identifier used to identify or correlate this publish tracked project script instance with related application state.
    /// </summary>
    /// <value>The project artifact identifier value exposed by <see cref="PublishTrackedProjectScriptResult"/>.</value>
    public Guid ProjectArtifactId { get; set; }
    /// <summary>
    /// Gets or sets the approved source hash value that forms part of the publish tracked project script state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The approved source hash value exposed by <see cref="PublishTrackedProjectScriptResult"/>.</value>
    public string ApprovedSourceHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the publish tracked project script state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="PublishTrackedProjectScriptResult"/>.</value>
    public string Status { get; set; } = string.Empty;
}

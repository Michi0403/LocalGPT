using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a LocalGPT project application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalGptProject
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this LocalGPT project instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="LocalGptProject"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the created at UTC associated with this LocalGPT project state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="LocalGptProject"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the updated at UTC associated with this LocalGPT project state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="LocalGptProject"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the name value that forms part of the LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="LocalGptProject"/>.</value>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the purpose value that forms part of the LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The purpose value exposed by <see cref="LocalGptProject"/>.</value>
    [Column(TypeName = "TEXT")]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the root path used by this LocalGPT project instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The root path value exposed by <see cref="LocalGptProject"/>.</value>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the project type value that forms part of the LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project type value exposed by <see cref="LocalGptProject"/>.</value>
    public string ProjectType { get; set; } = "DotNetSolution";

    /// <summary>
    /// Gets or sets the solution path used by this LocalGPT project instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The solution path value exposed by <see cref="LocalGptProject"/>.</value>
    public string SolutionPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the solution search pattern value that forms part of the LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The solution search pattern value exposed by <see cref="LocalGptProject"/>.</value>
    public string SolutionSearchPattern { get; set; } = @"(?i)\.(sln|slnx)$";

    /// <summary>
    /// Gets or sets the file include pattern value that forms part of the LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The file include pattern value exposed by <see cref="LocalGptProject"/>.</value>
    public string FileIncludePattern { get; set; } = @"(?s).*";

    /// <summary>
    /// Gets or sets the file exclude pattern value that forms part of the LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The file exclude pattern value exposed by <see cref="LocalGptProject"/>.</value>
    public string FileExcludePattern { get; set; } = @"(?i)(^|[\\/])(bin|obj|node_modules|\.git|\.vs|artifacts|security|secrets?)([\\/]|$)|(^|[\\/])(\.env(?:\..*)?|[^\\/]+\.(?:pfx|p12|key|pem))$";

    /// <summary>
    /// Gets or sets the current version value that forms part of the LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current version value exposed by <see cref="LocalGptProject"/>.</value>
    public string CurrentVersion { get; set; } = "0.1.0";

    /// <summary>
    /// Gets or sets the status value that forms part of the LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="LocalGptProject"/>.</value>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Gets or sets a value indicating whether recommend git applies to the LocalGPT project state.
    /// </summary>
    /// <value>The recommend git value exposed by <see cref="LocalGptProject"/>.</value>
    public bool RecommendGit { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether archived applies to the LocalGPT project state.
    /// </summary>
    /// <value>The is archived value exposed by <see cref="LocalGptProject"/>.</value>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Gets or sets the topics collection maintained or exposed by this LocalGPT project instance for downstream processing.
    /// </summary>
    /// <value>The topics value exposed by <see cref="LocalGptProject"/>.</value>
    public ICollection<LocalGptProjectTopic> Topics { get; set; } = [];

    /// <summary>
    /// Gets or sets the versions collection maintained or exposed by this LocalGPT project instance for downstream processing.
    /// </summary>
    /// <value>The versions value exposed by <see cref="LocalGptProject"/>.</value>
    public ICollection<LocalGptProjectVersion> Versions { get; set; } = [];

    /// <summary>
    /// Gets or sets the revisions collection maintained or exposed by this LocalGPT project instance for downstream processing.
    /// </summary>
    /// <value>The revisions value exposed by <see cref="LocalGptProject"/>.</value>
    public ICollection<LocalGptProjectRevision> Revisions { get; set; } = [];

    /// <summary>
    /// Gets or sets the requirements collection maintained or exposed by this LocalGPT project instance for downstream processing.
    /// </summary>
    /// <value>The requirements value exposed by <see cref="LocalGptProject"/>.</value>
    public ICollection<LocalGptProjectRequirement> Requirements { get; set; } = [];

    /// <summary>
    /// Gets or sets the artifacts collection maintained or exposed by this LocalGPT project instance for downstream processing.
    /// </summary>
    /// <value>The artifacts value exposed by <see cref="LocalGptProject"/>.</value>
    public ICollection<LocalGptProjectArtifact> Artifacts { get; set; } = [];

    /// <summary>
    /// Gets or sets the imported project documents associated with this project.
    /// </summary>
    /// <value>The project document imports associated with this project.</value>
    public ICollection<ProjectDocumentImport> DocumentImports { get; set; } = [];

    /// <summary>
    /// Navigates to the organic-skill assignments explicitly owned by this project.
    /// </summary>
    /// <value>The project's persisted organic-skill assignments.</value>
    public ICollection<ProjectOrganicSkillLink> OrganicSkillLinks { get; set; } = [];

    /// <summary>
    /// Navigates to embedded firmware plans whose optional project scope points at this project.
    /// </summary>
    /// <value>The embedded firmware plans associated with this project.</value>
    public ICollection<EmbeddedFirmwarePlanRecord> EmbeddedFirmwarePlans { get; set; } = [];

    /// <summary>
    /// Gets or sets the workspace roots collection maintained or exposed by this LocalGPT project instance for downstream processing.
    /// </summary>
    /// <value>The workspace roots value exposed by <see cref="LocalGptProject"/>.</value>
    public ICollection<ProjectWorkspaceRoot> WorkspaceRoots { get; set; } = [];

    /// <summary>
    /// Gets or sets the tracked files collection maintained or exposed by this LocalGPT project instance for downstream processing.
    /// </summary>
    /// <value>The tracked files value exposed by <see cref="LocalGptProject"/>.</value>
    public ICollection<LocalGptProjectTrackedFile> TrackedFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the build verifications collection maintained or exposed by this LocalGPT project instance for downstream processing.
    /// </summary>
    /// <value>The build verifications value exposed by <see cref="LocalGptProject"/>.</value>
    public ICollection<ProjectBuildVerification> BuildVerifications { get; set; } = [];
}

/// <summary>
/// Represents a LocalGPT project topic application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalGptProjectTopic
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this LocalGPT project topic instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="LocalGptProjectTopic"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this LocalGPT project topic instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="LocalGptProjectTopic"/>.</value>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Gets or sets the project value that forms part of the LocalGPT project topic state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project value exposed by <see cref="LocalGptProjectTopic"/>.</value>
    public LocalGptProject? Project { get; set; }

    /// <summary>
    /// Gets or sets the created at UTC associated with this LocalGPT project topic state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="LocalGptProjectTopic"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the updated at UTC associated with this LocalGPT project topic state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="LocalGptProjectTopic"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the name value that forms part of the LocalGPT project topic state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="LocalGptProjectTopic"/>.</value>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description value that forms part of the LocalGPT project topic state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="LocalGptProjectTopic"/>.</value>
    [Column(TypeName = "TEXT")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status value that forms part of the LocalGPT project topic state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="LocalGptProjectTopic"/>.</value>
    public string Status { get; set; } = "Planned";

    /// <summary>
    /// Gets or sets a value indicating whether user approved applies to the LocalGPT project topic state.
    /// </summary>
    /// <value>The is user approved value exposed by <see cref="LocalGptProjectTopic"/>.</value>
    public bool IsUserApproved { get; set; }

    /// <summary>
    /// Gets or sets the knowledge links collection maintained or exposed by this LocalGPT project topic instance for downstream processing.
    /// </summary>
    /// <value>The knowledge links value exposed by <see cref="LocalGptProjectTopic"/>.</value>
    public ICollection<LocalGptProjectTopicKnowledgeLink> KnowledgeLinks { get; set; } = [];
}

/// <summary>
/// Represents a LocalGPT project version application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalGptProjectVersion
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this LocalGPT project version instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="LocalGptProjectVersion"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this LocalGPT project version instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="LocalGptProjectVersion"/>.</value>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Gets or sets the project value that forms part of the LocalGPT project version state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project value exposed by <see cref="LocalGptProjectVersion"/>.</value>
    public LocalGptProject? Project { get; set; }

    /// <summary>
    /// Gets or sets the created at UTC associated with this LocalGPT project version state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="LocalGptProjectVersion"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the version value that forms part of the LocalGPT project version state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The version value exposed by <see cref="LocalGptProjectVersion"/>.</value>
    [Required]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notes value that forms part of the LocalGPT project version state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The notes value exposed by <see cref="LocalGptProjectVersion"/>.</value>
    [Column(TypeName = "TEXT")]
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path snapshot used by this LocalGPT project version instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path snapshot value exposed by <see cref="LocalGptProjectVersion"/>.</value>
    public string PathSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether current applies to the LocalGPT project version state.
    /// </summary>
    /// <value>The is current value exposed by <see cref="LocalGptProjectVersion"/>.</value>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the LocalGPT project version state.
    /// </summary>
    /// <value>The is user confirmed value exposed by <see cref="LocalGptProjectVersion"/>.</value>
    public bool IsUserConfirmed { get; set; }
}

/// <summary>
/// Represents a LocalGPT project topic knowledge link application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalGptProjectTopicKnowledgeLink
{
    /// <summary>
    /// Gets or sets the stable project topic identifier used to identify or correlate this LocalGPT project topic knowledge link instance with related application state.
    /// </summary>
    /// <value>The project topic identifier value exposed by <see cref="LocalGptProjectTopicKnowledgeLink"/>.</value>
    public Guid ProjectTopicId { get; set; }

    /// <summary>
    /// Gets or sets the project topic value that forms part of the LocalGPT project topic knowledge link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project topic value exposed by <see cref="LocalGptProjectTopicKnowledgeLink"/>.</value>
    public LocalGptProjectTopic? ProjectTopic { get; set; }

    /// <summary>
    /// Gets or sets the stable knowledge entry identifier used to identify or correlate this LocalGPT project topic knowledge link instance with related application state.
    /// </summary>
    /// <value>The knowledge entry identifier value exposed by <see cref="LocalGptProjectTopicKnowledgeLink"/>.</value>
    public Guid KnowledgeEntryId { get; set; }

    /// <summary>
    /// Gets or sets the knowledge entry value that forms part of the LocalGPT project topic knowledge link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The knowledge entry value exposed by <see cref="LocalGptProjectTopicKnowledgeLink"/>.</value>
    public CouncilKnowledgeEntry? KnowledgeEntry { get; set; }

    /// <summary>
    /// Gets or sets the linked at UTC associated with this LocalGPT project topic knowledge link state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The linked at UTC value exposed by <see cref="LocalGptProjectTopicKnowledgeLink"/>.</value>
    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the link reason value that forms part of the LocalGPT project topic knowledge link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The link reason value exposed by <see cref="LocalGptProjectTopicKnowledgeLink"/>.</value>
    public string LinkReason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether linked by human applies to the LocalGPT project topic knowledge link state.
    /// </summary>
    /// <value>The linked by human value exposed by <see cref="LocalGptProjectTopicKnowledgeLink"/>.</value>
    public bool LinkedByHuman { get; set; }
}

/// <summary>
/// Represents one human-readable project/topic association for a Council knowledge entry.
/// </summary>
/// <param name="ProjectId">Identifier of the owning project.</param>
/// <param name="ProjectTopicId">Identifier of the linked project topic.</param>
/// <param name="KnowledgeEntryId">Identifier of the linked Council knowledge entry.</param>
/// <param name="ProjectName">Human-readable project name.</param>
/// <param name="TopicName">Human-readable topic name.</param>
/// <param name="LinkReason">Persisted explanation for the relationship.</param>
/// <param name="LinkedAtUtc">UTC timestamp when the relationship was last refreshed.</param>
/// <param name="LinkedByHuman">Whether a human explicitly created or confirmed the relationship.</param>
public sealed record KnowledgeProjectTopicLinkSummary(
    Guid ProjectId,
    Guid ProjectTopicId,
    Guid KnowledgeEntryId,
    string ProjectName,
    string TopicName,
    string LinkReason,
    DateTime LinkedAtUtc,
    bool LinkedByHuman)
{
    /// <summary>Combines the owning project and topic names into the stable human-readable label used by relationship selectors.</summary>
    /// <value>The project name and topic name joined into one human-readable label.</value>
    public string DisplayName => $"{ProjectName} · {TopicName}";
}

/// <summary>
/// Represents a LocalGPT project summary application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="Name">Name value supplied to the LocalGPT project summary operation and used when producing its result.</param>
/// <param name="Purpose">Purpose value supplied to the LocalGPT project summary operation and used when producing its result.</param>
/// <param name="RootPath">Root path value supplied to the LocalGPT project summary operation and used when producing its result.</param>
/// <param name="CurrentVersion">Current version value supplied to the LocalGPT project summary operation and used when producing its result.</param>
/// <param name="Status">Status value supplied to the LocalGPT project summary operation and used when producing its result.</param>
/// <param name="RecommendGit">Value indicating whether recommend git should apply to this operation.</param>
/// <param name="IsArchived">Value indicating whether archived should apply to this operation.</param>
/// <param name="TopicCount">Topic count value supplied to the LocalGPT project summary operation and used when producing its result.</param>
/// <param name="VersionCount">Version count value supplied to the LocalGPT project summary operation and used when producing its result.</param>
/// <param name="UpdatedAtUtc">Updated at utc value supplied to the LocalGPT project summary operation and used when producing its result.</param>
public sealed record LocalGptProjectSummary(
    Guid Id,
    string Name,
    string Purpose,
    string RootPath,
    string CurrentVersion,
    string Status,
    bool RecommendGit,
    bool IsArchived,
    int TopicCount,
    int VersionCount,
    DateTime UpdatedAtUtc)
{
    /// <summary>
    /// Gets the display name value that forms part of the LocalGPT project summary state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="LocalGptProjectSummary"/>.</value>
    public string DisplayName => $"{Name} ({CurrentVersion})";
}

/// <summary>
/// Represents a LocalGPT project details application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalGptProjectDetails
{
    /// <summary>
    /// Gets or sets the project value that forms part of the LocalGPT project details state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project value exposed by <see cref="LocalGptProjectDetails"/>.</value>
    public required LocalGptProject Project { get; init; }

    /// <summary>
    /// Gets or sets the topics collection maintained or exposed by this LocalGPT project details instance for downstream processing.
    /// </summary>
    /// <value>The topics value exposed by <see cref="LocalGptProjectDetails"/>.</value>
    public IReadOnlyList<LocalGptProjectTopic> Topics { get; init; } = [];

    /// <summary>
    /// Gets or sets the versions collection maintained or exposed by this LocalGPT project details instance for downstream processing.
    /// </summary>
    /// <value>The versions value exposed by <see cref="LocalGptProjectDetails"/>.</value>
    public IReadOnlyList<LocalGptProjectVersion> Versions { get; init; } = [];

    /// <summary>
    /// Gets or sets the revisions collection maintained or exposed by this LocalGPT project details instance for downstream processing.
    /// </summary>
    /// <value>The revisions value exposed by <see cref="LocalGptProjectDetails"/>.</value>
    public IReadOnlyList<LocalGptProjectRevision> Revisions { get; init; } = [];

    /// <summary>
    /// Gets or sets the requirements collection maintained or exposed by this LocalGPT project details instance for downstream processing.
    /// </summary>
    /// <value>The requirements value exposed by <see cref="LocalGptProjectDetails"/>.</value>
    public IReadOnlyList<LocalGptProjectRequirement> Requirements { get; init; } = [];

    /// <summary>
    /// Gets or sets the artifacts collection maintained or exposed by this LocalGPT project details instance for downstream processing.
    /// </summary>
    /// <value>The artifacts value exposed by <see cref="LocalGptProjectDetails"/>.</value>
    public IReadOnlyList<LocalGptProjectArtifact> Artifacts { get; init; } = [];

    /// <summary>
    /// Gets or sets the workspace roots collection maintained or exposed by this LocalGPT project details instance for downstream processing.
    /// </summary>
    /// <value>The workspace roots value exposed by <see cref="LocalGptProjectDetails"/>.</value>
    public IReadOnlyList<ProjectWorkspaceRoot> WorkspaceRoots { get; init; } = [];

    /// <summary>
    /// Gets or sets the tracked files collection maintained or exposed by this LocalGPT project details instance for downstream processing.
    /// </summary>
    /// <value>The tracked files value exposed by <see cref="LocalGptProjectDetails"/>.</value>
    public IReadOnlyList<LocalGptProjectTrackedFile> TrackedFiles { get; init; } = [];

    /// <summary>
    /// Gets or sets the build verifications collection maintained or exposed by this LocalGPT project details instance for downstream processing.
    /// </summary>
    /// <value>The build verifications value exposed by <see cref="LocalGptProjectDetails"/>.</value>
    public IReadOnlyList<ProjectBuildVerification> BuildVerifications { get; init; } = [];
}

/// <summary>
/// Represents the input contract for save LocalGPT project, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SaveLocalGptProjectRequest
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this save LocalGPT project instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="SaveLocalGptProjectRequest"/>.</value>
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the name value that forms part of the save LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="SaveLocalGptProjectRequest"/>.</value>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the purpose value that forms part of the save LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The purpose value exposed by <see cref="SaveLocalGptProjectRequest"/>.</value>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the root path used by this save LocalGPT project instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The root path value exposed by <see cref="SaveLocalGptProjectRequest"/>.</value>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the project type value that forms part of the save LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project type value exposed by <see cref="SaveLocalGptProjectRequest"/>.</value>
    public string ProjectType { get; set; } = "DotNetSolution";

    /// <summary>
    /// Gets or sets the solution path used by this save LocalGPT project instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The solution path value exposed by <see cref="SaveLocalGptProjectRequest"/>.</value>
    public string SolutionPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the solution search pattern value that forms part of the save LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The solution search pattern value exposed by <see cref="SaveLocalGptProjectRequest"/>.</value>
    public string SolutionSearchPattern { get; set; } = @"(?i)\.(sln|slnx)$";

    /// <summary>
    /// Gets or sets the file include pattern value that forms part of the save LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The file include pattern value exposed by <see cref="SaveLocalGptProjectRequest"/>.</value>
    public string FileIncludePattern { get; set; } = @"(?s).*";

    /// <summary>
    /// Gets or sets the file exclude pattern value that forms part of the save LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The file exclude pattern value exposed by <see cref="SaveLocalGptProjectRequest"/>.</value>
    public string FileExcludePattern { get; set; } = @"(?i)(^|[\\/])(bin|obj|node_modules|\.git|\.vs|artifacts|security|secrets?)([\\/]|$)|(^|[\\/])(\.env(?:\..*)?|[^\\/]+\.(?:pfx|p12|key|pem))$";

    /// <summary>
    /// Gets or sets the current version value that forms part of the save LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current version value exposed by <see cref="SaveLocalGptProjectRequest"/>.</value>
    public string CurrentVersion { get; set; } = "0.1.0";

    /// <summary>
    /// Gets or sets the status value that forms part of the save LocalGPT project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="SaveLocalGptProjectRequest"/>.</value>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Gets or sets a value indicating whether recommend git applies to the save LocalGPT project state.
    /// </summary>
    /// <value>The recommend git value exposed by <see cref="SaveLocalGptProjectRequest"/>.</value>
    public bool RecommendGit { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether archived applies to the save LocalGPT project state.
    /// </summary>
    /// <value>The is archived value exposed by <see cref="SaveLocalGptProjectRequest"/>.</value>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the save LocalGPT project state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="SaveLocalGptProjectRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents the input contract for add LocalGPT project topic, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class AddLocalGptProjectTopicRequest
{
    /// <summary>
    /// Gets or sets the name value that forms part of the add LocalGPT project topic state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="AddLocalGptProjectTopicRequest"/>.</value>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description value that forms part of the add LocalGPT project topic state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="AddLocalGptProjectTopicRequest"/>.</value>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status value that forms part of the add LocalGPT project topic state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="AddLocalGptProjectTopicRequest"/>.</value>
    public string Status { get; set; } = "Planned";

    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the add LocalGPT project topic state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="AddLocalGptProjectTopicRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents the input contract for add LocalGPT project version, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class AddLocalGptProjectVersionRequest
{
    /// <summary>
    /// Gets or sets the version value that forms part of the add LocalGPT project version state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The version value exposed by <see cref="AddLocalGptProjectVersionRequest"/>.</value>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notes value that forms part of the add LocalGPT project version state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The notes value exposed by <see cref="AddLocalGptProjectVersionRequest"/>.</value>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path snapshot used by this add LocalGPT project version instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The path snapshot value exposed by <see cref="AddLocalGptProjectVersionRequest"/>.</value>
    public string PathSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether current applies to the add LocalGPT project version state.
    /// </summary>
    /// <value>The is current value exposed by <see cref="AddLocalGptProjectVersionRequest"/>.</value>
    public bool IsCurrent { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the add LocalGPT project version state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="AddLocalGptProjectVersionRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents the input contract for link project topic knowledge, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class LinkProjectTopicKnowledgeRequest
{
    /// <summary>
    /// Gets or sets the stable knowledge entry identifier used to identify or correlate this link project topic knowledge instance with related application state.
    /// </summary>
    /// <value>The knowledge entry identifier value exposed by <see cref="LinkProjectTopicKnowledgeRequest"/>.</value>
    public Guid KnowledgeEntryId { get; set; }

    /// <summary>
    /// Gets or sets the link reason value that forms part of the link project topic knowledge state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The link reason value exposed by <see cref="LinkProjectTopicKnowledgeRequest"/>.</value>
    public string LinkReason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the link project topic knowledge state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="LinkProjectTopicKnowledgeRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

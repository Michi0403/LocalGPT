using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a LocalGPT project revision application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalGptProjectRevision
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this LocalGPT project revision instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this LocalGPT project revision instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the project value that forms part of the LocalGPT project revision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public LocalGptProject? Project { get; set; }
    /// <summary>
    /// Gets or sets the stable parent revision identifier used to identify or correlate this LocalGPT project revision instance with related application state.
    /// </summary>
    /// <value>The parent revision identifier value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public Guid? ParentRevisionId { get; set; }
    /// <summary>
    /// Gets or sets the parent revision value that forms part of the LocalGPT project revision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parent revision value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public LocalGptProjectRevision? ParentRevision { get; set; }
    /// <summary>
    /// Gets or sets the child revisions collection maintained or exposed by this LocalGPT project revision instance for downstream processing.
    /// </summary>
    /// <value>The child revisions value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public ICollection<LocalGptProjectRevision> ChildRevisions { get; set; } = [];
    /// <summary>
    /// Gets or sets the created at UTC associated with this LocalGPT project revision state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this LocalGPT project revision state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the branch name value that forms part of the LocalGPT project revision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The branch name value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    [Required, MaxLength(160)] public string BranchName { get; set; } = "main";
    /// <summary>
    /// Gets or sets the revision name value that forms part of the LocalGPT project revision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision name value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    [Required, MaxLength(160)] public string RevisionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the summary value that forms part of the LocalGPT project revision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The summary value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    [Column(TypeName = "TEXT")] public string Summary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the project structure JSON value that forms part of the LocalGPT project revision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project structure JSON value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    [Column(TypeName = "TEXT")] public string ProjectStructureJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets the created by value that forms part of the LocalGPT project revision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The created by value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    [MaxLength(120)] public string CreatedBy { get; set; } = "Human User";
    /// <summary>
    /// Gets or sets a value indicating whether current applies to the LocalGPT project revision state.
    /// </summary>
    /// <value>The is current value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public bool IsCurrent { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user approved applies to the LocalGPT project revision state.
    /// </summary>
    /// <value>The is user approved value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public bool IsUserApproved { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether compile verified applies to the LocalGPT project revision state.
    /// </summary>
    /// <value>The compile verified value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public bool CompileVerified { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether council verified applies to the LocalGPT project revision state.
    /// </summary>
    /// <value>The council verified value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public bool CouncilVerified { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether ready for testing applies to the LocalGPT project revision state.
    /// </summary>
    /// <value>The ready for testing value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public bool ReadyForTesting { get; set; }
    /// <summary>
    /// Gets or sets the approved for testing at UTC associated with this LocalGPT project revision state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The approved for testing at UTC value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public DateTime? ApprovedForTestingAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the source snapshot hash value that forms part of the LocalGPT project revision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source snapshot hash value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    [MaxLength(128)] public string SourceSnapshotHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the snapshot archive path used by this LocalGPT project revision instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The snapshot archive path value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    [MaxLength(2048)] public string SnapshotArchivePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source root path used by this LocalGPT project revision instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The source root path value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    [MaxLength(2048)] public string SourceRootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the solution path used by this LocalGPT project revision instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The solution path value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    [MaxLength(2048)] public string SolutionPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the tracked files collection maintained or exposed by this LocalGPT project revision instance for downstream processing.
    /// </summary>
    /// <value>The tracked files value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public ICollection<LocalGptProjectTrackedFile> TrackedFiles { get; set; } = [];
    /// <summary>
    /// Gets or sets the build verifications collection maintained or exposed by this LocalGPT project revision instance for downstream processing.
    /// </summary>
    /// <value>The build verifications value exposed by <see cref="LocalGptProjectRevision"/>.</value>
    public ICollection<ProjectBuildVerification> BuildVerifications { get; set; } = [];
    /// <summary>
    /// Navigates to the requirement records whose scope is anchored to this exact project revision.
    /// </summary>
    /// <value>The revision-scoped requirements.</value>
    public ICollection<LocalGptProjectRequirement> Requirements { get; set; } = [];
    /// <summary>
    /// Navigates to the generated or reviewed artifact records whose scope is anchored to this exact project revision.
    /// </summary>
    /// <value>The revision-scoped project artifacts.</value>
    public ICollection<LocalGptProjectArtifact> Artifacts { get; set; } = [];
    /// <summary>
    /// Gets or sets imported project documents explicitly scoped to this revision.
    /// </summary>
    /// <value>The revision-scoped document imports.</value>
    public ICollection<ProjectDocumentImport> DocumentImports { get; set; } = [];
}

/// <summary>
/// Represents a LocalGPT project requirement application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalGptProjectRequirement
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this LocalGPT project requirement instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this LocalGPT project requirement instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the project value that forms part of the LocalGPT project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    public LocalGptProject? Project { get; set; }
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this LocalGPT project requirement instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets the revision value that forms part of the LocalGPT project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    public LocalGptProjectRevision? Revision { get; set; }
    /// <summary>
    /// Gets or sets the created at UTC associated with this LocalGPT project requirement state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this LocalGPT project requirement state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the name value that forms part of the LocalGPT project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    [Required, MaxLength(240)] public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the LocalGPT project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    [Column(TypeName = "TEXT")] public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the requirement type value that forms part of the LocalGPT project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requirement type value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    [MaxLength(80)] public string RequirementType { get; set; } = "Functional";
    /// <summary>
    /// Gets or sets the status value that forms part of the LocalGPT project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    [MaxLength(80)] public string Status { get; set; } = "Planned";
    /// <summary>
    /// Gets or sets the priority value that forms part of the LocalGPT project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The priority value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    [MaxLength(40)] public string Priority { get; set; } = "Normal";
    /// <summary>
    /// Gets or sets the required capability value that forms part of the LocalGPT project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The required capability value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    [MaxLength(240)] public string RequiredCapability { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source kind value that forms part of the LocalGPT project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source kind value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    [MaxLength(160)] public string SourceKind { get; set; } = "Human";
    /// <summary>
    /// Gets or sets the council rating value that forms part of the LocalGPT project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council rating value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    public int CouncilRating { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user approved applies to the LocalGPT project requirement state.
    /// </summary>
    /// <value>The is user approved value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    public bool IsUserApproved { get; set; }
    /// <summary>
    /// Gets or sets the links collection maintained or exposed by this LocalGPT project requirement instance for downstream processing.
    /// </summary>
    /// <value>The links value exposed by <see cref="LocalGptProjectRequirement"/>.</value>
    public ICollection<LocalGptProjectRequirementLink> Links { get; set; } = [];
    /// <summary>
    /// Gets or sets project artifacts that explicitly satisfy or document this requirement.
    /// </summary>
    /// <value>The artifacts associated with this requirement.</value>
    public ICollection<LocalGptProjectArtifact> Artifacts { get; set; } = [];
}

/// <summary>
/// Represents a LocalGPT project requirement link application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalGptProjectRequirementLink
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this LocalGPT project requirement link instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="LocalGptProjectRequirementLink"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable requirement identifier used to identify or correlate this LocalGPT project requirement link instance with related application state.
    /// </summary>
    /// <value>The requirement identifier value exposed by <see cref="LocalGptProjectRequirementLink"/>.</value>
    public Guid RequirementId { get; set; }
    /// <summary>
    /// Gets or sets the requirement value that forms part of the LocalGPT project requirement link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requirement value exposed by <see cref="LocalGptProjectRequirementLink"/>.</value>
    public LocalGptProjectRequirement? Requirement { get; set; }
    /// <summary>
    /// Gets or sets the linked at UTC associated with this LocalGPT project requirement link state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The linked at UTC value exposed by <see cref="LocalGptProjectRequirementLink"/>.</value>
    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the target kind value that forms part of the LocalGPT project requirement link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target kind value exposed by <see cref="LocalGptProjectRequirementLink"/>.</value>
    [Required, MaxLength(80)] public string TargetKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the target name value that forms part of the LocalGPT project requirement link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target name value exposed by <see cref="LocalGptProjectRequirementLink"/>.</value>
    [Required, MaxLength(240)] public string TargetName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable target identifier used to identify or correlate this LocalGPT project requirement link instance with related application state.
    /// </summary>
    /// <value>The target identifier value exposed by <see cref="LocalGptProjectRequirementLink"/>.</value>
    [MaxLength(160)] public string TargetId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the target table value that forms part of the LocalGPT project requirement link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target table value exposed by <see cref="LocalGptProjectRequirementLink"/>.</value>
    [MaxLength(160)] public string TargetTable { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the link purpose value that forms part of the LocalGPT project requirement link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The link purpose value exposed by <see cref="LocalGptProjectRequirementLink"/>.</value>
    [MaxLength(1000)] public string LinkPurpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the council review status value that forms part of the LocalGPT project requirement link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council review status value exposed by <see cref="LocalGptProjectRequirementLink"/>.</value>
    [MaxLength(80)] public string CouncilReviewStatus { get; set; } = "NotReviewed";
    /// <summary>
    /// Gets or sets a value indicating whether user approved applies to the LocalGPT project requirement link state.
    /// </summary>
    /// <value>The is user approved value exposed by <see cref="LocalGptProjectRequirementLink"/>.</value>
    public bool IsUserApproved { get; set; }
}

/// <summary>
/// Represents a LocalGPT project artifact application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalGptProjectArtifact
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this LocalGPT project artifact instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this LocalGPT project artifact instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the project value that forms part of the LocalGPT project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    public LocalGptProject? Project { get; set; }
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this LocalGPT project artifact instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets the revision value that forms part of the LocalGPT project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    public LocalGptProjectRevision? Revision { get; set; }
    /// <summary>
    /// Gets or sets the stable requirement identifier used to identify or correlate this LocalGPT project artifact instance with related application state.
    /// </summary>
    /// <value>The requirement identifier value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    public Guid? RequirementId { get; set; }
    /// <summary>
    /// Gets or sets the requirement value that forms part of the LocalGPT project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requirement value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    public LocalGptProjectRequirement? Requirement { get; set; }
    /// <summary>
    /// Gets or sets the created at UTC associated with this LocalGPT project artifact state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this LocalGPT project artifact state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the artifact kind value that forms part of the LocalGPT project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The artifact kind value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    [Required, MaxLength(80)] public string ArtifactKind { get; set; } = "Configuration";
    /// <summary>
    /// Gets or sets the name value that forms part of the LocalGPT project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    [Required, MaxLength(240)] public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the value value that forms part of the LocalGPT project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The value value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    [Column(TypeName = "TEXT")] public string Value { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the data type value that forms part of the LocalGPT project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The data type value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    [MaxLength(120)] public string DataType { get; set; } = "string";
    /// <summary>
    /// Gets or sets the flags value that forms part of the LocalGPT project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The flags value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    [MaxLength(160)] public string Flags { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the LocalGPT project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the council review status value that forms part of the LocalGPT project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council review status value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    [MaxLength(80)] public string CouncilReviewStatus { get; set; } = "NotReviewed";
    /// <summary>
    /// Gets or sets a value indicating whether sensitive applies to the LocalGPT project artifact state.
    /// </summary>
    /// <value>The is sensitive value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    public bool IsSensitive { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user approved applies to the LocalGPT project artifact state.
    /// </summary>
    /// <value>The is user approved value exposed by <see cref="LocalGptProjectArtifact"/>.</value>
    public bool IsUserApproved { get; set; }
}

/// <summary>
/// Represents a project document import application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectDocumentImport
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this project document import instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="ProjectDocumentImport"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project document import instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectDocumentImport"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the project value that forms part of the project document import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project value exposed by <see cref="ProjectDocumentImport"/>.</value>
    public LocalGptProject? Project { get; set; }
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this project document import instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="ProjectDocumentImport"/>.</value>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets the revision value that forms part of the project document import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision value exposed by <see cref="ProjectDocumentImport"/>.</value>
    public LocalGptProjectRevision? Revision { get; set; }
    /// <summary>
    /// Gets or sets the imported at UTC associated with this project document import state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The imported at UTC value exposed by <see cref="ProjectDocumentImport"/>.</value>
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the source name value that forms part of the project document import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source name value exposed by <see cref="ProjectDocumentImport"/>.</value>
    [Required, MaxLength(260)] public string SourceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source URI that identifies the network or application endpoint associated with this project document import state.
    /// </summary>
    /// <value>The source URI value exposed by <see cref="ProjectDocumentImport"/>.</value>
    [MaxLength(2048)] public string SourceUri { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the content hash value that forms part of the project document import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content hash value exposed by <see cref="ProjectDocumentImport"/>.</value>
    [Required, MaxLength(128)] public string ContentHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the content type value that forms part of the project document import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content type value exposed by <see cref="ProjectDocumentImport"/>.</value>
    [MaxLength(120)] public string ContentType { get; set; } = "text/plain";
    /// <summary>
    /// Gets or sets the encoding name value that forms part of the project document import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The encoding name value exposed by <see cref="ProjectDocumentImport"/>.</value>
    [MaxLength(80)] public string EncodingName { get; set; } = "utf-8";
    /// <summary>
    /// Gets or sets the extracted text value that forms part of the project document import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The extracted text value exposed by <see cref="ProjectDocumentImport"/>.</value>
    [Column(TypeName = "TEXT")] public string ExtractedText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the project document import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="ProjectDocumentImport"/>.</value>
    [MaxLength(80)] public string Status { get; set; } = "Imported";
    /// <summary>
    /// Gets or sets the safety notes value that forms part of the project document import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The safety notes value exposed by <see cref="ProjectDocumentImport"/>.</value>
    [MaxLength(2000)] public string SafetyNotes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether user approved applies to the project document import state.
    /// </summary>
    /// <value>The is user approved value exposed by <see cref="ProjectDocumentImport"/>.</value>
    public bool IsUserApproved { get; set; }
}

/// <summary>
/// Represents a council model preset application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CouncilModelPreset
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this council model preset instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="CouncilModelPreset"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the created at UTC associated with this council model preset state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="CouncilModelPreset"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this council model preset state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CouncilModelPreset"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the name value that forms part of the council model preset state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="CouncilModelPreset"/>.</value>
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the council model preset state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="CouncilModelPreset"/>.</value>
    [MaxLength(1000)] public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the model names JSON value that forms part of the council model preset state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model names JSON value exposed by <see cref="CouncilModelPreset"/>.</value>
    [Required, Column(TypeName = "TEXT")] public string ModelNamesJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the model routes JSON value that forms part of the council model preset state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model routes JSON value exposed by <see cref="CouncilModelPreset"/>.</value>
    [Required, Column(TypeName = "TEXT")] public string ModelRoutesJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets a value indicating whether parallel hardware roads applies to the council model preset state.
    /// </summary>
    /// <value>The allow parallel hardware roads value exposed by <see cref="CouncilModelPreset"/>.</value>
    public bool AllowParallelHardwareRoads { get; set; } = true;
    /// <summary>
    /// Gets or sets the max output tokens value that forms part of the council model preset state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max output tokens value exposed by <see cref="CouncilModelPreset"/>.</value>
    public int MaxOutputTokens { get; set; } = 4096;
    /// <summary>
    /// Gets or sets the max context tokens value that forms part of the council model preset state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max context tokens value exposed by <see cref="CouncilModelPreset"/>.</value>
    public int MaxContextTokens { get; set; } = 32768;
    /// <summary>
    /// Gets or sets the max parallel models value that forms part of the council model preset state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max parallel models value exposed by <see cref="CouncilModelPreset"/>.</value>
    public int MaxParallelModels { get; set; } = 1;
    /// <summary>
    /// Gets or sets the Ollama num GPU value that forms part of the council model preset state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The Ollama num GPU value exposed by <see cref="CouncilModelPreset"/>.</value>
    public int? OllamaNumGpu { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether memory applies to the council model preset state.
    /// </summary>
    /// <value>The include memory value exposed by <see cref="CouncilModelPreset"/>.</value>
    public bool IncludeMemory { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether generate artifacts applies to the council model preset state.
    /// </summary>
    /// <value>The generate artifacts value exposed by <see cref="CouncilModelPreset"/>.</value>
    public bool GenerateArtifacts { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether create project per run applies to the council model preset state.
    /// </summary>
    /// <value>The create project per run value exposed by <see cref="CouncilModelPreset"/>.</value>
    public bool CreateProjectPerRun { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether default applies to the council model preset state.
    /// </summary>
    /// <value>The is default value exposed by <see cref="CouncilModelPreset"/>.</value>
    public bool IsDefault { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether archived applies to the council model preset state.
    /// </summary>
    /// <value>The is archived value exposed by <see cref="CouncilModelPreset"/>.</value>
    public bool IsArchived { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user approved applies to the council model preset state.
    /// </summary>
    /// <value>The is user approved value exposed by <see cref="CouncilModelPreset"/>.</value>
    public bool IsUserApproved { get; set; }
}

/// <summary>
/// Represents a sqlite editor field override application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class SqliteEditorFieldOverride
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this sqlite editor field override instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="SqliteEditorFieldOverride"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the updated at UTC associated with this sqlite editor field override state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="SqliteEditorFieldOverride"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the table name value that forms part of the sqlite editor field override state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The table name value exposed by <see cref="SqliteEditorFieldOverride"/>.</value>
    [Required, MaxLength(160)] public string TableName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the column name value that forms part of the sqlite editor field override state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The column name value exposed by <see cref="SqliteEditorFieldOverride"/>.</value>
    [Required, MaxLength(160)] public string ColumnName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the editor kind value that forms part of the sqlite editor field override state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The editor kind value exposed by <see cref="SqliteEditorFieldOverride"/>.</value>
    [Required, MaxLength(40)] public string EditorKind { get; set; } = "Automatic";
    /// <summary>
    /// Gets or sets the input mask value that forms part of the sqlite editor field override state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The input mask value exposed by <see cref="SqliteEditorFieldOverride"/>.</value>
    [MaxLength(240)] public string InputMask { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the format string value that forms part of the sqlite editor field override state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The format string value exposed by <see cref="SqliteEditorFieldOverride"/>.</value>
    [MaxLength(160)] public string FormatString { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the null text value that forms part of the sqlite editor field override state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The null text value exposed by <see cref="SqliteEditorFieldOverride"/>.</value>
    [MaxLength(120)] public string NullText { get; set; } = "[null]";
    /// <summary>
    /// Gets or sets a value indicating whether sensitive applies to the sqlite editor field override state.
    /// </summary>
    /// <value>The is sensitive value exposed by <see cref="SqliteEditorFieldOverride"/>.</value>
    public bool IsSensitive { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether human approval applies to the sqlite editor field override state.
    /// </summary>
    /// <value>The require human approval value exposed by <see cref="SqliteEditorFieldOverride"/>.</value>
    public bool RequireHumanApproval { get; set; }
}

/// <summary>
/// Represents a council knowledge user rating application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CouncilKnowledgeUserRating
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this council knowledge user rating instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="CouncilKnowledgeUserRating"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable knowledge entry identifier used to identify or correlate this council knowledge user rating instance with related application state.
    /// </summary>
    /// <value>The knowledge entry identifier value exposed by <see cref="CouncilKnowledgeUserRating"/>.</value>
    public Guid KnowledgeEntryId { get; set; }
    /// <summary>
    /// Gets or sets the knowledge entry value that forms part of the council knowledge user rating state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The knowledge entry value exposed by <see cref="CouncilKnowledgeUserRating"/>.</value>
    public CouncilKnowledgeEntry? KnowledgeEntry { get; set; }
    /// <summary>
    /// Gets or sets the created at UTC associated with this council knowledge user rating state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="CouncilKnowledgeUserRating"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this council knowledge user rating state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CouncilKnowledgeUserRating"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the rating value that forms part of the council knowledge user rating state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rating value exposed by <see cref="CouncilKnowledgeUserRating"/>.</value>
    public int Rating { get; set; }
    /// <summary>
    /// Gets or sets the accuracy status value that forms part of the council knowledge user rating state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The accuracy status value exposed by <see cref="CouncilKnowledgeUserRating"/>.</value>
    [MaxLength(80)] public string AccuracyStatus { get; set; } = "Unrated";
    /// <summary>
    /// Gets or sets the notes value that forms part of the council knowledge user rating state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The notes value exposed by <see cref="CouncilKnowledgeUserRating"/>.</value>
    [MaxLength(4000)] public string Notes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether approved for council use applies to the council knowledge user rating state.
    /// </summary>
    /// <value>The approved for council use value exposed by <see cref="CouncilKnowledgeUserRating"/>.</value>
    public bool ApprovedForCouncilUse { get; set; }
    /// <summary>
    /// Gets or sets the rated by value that forms part of the council knowledge user rating state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The rated by value exposed by <see cref="CouncilKnowledgeUserRating"/>.</value>
    [MaxLength(120)] public string RatedBy { get; set; } = "Human User";
}

/// <summary>
/// Represents the input contract for save project revision, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SaveProjectRevisionRequest
{
    /// <summary>
    /// Gets or sets the stable parent revision identifier used to identify or correlate this save project revision instance with related application state.
    /// </summary>
    /// <value>The parent revision identifier value exposed by <see cref="SaveProjectRevisionRequest"/>.</value>
    public Guid? ParentRevisionId { get; set; }
    /// <summary>
    /// Gets or sets the branch name value that forms part of the save project revision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The branch name value exposed by <see cref="SaveProjectRevisionRequest"/>.</value>
    public string BranchName { get; set; } = "main";
    /// <summary>
    /// Gets or sets the revision name value that forms part of the save project revision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision name value exposed by <see cref="SaveProjectRevisionRequest"/>.</value>
    public string RevisionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the summary value that forms part of the save project revision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The summary value exposed by <see cref="SaveProjectRevisionRequest"/>.</value>
    public string Summary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the project structure JSON value that forms part of the save project revision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project structure JSON value exposed by <see cref="SaveProjectRevisionRequest"/>.</value>
    public string ProjectStructureJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets a value indicating whether current applies to the save project revision state.
    /// </summary>
    /// <value>The is current value exposed by <see cref="SaveProjectRevisionRequest"/>.</value>
    public bool IsCurrent { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the save project revision state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="SaveProjectRevisionRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents the input contract for save project requirement, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SaveProjectRequirementRequest
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this save project requirement instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="SaveProjectRequirementRequest"/>.</value>
    public Guid? Id { get; set; }
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this save project requirement instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="SaveProjectRequirementRequest"/>.</value>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets the name value that forms part of the save project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="SaveProjectRequirementRequest"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the save project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="SaveProjectRequirementRequest"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the requirement type value that forms part of the save project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requirement type value exposed by <see cref="SaveProjectRequirementRequest"/>.</value>
    public string RequirementType { get; set; } = "Functional";
    /// <summary>
    /// Gets or sets the status value that forms part of the save project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="SaveProjectRequirementRequest"/>.</value>
    public string Status { get; set; } = "Planned";
    /// <summary>
    /// Gets or sets the priority value that forms part of the save project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The priority value exposed by <see cref="SaveProjectRequirementRequest"/>.</value>
    public string Priority { get; set; } = "Normal";
    /// <summary>
    /// Gets or sets the required capability value that forms part of the save project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The required capability value exposed by <see cref="SaveProjectRequirementRequest"/>.</value>
    public string RequiredCapability { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the council rating value that forms part of the save project requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council rating value exposed by <see cref="SaveProjectRequirementRequest"/>.</value>
    public int CouncilRating { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the save project requirement state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="SaveProjectRequirementRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}


/// <summary>
/// Represents the input contract for save project requirement link, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SaveProjectRequirementLinkRequest
{
    /// <summary>
    /// Gets or sets the stable requirement identifier used to identify or correlate this save project requirement link instance with related application state.
    /// </summary>
    /// <value>The requirement identifier value exposed by <see cref="SaveProjectRequirementLinkRequest"/>.</value>
    public Guid RequirementId { get; set; }
    /// <summary>
    /// Gets or sets the target kind value that forms part of the save project requirement link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target kind value exposed by <see cref="SaveProjectRequirementLinkRequest"/>.</value>
    public string TargetKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the target name value that forms part of the save project requirement link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target name value exposed by <see cref="SaveProjectRequirementLinkRequest"/>.</value>
    public string TargetName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable target identifier used to identify or correlate this save project requirement link instance with related application state.
    /// </summary>
    /// <value>The target identifier value exposed by <see cref="SaveProjectRequirementLinkRequest"/>.</value>
    public string TargetId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the target table value that forms part of the save project requirement link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target table value exposed by <see cref="SaveProjectRequirementLinkRequest"/>.</value>
    public string TargetTable { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the link purpose value that forms part of the save project requirement link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The link purpose value exposed by <see cref="SaveProjectRequirementLinkRequest"/>.</value>
    public string LinkPurpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the save project requirement link state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="SaveProjectRequirementLinkRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents the input contract for save project artifact, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SaveProjectArtifactRequest
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this save project artifact instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="SaveProjectArtifactRequest"/>.</value>
    public Guid? Id { get; set; }
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this save project artifact instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="SaveProjectArtifactRequest"/>.</value>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets the stable requirement identifier used to identify or correlate this save project artifact instance with related application state.
    /// </summary>
    /// <value>The requirement identifier value exposed by <see cref="SaveProjectArtifactRequest"/>.</value>
    public Guid? RequirementId { get; set; }
    /// <summary>
    /// Gets or sets the artifact kind value that forms part of the save project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The artifact kind value exposed by <see cref="SaveProjectArtifactRequest"/>.</value>
    public string ArtifactKind { get; set; } = "Configuration";
    /// <summary>
    /// Gets or sets the name value that forms part of the save project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="SaveProjectArtifactRequest"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the value value that forms part of the save project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The value value exposed by <see cref="SaveProjectArtifactRequest"/>.</value>
    public string Value { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the data type value that forms part of the save project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The data type value exposed by <see cref="SaveProjectArtifactRequest"/>.</value>
    public string DataType { get; set; } = "string";
    /// <summary>
    /// Gets or sets the flags value that forms part of the save project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The flags value exposed by <see cref="SaveProjectArtifactRequest"/>.</value>
    public string Flags { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the save project artifact state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="SaveProjectArtifactRequest"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether sensitive applies to the save project artifact state.
    /// </summary>
    /// <value>The is sensitive value exposed by <see cref="SaveProjectArtifactRequest"/>.</value>
    public bool IsSensitive { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the save project artifact state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="SaveProjectArtifactRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

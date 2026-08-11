using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a local gpt project revision.
/// </summary>
public sealed class LocalGptProjectRevision
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets project.
    /// </summary>
    public LocalGptProject? Project { get; set; }
    /// <summary>
    /// Gets or sets parent revision identifier.
    /// </summary>
    public Guid? ParentRevisionId { get; set; }
    /// <summary>
    /// Gets or sets parent revision.
    /// </summary>
    public LocalGptProjectRevision? ParentRevision { get; set; }
    /// <summary>
    /// Gets or sets child revisions.
    /// </summary>
    public ICollection<LocalGptProjectRevision> ChildRevisions { get; set; } = [];
    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets branch name.
    /// </summary>
    [Required, MaxLength(160)] public string BranchName { get; set; } = "main";
    /// <summary>
    /// Gets or sets revision name.
    /// </summary>
    [Required, MaxLength(160)] public string RevisionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets summary.
    /// </summary>
    [Column(TypeName = "TEXT")] public string Summary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets project structure JSON.
    /// </summary>
    [Column(TypeName = "TEXT")] public string ProjectStructureJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets created by.
    /// </summary>
    [MaxLength(120)] public string CreatedBy { get; set; } = "Human User";
    /// <summary>
    /// Gets or sets is current.
    /// </summary>
    public bool IsCurrent { get; set; }
    /// <summary>
    /// Gets or sets is user approved.
    /// </summary>
    public bool IsUserApproved { get; set; }
    /// <summary>
    /// Gets or sets compile verified.
    /// </summary>
    public bool CompileVerified { get; set; }
    /// <summary>
    /// Gets or sets council verified.
    /// </summary>
    public bool CouncilVerified { get; set; }
    /// <summary>
    /// Gets or sets ready for testing.
    /// </summary>
    public bool ReadyForTesting { get; set; }
    /// <summary>
    /// Gets or sets approved for testing at UTC.
    /// </summary>
    public DateTime? ApprovedForTestingAtUtc { get; set; }
    /// <summary>
    /// Gets or sets source snapshot hash.
    /// </summary>
    [MaxLength(128)] public string SourceSnapshotHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets snapshot archive path.
    /// </summary>
    [MaxLength(2048)] public string SnapshotArchivePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source root path.
    /// </summary>
    [MaxLength(2048)] public string SourceRootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets solution path.
    /// </summary>
    [MaxLength(2048)] public string SolutionPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets tracked files.
    /// </summary>
    public ICollection<LocalGptProjectTrackedFile> TrackedFiles { get; set; } = [];
    /// <summary>
    /// Gets or sets build verifications.
    /// </summary>
    public ICollection<ProjectBuildVerification> BuildVerifications { get; set; } = [];
}

/// <summary>
/// Represents a local gpt project requirement.
/// </summary>
public sealed class LocalGptProjectRequirement
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets project.
    /// </summary>
    public LocalGptProject? Project { get; set; }
    /// <summary>
    /// Gets or sets revision identifier.
    /// </summary>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets revision.
    /// </summary>
    public LocalGptProjectRevision? Revision { get; set; }
    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    [Required, MaxLength(240)] public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    [Column(TypeName = "TEXT")] public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets requirement type.
    /// </summary>
    [MaxLength(80)] public string RequirementType { get; set; } = "Functional";
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    [MaxLength(80)] public string Status { get; set; } = "Planned";
    /// <summary>
    /// Gets or sets priority.
    /// </summary>
    [MaxLength(40)] public string Priority { get; set; } = "Normal";
    /// <summary>
    /// Gets or sets required capability.
    /// </summary>
    [MaxLength(240)] public string RequiredCapability { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source kind.
    /// </summary>
    [MaxLength(160)] public string SourceKind { get; set; } = "Human";
    /// <summary>
    /// Gets or sets council rating.
    /// </summary>
    public int CouncilRating { get; set; }
    /// <summary>
    /// Gets or sets is user approved.
    /// </summary>
    public bool IsUserApproved { get; set; }
    /// <summary>
    /// Gets or sets links.
    /// </summary>
    public ICollection<LocalGptProjectRequirementLink> Links { get; set; } = [];
}

/// <summary>
/// Represents a local gpt project requirement link.
/// </summary>
public sealed class LocalGptProjectRequirementLink
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets requirement identifier.
    /// </summary>
    public Guid RequirementId { get; set; }
    /// <summary>
    /// Gets or sets requirement.
    /// </summary>
    public LocalGptProjectRequirement? Requirement { get; set; }
    /// <summary>
    /// Gets or sets linked at UTC.
    /// </summary>
    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets target kind.
    /// </summary>
    [Required, MaxLength(80)] public string TargetKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets target name.
    /// </summary>
    [Required, MaxLength(240)] public string TargetName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets target identifier.
    /// </summary>
    [MaxLength(160)] public string TargetId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets target table.
    /// </summary>
    [MaxLength(160)] public string TargetTable { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets link purpose.
    /// </summary>
    [MaxLength(1000)] public string LinkPurpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets council review status.
    /// </summary>
    [MaxLength(80)] public string CouncilReviewStatus { get; set; } = "NotReviewed";
    /// <summary>
    /// Gets or sets is user approved.
    /// </summary>
    public bool IsUserApproved { get; set; }
}

/// <summary>
/// Represents a local gpt project artifact.
/// </summary>
public sealed class LocalGptProjectArtifact
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets project.
    /// </summary>
    public LocalGptProject? Project { get; set; }
    /// <summary>
    /// Gets or sets revision identifier.
    /// </summary>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets revision.
    /// </summary>
    public LocalGptProjectRevision? Revision { get; set; }
    /// <summary>
    /// Gets or sets requirement identifier.
    /// </summary>
    public Guid? RequirementId { get; set; }
    /// <summary>
    /// Gets or sets requirement.
    /// </summary>
    public LocalGptProjectRequirement? Requirement { get; set; }
    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets artifact kind.
    /// </summary>
    [Required, MaxLength(80)] public string ArtifactKind { get; set; } = "Configuration";
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    [Required, MaxLength(240)] public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets value.
    /// </summary>
    [Column(TypeName = "TEXT")] public string Value { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets data type.
    /// </summary>
    [MaxLength(120)] public string DataType { get; set; } = "string";
    /// <summary>
    /// Gets or sets flags.
    /// </summary>
    [MaxLength(160)] public string Flags { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets council review status.
    /// </summary>
    [MaxLength(80)] public string CouncilReviewStatus { get; set; } = "NotReviewed";
    /// <summary>
    /// Gets or sets is sensitive.
    /// </summary>
    public bool IsSensitive { get; set; }
    /// <summary>
    /// Gets or sets is user approved.
    /// </summary>
    public bool IsUserApproved { get; set; }
}

/// <summary>
/// Represents a project document import.
/// </summary>
public sealed class ProjectDocumentImport
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets project.
    /// </summary>
    public LocalGptProject? Project { get; set; }
    /// <summary>
    /// Gets or sets revision identifier.
    /// </summary>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets revision.
    /// </summary>
    public LocalGptProjectRevision? Revision { get; set; }
    /// <summary>
    /// Gets or sets imported at UTC.
    /// </summary>
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets source name.
    /// </summary>
    [Required, MaxLength(260)] public string SourceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source URI.
    /// </summary>
    [MaxLength(2048)] public string SourceUri { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets content hash.
    /// </summary>
    [Required, MaxLength(128)] public string ContentHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets content type.
    /// </summary>
    [MaxLength(120)] public string ContentType { get; set; } = "text/plain";
    /// <summary>
    /// Gets or sets encoding name.
    /// </summary>
    [MaxLength(80)] public string EncodingName { get; set; } = "utf-8";
    /// <summary>
    /// Gets or sets extracted text.
    /// </summary>
    [Column(TypeName = "TEXT")] public string ExtractedText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    [MaxLength(80)] public string Status { get; set; } = "Imported";
    /// <summary>
    /// Gets or sets safety notes.
    /// </summary>
    [MaxLength(2000)] public string SafetyNotes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets is user approved.
    /// </summary>
    public bool IsUserApproved { get; set; }
}

/// <summary>
/// Represents a council model preset.
/// </summary>
public sealed class CouncilModelPreset
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    [MaxLength(1000)] public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets model names JSON.
    /// </summary>
    [Required, Column(TypeName = "TEXT")] public string ModelNamesJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets model routes JSON.
    /// </summary>
    [Required, Column(TypeName = "TEXT")] public string ModelRoutesJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets allow parallel hardware roads.
    /// </summary>
    public bool AllowParallelHardwareRoads { get; set; } = true;
    /// <summary>
    /// Gets or sets max output tokens.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 4096;
    /// <summary>
    /// Gets or sets max context tokens.
    /// </summary>
    public int MaxContextTokens { get; set; } = 32768;
    /// <summary>
    /// Gets or sets max parallel models.
    /// </summary>
    public int MaxParallelModels { get; set; } = 1;
    /// <summary>
    /// Gets or sets ollama num gpu.
    /// </summary>
    public int? OllamaNumGpu { get; set; }
    /// <summary>
    /// Gets or sets include memory.
    /// </summary>
    public bool IncludeMemory { get; set; } = true;
    /// <summary>
    /// Gets or sets generate artifacts.
    /// </summary>
    public bool GenerateArtifacts { get; set; }
    /// <summary>
    /// Gets or sets create project per run.
    /// </summary>
    public bool CreateProjectPerRun { get; set; } = true;
    /// <summary>
    /// Gets or sets is default.
    /// </summary>
    public bool IsDefault { get; set; }
    /// <summary>
    /// Gets or sets is archived.
    /// </summary>
    public bool IsArchived { get; set; }
    /// <summary>
    /// Gets or sets is user approved.
    /// </summary>
    public bool IsUserApproved { get; set; }
}

/// <summary>
/// Represents a sqlite editor field override.
/// </summary>
public sealed class SqliteEditorFieldOverride
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets table name.
    /// </summary>
    [Required, MaxLength(160)] public string TableName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets column name.
    /// </summary>
    [Required, MaxLength(160)] public string ColumnName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets editor kind.
    /// </summary>
    [Required, MaxLength(40)] public string EditorKind { get; set; } = "Automatic";
    /// <summary>
    /// Gets or sets input mask.
    /// </summary>
    [MaxLength(240)] public string InputMask { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets format string.
    /// </summary>
    [MaxLength(160)] public string FormatString { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets null text.
    /// </summary>
    [MaxLength(120)] public string NullText { get; set; } = "[null]";
    /// <summary>
    /// Gets or sets is sensitive.
    /// </summary>
    public bool IsSensitive { get; set; }
    /// <summary>
    /// Gets or sets require human approval.
    /// </summary>
    public bool RequireHumanApproval { get; set; }
}

/// <summary>
/// Represents a council knowledge user rating.
/// </summary>
public sealed class CouncilKnowledgeUserRating
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets knowledge entry identifier.
    /// </summary>
    public Guid KnowledgeEntryId { get; set; }
    /// <summary>
    /// Gets or sets knowledge entry.
    /// </summary>
    public CouncilKnowledgeEntry? KnowledgeEntry { get; set; }
    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets rating.
    /// </summary>
    public int Rating { get; set; }
    /// <summary>
    /// Gets or sets accuracy status.
    /// </summary>
    [MaxLength(80)] public string AccuracyStatus { get; set; } = "Unrated";
    /// <summary>
    /// Gets or sets notes.
    /// </summary>
    [MaxLength(4000)] public string Notes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets approved for council use.
    /// </summary>
    public bool ApprovedForCouncilUse { get; set; }
    /// <summary>
    /// Gets or sets rated by.
    /// </summary>
    [MaxLength(120)] public string RatedBy { get; set; } = "Human User";
}

/// <summary>
/// Represents a save project revision request.
/// </summary>
public sealed class SaveProjectRevisionRequest
{
    /// <summary>
    /// Gets or sets parent revision identifier.
    /// </summary>
    public Guid? ParentRevisionId { get; set; }
    /// <summary>
    /// Gets or sets branch name.
    /// </summary>
    public string BranchName { get; set; } = "main";
    /// <summary>
    /// Gets or sets revision name.
    /// </summary>
    public string RevisionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets summary.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets project structure JSON.
    /// </summary>
    public string ProjectStructureJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets is current.
    /// </summary>
    public bool IsCurrent { get; set; } = true;
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a save project requirement request.
/// </summary>
public sealed class SaveProjectRequirementRequest
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid? Id { get; set; }
    /// <summary>
    /// Gets or sets revision identifier.
    /// </summary>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets requirement type.
    /// </summary>
    public string RequirementType { get; set; } = "Functional";
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = "Planned";
    /// <summary>
    /// Gets or sets priority.
    /// </summary>
    public string Priority { get; set; } = "Normal";
    /// <summary>
    /// Gets or sets required capability.
    /// </summary>
    public string RequiredCapability { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets council rating.
    /// </summary>
    public int CouncilRating { get; set; }
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}


/// <summary>
/// Represents a save project requirement link request.
/// </summary>
public sealed class SaveProjectRequirementLinkRequest
{
    /// <summary>
    /// Gets or sets requirement identifier.
    /// </summary>
    public Guid RequirementId { get; set; }
    /// <summary>
    /// Gets or sets target kind.
    /// </summary>
    public string TargetKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets target name.
    /// </summary>
    public string TargetName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets target identifier.
    /// </summary>
    public string TargetId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets target table.
    /// </summary>
    public string TargetTable { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets link purpose.
    /// </summary>
    public string LinkPurpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a save project artifact request.
/// </summary>
public sealed class SaveProjectArtifactRequest
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid? Id { get; set; }
    /// <summary>
    /// Gets or sets revision identifier.
    /// </summary>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets requirement identifier.
    /// </summary>
    public Guid? RequirementId { get; set; }
    /// <summary>
    /// Gets or sets artifact kind.
    /// </summary>
    public string ArtifactKind { get; set; } = "Configuration";
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets value.
    /// </summary>
    public string Value { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets data type.
    /// </summary>
    public string DataType { get; set; } = "string";
    /// <summary>
    /// Gets or sets flags.
    /// </summary>
    public string Flags { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets is sensitive.
    /// </summary>
    public bool IsSensitive { get; set; }
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

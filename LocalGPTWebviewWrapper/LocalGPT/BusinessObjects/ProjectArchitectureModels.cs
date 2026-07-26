using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

public sealed class LocalGptProjectRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public LocalGptProject? Project { get; set; }
    public Guid? ParentRevisionId { get; set; }
    public LocalGptProjectRevision? ParentRevision { get; set; }
    public ICollection<LocalGptProjectRevision> ChildRevisions { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(160)] public string BranchName { get; set; } = "main";
    [Required, MaxLength(160)] public string RevisionName { get; set; } = string.Empty;
    [Column(TypeName = "TEXT")] public string Summary { get; set; } = string.Empty;
    [Column(TypeName = "TEXT")] public string ProjectStructureJson { get; set; } = "{}";
    [MaxLength(120)] public string CreatedBy { get; set; } = "Human User";
    public bool IsCurrent { get; set; }
    public bool IsUserApproved { get; set; }
}

public sealed class LocalGptProjectRequirement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public LocalGptProject? Project { get; set; }
    public Guid? RevisionId { get; set; }
    public LocalGptProjectRevision? Revision { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(240)] public string Name { get; set; } = string.Empty;
    [Column(TypeName = "TEXT")] public string Description { get; set; } = string.Empty;
    [MaxLength(80)] public string RequirementType { get; set; } = "Functional";
    [MaxLength(80)] public string Status { get; set; } = "Planned";
    [MaxLength(40)] public string Priority { get; set; } = "Normal";
    [MaxLength(240)] public string RequiredCapability { get; set; } = string.Empty;
    [MaxLength(160)] public string SourceKind { get; set; } = "Human";
    public int CouncilRating { get; set; }
    public bool IsUserApproved { get; set; }
    public ICollection<LocalGptProjectRequirementLink> Links { get; set; } = [];
}

public sealed class LocalGptProjectRequirementLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequirementId { get; set; }
    public LocalGptProjectRequirement? Requirement { get; set; }
    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(80)] public string TargetKind { get; set; } = string.Empty;
    [Required, MaxLength(240)] public string TargetName { get; set; } = string.Empty;
    [MaxLength(160)] public string TargetId { get; set; } = string.Empty;
    [MaxLength(160)] public string TargetTable { get; set; } = string.Empty;
    [MaxLength(1000)] public string LinkPurpose { get; set; } = string.Empty;
    [MaxLength(80)] public string CouncilReviewStatus { get; set; } = "NotReviewed";
    public bool IsUserApproved { get; set; }
}

public sealed class LocalGptProjectArtifact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public LocalGptProject? Project { get; set; }
    public Guid? RevisionId { get; set; }
    public LocalGptProjectRevision? Revision { get; set; }
    public Guid? RequirementId { get; set; }
    public LocalGptProjectRequirement? Requirement { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(80)] public string ArtifactKind { get; set; } = "Configuration";
    [Required, MaxLength(240)] public string Name { get; set; } = string.Empty;
    [Column(TypeName = "TEXT")] public string Value { get; set; } = string.Empty;
    [MaxLength(120)] public string DataType { get; set; } = "string";
    [MaxLength(160)] public string Flags { get; set; } = string.Empty;
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    [MaxLength(80)] public string CouncilReviewStatus { get; set; } = "NotReviewed";
    public bool IsSensitive { get; set; }
    public bool IsUserApproved { get; set; }
}

public sealed class ProjectDocumentImport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public LocalGptProject? Project { get; set; }
    public Guid? RevisionId { get; set; }
    public LocalGptProjectRevision? Revision { get; set; }
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(260)] public string SourceName { get; set; } = string.Empty;
    [MaxLength(2048)] public string SourceUri { get; set; } = string.Empty;
    [Required, MaxLength(128)] public string ContentHash { get; set; } = string.Empty;
    [MaxLength(120)] public string ContentType { get; set; } = "text/plain";
    [MaxLength(80)] public string EncodingName { get; set; } = "utf-8";
    [Column(TypeName = "TEXT")] public string ExtractedText { get; set; } = string.Empty;
    [MaxLength(80)] public string Status { get; set; } = "Imported";
    [MaxLength(2000)] public string SafetyNotes { get; set; } = string.Empty;
    public bool IsUserApproved { get; set; }
}

public sealed class CouncilModelPreset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(1000)] public string Description { get; set; } = string.Empty;
    [Required, Column(TypeName = "TEXT")] public string ModelNamesJson { get; set; } = "[]";
    [Required, Column(TypeName = "TEXT")] public string ModelRoutesJson { get; set; } = "[]";
    public bool AllowParallelHardwareRoads { get; set; } = true;
    public int MaxOutputTokens { get; set; } = 4096;
    public int MaxContextTokens { get; set; } = 32768;
    public int MaxParallelModels { get; set; } = 1;
    public int? OllamaNumGpu { get; set; }
    public bool IncludeMemory { get; set; } = true;
    public bool GenerateArtifacts { get; set; }
    public bool CreateProjectPerRun { get; set; } = true;
    public bool IsDefault { get; set; }
    public bool IsArchived { get; set; }
    public bool IsUserApproved { get; set; }
}

public sealed class SqliteEditorFieldOverride
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(160)] public string TableName { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string ColumnName { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string EditorKind { get; set; } = "Automatic";
    [MaxLength(240)] public string InputMask { get; set; } = string.Empty;
    [MaxLength(160)] public string FormatString { get; set; } = string.Empty;
    [MaxLength(120)] public string NullText { get; set; } = "[null]";
    public bool IsSensitive { get; set; }
    public bool RequireHumanApproval { get; set; }
}

public sealed class CouncilKnowledgeUserRating
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid KnowledgeEntryId { get; set; }
    public CouncilKnowledgeEntry? KnowledgeEntry { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public int Rating { get; set; }
    [MaxLength(80)] public string AccuracyStatus { get; set; } = "Unrated";
    [MaxLength(4000)] public string Notes { get; set; } = string.Empty;
    public bool ApprovedForCouncilUse { get; set; }
    [MaxLength(120)] public string RatedBy { get; set; } = "Human User";
}

public sealed class SaveProjectRevisionRequest
{
    public Guid? ParentRevisionId { get; set; }
    public string BranchName { get; set; } = "main";
    public string RevisionName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ProjectStructureJson { get; set; } = "{}";
    public bool IsCurrent { get; set; } = true;
    public bool UserConfirmed { get; set; }
}

public sealed class SaveProjectRequirementRequest
{
    public Guid? Id { get; set; }
    public Guid? RevisionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequirementType { get; set; } = "Functional";
    public string Status { get; set; } = "Planned";
    public string Priority { get; set; } = "Normal";
    public string RequiredCapability { get; set; } = string.Empty;
    public int CouncilRating { get; set; }
    public bool UserConfirmed { get; set; }
}


public sealed class SaveProjectRequirementLinkRequest
{
    public Guid RequirementId { get; set; }
    public string TargetKind { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string TargetTable { get; set; } = string.Empty;
    public string LinkPurpose { get; set; } = string.Empty;
    public bool UserConfirmed { get; set; }
}

public sealed class SaveProjectArtifactRequest
{
    public Guid? Id { get; set; }
    public Guid? RevisionId { get; set; }
    public Guid? RequirementId { get; set; }
    public string ArtifactKind { get; set; } = "Configuration";
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public string Flags { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSensitive { get; set; }
    public bool UserConfirmed { get; set; }
}

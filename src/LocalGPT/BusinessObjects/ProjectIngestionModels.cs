namespace LocalGPT.BusinessObjects;

/// <summary>Describes one quarantined file or archive discovered before project promotion.</summary>
public sealed class ProjectIngestionFileEvidence
{
    public string RelativePath { get; set; } = string.Empty;
    public long Length { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public bool IsArchive { get; set; }
    public int ArchiveEntryCount { get; set; }
    public long ArchiveExpandedBytes { get; set; }
    public List<string> TypeHints { get; set; } = [];
    public List<string> ApprovedRegexMatches { get; set; } = [];
}

/// <summary>One independent Council/model review recorded against a quarantined workspace.</summary>
public sealed class ProjectIngestionReviewEvidence
{
    public string ReviewerIdentity { get; set; } = string.Empty;
    public string ReviewerKind { get; set; } = string.Empty;
    public bool Approved { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset ReviewedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Durable quarantine gate state written alongside an uploaded workspace.</summary>
public sealed class ProjectIngestionGateRecord
{
    public int SchemaVersion { get; set; } = 1;
    public string WorkspaceName { get; set; } = string.Empty;
    public string Status { get; set; } = "Quarantined";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<ProjectIngestionFileEvidence> Files { get; set; } = [];
    public List<string> ProjectKinds { get; set; } = [];
    public List<string> Toolchains { get; set; } = [];
    public List<string> Domains { get; set; } = [];
    public List<string> MatchedEvidenceRules { get; set; } = [];
    public List<string> ApprovedKnowledgeHints { get; set; } = [];
    public List<string> RejectionReasons { get; set; } = [];
    public List<ProjectIngestionReviewEvidence> Reviews { get; set; } = [];
    public int RequiredIndependentApprovals { get; set; } = 2;
    public bool DeterministicChecksPassed { get; set; }
    public bool UserApprovedPromotion { get; set; }
    public string PromotedRoot { get; set; } = string.Empty;
    public DateTimeOffset? PromotedAtUtc { get; set; }
}

/// <summary>Input used to submit one independent ingestion review.</summary>
public sealed class ProjectIngestionReviewRequest
{
    public string WorkspaceName { get; set; } = string.Empty;
    public bool Approved { get; set; }
    public string Notes { get; set; } = string.Empty;
}


/// <summary>Data-driven classification result produced from curator-approved project-evidence regex rules.</summary>
public sealed class ProjectEvidenceClassification
{
    public List<string> Domains { get; set; } = [];
    public List<string> ProjectKinds { get; set; } = [];
    public List<string> Toolchains { get; set; } = [];
    public List<string> MatchedRuleNames { get; set; } = [];
}

/// <summary>One file declared for bounded hash-verified blob reconstruction.</summary>
public sealed class ProjectBlobFileManifest
{
    public string RelativePath { get; set; } = string.Empty;
    public long Length { get; set; }
    public int ChunkCount { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
}

/// <summary>Canonical manifest for a multi-file reconstruction session.</summary>
public sealed class ProjectBlobManifest
{
    public string Name { get; set; } = "reconstructed-project";
    public List<ProjectBlobFileManifest> Files { get; set; } = [];
    public string ManifestSha256 { get; set; } = string.Empty;
}

/// <summary>State returned for one bounded file/blob reconstruction session.</summary>
public sealed class ProjectBlobSessionSnapshot
{
    public Guid SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DeclaredFileCount { get; set; }
    public int ReceivedChunkCount { get; set; }
    public long DeclaredBytes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public bool Finalized { get; set; }
    public string WorkspaceName { get; set; } = string.Empty;
}

/// <summary>One semantic action exposed by the ASCII/game interaction layer.</summary>
public sealed class AsciiSemanticActionDescriptor
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Game";
    public bool RequiresHumanConfirmation { get; set; }
}

/// <summary>Request to invoke an authoritative semantic ASCII/game action.</summary>
public sealed class AsciiSemanticActionRequest
{
    public Guid SessionId { get; set; }
    public string Action { get; set; } = string.Empty;
    public long? ExpectedTurn { get; set; }
}

/// <summary>Persistent user enrollment of one already-trusted 1-Wire peer as a Council work host.</summary>
public sealed class OneWireCouncilHostEnrollment
{
    public string PeerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset EnrolledAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool Enabled { get; set; } = true;
}

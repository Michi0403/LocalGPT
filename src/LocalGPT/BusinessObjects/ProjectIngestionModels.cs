namespace LocalGPT.BusinessObjects;

/// <summary>Describes one quarantined file or archive discovered before project promotion.</summary>
public sealed class ProjectIngestionFileEvidence
{
    /// <summary>
    /// Gets or sets the relative path used by this project ingestion file evidence instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="ProjectIngestionFileEvidence"/>.</value>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the length that quantifies the associated project ingestion file evidence data.
    /// </summary>
    /// <value>The length value exposed by <see cref="ProjectIngestionFileEvidence"/>.</value>
    public long Length { get; set; }
    /// <summary>
    /// Gets or sets the SHA-256 value that forms part of the project ingestion file evidence state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The SHA-256 value exposed by <see cref="ProjectIngestionFileEvidence"/>.</value>
    public string Sha256 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the kind value that forms part of the project ingestion file evidence state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="ProjectIngestionFileEvidence"/>.</value>
    public string Kind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether archive applies to the project ingestion file evidence state.
    /// </summary>
    /// <value>The is archive value exposed by <see cref="ProjectIngestionFileEvidence"/>.</value>
    public bool IsArchive { get; set; }
    /// <summary>
    /// Gets or sets the archive entry count that quantifies the associated project ingestion file evidence data.
    /// </summary>
    /// <value>The archive entry count value exposed by <see cref="ProjectIngestionFileEvidence"/>.</value>
    public int ArchiveEntryCount { get; set; }
    /// <summary>
    /// Gets or sets the archive expanded bytes value that forms part of the project ingestion file evidence state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The archive expanded bytes value exposed by <see cref="ProjectIngestionFileEvidence"/>.</value>
    public long ArchiveExpandedBytes { get; set; }
    /// <summary>
    /// Gets or sets the type hints collection maintained or exposed by this project ingestion file evidence instance for downstream processing.
    /// </summary>
    /// <value>The type hints value exposed by <see cref="ProjectIngestionFileEvidence"/>.</value>
    public List<string> TypeHints { get; set; } = [];
    /// <summary>
    /// Gets or sets the approved regex matches collection maintained or exposed by this project ingestion file evidence instance for downstream processing.
    /// </summary>
    /// <value>The approved regex matches value exposed by <see cref="ProjectIngestionFileEvidence"/>.</value>
    public List<string> ApprovedRegexMatches { get; set; } = [];
}

/// <summary>One independent Council/model review recorded against a quarantined workspace.</summary>
public sealed class ProjectIngestionReviewEvidence
{
    /// <summary>
    /// Gets or sets the reviewer identity value that forms part of the project ingestion review evidence state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reviewer identity value exposed by <see cref="ProjectIngestionReviewEvidence"/>.</value>
    public string ReviewerIdentity { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the reviewer kind value that forms part of the project ingestion review evidence state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reviewer kind value exposed by <see cref="ProjectIngestionReviewEvidence"/>.</value>
    public string ReviewerKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether approved applies to the project ingestion review evidence state.
    /// </summary>
    /// <value>The approved value exposed by <see cref="ProjectIngestionReviewEvidence"/>.</value>
    public bool Approved { get; set; }
    /// <summary>
    /// Gets or sets the notes value that forms part of the project ingestion review evidence state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The notes value exposed by <see cref="ProjectIngestionReviewEvidence"/>.</value>
    public string Notes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the reviewed at UTC associated with this project ingestion review evidence state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The reviewed at UTC value exposed by <see cref="ProjectIngestionReviewEvidence"/>.</value>
    public DateTimeOffset ReviewedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Durable quarantine gate state written alongside an uploaded workspace.</summary>
public sealed class ProjectIngestionGateRecord
{
    /// <summary>
    /// Gets or sets the schema version value that forms part of the project ingestion gate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The schema version value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public int SchemaVersion { get; set; } = 1;
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the project ingestion gate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the project ingestion gate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public string Status { get; set; } = "Quarantined";
    /// <summary>
    /// Gets or sets the created at UTC associated with this project ingestion gate state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this project ingestion gate state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the files collection maintained or exposed by this project ingestion gate instance for downstream processing.
    /// </summary>
    /// <value>The files value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public List<ProjectIngestionFileEvidence> Files { get; set; } = [];
    /// <summary>
    /// Gets or sets the project kinds collection maintained or exposed by this project ingestion gate instance for downstream processing.
    /// </summary>
    /// <value>The project kinds value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public List<string> ProjectKinds { get; set; } = [];
    /// <summary>
    /// Gets or sets the toolchains collection maintained or exposed by this project ingestion gate instance for downstream processing.
    /// </summary>
    /// <value>The toolchains value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public List<string> Toolchains { get; set; } = [];
    /// <summary>
    /// Gets or sets the domains collection maintained or exposed by this project ingestion gate instance for downstream processing.
    /// </summary>
    /// <value>The domains value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public List<string> Domains { get; set; } = [];
    /// <summary>
    /// Gets or sets the matched evidence rules collection maintained or exposed by this project ingestion gate instance for downstream processing.
    /// </summary>
    /// <value>The matched evidence rules value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public List<string> MatchedEvidenceRules { get; set; } = [];
    /// <summary>
    /// Gets or sets the approved knowledge hints collection maintained or exposed by this project ingestion gate instance for downstream processing.
    /// </summary>
    /// <value>The approved knowledge hints value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public List<string> ApprovedKnowledgeHints { get; set; } = [];
    /// <summary>
    /// Gets or sets the rejection reasons collection maintained or exposed by this project ingestion gate instance for downstream processing.
    /// </summary>
    /// <value>The rejection reasons value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public List<string> RejectionReasons { get; set; } = [];
    /// <summary>
    /// Gets or sets the repositories collection maintained or exposed by this project ingestion gate instance for downstream processing.
    /// </summary>
    /// <value>The repositories value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public List<RepositoryEvidenceIdentity> Repositories { get; set; } = [];
    /// <summary>
    /// Gets or sets the reviews collection maintained or exposed by this project ingestion gate instance for downstream processing.
    /// </summary>
    /// <value>The reviews value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public List<ProjectIngestionReviewEvidence> Reviews { get; set; } = [];
    /// <summary>
    /// Gets or sets the required independent approvals value that forms part of the project ingestion gate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The required independent approvals value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public int RequiredIndependentApprovals { get; set; } = 2;
    /// <summary>
    /// Gets or sets a value indicating whether deterministic checks passed applies to the project ingestion gate state.
    /// </summary>
    /// <value>The deterministic checks passed value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public bool DeterministicChecksPassed { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user approved promotion applies to the project ingestion gate state.
    /// </summary>
    /// <value>The user approved promotion value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public bool UserApprovedPromotion { get; set; }
    /// <summary>
    /// Gets or sets the promoted root value that forms part of the project ingestion gate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The promoted root value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public string PromotedRoot { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the promoted at UTC associated with this project ingestion gate state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The promoted at UTC value exposed by <see cref="ProjectIngestionGateRecord"/>.</value>
    public DateTimeOffset? PromotedAtUtc { get; set; }
}

/// <summary>Input used to submit one independent ingestion review.</summary>
public sealed class ProjectIngestionReviewRequest
{
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the project ingestion review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="ProjectIngestionReviewRequest"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether approved applies to the project ingestion review state.
    /// </summary>
    /// <value>The approved value exposed by <see cref="ProjectIngestionReviewRequest"/>.</value>
    public bool Approved { get; set; }
    /// <summary>
    /// Gets or sets the notes value that forms part of the project ingestion review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The notes value exposed by <see cref="ProjectIngestionReviewRequest"/>.</value>
    public string Notes { get; set; } = string.Empty;
}


/// <summary>Describes one repository identity inferred from bounded technical markers without executing uploaded content.</summary>
public sealed class RepositoryEvidenceIdentity
{
    /// <summary>
    /// Gets or sets the kind value that forms part of the repository evidence identity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="RepositoryEvidenceIdentity"/>.</value>
    public string Kind { get; set; } = "Repository";
    /// <summary>
    /// Gets or sets the name value that forms part of the repository evidence identity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="RepositoryEvidenceIdentity"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the version value that forms part of the repository evidence identity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The version value exposed by <see cref="RepositoryEvidenceIdentity"/>.</value>
    public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the root hint value that forms part of the repository evidence identity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The root hint value exposed by <see cref="RepositoryEvidenceIdentity"/>.</value>
    public string RootHint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether git metadata detected applies to the repository evidence identity state.
    /// </summary>
    /// <value>The git metadata detected value exposed by <see cref="RepositoryEvidenceIdentity"/>.</value>
    public bool GitMetadataDetected { get; set; }
    /// <summary>
    /// Gets or sets the markers collection maintained or exposed by this repository evidence identity instance for downstream processing.
    /// </summary>
    /// <value>The markers value exposed by <see cref="RepositoryEvidenceIdentity"/>.</value>
    public List<string> Markers { get; set; } = [];
}

/// <summary>Data-driven classification result produced from curator-approved rules plus bounded repository identity markers.</summary>
public sealed class ProjectEvidenceClassification
{
    /// <summary>
    /// Gets or sets the domains collection maintained or exposed by this project evidence classification instance for downstream processing.
    /// </summary>
    /// <value>The domains value exposed by <see cref="ProjectEvidenceClassification"/>.</value>
    public List<string> Domains { get; set; } = [];
    /// <summary>
    /// Gets or sets the project kinds collection maintained or exposed by this project evidence classification instance for downstream processing.
    /// </summary>
    /// <value>The project kinds value exposed by <see cref="ProjectEvidenceClassification"/>.</value>
    public List<string> ProjectKinds { get; set; } = [];
    /// <summary>
    /// Gets or sets the toolchains collection maintained or exposed by this project evidence classification instance for downstream processing.
    /// </summary>
    /// <value>The toolchains value exposed by <see cref="ProjectEvidenceClassification"/>.</value>
    public List<string> Toolchains { get; set; } = [];
    /// <summary>
    /// Gets or sets the matched rule names collection maintained or exposed by this project evidence classification instance for downstream processing.
    /// </summary>
    /// <value>The matched rule names value exposed by <see cref="ProjectEvidenceClassification"/>.</value>
    public List<string> MatchedRuleNames { get; set; } = [];
    /// <summary>
    /// Gets or sets the repositories collection maintained or exposed by this project evidence classification instance for downstream processing.
    /// </summary>
    /// <value>The repositories value exposed by <see cref="ProjectEvidenceClassification"/>.</value>
    public List<RepositoryEvidenceIdentity> Repositories { get; set; } = [];
}

/// <summary>One file declared for bounded hash-verified blob reconstruction.</summary>
public sealed class ProjectBlobFileManifest
{
    /// <summary>
    /// Gets or sets the relative path used by this project blob file manifest instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="ProjectBlobFileManifest"/>.</value>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the length that quantifies the associated project blob file manifest data.
    /// </summary>
    /// <value>The length value exposed by <see cref="ProjectBlobFileManifest"/>.</value>
    public long Length { get; set; }
    /// <summary>
    /// Gets or sets the chunk count that quantifies the associated project blob file manifest data.
    /// </summary>
    /// <value>The chunk count value exposed by <see cref="ProjectBlobFileManifest"/>.</value>
    public int ChunkCount { get; set; }
    /// <summary>
    /// Gets or sets the SHA-256 value that forms part of the project blob file manifest state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The SHA-256 value exposed by <see cref="ProjectBlobFileManifest"/>.</value>
    public string Sha256 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the content type value that forms part of the project blob file manifest state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content type value exposed by <see cref="ProjectBlobFileManifest"/>.</value>
    public string ContentType { get; set; } = "application/octet-stream";
}

/// <summary>Canonical manifest for a multi-file reconstruction session.</summary>
public sealed class ProjectBlobManifest
{
    /// <summary>
    /// Gets or sets the name value that forms part of the project blob manifest state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="ProjectBlobManifest"/>.</value>
    public string Name { get; set; } = "reconstructed-project";
    /// <summary>
    /// Gets or sets the files collection maintained or exposed by this project blob manifest instance for downstream processing.
    /// </summary>
    /// <value>The files value exposed by <see cref="ProjectBlobManifest"/>.</value>
    public List<ProjectBlobFileManifest> Files { get; set; } = [];
    /// <summary>
    /// Gets or sets the manifest SHA-256 value that forms part of the project blob manifest state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The manifest SHA-256 value exposed by <see cref="ProjectBlobManifest"/>.</value>
    public string ManifestSha256 { get; set; } = string.Empty;
}

/// <summary>State returned for one bounded file/blob reconstruction session.</summary>
public sealed class ProjectBlobSessionSnapshot
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this project blob session snapshot instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="ProjectBlobSessionSnapshot"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the name value that forms part of the project blob session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="ProjectBlobSessionSnapshot"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the declared file count that quantifies the associated project blob session snapshot data.
    /// </summary>
    /// <value>The declared file count value exposed by <see cref="ProjectBlobSessionSnapshot"/>.</value>
    public int DeclaredFileCount { get; set; }
    /// <summary>
    /// Gets or sets the received chunk count that quantifies the associated project blob session snapshot data.
    /// </summary>
    /// <value>The received chunk count value exposed by <see cref="ProjectBlobSessionSnapshot"/>.</value>
    public int ReceivedChunkCount { get; set; }
    /// <summary>
    /// Gets or sets the declared bytes value that forms part of the project blob session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The declared bytes value exposed by <see cref="ProjectBlobSessionSnapshot"/>.</value>
    public long DeclaredBytes { get; set; }
    /// <summary>
    /// Gets or sets the created at UTC associated with this project blob session snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="ProjectBlobSessionSnapshot"/>.</value>
    public DateTimeOffset CreatedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether finalized applies to the project blob session snapshot state.
    /// </summary>
    /// <value>The finalized value exposed by <see cref="ProjectBlobSessionSnapshot"/>.</value>
    public bool Finalized { get; set; }
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the project blob session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="ProjectBlobSessionSnapshot"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;
}

/// <summary>One semantic action exposed by the ASCII/game interaction layer.</summary>
public sealed class AsciiSemanticActionDescriptor
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this ascii semantic action instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="AsciiSemanticActionDescriptor"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the ascii semantic action state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="AsciiSemanticActionDescriptor"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the ascii semantic action state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="AsciiSemanticActionDescriptor"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the category value that forms part of the ascii semantic action state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The category value exposed by <see cref="AsciiSemanticActionDescriptor"/>.</value>
    public string Category { get; set; } = "Game";
    /// <summary>
    /// Gets or sets a value indicating whether requires human confirmation applies to the ascii semantic action state.
    /// </summary>
    /// <value>The requires human confirmation value exposed by <see cref="AsciiSemanticActionDescriptor"/>.</value>
    public bool RequiresHumanConfirmation { get; set; }
}

/// <summary>Request to invoke an authoritative semantic ASCII/game action.</summary>
public sealed class AsciiSemanticActionRequest
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this ascii semantic action instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="AsciiSemanticActionRequest"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the action value that forms part of the ascii semantic action state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The action value exposed by <see cref="AsciiSemanticActionRequest"/>.</value>
    public string Action { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the expected turn value that forms part of the ascii semantic action state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The expected turn value exposed by <see cref="AsciiSemanticActionRequest"/>.</value>
    public long? ExpectedTurn { get; set; }
}

/// <summary>Persistent user enrollment of one already-trusted 1-Wire peer as a Council work host.</summary>
public sealed class OneWireCouncilHostEnrollment
{
    /// <summary>
    /// Gets or sets the stable peer identifier used to identify or correlate this one wire council host enrollment instance with related application state.
    /// </summary>
    /// <value>The peer identifier value exposed by <see cref="OneWireCouncilHostEnrollment"/>.</value>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the one wire council host enrollment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="OneWireCouncilHostEnrollment"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the enrolled at UTC associated with this one wire council host enrollment state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The enrolled at UTC value exposed by <see cref="OneWireCouncilHostEnrollment"/>.</value>
    public DateTimeOffset EnrolledAtUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the one wire council host enrollment state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="OneWireCouncilHostEnrollment"/>.</value>
    public bool Enabled { get; set; } = true;
}

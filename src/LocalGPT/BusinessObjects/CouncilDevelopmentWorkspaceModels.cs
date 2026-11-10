namespace LocalGPT.BusinessObjects;

/// <summary>One source declaration reviewed for either CodeDOM preview or approved workspace materialization.</summary>
public sealed class CouncilWorkspaceCodeDomRequest
{
    /// <summary>Existing isolated Council workspace.</summary>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>Declaration accepted by the existing shared CodeDOM source service.</summary>
    public CodeDomTypeSpec? CodeDomType { get; set; }
    /// <summary>Emit a standalone sample entry point only when the caller explicitly requests one.</summary>
    public bool IncludeSandboxEntryPoint { get; set; }
}

/// <summary>Request to execute exactly one existing workspace source file inside an isolated guest.</summary>
public sealed class CouncilWorkspaceSandboxRunRequest
{
    /// <summary>Existing isolated Council workspace.</summary>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>Relative source path; never a host-absolute path.</summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>Installed guest language key (validated against the configured provider).</summary>
    public string LanguageKey { get; set; } = string.Empty;
    /// <summary>SHA-256 captured from the inspected source before approving its execution.</summary>
    public string ExpectedSourceSha256 { get; set; } = string.Empty;
    /// <summary>Optional stable execution session.</summary>
    public Guid? SessionId { get; set; }
    /// <summary>Optional linked conversation.</summary>
    public Guid? ConversationId { get; set; }
    /// <summary>Optional linked project.</summary>
    public Guid? ProjectId { get; set; }
}

/// <summary>Selects exactly one artifact evidence path for Council completion verification.</summary>
public sealed class CouncilDevelopmentArtifactVerificationRequest
{
    /// <summary>Existing approved code-generation review, when using the reviewed generation path.</summary>
    public Guid? ReviewId { get; set; }
    /// <summary>Staged repository workspace, when using approved source editing.</summary>
    public string WorkspaceName { get; set; } = string.Empty;
}

/// <summary>Persisted fingerprint of one source file at the moment of approved repository staging.</summary>
public sealed class CouncilStagedSourceFingerprint
{
    /// <summary>Normalized relative path.</summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>SHA-256 of the original staged file bytes.</summary>
    public string Sha256 { get; set; } = string.Empty;
}

/// <summary>Repository stage baseline stored outside the editable workspace and never packaged as source.</summary>
public sealed class CouncilRepositoryStageBaseline
{
    /// <summary>Isolated workspace identifier.</summary>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>Original byte fingerprints, without source code or personal data.</summary>
    public List<CouncilStagedSourceFingerprint> Files { get; set; } = [];
}

/// <summary>Source and ZIP verification result, independent of AI prose.</summary>
public sealed class CouncilRepositoryArtifactEvidence
{
    /// <summary>Whether changed source and a byte-matching complete archive were found.</summary>
    public bool Completed { get; set; }
    /// <summary>Plain-language diagnostic; not a substitute for compiler evidence.</summary>
    public string Diagnostic { get; set; } = string.Empty;
    /// <summary>Paths changed since the immutable approved staging baseline.</summary>
    public List<string> ChangedPaths { get; set; } = [];
    /// <summary>Application-relative URL of the validated archive when complete.</summary>
    public string DownloadUrl { get; set; } = string.Empty;
}

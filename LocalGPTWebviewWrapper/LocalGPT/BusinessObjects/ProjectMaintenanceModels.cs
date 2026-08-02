using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

public sealed class ProjectWorkspaceRoot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(2048)] public string RootPath { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string ScopeKind { get; set; } = "Global";
    public Guid? ProjectId { get; set; }
    public LocalGptProject? Project { get; set; }
    [MaxLength(240)] public string ProjectTypePattern { get; set; } = string.Empty;
    [MaxLength(1000)] public string SolutionPattern { get; set; } = @"(?i)\.(sln|slnx)$";
    [MaxLength(80)] public string EnvironmentKind { get; set; } = "LocalHost";
    [MaxLength(2048)] public string EnvironmentRootPath { get; set; } = string.Empty;
    public Guid? PreferredCompilerInstallationId { get; set; }
    [Column(TypeName = "TEXT")] public string BuildArguments { get; set; } = string.Empty;
    [Column(TypeName = "TEXT")] public string EnvironmentVariablesJson { get; set; } = "{}";
    [Column(TypeName = "TEXT")] public string DefaultSubdirectoriesJson { get; set; } = "[\"src\",\"docs\",\"tests\",\"artifacts\"]";
    [Column(TypeName = "TEXT")] public string AccessPolicyJson { get; set; } = "[]";
    [Column(TypeName = "TEXT")] public string ExpectedStructureRegex { get; set; } = string.Empty;
    [MaxLength(80)] public string LastPermissionStatus { get; set; } = "NotChecked";
    [MaxLength(4000)] public string LastPermissionSummary { get; set; } = string.Empty;
    public bool LastPermissionReadAccess { get; set; }
    public bool LastPermissionWriteAccess { get; set; }
    public DateTime? LastPermissionCheckedAtUtc { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastResolvedAtUtc { get; set; }
    [MaxLength(80)] public string LastResolutionStatus { get; set; } = "NotResolved";
}

public sealed class ProjectCompilerInstallation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string Language { get; set; } = string.Empty;
    [Required, MaxLength(2048)] public string ExecutablePath { get; set; } = string.Empty;
    [MaxLength(2048)] public string CompilerHomePath { get; set; } = string.Empty;
    [MaxLength(160)] public string Version { get; set; } = string.Empty;
    [MaxLength(80)] public string Architecture { get; set; } = string.Empty;
    [MaxLength(80)] public string DiscoverySource { get; set; } = "Custom";
    [MaxLength(500)] public string ValidationArguments { get; set; } = "--version";
    [Column(TypeName = "TEXT")] public string EnvironmentVariablesJson { get; set; } = "{}";
    public bool IsEnabled { get; set; } = true;
    public bool IsDefaultForLanguage { get; set; }
    public DateTime? LastValidatedAtUtc { get; set; }
    public bool LastValidationSucceeded { get; set; }
    [MaxLength(4000)] public string LastValidationMessage { get; set; } = string.Empty;
}

public sealed class LocalGptProjectTrackedFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public LocalGptProject? Project { get; set; }
    public Guid? RevisionId { get; set; }
    public LocalGptProjectRevision? Revision { get; set; }
    [Required, MaxLength(128)] public string StableFileKey { get; set; } = string.Empty;
    [Required, MaxLength(2048)] public string AbsolutePath { get; set; } = string.Empty;
    [Required, MaxLength(2048)] public string ProjectRelativePath { get; set; } = string.Empty;
    [MaxLength(2048)] public string WorkspaceRelativePath { get; set; } = string.Empty;
    [MaxLength(2048)] public string SolutionPath { get; set; } = string.Empty;
    [MaxLength(2048)] public string ProjectFilePath { get; set; } = string.Empty;
    [Required, MaxLength(260)] public string FileName { get; set; } = string.Empty;
    [MaxLength(40)] public string Extension { get; set; } = string.Empty;
    [MaxLength(120)] public string ContentType { get; set; } = "text/plain";
    [MaxLength(80)] public string EncodingName { get; set; } = "utf-8";
    [MaxLength(120)] public string FileRole { get; set; } = "Source";
    [Column(TypeName = "TEXT")] public string StructureRegex { get; set; } = string.Empty;
    [Column(TypeName = "TEXT")] public string ContentFormatRegex { get; set; } = string.Empty;
    [Required, MaxLength(128)] public string ContentHash { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime? LastWriteTimeUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
    public bool Exists { get; set; } = true;
    public bool IsGenerated { get; set; }
    public bool IsUserApproved { get; set; }
}

public sealed class ProjectBuildVerification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public LocalGptProject? Project { get; set; }
    public Guid RevisionId { get; set; }
    public LocalGptProjectRevision? Revision { get; set; }
    public Guid? CompilerInstallationId { get; set; }
    public ProjectCompilerInstallation? CompilerInstallation { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    [MaxLength(80)] public string Configuration { get; set; } = "Debug";
    [MaxLength(160)] public string TargetFramework { get; set; } = string.Empty;
    [MaxLength(80)] public string RuntimeIdentifier { get; set; } = string.Empty;
    [Required, MaxLength(2048)] public string ExecutablePath { get; set; } = string.Empty;
    [Column(TypeName = "TEXT")] public string Arguments { get; set; } = string.Empty;
    [Required, MaxLength(2048)] public string WorkingDirectory { get; set; } = string.Empty;
    public int? ExitCode { get; set; }
    public bool BuildSucceeded { get; set; }
    public bool TestsExecuted { get; set; }
    public bool TestsSucceeded { get; set; }
    public bool SourceChangedDuringVerification { get; set; }
    public bool CouncilReviewSucceeded { get; set; }
    public bool UserApprovedReadyForTest { get; set; }
    [MaxLength(2048)] public string OutputLogPath { get; set; } = string.Empty;
    [MaxLength(2048)] public string EvidenceManifestPath { get; set; } = string.Empty;
    [MaxLength(128)] public string OutputHash { get; set; } = string.Empty;
    [MaxLength(128)] public string SourceSnapshotHash { get; set; } = string.Empty;
    [MaxLength(2048)] public string SnapshotArchivePath { get; set; } = string.Empty;
    [Column(TypeName = "TEXT")] public string CouncilReviewSummary { get; set; } = string.Empty;
    [Column(TypeName = "TEXT")] public string Summary { get; set; } = string.Empty;
}

public sealed class SaveProjectWorkspaceRootRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public string ScopeKind { get; set; } = "Global";
    public Guid? ProjectId { get; set; }
    public string ProjectTypePattern { get; set; } = string.Empty;
    public string SolutionPattern { get; set; } = @"(?i)\.(sln|slnx)$";
    public string EnvironmentKind { get; set; } = "LocalHost";
    public string EnvironmentRootPath { get; set; } = string.Empty;
    public Guid? PreferredCompilerInstallationId { get; set; }
    public string BuildArguments { get; set; } = string.Empty;
    public string EnvironmentVariablesJson { get; set; } = "{}";
    public string DefaultSubdirectoriesJson { get; set; } = "[\"src\",\"docs\",\"tests\",\"artifacts\"]";
    public string AccessPolicyJson { get; set; } = "[]";
    public string ExpectedStructureRegex { get; set; } = string.Empty;
    public int Priority { get; set; } = 100;
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool UserConfirmed { get; set; }
}

public sealed class SaveProjectCompilerInstallationRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string CompilerHomePath { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public string DiscoverySource { get; set; } = "Custom";
    public string ValidationArguments { get; set; } = "--version";
    public string EnvironmentVariablesJson { get; set; } = "{}";
    public bool IsEnabled { get; set; } = true;
    public bool IsDefaultForLanguage { get; set; }
    public bool UserConfirmed { get; set; }
}

public sealed class DiscoverProjectCompilersRequest
{
    public List<string> CustomSearchRoots { get; set; } = [];
    public bool SaveDiscovered { get; set; } = true;
    public bool UserConfirmed { get; set; }
}

public sealed class ScanProjectFilesRequest
{
    public Guid? RevisionId { get; set; }
    public int MaximumFiles { get; set; } = 20000;
    public long MaximumTextFileBytes { get; set; } = 4 * 1024 * 1024;
    public long MaximumFileBytes { get; set; } = 512L * 1024 * 1024;
    public bool UserConfirmed { get; set; }
}

public sealed class SaveTrackedFilePatternRequest
{
    public string StructureRegex { get; set; } = string.Empty;
    public string ContentFormatRegex { get; set; } = string.Empty;
    public string FileRole { get; set; } = "Source";
    public bool UserConfirmed { get; set; }
}

public sealed class RegisterRevisionWorkspaceRequest
{
    public string SourceRootPath { get; set; } = string.Empty;
    public string SolutionPath { get; set; } = string.Empty;
    public bool UserConfirmed { get; set; }
}

public sealed class RunProjectBuildVerificationRequest
{
    public Guid RevisionId { get; set; }
    public Guid CompilerInstallationId { get; set; }
    public string Configuration { get; set; } = "Debug";
    public string Arguments { get; set; } = string.Empty;
    public string TestArguments { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 900;
    public bool UserConfirmed { get; set; }
}

public sealed class RecordCouncilBuildReviewRequest
{
    public string Summary { get; set; } = string.Empty;
    public bool CompileErrorsAbsent { get; set; }
    public bool UserConfirmed { get; set; }
}

public sealed class ApproveRevisionReadyForTestRequest
{
    public Guid VerificationId { get; set; }
    public bool RequireTests { get; set; } = true;
    public bool CreateLosslessSnapshot { get; set; } = true;
    public bool UserConfirmed { get; set; }
}

public sealed record ProjectWorkspaceResolution(
    Guid? WorkspaceRootId,
    string RootPath,
    string ScopeKind,
    string MatchReason,
    bool Exists);

public sealed record ProjectScanResult(
    Guid ProjectId,
    Guid? RevisionId,
    string ProjectRootPath,
    string SolutionPath,
    int FilesSeen,
    int FilesStored,
    int FilesSkipped,
    IReadOnlyList<string> Warnings);

public sealed class WorkspaceAccessPolicyRule
{
    public string Name { get; set; } = string.Empty;
    public string RelativePathRegex { get; set; } = @"(?s).*";
    public string ExpectedEntryKind { get; set; } = "Either";
    public string RequiredAccess { get; set; } = "Read";
    public string Severity { get; set; } = "Warning";
    public bool Required { get; set; } = true;
    public bool CouncilMaintained { get; set; } = true;
}

public sealed record WorkspacePermissionFinding(
    string Severity,
    string Code,
    string Message,
    string RelativePath = "");

public sealed record WorkspacePermissionAssessment(
    Guid WorkspaceRootId,
    string Status,
    DateTime CheckedAtUtc,
    bool RootExists,
    bool ReadAccess,
    bool WriteAccess,
    string EnvironmentRootPath,
    Guid? PreferredCompilerInstallationId,
    IReadOnlyList<string> ExpectedSubdirectories,
    IReadOnlyList<WorkspacePermissionFinding> Findings);

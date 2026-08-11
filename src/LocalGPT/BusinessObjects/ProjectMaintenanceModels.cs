using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a project workspace root.
/// </summary>
public sealed class ProjectWorkspaceRoot
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
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets root path.
    /// </summary>
    [Required, MaxLength(2048)] public string RootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets scope kind.
    /// </summary>
    [Required, MaxLength(40)] public string ScopeKind { get; set; } = "Global";
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets project.
    /// </summary>
    public LocalGptProject? Project { get; set; }
    /// <summary>
    /// Gets or sets project type pattern.
    /// </summary>
    [MaxLength(240)] public string ProjectTypePattern { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets solution pattern.
    /// </summary>
    [MaxLength(1000)] public string SolutionPattern { get; set; } = @"(?i)\.(sln|slnx)$";
    /// <summary>
    /// Gets or sets environment kind.
    /// </summary>
    [MaxLength(80)] public string EnvironmentKind { get; set; } = "LocalHost";
    /// <summary>
    /// Gets or sets environment root path.
    /// </summary>
    [MaxLength(2048)] public string EnvironmentRootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets preferred compiler installation identifier.
    /// </summary>
    public Guid? PreferredCompilerInstallationId { get; set; }
    /// <summary>
    /// Gets or sets build arguments.
    /// </summary>
    [Column(TypeName = "TEXT")] public string BuildArguments { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets environment variables JSON.
    /// </summary>
    [Column(TypeName = "TEXT")] public string EnvironmentVariablesJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets default subdirectories JSON.
    /// </summary>
    [Column(TypeName = "TEXT")] public string DefaultSubdirectoriesJson { get; set; } = "[\"src\",\"docs\",\"tests\",\"artifacts\"]";
    /// <summary>
    /// Gets or sets access policy JSON.
    /// </summary>
    [Column(TypeName = "TEXT")] public string AccessPolicyJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets expected structure regex.
    /// </summary>
    [Column(TypeName = "TEXT")] public string ExpectedStructureRegex { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets last permission status.
    /// </summary>
    [MaxLength(80)] public string LastPermissionStatus { get; set; } = "NotChecked";
    /// <summary>
    /// Gets or sets last permission summary.
    /// </summary>
    [MaxLength(4000)] public string LastPermissionSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets last permission read access.
    /// </summary>
    public bool LastPermissionReadAccess { get; set; }
    /// <summary>
    /// Gets or sets last permission write access.
    /// </summary>
    public bool LastPermissionWriteAccess { get; set; }
    /// <summary>
    /// Gets or sets last permission checked at UTC.
    /// </summary>
    public DateTime? LastPermissionCheckedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets priority.
    /// </summary>
    public int Priority { get; set; } = 100;
    /// <summary>
    /// Gets or sets is default.
    /// </summary>
    public bool IsDefault { get; set; }
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets last resolved at UTC.
    /// </summary>
    public DateTime? LastResolvedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets last resolution status.
    /// </summary>
    [MaxLength(80)] public string LastResolutionStatus { get; set; } = "NotResolved";
}

/// <summary>
/// Represents a project compiler installation.
/// </summary>
public sealed class ProjectCompilerInstallation
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
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets language.
    /// </summary>
    [Required, MaxLength(80)] public string Language { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets executable path.
    /// </summary>
    [Required, MaxLength(2048)] public string ExecutablePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets compiler home path.
    /// </summary>
    [MaxLength(2048)] public string CompilerHomePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets version.
    /// </summary>
    [MaxLength(160)] public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets architecture.
    /// </summary>
    [MaxLength(80)] public string Architecture { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets discovery source.
    /// </summary>
    [MaxLength(80)] public string DiscoverySource { get; set; } = "Custom";
    /// <summary>
    /// Gets or sets validation arguments.
    /// </summary>
    [MaxLength(500)] public string ValidationArguments { get; set; } = "--version";
    /// <summary>
    /// Gets or sets environment variables JSON.
    /// </summary>
    [Column(TypeName = "TEXT")] public string EnvironmentVariablesJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets is default for language.
    /// </summary>
    public bool IsDefaultForLanguage { get; set; }
    /// <summary>
    /// Gets or sets last validated at UTC.
    /// </summary>
    public DateTime? LastValidatedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets last validation succeeded.
    /// </summary>
    public bool LastValidationSucceeded { get; set; }
    /// <summary>
    /// Gets or sets last validation message.
    /// </summary>
    [MaxLength(4000)] public string LastValidationMessage { get; set; } = string.Empty;
}

/// <summary>
/// Represents a local gpt project tracked file.
/// </summary>
public sealed class LocalGptProjectTrackedFile
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
    /// Gets or sets stable file key.
    /// </summary>
    [Required, MaxLength(128)] public string StableFileKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets absolute path.
    /// </summary>
    [Required, MaxLength(2048)] public string AbsolutePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets project relative path.
    /// </summary>
    [Required, MaxLength(2048)] public string ProjectRelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets workspace relative path.
    /// </summary>
    [MaxLength(2048)] public string WorkspaceRelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets solution path.
    /// </summary>
    [MaxLength(2048)] public string SolutionPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets project file path.
    /// </summary>
    [MaxLength(2048)] public string ProjectFilePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets file name.
    /// </summary>
    [Required, MaxLength(260)] public string FileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets extension.
    /// </summary>
    [MaxLength(40)] public string Extension { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets content type.
    /// </summary>
    [MaxLength(120)] public string ContentType { get; set; } = "text/plain";
    /// <summary>
    /// Gets or sets encoding name.
    /// </summary>
    [MaxLength(80)] public string EncodingName { get; set; } = "utf-8";
    /// <summary>
    /// Gets or sets file role.
    /// </summary>
    [MaxLength(120)] public string FileRole { get; set; } = "Source";
    /// <summary>
    /// Gets or sets structure regex.
    /// </summary>
    [Column(TypeName = "TEXT")] public string StructureRegex { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets content format regex.
    /// </summary>
    [Column(TypeName = "TEXT")] public string ContentFormatRegex { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets content hash.
    /// </summary>
    [Required, MaxLength(128)] public string ContentHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets size bytes.
    /// </summary>
    public long SizeBytes { get; set; }
    /// <summary>
    /// Gets or sets last write time UTC.
    /// </summary>
    public DateTime? LastWriteTimeUtc { get; set; }
    /// <summary>
    /// Gets or sets last seen at UTC.
    /// </summary>
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets exists.
    /// </summary>
    public bool Exists { get; set; } = true;
    /// <summary>
    /// Gets or sets is generated.
    /// </summary>
    public bool IsGenerated { get; set; }
    /// <summary>
    /// Gets or sets is user approved.
    /// </summary>
    public bool IsUserApproved { get; set; }
}

/// <summary>
/// Represents a project build verification.
/// </summary>
public sealed class ProjectBuildVerification
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
    public Guid RevisionId { get; set; }
    /// <summary>
    /// Gets or sets revision.
    /// </summary>
    public LocalGptProjectRevision? Revision { get; set; }
    /// <summary>
    /// Gets or sets compiler installation identifier.
    /// </summary>
    public Guid? CompilerInstallationId { get; set; }
    /// <summary>
    /// Gets or sets compiler installation.
    /// </summary>
    public ProjectCompilerInstallation? CompilerInstallation { get; set; }
    /// <summary>
    /// Gets or sets started at UTC.
    /// </summary>
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets completed at UTC.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets configuration.
    /// </summary>
    [MaxLength(80)] public string Configuration { get; set; } = "Debug";
    /// <summary>
    /// Gets or sets target framework.
    /// </summary>
    [MaxLength(160)] public string TargetFramework { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets runtime identifier.
    /// </summary>
    [MaxLength(80)] public string RuntimeIdentifier { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets executable path.
    /// </summary>
    [Required, MaxLength(2048)] public string ExecutablePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets arguments.
    /// </summary>
    [Column(TypeName = "TEXT")] public string Arguments { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets working directory.
    /// </summary>
    [Required, MaxLength(2048)] public string WorkingDirectory { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets exit code.
    /// </summary>
    public int? ExitCode { get; set; }
    /// <summary>
    /// Gets or sets build succeeded.
    /// </summary>
    public bool BuildSucceeded { get; set; }
    /// <summary>
    /// Gets or sets tests executed.
    /// </summary>
    public bool TestsExecuted { get; set; }
    /// <summary>
    /// Gets or sets tests succeeded.
    /// </summary>
    public bool TestsSucceeded { get; set; }
    /// <summary>
    /// Gets or sets source changed during verification.
    /// </summary>
    public bool SourceChangedDuringVerification { get; set; }
    /// <summary>
    /// Gets or sets council review succeeded.
    /// </summary>
    public bool CouncilReviewSucceeded { get; set; }
    /// <summary>
    /// Gets or sets user approved ready for test.
    /// </summary>
    public bool UserApprovedReadyForTest { get; set; }
    /// <summary>
    /// Gets or sets output log path.
    /// </summary>
    [MaxLength(2048)] public string OutputLogPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets evidence manifest path.
    /// </summary>
    [MaxLength(2048)] public string EvidenceManifestPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets output hash.
    /// </summary>
    [MaxLength(128)] public string OutputHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source snapshot hash.
    /// </summary>
    [MaxLength(128)] public string SourceSnapshotHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets snapshot archive path.
    /// </summary>
    [MaxLength(2048)] public string SnapshotArchivePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets council review summary.
    /// </summary>
    [Column(TypeName = "TEXT")] public string CouncilReviewSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets summary.
    /// </summary>
    [Column(TypeName = "TEXT")] public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Represents a save project workspace root request.
/// </summary>
public sealed class SaveProjectWorkspaceRootRequest
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid? Id { get; set; }
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets root path.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets scope kind.
    /// </summary>
    public string ScopeKind { get; set; } = "Global";
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets project type pattern.
    /// </summary>
    public string ProjectTypePattern { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets solution pattern.
    /// </summary>
    public string SolutionPattern { get; set; } = @"(?i)\.(sln|slnx)$";
    /// <summary>
    /// Gets or sets environment kind.
    /// </summary>
    public string EnvironmentKind { get; set; } = "LocalHost";
    /// <summary>
    /// Gets or sets environment root path.
    /// </summary>
    public string EnvironmentRootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets preferred compiler installation identifier.
    /// </summary>
    public Guid? PreferredCompilerInstallationId { get; set; }
    /// <summary>
    /// Gets or sets build arguments.
    /// </summary>
    public string BuildArguments { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets environment variables JSON.
    /// </summary>
    public string EnvironmentVariablesJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets default subdirectories JSON.
    /// </summary>
    public string DefaultSubdirectoriesJson { get; set; } = "[\"src\",\"docs\",\"tests\",\"artifacts\"]";
    /// <summary>
    /// Gets or sets access policy JSON.
    /// </summary>
    public string AccessPolicyJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets expected structure regex.
    /// </summary>
    public string ExpectedStructureRegex { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets priority.
    /// </summary>
    public int Priority { get; set; } = 100;
    /// <summary>
    /// Gets or sets is default.
    /// </summary>
    public bool IsDefault { get; set; }
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a save project compiler installation request.
/// </summary>
public sealed class SaveProjectCompilerInstallationRequest
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid? Id { get; set; }
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets language.
    /// </summary>
    public string Language { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets executable path.
    /// </summary>
    public string ExecutablePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets compiler home path.
    /// </summary>
    public string CompilerHomePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets version.
    /// </summary>
    public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets architecture.
    /// </summary>
    public string Architecture { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets discovery source.
    /// </summary>
    public string DiscoverySource { get; set; } = "Custom";
    /// <summary>
    /// Gets or sets validation arguments.
    /// </summary>
    public string ValidationArguments { get; set; } = "--version";
    /// <summary>
    /// Gets or sets environment variables JSON.
    /// </summary>
    public string EnvironmentVariablesJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets is default for language.
    /// </summary>
    public bool IsDefaultForLanguage { get; set; }
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>Describes one approval-gated compiler and runtime discovery request.</summary>
[DocumentationUpdated("2.1.23")]
public sealed class DiscoverProjectCompilersRequest
{
    /// <summary>Gets or sets additional absolute directories that LocalGPT may inspect.</summary>
    public List<string> CustomSearchRoots { get; set; } = [];

    /// <summary>Gets or sets optional newline-delimited search roots for UI and HTTP clients.</summary>
    public string CustomSearchRootsText { get; set; } = string.Empty;

    /// <summary>Gets or sets whether detected executable profiles are persisted in the project database.</summary>
    public bool SaveDiscovered { get; set; } = true;

    /// <summary>Gets or sets whether the user explicitly approved bounded local path discovery and persistence.</summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a scan project files request.
/// </summary>
public sealed class ScanProjectFilesRequest
{
    /// <summary>
    /// Gets or sets revision identifier.
    /// </summary>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets an optional caller-requested maximum file count; values less than or equal to zero use the database-backed <see cref="LocalGptRuntimeValue.MaxFiles"/> policy.
    /// </summary>
    public int MaximumFiles { get; set; }
    /// <summary>
    /// Gets or sets maximum text file bytes.
    /// </summary>
    public long MaximumTextFileBytes { get; set; } = 4 * 1024 * 1024;
    /// <summary>
    /// Gets or sets an optional caller-requested maximum file size; values less than or equal to zero use the database-backed <see cref="LocalGptRuntimeValue.MaxSingleFileBytes"/> policy.
    /// </summary>
    public long MaximumFileBytes { get; set; }
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a save tracked file pattern request.
/// </summary>
public sealed class SaveTrackedFilePatternRequest
{
    /// <summary>
    /// Gets or sets structure regex.
    /// </summary>
    public string StructureRegex { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets content format regex.
    /// </summary>
    public string ContentFormatRegex { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets file role.
    /// </summary>
    public string FileRole { get; set; } = "Source";
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a register revision workspace request.
/// </summary>
public sealed class RegisterRevisionWorkspaceRequest
{
    /// <summary>
    /// Gets or sets source root path.
    /// </summary>
    public string SourceRootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets solution path.
    /// </summary>
    public string SolutionPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a run project build verification request.
/// </summary>
public sealed class RunProjectBuildVerificationRequest
{
    /// <summary>
    /// Gets or sets revision identifier.
    /// </summary>
    public Guid RevisionId { get; set; }
    /// <summary>
    /// Gets or sets compiler installation identifier.
    /// </summary>
    public Guid CompilerInstallationId { get; set; }
    /// <summary>
    /// Gets or sets configuration.
    /// </summary>
    public string Configuration { get; set; } = "Debug";
    /// <summary>
    /// Gets or sets arguments.
    /// </summary>
    public string Arguments { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets test arguments.
    /// </summary>
    public string TestArguments { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets timeout seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 900;
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a record council build review request.
/// </summary>
public sealed class RecordCouncilBuildReviewRequest
{
    /// <summary>
    /// Gets or sets summary.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets compile errors absent.
    /// </summary>
    public bool CompileErrorsAbsent { get; set; }
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents an approve revision ready for test request.
/// </summary>
public sealed class ApproveRevisionReadyForTestRequest
{
    /// <summary>
    /// Gets or sets verification identifier.
    /// </summary>
    public Guid VerificationId { get; set; }
    /// <summary>
    /// Gets or sets require tests.
    /// </summary>
    public bool RequireTests { get; set; } = true;
    /// <summary>
    /// Gets or sets create lossless snapshot.
    /// </summary>
    public bool CreateLosslessSnapshot { get; set; } = true;
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a project workspace resolution.
/// </summary>
public sealed record ProjectWorkspaceResolution(
    Guid? WorkspaceRootId,
    string RootPath,
    string ScopeKind,
    string MatchReason,
    bool Exists);

/// <summary>
/// Represents a project scan result.
/// </summary>
public sealed record ProjectScanResult(
    Guid ProjectId,
    Guid? RevisionId,
    string ProjectRootPath,
    string SolutionPath,
    int FilesSeen,
    int FilesStored,
    int FilesSkipped,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Represents a workspace access policy rule.
/// </summary>
public sealed class WorkspaceAccessPolicyRule
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets relative path regex.
    /// </summary>
    public string RelativePathRegex { get; set; } = @"(?s).*";
    /// <summary>
    /// Gets or sets expected entry kind.
    /// </summary>
    public string ExpectedEntryKind { get; set; } = "Either";
    /// <summary>
    /// Gets or sets required access.
    /// </summary>
    public string RequiredAccess { get; set; } = "Read";
    /// <summary>
    /// Gets or sets severity.
    /// </summary>
    public string Severity { get; set; } = "Warning";
    /// <summary>
    /// Gets or sets required.
    /// </summary>
    public bool Required { get; set; } = true;
    /// <summary>
    /// Gets or sets council maintained.
    /// </summary>
    public bool CouncilMaintained { get; set; } = true;
}

/// <summary>
/// Represents a workspace permission finding.
/// </summary>
public sealed record WorkspacePermissionFinding(
    string Severity,
    string Code,
    string Message,
    string RelativePath = "");

/// <summary>
/// Represents a workspace permission assessment.
/// </summary>
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

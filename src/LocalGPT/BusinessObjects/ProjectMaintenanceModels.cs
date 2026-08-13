using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a project workspace root application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectWorkspaceRoot
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this project workspace root instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the created at UTC associated with this project workspace root state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this project workspace root state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the name value that forms part of the project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the root path used by this project workspace root instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The root path value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    [Required, MaxLength(2048)] public string RootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the scope kind value that forms part of the project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scope kind value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    [Required, MaxLength(40)] public string ScopeKind { get; set; } = "Global";
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project workspace root instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the project value that forms part of the project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    public LocalGptProject? Project { get; set; }
    /// <summary>
    /// Gets or sets the project type pattern value that forms part of the project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project type pattern value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    [MaxLength(240)] public string ProjectTypePattern { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the solution pattern value that forms part of the project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The solution pattern value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    [MaxLength(1000)] public string SolutionPattern { get; set; } = @"(?i)\.(sln|slnx)$";
    /// <summary>
    /// Gets or sets the environment kind value that forms part of the project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The environment kind value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    [MaxLength(80)] public string EnvironmentKind { get; set; } = "LocalHost";
    /// <summary>
    /// Gets or sets the environment root path used by this project workspace root instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The environment root path value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    [MaxLength(2048)] public string EnvironmentRootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable preferred compiler installation identifier used to identify or correlate this project workspace root instance with related application state.
    /// </summary>
    /// <value>The preferred compiler installation identifier value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    public Guid? PreferredCompilerInstallationId { get; set; }
    /// <summary>
    /// Gets or sets the build arguments value that forms part of the project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The build arguments value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    [Column(TypeName = "TEXT")] public string BuildArguments { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the environment variables JSON value that forms part of the project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The environment variables JSON value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    [Column(TypeName = "TEXT")] public string EnvironmentVariablesJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets the default subdirectories JSON value that forms part of the project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default subdirectories JSON value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    [Column(TypeName = "TEXT")] public string DefaultSubdirectoriesJson { get; set; } = "[\"src\",\"docs\",\"tests\",\"artifacts\"]";
    /// <summary>
    /// Gets or sets the access policy JSON value that forms part of the project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The access policy JSON value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    [Column(TypeName = "TEXT")] public string AccessPolicyJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the expected structure regex value that forms part of the project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The expected structure regex value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    [Column(TypeName = "TEXT")] public string ExpectedStructureRegex { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the last permission status value that forms part of the project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last permission status value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    [MaxLength(80)] public string LastPermissionStatus { get; set; } = "NotChecked";
    /// <summary>
    /// Gets or sets the last permission summary value that forms part of the project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last permission summary value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    [MaxLength(4000)] public string LastPermissionSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether last permission read access applies to the project workspace root state.
    /// </summary>
    /// <value>The last permission read access value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    public bool LastPermissionReadAccess { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether last permission write access applies to the project workspace root state.
    /// </summary>
    /// <value>The last permission write access value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    public bool LastPermissionWriteAccess { get; set; }
    /// <summary>
    /// Gets or sets the last permission checked at UTC associated with this project workspace root state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last permission checked at UTC value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    public DateTime? LastPermissionCheckedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the priority value that forms part of the project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The priority value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    public int Priority { get; set; } = 100;
    /// <summary>
    /// Gets or sets a value indicating whether default applies to the project workspace root state.
    /// </summary>
    /// <value>The is default value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    public bool IsDefault { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the project workspace root state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the last resolved at UTC associated with this project workspace root state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last resolved at UTC value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    public DateTime? LastResolvedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the last resolution status value that forms part of the project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last resolution status value exposed by <see cref="ProjectWorkspaceRoot"/>.</value>
    [MaxLength(80)] public string LastResolutionStatus { get; set; } = "NotResolved";
}

/// <summary>
/// Represents a project compiler installation application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectCompilerInstallation
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this project compiler installation instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the created at UTC associated with this project compiler installation state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this project compiler installation state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the name value that forms part of the project compiler installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the language value that forms part of the project compiler installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The language value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    [Required, MaxLength(80)] public string Language { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the executable path used by this project compiler installation instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The executable path value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    [Required, MaxLength(2048)] public string ExecutablePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the compiler home path used by this project compiler installation instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The compiler home path value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    [MaxLength(2048)] public string CompilerHomePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the version value that forms part of the project compiler installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The version value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    [MaxLength(160)] public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the architecture value that forms part of the project compiler installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The architecture value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    [MaxLength(80)] public string Architecture { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the discovery source value that forms part of the project compiler installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The discovery source value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    [MaxLength(80)] public string DiscoverySource { get; set; } = "Custom";
    /// <summary>
    /// Gets or sets the validation arguments value that forms part of the project compiler installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The validation arguments value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    [MaxLength(500)] public string ValidationArguments { get; set; } = "--version";
    /// <summary>
    /// Gets or sets the environment variables JSON value that forms part of the project compiler installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The environment variables JSON value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    [Column(TypeName = "TEXT")] public string EnvironmentVariablesJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the project compiler installation state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether default for language applies to the project compiler installation state.
    /// </summary>
    /// <value>The is default for language value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    public bool IsDefaultForLanguage { get; set; }
    /// <summary>
    /// Gets or sets the last validated at UTC associated with this project compiler installation state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last validated at UTC value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    public DateTime? LastValidatedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether last validation succeeded applies to the project compiler installation state.
    /// </summary>
    /// <value>The last validation succeeded value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    public bool LastValidationSucceeded { get; set; }
    /// <summary>
    /// Gets or sets the last validation message value that forms part of the project compiler installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last validation message value exposed by <see cref="ProjectCompilerInstallation"/>.</value>
    [MaxLength(4000)] public string LastValidationMessage { get; set; } = string.Empty;
}

/// <summary>
/// Represents a LocalGPT project tracked file application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalGptProjectTrackedFile
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this LocalGPT project tracked file instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this LocalGPT project tracked file instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the project value that forms part of the LocalGPT project tracked file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    public LocalGptProject? Project { get; set; }
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this LocalGPT project tracked file instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets the revision value that forms part of the LocalGPT project tracked file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    public LocalGptProjectRevision? Revision { get; set; }
    /// <summary>
    /// Gets or sets the stable stable file key used to identify or correlate this LocalGPT project tracked file instance with related application state.
    /// </summary>
    /// <value>The stable file key value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    [Required, MaxLength(128)] public string StableFileKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the absolute path used by this LocalGPT project tracked file instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The absolute path value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    [Required, MaxLength(2048)] public string AbsolutePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the project relative path used by this LocalGPT project tracked file instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The project relative path value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    [Required, MaxLength(2048)] public string ProjectRelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the workspace relative path used by this LocalGPT project tracked file instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The workspace relative path value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    [MaxLength(2048)] public string WorkspaceRelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the solution path used by this LocalGPT project tracked file instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The solution path value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    [MaxLength(2048)] public string SolutionPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the project file path used by this LocalGPT project tracked file instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The project file path value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    [MaxLength(2048)] public string ProjectFilePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the file name used by this LocalGPT project tracked file instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The file name value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    [Required, MaxLength(260)] public string FileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the extension value that forms part of the LocalGPT project tracked file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The extension value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    [MaxLength(40)] public string Extension { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the content type value that forms part of the LocalGPT project tracked file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content type value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    [MaxLength(120)] public string ContentType { get; set; } = "text/plain";
    /// <summary>
    /// Gets or sets the encoding name value that forms part of the LocalGPT project tracked file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The encoding name value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    [MaxLength(80)] public string EncodingName { get; set; } = "utf-8";
    /// <summary>
    /// Gets or sets the file role value that forms part of the LocalGPT project tracked file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The file role value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    [MaxLength(120)] public string FileRole { get; set; } = "Source";
    /// <summary>
    /// Gets or sets the structure regex value that forms part of the LocalGPT project tracked file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The structure regex value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    [Column(TypeName = "TEXT")] public string StructureRegex { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the content format regex value that forms part of the LocalGPT project tracked file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content format regex value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    [Column(TypeName = "TEXT")] public string ContentFormatRegex { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the content hash value that forms part of the LocalGPT project tracked file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content hash value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    [Required, MaxLength(128)] public string ContentHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the size bytes value that forms part of the LocalGPT project tracked file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The size bytes value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    public long SizeBytes { get; set; }
    /// <summary>
    /// Gets or sets the last write time UTC associated with this LocalGPT project tracked file state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last write time UTC value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    public DateTime? LastWriteTimeUtc { get; set; }
    /// <summary>
    /// Gets or sets the last seen at UTC associated with this LocalGPT project tracked file state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last seen at UTC value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets a value indicating whether exists applies to the LocalGPT project tracked file state.
    /// </summary>
    /// <value>The exists value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    public bool Exists { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether generated applies to the LocalGPT project tracked file state.
    /// </summary>
    /// <value>The is generated value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    public bool IsGenerated { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user approved applies to the LocalGPT project tracked file state.
    /// </summary>
    /// <value>The is user approved value exposed by <see cref="LocalGptProjectTrackedFile"/>.</value>
    public bool IsUserApproved { get; set; }
}

/// <summary>
/// Represents a project build verification application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectBuildVerification
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this project build verification instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project build verification instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the project value that forms part of the project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public LocalGptProject? Project { get; set; }
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this project build verification instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public Guid RevisionId { get; set; }
    /// <summary>
    /// Gets or sets the revision value that forms part of the project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public LocalGptProjectRevision? Revision { get; set; }
    /// <summary>
    /// Gets or sets the stable compiler installation identifier used to identify or correlate this project build verification instance with related application state.
    /// </summary>
    /// <value>The compiler installation identifier value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public Guid? CompilerInstallationId { get; set; }
    /// <summary>
    /// Gets or sets the compiler installation value that forms part of the project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The compiler installation value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public ProjectCompilerInstallation? CompilerInstallation { get; set; }
    /// <summary>
    /// Gets or sets the started at UTC associated with this project build verification state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The started at UTC value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the completed at UTC associated with this project build verification state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The completed at UTC value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public DateTime? CompletedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the configuration value that forms part of the project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The configuration value exposed by <see cref="ProjectBuildVerification"/>.</value>
    [MaxLength(80)] public string Configuration { get; set; } = "Debug";
    /// <summary>
    /// Gets or sets the target framework value that forms part of the project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target framework value exposed by <see cref="ProjectBuildVerification"/>.</value>
    [MaxLength(160)] public string TargetFramework { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the runtime identifier value that forms part of the project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The runtime identifier value exposed by <see cref="ProjectBuildVerification"/>.</value>
    [MaxLength(80)] public string RuntimeIdentifier { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the executable path used by this project build verification instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The executable path value exposed by <see cref="ProjectBuildVerification"/>.</value>
    [Required, MaxLength(2048)] public string ExecutablePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the arguments value that forms part of the project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The arguments value exposed by <see cref="ProjectBuildVerification"/>.</value>
    [Column(TypeName = "TEXT")] public string Arguments { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the working directory used by this project build verification instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The working directory value exposed by <see cref="ProjectBuildVerification"/>.</value>
    [Required, MaxLength(2048)] public string WorkingDirectory { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the exit code value that forms part of the project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The exit code value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public int? ExitCode { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether build succeeded applies to the project build verification state.
    /// </summary>
    /// <value>The build succeeded value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public bool BuildSucceeded { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether tests executed applies to the project build verification state.
    /// </summary>
    /// <value>The tests executed value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public bool TestsExecuted { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether tests succeeded applies to the project build verification state.
    /// </summary>
    /// <value>The tests succeeded value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public bool TestsSucceeded { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether source changed during verification applies to the project build verification state.
    /// </summary>
    /// <value>The source changed during verification value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public bool SourceChangedDuringVerification { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether council review succeeded applies to the project build verification state.
    /// </summary>
    /// <value>The council review succeeded value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public bool CouncilReviewSucceeded { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user approved ready for test applies to the project build verification state.
    /// </summary>
    /// <value>The user approved ready for test value exposed by <see cref="ProjectBuildVerification"/>.</value>
    public bool UserApprovedReadyForTest { get; set; }
    /// <summary>
    /// Gets or sets the output log path used by this project build verification instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The output log path value exposed by <see cref="ProjectBuildVerification"/>.</value>
    [MaxLength(2048)] public string OutputLogPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the evidence manifest path used by this project build verification instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The evidence manifest path value exposed by <see cref="ProjectBuildVerification"/>.</value>
    [MaxLength(2048)] public string EvidenceManifestPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the output hash value that forms part of the project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The output hash value exposed by <see cref="ProjectBuildVerification"/>.</value>
    [MaxLength(128)] public string OutputHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source snapshot hash value that forms part of the project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source snapshot hash value exposed by <see cref="ProjectBuildVerification"/>.</value>
    [MaxLength(128)] public string SourceSnapshotHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the snapshot archive path used by this project build verification instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The snapshot archive path value exposed by <see cref="ProjectBuildVerification"/>.</value>
    [MaxLength(2048)] public string SnapshotArchivePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the council review summary value that forms part of the project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council review summary value exposed by <see cref="ProjectBuildVerification"/>.</value>
    [Column(TypeName = "TEXT")] public string CouncilReviewSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the summary value that forms part of the project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The summary value exposed by <see cref="ProjectBuildVerification"/>.</value>
    [Column(TypeName = "TEXT")] public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Represents the input contract for save project workspace root, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SaveProjectWorkspaceRootRequest
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this save project workspace root instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public Guid? Id { get; set; }
    /// <summary>
    /// Gets or sets the name value that forms part of the save project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the root path used by this save project workspace root instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The root path value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public string RootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the scope kind value that forms part of the save project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scope kind value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public string ScopeKind { get; set; } = "Global";
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this save project workspace root instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the project type pattern value that forms part of the save project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project type pattern value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public string ProjectTypePattern { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the solution pattern value that forms part of the save project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The solution pattern value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public string SolutionPattern { get; set; } = @"(?i)\.(sln|slnx)$";
    /// <summary>
    /// Gets or sets the environment kind value that forms part of the save project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The environment kind value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public string EnvironmentKind { get; set; } = "LocalHost";
    /// <summary>
    /// Gets or sets the environment root path used by this save project workspace root instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The environment root path value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public string EnvironmentRootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable preferred compiler installation identifier used to identify or correlate this save project workspace root instance with related application state.
    /// </summary>
    /// <value>The preferred compiler installation identifier value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public Guid? PreferredCompilerInstallationId { get; set; }
    /// <summary>
    /// Gets or sets the build arguments value that forms part of the save project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The build arguments value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public string BuildArguments { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the environment variables JSON value that forms part of the save project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The environment variables JSON value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public string EnvironmentVariablesJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets the default subdirectories JSON value that forms part of the save project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default subdirectories JSON value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public string DefaultSubdirectoriesJson { get; set; } = "[\"src\",\"docs\",\"tests\",\"artifacts\"]";
    /// <summary>
    /// Gets or sets the access policy JSON value that forms part of the save project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The access policy JSON value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public string AccessPolicyJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the expected structure regex value that forms part of the save project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The expected structure regex value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public string ExpectedStructureRegex { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the priority value that forms part of the save project workspace root state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The priority value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public int Priority { get; set; } = 100;
    /// <summary>
    /// Gets or sets a value indicating whether default applies to the save project workspace root state.
    /// </summary>
    /// <value>The is default value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public bool IsDefault { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the save project workspace root state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the save project workspace root state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="SaveProjectWorkspaceRootRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents the input contract for save project compiler installation, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SaveProjectCompilerInstallationRequest
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this save project compiler installation instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="SaveProjectCompilerInstallationRequest"/>.</value>
    public Guid? Id { get; set; }
    /// <summary>
    /// Gets or sets the name value that forms part of the save project compiler installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="SaveProjectCompilerInstallationRequest"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the language value that forms part of the save project compiler installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The language value exposed by <see cref="SaveProjectCompilerInstallationRequest"/>.</value>
    public string Language { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the executable path used by this save project compiler installation instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The executable path value exposed by <see cref="SaveProjectCompilerInstallationRequest"/>.</value>
    public string ExecutablePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the compiler home path used by this save project compiler installation instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The compiler home path value exposed by <see cref="SaveProjectCompilerInstallationRequest"/>.</value>
    public string CompilerHomePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the version value that forms part of the save project compiler installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The version value exposed by <see cref="SaveProjectCompilerInstallationRequest"/>.</value>
    public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the architecture value that forms part of the save project compiler installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The architecture value exposed by <see cref="SaveProjectCompilerInstallationRequest"/>.</value>
    public string Architecture { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the discovery source value that forms part of the save project compiler installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The discovery source value exposed by <see cref="SaveProjectCompilerInstallationRequest"/>.</value>
    public string DiscoverySource { get; set; } = "Custom";
    /// <summary>
    /// Gets or sets the validation arguments value that forms part of the save project compiler installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The validation arguments value exposed by <see cref="SaveProjectCompilerInstallationRequest"/>.</value>
    public string ValidationArguments { get; set; } = "--version";
    /// <summary>
    /// Gets or sets the environment variables JSON value that forms part of the save project compiler installation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The environment variables JSON value exposed by <see cref="SaveProjectCompilerInstallationRequest"/>.</value>
    public string EnvironmentVariablesJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the save project compiler installation state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="SaveProjectCompilerInstallationRequest"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether default for language applies to the save project compiler installation state.
    /// </summary>
    /// <value>The is default for language value exposed by <see cref="SaveProjectCompilerInstallationRequest"/>.</value>
    public bool IsDefaultForLanguage { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the save project compiler installation state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="SaveProjectCompilerInstallationRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>Describes one approval-gated compiler and runtime discovery request.</summary>
[DocumentationUpdated("2.1.23")]
public sealed class DiscoverProjectCompilersRequest
{
    /// <summary>
    /// Gets or sets the custom search roots collection maintained or exposed by this discover project compilers instance for downstream processing.
    /// </summary>
    /// <value>The custom search roots value exposed by <see cref="DiscoverProjectCompilersRequest"/>.</value>
    public List<string> CustomSearchRoots { get; set; } = [];

    /// <summary>Gets or sets optional newline-delimited search roots for UI and HTTP clients.</summary>
    /// <value>The custom search roots text value exposed by <see cref="DiscoverProjectCompilersRequest"/>.</value>
    public string CustomSearchRootsText { get; set; } = string.Empty;

    /// <summary>Gets or sets whether detected executable profiles are persisted in the project database.</summary>
    /// <value>The save discovered value exposed by <see cref="DiscoverProjectCompilersRequest"/>.</value>
    public bool SaveDiscovered { get; set; } = true;

    /// <summary>Gets or sets whether the user explicitly approved bounded local path discovery and persistence.</summary>
    /// <value>The user confirmed value exposed by <see cref="DiscoverProjectCompilersRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents the input contract for scan project files, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class ScanProjectFilesRequest
{
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this scan project files instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="ScanProjectFilesRequest"/>.</value>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets an optional caller-requested maximum file count; values less than or equal to zero use the database-backed <see cref="LocalGptRuntimeValue.MaxFiles"/> policy.
    /// </summary>
    /// <value>The maximum files value exposed by <see cref="ScanProjectFilesRequest"/>.</value>
    public int MaximumFiles { get; set; }
    /// <summary>
    /// Gets or sets the maximum text file bytes value that forms part of the scan project files state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum text file bytes value exposed by <see cref="ScanProjectFilesRequest"/>.</value>
    public long MaximumTextFileBytes { get; set; } = 4 * 1024 * 1024;
    /// <summary>
    /// Gets or sets an optional caller-requested maximum file size; values less than or equal to zero use the database-backed <see cref="LocalGptRuntimeValue.MaxSingleFileBytes"/> policy.
    /// </summary>
    /// <value>The maximum file bytes value exposed by <see cref="ScanProjectFilesRequest"/>.</value>
    public long MaximumFileBytes { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the scan project files state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="ScanProjectFilesRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents the input contract for save tracked file pattern, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SaveTrackedFilePatternRequest
{
    /// <summary>
    /// Gets or sets the structure regex value that forms part of the save tracked file pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The structure regex value exposed by <see cref="SaveTrackedFilePatternRequest"/>.</value>
    public string StructureRegex { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the content format regex value that forms part of the save tracked file pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content format regex value exposed by <see cref="SaveTrackedFilePatternRequest"/>.</value>
    public string ContentFormatRegex { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the file role value that forms part of the save tracked file pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The file role value exposed by <see cref="SaveTrackedFilePatternRequest"/>.</value>
    public string FileRole { get; set; } = "Source";
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the save tracked file pattern state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="SaveTrackedFilePatternRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents the input contract for register revision workspace, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class RegisterRevisionWorkspaceRequest
{
    /// <summary>
    /// Gets or sets the source root path used by this register revision workspace instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The source root path value exposed by <see cref="RegisterRevisionWorkspaceRequest"/>.</value>
    public string SourceRootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the solution path used by this register revision workspace instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The solution path value exposed by <see cref="RegisterRevisionWorkspaceRequest"/>.</value>
    public string SolutionPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the register revision workspace state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="RegisterRevisionWorkspaceRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents the input contract for run project build verification, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class RunProjectBuildVerificationRequest
{
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this run project build verification instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="RunProjectBuildVerificationRequest"/>.</value>
    public Guid RevisionId { get; set; }
    /// <summary>
    /// Gets or sets the stable compiler installation identifier used to identify or correlate this run project build verification instance with related application state.
    /// </summary>
    /// <value>The compiler installation identifier value exposed by <see cref="RunProjectBuildVerificationRequest"/>.</value>
    public Guid CompilerInstallationId { get; set; }
    /// <summary>
    /// Gets or sets the configuration value that forms part of the run project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The configuration value exposed by <see cref="RunProjectBuildVerificationRequest"/>.</value>
    public string Configuration { get; set; } = "Debug";
    /// <summary>
    /// Gets or sets the arguments value that forms part of the run project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The arguments value exposed by <see cref="RunProjectBuildVerificationRequest"/>.</value>
    public string Arguments { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the test arguments value that forms part of the run project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The test arguments value exposed by <see cref="RunProjectBuildVerificationRequest"/>.</value>
    public string TestArguments { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the timeout seconds value that forms part of the run project build verification state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The timeout seconds value exposed by <see cref="RunProjectBuildVerificationRequest"/>.</value>
    public int TimeoutSeconds { get; set; } = 900;
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the run project build verification state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="RunProjectBuildVerificationRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents the input contract for record council build review, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class RecordCouncilBuildReviewRequest
{
    /// <summary>
    /// Gets or sets the summary value that forms part of the record council build review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The summary value exposed by <see cref="RecordCouncilBuildReviewRequest"/>.</value>
    public string Summary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether compile errors absent applies to the record council build review state.
    /// </summary>
    /// <value>The compile errors absent value exposed by <see cref="RecordCouncilBuildReviewRequest"/>.</value>
    public bool CompileErrorsAbsent { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the record council build review state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="RecordCouncilBuildReviewRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents the input contract for approve revision ready for test, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class ApproveRevisionReadyForTestRequest
{
    /// <summary>
    /// Gets or sets the stable verification identifier used to identify or correlate this approve revision ready for test instance with related application state.
    /// </summary>
    /// <value>The verification identifier value exposed by <see cref="ApproveRevisionReadyForTestRequest"/>.</value>
    public Guid VerificationId { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether tests applies to the approve revision ready for test state.
    /// </summary>
    /// <value>The require tests value exposed by <see cref="ApproveRevisionReadyForTestRequest"/>.</value>
    public bool RequireTests { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether create lossless snapshot applies to the approve revision ready for test state.
    /// </summary>
    /// <value>The create lossless snapshot value exposed by <see cref="ApproveRevisionReadyForTestRequest"/>.</value>
    public bool CreateLosslessSnapshot { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the approve revision ready for test state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="ApproveRevisionReadyForTestRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a project workspace resolution application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="WorkspaceRootId">Identifier of the workspace root to use for this operation.</param>
/// <param name="RootPath">Root path value supplied to the project workspace resolution operation and used when producing its result.</param>
/// <param name="ScopeKind">Scope kind value supplied to the project workspace resolution operation and used when producing its result.</param>
/// <param name="MatchReason">Match reason value supplied to the project workspace resolution operation and used when producing its result.</param>
/// <param name="Exists">Value indicating whether exists should apply to this operation.</param>
public sealed record ProjectWorkspaceResolution(
    Guid? WorkspaceRootId,
    string RootPath,
    string ScopeKind,
    string MatchReason,
    bool Exists);

/// <summary>
/// Represents the outcome of project scan, carrying the data and status produced by the corresponding application operation.
/// </summary>
/// <param name="ProjectId">Identifier of the project to use for this operation.</param>
/// <param name="RevisionId">Identifier of the revision to use for this operation.</param>
/// <param name="ProjectRootPath">Project root path value supplied to the project scan operation and used when producing its result.</param>
/// <param name="SolutionPath">Solution path value supplied to the project scan operation and used when producing its result.</param>
/// <param name="FilesSeen">Files seen value supplied to the project scan operation and used when producing its result.</param>
/// <param name="FilesStored">Files stored value supplied to the project scan operation and used when producing its result.</param>
/// <param name="FilesSkipped">Files skipped value supplied to the project scan operation and used when producing its result.</param>
/// <param name="Warnings">String dependency used by the project scan workflow to provide the corresponding application capability.</param>
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
/// Represents a workspace access policy rule application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class WorkspaceAccessPolicyRule
{
    /// <summary>
    /// Gets or sets the name value that forms part of the workspace access policy rule state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="WorkspaceAccessPolicyRule"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the relative path regex used by this workspace access policy rule instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path regex value exposed by <see cref="WorkspaceAccessPolicyRule"/>.</value>
    public string RelativePathRegex { get; set; } = @"(?s).*";
    /// <summary>
    /// Gets or sets the expected entry kind value that forms part of the workspace access policy rule state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The expected entry kind value exposed by <see cref="WorkspaceAccessPolicyRule"/>.</value>
    public string ExpectedEntryKind { get; set; } = "Either";
    /// <summary>
    /// Gets or sets the required access value that forms part of the workspace access policy rule state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The required access value exposed by <see cref="WorkspaceAccessPolicyRule"/>.</value>
    public string RequiredAccess { get; set; } = "Read";
    /// <summary>
    /// Gets or sets the severity value that forms part of the workspace access policy rule state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The severity value exposed by <see cref="WorkspaceAccessPolicyRule"/>.</value>
    public string Severity { get; set; } = "Warning";
    /// <summary>
    /// Gets or sets a value indicating whether the value is required applies to the workspace access policy rule state.
    /// </summary>
    /// <value>The required value exposed by <see cref="WorkspaceAccessPolicyRule"/>.</value>
    public bool Required { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether council maintained applies to the workspace access policy rule state.
    /// </summary>
    /// <value>The council maintained value exposed by <see cref="WorkspaceAccessPolicyRule"/>.</value>
    public bool CouncilMaintained { get; set; } = true;
}

/// <summary>
/// Represents a workspace permission finding application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Severity">Severity value supplied to the workspace permission finding operation and used when producing its result.</param>
/// <param name="Code">Code value supplied to the workspace permission finding operation and used when producing its result.</param>
/// <param name="Message">Message value supplied to the workspace permission finding operation and used when producing its result.</param>
/// <param name="RelativePath">Relative path value supplied to the workspace permission finding operation and used when producing its result.</param>
public sealed record WorkspacePermissionFinding(
    string Severity,
    string Code,
    string Message,
    string RelativePath = "");

/// <summary>
/// Represents a workspace permission assessment application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="WorkspaceRootId">Identifier of the workspace root to use for this operation.</param>
/// <param name="Status">Status value supplied to the workspace permission assessment operation and used when producing its result.</param>
/// <param name="CheckedAtUtc">Checked at utc value supplied to the workspace permission assessment operation and used when producing its result.</param>
/// <param name="RootExists">Value indicating whether root exists should apply to this operation.</param>
/// <param name="ReadAccess">Value indicating whether read access should apply to this operation.</param>
/// <param name="WriteAccess">Value indicating whether write access should apply to this operation.</param>
/// <param name="EnvironmentRootPath">Environment root path value supplied to the workspace permission assessment operation and used when producing its result.</param>
/// <param name="PreferredCompilerInstallationId">Identifier of the preferred compiler installation to use for this operation.</param>
/// <param name="ExpectedSubdirectories">String dependency used by the workspace permission assessment workflow to provide the corresponding application capability.</param>
/// <param name="Findings">Workspace permission finding dependency used by the workspace permission assessment workflow to provide the corresponding application capability.</param>
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

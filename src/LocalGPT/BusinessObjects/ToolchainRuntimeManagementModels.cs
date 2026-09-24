namespace LocalGPT.BusinessObjects;

/// <summary>Identifies where an environment-variable value is read from or written to by the toolchain workbench.</summary>
public enum ToolchainEnvironmentScope
{
    /// <summary>The effective value visible to the running LocalGPT process after all active overrides are applied.</summary>
    Effective,
    /// <summary>A temporary value stored only in the current LocalGPT process environment.</summary>
    Process,
    /// <summary>A LocalGPT-persisted application override applied to the process when the toolchain environment service starts.</summary>
    Application,
    /// <summary>The current operating-system user's persistent environment, when the platform supports it.</summary>
    User,
    /// <summary>The machine-wide persistent environment, when the platform supports it and permissions allow mutation.</summary>
    Machine
}

/// <summary>Configures environment-variable overrides that LocalGPT owns without replacing operating-system global settings.</summary>
public sealed class ToolchainEnvironmentOptions
{
    /// <summary>Gets or sets LocalGPT-owned application overrides keyed by environment-variable name.</summary>
    /// <value>The application overrides persisted in LocalGPT configuration.</value>
    public Dictionary<string, string> ApplicationOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Describes one visible environment-variable value and the scope that supplied it.</summary>
public sealed class ToolchainEnvironmentEntry
{
    /// <summary>Gets or sets the variable name.</summary>
    /// <value>The environment-variable name.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the value shown to the local operator, which may be redacted for credential-like names.</summary>
    /// <value>The visible environment-variable value.</value>
    public string Value { get; set; } = string.Empty;
    /// <summary>Gets or sets the scope represented by this row.</summary>
    /// <value>The environment scope.</value>
    public ToolchainEnvironmentScope Scope { get; set; }
    /// <summary>Gets or sets the optional registered toolchain installation associated with this row.</summary>
    /// <value>The toolchain installation identifier, or <see langword="null"/> for application-wide state.</value>
    public Guid? ToolchainInstallationId { get; set; }
    /// <summary>Gets or sets a short description of the source used for the row.</summary>
    /// <value>The source label.</value>
    public string Source { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the variable name looks credential-like and its value was therefore redacted.</summary>
    /// <value><see langword="true"/> when the displayed value is redacted.</value>
    public bool IsSensitive { get; set; }
    /// <summary>Gets or sets whether LocalGPT can attempt to mutate this scope on the current platform.</summary>
    /// <value><see langword="true"/> when the scope is writable in principle.</value>
    public bool IsWritable { get; set; }
    /// <summary>Gets or sets whether another active scope currently changes the value seen by LocalGPT.</summary>
    /// <value><see langword="true"/> when the row is shadowed by an active override.</value>
    public bool IsOverridden { get; set; }
}

/// <summary>Collects environment-variable rows for the current process and supported persistent scopes.</summary>
public sealed class ToolchainEnvironmentSnapshot
{
    /// <summary>Gets or sets the detected platform label.</summary>
    /// <value>The runtime platform label.</value>
    public string Platform { get; set; } = string.Empty;
    /// <summary>Gets or sets whether user and machine environment scopes can be mutated through the current runtime.</summary>
    /// <value><see langword="true"/> when persistent operating-system scopes are supported.</value>
    public bool SupportsPersistentOperatingSystemScopes { get; set; }
    /// <summary>Gets or sets the visible environment rows.</summary>
    /// <value>The bounded, sorted environment rows.</value>
    public List<ToolchainEnvironmentEntry> Entries { get; set; } = [];
}

/// <summary>Requests one explicit environment-variable change through the toolchain environment service.</summary>
public sealed class ToolchainEnvironmentChangeRequest
{
    /// <summary>Gets or sets the variable name to mutate.</summary>
    /// <value>The requested environment-variable name.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the replacement value. It is ignored when <see cref="Remove"/> is true.</summary>
    /// <value>The requested environment-variable value.</value>
    public string Value { get; set; } = string.Empty;
    /// <summary>Gets or sets the writable scope to mutate.</summary>
    /// <value>The requested environment-variable scope.</value>
    public ToolchainEnvironmentScope Scope { get; set; } = ToolchainEnvironmentScope.Application;
    /// <summary>Gets or sets the optional registered toolchain installation that owns this override.</summary>
    /// <value>The installation identifier, or <see langword="null"/> for LocalGPT-wide state.</value>
    public Guid? ToolchainInstallationId { get; set; }
    /// <summary>Gets or sets whether the value should be removed from the selected scope.</summary>
    /// <value><see langword="true"/> to remove the value.</value>
    public bool Remove { get; set; }
    /// <summary>Gets or sets whether the human operator explicitly confirmed the mutation.</summary>
    /// <value><see langword="true"/> when exact human confirmation was supplied.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>Describes one reviewed direct-download source for a compiler, runtime, SDK, or build tool.</summary>
public sealed class ToolchainAcquisitionSource
{
    /// <summary>Gets or sets the stable catalog key.</summary>
    /// <value>The source key used by controller and DXFunction calls.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>Gets or sets the matching local discovery profile key.</summary>
    /// <value>The toolchain knowledge profile key.</value>
    public string ProfileKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the human-readable tool name.</summary>
    /// <value>The display name.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the publisher or upstream project.</summary>
    /// <value>The publisher label.</value>
    public string Publisher { get; set; } = string.Empty;
    /// <summary>Gets or sets the pinned or channel version described by the source.</summary>
    /// <value>The catalog version label.</value>
    public string Version { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the source is an upstream manufacturer download or a GitHub-hosted project artifact.</summary>
    /// <value>The source-kind label.</value>
    public string SourceKind { get; set; } = string.Empty;
    /// <summary>Gets or sets the HTTPS download URI selected by the reviewed catalog.</summary>
    /// <value>The direct download URI.</value>
    public string DownloadUri { get; set; } = string.Empty;
    /// <summary>Gets or sets the local file name used after download.</summary>
    /// <value>The bounded output file name.</value>
    public string FileName { get; set; } = string.Empty;
    /// <summary>Gets or sets the operating-system family for which this catalog row is intended.</summary>
    /// <value>One of windows, linux, macos, or any.</value>
    public string Platform { get; set; } = string.Empty;
    /// <summary>Gets or sets concise operator guidance for what the downloaded artifact does.</summary>
    /// <value>The install or linking hint.</value>
    public string InstallHint { get; set; } = string.Empty;
    /// <summary>Gets or sets whether this source matches the currently running platform.</summary>
    /// <value><see langword="true"/> when the source is directly applicable to the current platform.</value>
    public bool SupportedOnCurrentPlatform { get; set; }
}

/// <summary>Requests a reviewed direct toolchain download by stable catalog key.</summary>
public sealed class ToolchainAcquisitionDownloadRequest
{
    /// <summary>Gets or sets the stable source key to download.</summary>
    /// <value>The reviewed catalog key.</value>
    public string SourceKey { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the human operator explicitly approved the network download.</summary>
    /// <value><see langword="true"/> when exact human confirmation was supplied.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>Reports the result of one reviewed direct toolchain download.</summary>
public sealed class ToolchainAcquisitionDownloadResult
{
    /// <summary>Gets or sets the stable source key that was downloaded.</summary>
    /// <value>The reviewed catalog key.</value>
    public string SourceKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the local downloaded file path.</summary>
    /// <value>The managed download path.</value>
    public string LocalPath { get; set; } = string.Empty;
    /// <summary>Gets or sets the number of bytes written.</summary>
    /// <value>The downloaded byte count.</value>
    public long Bytes { get; set; }
    /// <summary>Gets or sets the SHA-256 digest of the downloaded bytes.</summary>
    /// <value>The lowercase hexadecimal digest.</value>
    public string Sha256 { get; set; } = string.Empty;
    /// <summary>Gets or sets when the completed file was promoted into the managed download directory.</summary>
    /// <value>The UTC completion timestamp.</value>
    public DateTime DownloadedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets the catalog install/linking hint associated with the artifact.</summary>
    /// <value>The follow-up operator guidance.</value>
    public string InstallHint { get; set; } = string.Empty;
}

/// <summary>Identifies how a persisted toolchain execution profile starts its configured process.</summary>
public enum ToolchainExecutionKind
{
    /// <summary>Starts the registered toolchain executable directly.</summary>
    ToolchainExecutable,
    /// <summary>Starts a module through the registered runtime, for example <c>python -m whisper</c>.</summary>
    Module,
    /// <summary>Starts a script through the registered runtime.</summary>
    Script,
    /// <summary>Starts the profile entry point as an executable without a shell.</summary>
    Executable
}

/// <summary>Database-backed, runtime-agnostic process profile owned by the Toolchains subsystem.</summary>
public sealed class ToolchainExecutionProfile
{
    /// <summary>Gets or sets the stable profile identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Gets or sets the stable user-facing profile key.</summary>
    public string ProfileKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional toolchain installation backing this process.</summary>
    public Guid? ToolchainInstallationId { get; set; }
    /// <summary>Gets or sets the capability exposed by this profile, for example <c>speech.transcribe</c>.</summary>
    public string CapabilityKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the process start strategy.</summary>
    public ToolchainExecutionKind ExecutionKind { get; set; } = ToolchainExecutionKind.ToolchainExecutable;
    /// <summary>Gets or sets the module, script, or executable entry point.</summary>
    public string EntryPoint { get; set; } = string.Empty;
    /// <summary>Gets or sets the JSON array containing default process arguments.</summary>
    public string ArgumentsJson { get; set; } = "[]";
    /// <summary>Gets or sets the optional working directory.</summary>
    public string WorkingDirectory { get; set; } = string.Empty;
    /// <summary>Gets or sets the JSON object containing profile-local environment overrides.</summary>
    public string EnvironmentVariablesJson { get; set; } = "{}";
    /// <summary>Gets or sets arbitrary profile configuration owned by the capability adapter.</summary>
    public string ConfigurationJson { get; set; } = "{}";
    /// <summary>Gets or sets whether this profile is enabled.</summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets whether this profile is the default for its capability.</summary>
    public bool IsDefaultForCapability { get; set; }
    /// <summary>Gets or sets whether execution requires explicit human approval unless user policy pre-authorizes the function.</summary>
    public bool RequiresApproval { get; set; } = true;
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets the actor that last changed the profile.</summary>
    public string UpdatedBy { get; set; } = "CurrentUser";
}

/// <summary>Persists one LocalGPT-controlled environment mutation so UI policy and operating-system state remain auditable together.</summary>
public sealed class ToolchainEnvironmentOverrideRecord
{
    /// <summary>Gets or sets the stable record identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Gets or sets the optional toolchain installation this override belongs to.</summary>
    public Guid? ToolchainInstallationId { get; set; }
    /// <summary>Gets or sets the environment-variable name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the controlled scope.</summary>
    public ToolchainEnvironmentScope Scope { get; set; } = ToolchainEnvironmentScope.Application;
    /// <summary>Gets or sets the stored value. It is never returned through AI-facing inventory functions.</summary>
    public string Value { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the override should be applied when LocalGPT starts.</summary>
    public bool ApplyOnStartup { get; set; }
    /// <summary>Gets or sets when LocalGPT last changed this scope.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets who last changed the record.</summary>
    public string UpdatedBy { get; set; } = "CurrentUser";
}

/// <summary>Requests persistence of one reusable toolchain execution profile.</summary>
public sealed class SaveToolchainExecutionProfileRequest
{
    /// <summary>Gets or sets the profile being saved.</summary>
    public ToolchainExecutionProfile Profile { get; set; } = new();
    /// <summary>Gets or sets whether the consequential configuration mutation has been approved.</summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>Requests execution of a persisted toolchain profile without invoking a shell.</summary>
public sealed class ToolchainProcessExecutionRequest
{
    /// <summary>Gets or sets the persisted profile key.</summary>
    public string ProfileKey { get; set; } = string.Empty;
    /// <summary>Gets or sets additional argument values appended after persisted arguments.</summary>
    public List<string> Arguments { get; set; } = [];
    /// <summary>Gets or sets whether the process execution has explicit user approval.</summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>Reports the bounded output of one persisted toolchain process execution.</summary>
public sealed class ToolchainProcessExecutionResult
{
    /// <summary>Gets or sets the profile key that ran.</summary>
    public string ProfileKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the process exit code.</summary>
    public int ExitCode { get; set; }
    /// <summary>Gets or sets bounded standard output.</summary>
    public string StandardOutput { get; set; } = string.Empty;
    /// <summary>Gets or sets bounded standard error.</summary>
    public string StandardError { get; set; } = string.Empty;
    /// <summary>Gets or sets the UTC completion time.</summary>
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
}

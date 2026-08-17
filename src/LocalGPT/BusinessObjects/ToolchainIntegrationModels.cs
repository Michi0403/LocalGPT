using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

/// <summary>Identifies the operating-system family used while resolving a toolchain installation.</summary>
public enum ToolchainPlatformKind
{
    /// <summary>Microsoft Windows.</summary>
    Windows,
    /// <summary>Linux and compatible Unix-like distributions.</summary>
    Linux,
    /// <summary>Apple macOS.</summary>
    MacOS,
    /// <summary>An operating system without a dedicated LocalGPT profile.</summary>
    Other
}

/// <summary>Represents one structured environment-variable value associated with a compiler or runtime toolchain.</summary>
public sealed class ToolchainEnvironmentVariableSetting
{
    /// <summary>
    /// Gets or sets the name value that forms part of the toolchain environment variable setting state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="ToolchainEnvironmentVariableSetting"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the value value that forms part of the toolchain environment variable setting state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The value value exposed by <see cref="ToolchainEnvironmentVariableSetting"/>.</value>
    public string Value { get; set; } = string.Empty;
    /// <summary>Gets or sets the discovery source, for example JAVA_HOME, profile, or user.</summary>
    /// <value>The source value exposed by <see cref="ToolchainEnvironmentVariableSetting"/>.</value>
    public string Source { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the value is applied when LocalGPT launches the tool.</summary>
    /// <value>The is enabled value exposed by <see cref="ToolchainEnvironmentVariableSetting"/>.</value>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>Describes one knowledge-backed compiler or runtime toolchain family.</summary>
public sealed class ToolchainKnowledgeProfile
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this toolchain knowledge profile instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the toolchain knowledge profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the language value that forms part of the toolchain knowledge profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The language value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public string Language { get; set; } = string.Empty;
    /// <summary>Gets or sets a short toolchain kind such as compiler, runtime, build-tool, or package-manager.</summary>
    /// <value>The kind value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Gets or sets executable file names or command aliases that may identify this toolchain.</summary>
    /// <value>The executable names value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public List<string> ExecutableNames { get; set; } = [];
    /// <summary>Gets or sets environment variables whose values point at a toolchain home/root.</summary>
    /// <value>The environment root variables value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public List<string> EnvironmentRootVariables { get; set; } = [];
    /// <summary>
    /// Gets or sets the common search roots collection maintained or exposed by this toolchain knowledge profile instance for downstream processing.
    /// </summary>
    /// <value>The common search roots value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public List<string> CommonSearchRoots { get; set; } = [];
    /// <summary>
    /// Gets or sets the windows search roots collection maintained or exposed by this toolchain knowledge profile instance for downstream processing.
    /// </summary>
    /// <value>The windows search roots value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public List<string> WindowsSearchRoots { get; set; } = [];
    /// <summary>
    /// Gets or sets the linux search roots collection maintained or exposed by this toolchain knowledge profile instance for downstream processing.
    /// </summary>
    /// <value>The linux search roots value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public List<string> LinuxSearchRoots { get; set; } = [];
    /// <summary>
    /// Gets or sets the mac OS search roots collection maintained or exposed by this toolchain knowledge profile instance for downstream processing.
    /// </summary>
    /// <value>The mac OS search roots value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public List<string> MacOsSearchRoots { get; set; } = [];
    /// <summary>Gets or sets arguments used for a bounded local version probe.</summary>
    /// <value>The validation arguments value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public string ValidationArguments { get; set; } = "--version";
    /// <summary>Gets or sets the database regex-pattern name used to extract a version token from probe output.</summary>
    /// <value>The version regex pattern name value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public string VersionRegexPatternName { get; set; } = "builtin.toolchain-version-token";
    /// <summary>Gets or sets project marker file names that provide contextual evidence for this toolchain.</summary>
    /// <value>The project markers value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public List<string> ProjectMarkers { get; set; } = [];
    /// <summary>Gets or sets optional tags used to locate version-specific knowledge articles.</summary>
    /// <value>The context tags value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public List<string> ContextTags { get; set; } = [];
    /// <summary>Gets or sets the maximum directory depth for knowledge-root traversal.</summary>
    /// <value>The maximum search depth value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public int MaximumSearchDepth { get; set; } = 3;
    /// <summary>
    /// Gets or sets the stable knowledge entry identifier used to identify or correlate this toolchain knowledge profile instance with related application state.
    /// </summary>
    /// <value>The knowledge entry identifier value exposed by <see cref="ToolchainKnowledgeProfile"/>.</value>
    public Guid KnowledgeEntryId { get; set; }
}

/// <summary>Represents one locally discovered toolchain executable before it is persisted as a project compiler installation.</summary>
public sealed class ToolchainDiscoveryCandidate
{
    /// <summary>
    /// Gets or sets the stable profile key used to identify or correlate this toolchain discovery candidate instance with related application state.
    /// </summary>
    /// <value>The profile key value exposed by <see cref="ToolchainDiscoveryCandidate"/>.</value>
    public string ProfileKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the name value that forms part of the toolchain discovery candidate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="ToolchainDiscoveryCandidate"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the language value that forms part of the toolchain discovery candidate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The language value exposed by <see cref="ToolchainDiscoveryCandidate"/>.</value>
    public string Language { get; set; } = string.Empty;
    /// <summary>Gets or sets the knowledge-defined toolchain kind such as compiler, runtime, build-tool, SDK, or package-manager.</summary>
    /// <value>The kind value exposed by <see cref="ToolchainDiscoveryCandidate"/>.</value>
    public string Kind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the executable path used by this toolchain discovery candidate instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The executable path value exposed by <see cref="ToolchainDiscoveryCandidate"/>.</value>
    public string ExecutablePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the toolchain home path used by this toolchain discovery candidate instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The toolchain home path value exposed by <see cref="ToolchainDiscoveryCandidate"/>.</value>
    public string ToolchainHomePath { get; set; } = string.Empty;
    /// <summary>Gets or sets the discovery source such as PATH, environment variable, knowledge root, or user root.</summary>
    /// <value>The discovery source value exposed by <see cref="ToolchainDiscoveryCandidate"/>.</value>
    public string DiscoverySource { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the platform value that forms part of the toolchain discovery candidate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The platform value exposed by <see cref="ToolchainDiscoveryCandidate"/>.</value>
    public ToolchainPlatformKind Platform { get; set; }
    /// <summary>
    /// Gets or sets the validation arguments value that forms part of the toolchain discovery candidate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The validation arguments value exposed by <see cref="ToolchainDiscoveryCandidate"/>.</value>
    public string ValidationArguments { get; set; } = "--version";
    /// <summary>Gets or sets the version regex-pattern name supplied by knowledge.</summary>
    /// <value>The version regex pattern name value exposed by <see cref="ToolchainDiscoveryCandidate"/>.</value>
    public string VersionRegexPatternName { get; set; } = "builtin.toolchain-version-token";
    /// <summary>
    /// Gets or sets the stable knowledge entry identifier used to identify or correlate this toolchain discovery candidate instance with related application state.
    /// </summary>
    /// <value>The knowledge entry identifier value exposed by <see cref="ToolchainDiscoveryCandidate"/>.</value>
    public Guid KnowledgeEntryId { get; set; }
    /// <summary>Gets or sets the bounded structured environment variables relevant to this executable.</summary>
    /// <value>The environment variables value exposed by <see cref="ToolchainDiscoveryCandidate"/>.</value>
    public List<ToolchainEnvironmentVariableSetting> EnvironmentVariables { get; set; } = [];
}

/// <summary>Describes whether LocalGPT has knowledge for an exact discovered toolchain version.</summary>
public sealed class ToolchainVersionKnowledgeResult
{
    /// <summary>
    /// Gets or sets the stable profile key used to identify or correlate this toolchain version knowledge instance with related application state.
    /// </summary>
    /// <value>The profile key value exposed by <see cref="ToolchainVersionKnowledgeResult"/>.</value>
    public string ProfileKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the version value that forms part of the toolchain version knowledge state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The version value exposed by <see cref="ToolchainVersionKnowledgeResult"/>.</value>
    public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether knowledge applies to the toolchain version knowledge state.
    /// </summary>
    /// <value>The has knowledge value exposed by <see cref="ToolchainVersionKnowledgeResult"/>.</value>
    public bool HasKnowledge { get; set; }
    /// <summary>
    /// Gets or sets the stable knowledge entry identifier used to identify or correlate this toolchain version knowledge instance with related application state.
    /// </summary>
    /// <value>The knowledge entry identifier value exposed by <see cref="ToolchainVersionKnowledgeResult"/>.</value>
    public Guid? KnowledgeEntryId { get; set; }
    /// <summary>Gets or sets a bounded status for UI/API callers.</summary>
    /// <value>The status value exposed by <see cref="ToolchainVersionKnowledgeResult"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional Human Collaboration request identifier when missing knowledge was requested.</summary>
    /// <value>The human request identifier value exposed by <see cref="ToolchainVersionKnowledgeResult"/>.</value>
    public Guid? HumanRequestId { get; set; }
}

/// <summary>Describes a request to ask the local user for missing toolchain-version knowledge.</summary>
public sealed class ToolchainKnowledgeGapRequest
{
    /// <summary>
    /// Gets or sets the stable profile key used to identify or correlate this toolchain knowledge gap instance with related application state.
    /// </summary>
    /// <value>The profile key value exposed by <see cref="ToolchainKnowledgeGapRequest"/>.</value>
    public string ProfileKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the version value that forms part of the toolchain knowledge gap state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The version value exposed by <see cref="ToolchainKnowledgeGapRequest"/>.</value>
    public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the context value that forms part of the toolchain knowledge gap state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The context value exposed by <see cref="ToolchainKnowledgeGapRequest"/>.</value>
    public string Context { get; set; } = string.Empty;
}

/// <summary>Carries a stored toolchain identifier for validation or deletion operations.</summary>
public sealed class ToolchainInstallationActionRequest
{
    /// <summary>
    /// Gets or sets the stable compiler identifier used to identify or correlate this toolchain installation action instance with related application state.
    /// </summary>
    /// <value>The compiler identifier value exposed by <see cref="ToolchainInstallationActionRequest"/>.</value>
    public Guid CompilerId { get; set; }
    /// <summary>Gets or sets whether the local user explicitly confirmed the native or destructive operation.</summary>
    /// <value>The user confirmed value exposed by <see cref="ToolchainInstallationActionRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

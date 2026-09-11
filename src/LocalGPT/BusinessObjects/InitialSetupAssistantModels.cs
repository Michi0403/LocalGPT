using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Identifies the shell adapter used by LocalGPT's reviewable cross-platform console command service.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LocalConsoleShellKind
{
    /// <summary>Selects PowerShell on Windows and Bash on Unix-like systems.</summary>
    Auto,
    /// <summary>Executes an executable directly without a command shell.</summary>
    Direct,
    /// <summary>Runs the command through PowerShell or pwsh.</summary>
    PowerShell,
    /// <summary>Runs the command through Bash.</summary>
    Bash,
    /// <summary>Runs the command through Z shell when it is available.</summary>
    Zsh,
    /// <summary>Runs the command through a POSIX sh-compatible shell.</summary>
    Sh,
    /// <summary>Runs the command through Windows cmd.exe.</summary>
    Cmd
}

/// <summary>Represents one explicitly reviewable local command request.</summary>
public sealed class LocalConsoleCommandRequest
{
    /// <summary>
    /// Gets or sets the display name value that forms part of the local console command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="LocalConsoleCommandRequest"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the shell family used to run the request.</summary>
    /// <value>The shell value exposed by <see cref="LocalConsoleCommandRequest"/>.</value>
    public LocalConsoleShellKind Shell { get; set; } = LocalConsoleShellKind.Auto;
    /// <summary>
    /// Gets or sets the command text value that forms part of the local console command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The command text value exposed by <see cref="LocalConsoleCommandRequest"/>.</value>
    public string CommandText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the executable value that forms part of the local console command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The executable value exposed by <see cref="LocalConsoleCommandRequest"/>.</value>
    public string Executable { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the arguments collection maintained or exposed by this local console command instance for downstream processing.
    /// </summary>
    /// <value>The arguments value exposed by <see cref="LocalConsoleCommandRequest"/>.</value>
    public List<string> Arguments { get; set; } = [];
    /// <summary>
    /// Gets or sets the working directory used by this local console command instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The working directory value exposed by <see cref="LocalConsoleCommandRequest"/>.</value>
    public string WorkingDirectory { get; set; } = string.Empty;
    /// <summary>Gets or sets bounded environment overrides without replacing the inherited environment.</summary>
    /// <value>The environment value exposed by <see cref="LocalConsoleCommandRequest"/>.</value>
    public List<ToolchainEnvironmentVariableSetting> Environment { get; set; } = [];
    /// <summary>
    /// Gets or sets the timeout seconds value that forms part of the local console command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The timeout seconds value exposed by <see cref="LocalConsoleCommandRequest"/>.</value>
    public int TimeoutSeconds { get; set; } = 300;
    /// <summary>Gets or sets whether the command is a read-only discovery/listing probe that may run without consequential approval.</summary>
    /// <value>The is read only value exposed by <see cref="LocalConsoleCommandRequest"/>.</value>
    public bool IsReadOnly { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the local console command state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="LocalConsoleCommandRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>Represents one output event emitted by the shared LocalGPT command console.</summary>
public sealed class LocalConsoleOutputEvent
{
    /// <summary>
    /// Gets or sets the stable operation identifier used to identify or correlate this local console output event instance with related application state.
    /// </summary>
    /// <value>The operation identifier value exposed by <see cref="LocalConsoleOutputEvent"/>.</value>
    public Guid OperationId { get; set; }
    /// <summary>
    /// Gets or sets the timestamp UTC associated with this local console output event state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The timestamp UTC value exposed by <see cref="LocalConsoleOutputEvent"/>.</value>
    public DateTimeOffset TimestampUtc { get; set; }
    /// <summary>
    /// Gets or sets the display name value that forms part of the local console output event state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="LocalConsoleOutputEvent"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the stream name, such as stdout, stderr, system, or command.</summary>
    /// <value>The stream value exposed by <see cref="LocalConsoleOutputEvent"/>.</value>
    public string Stream { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the text value that forms part of the local console output event state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The text value exposed by <see cref="LocalConsoleOutputEvent"/>.</value>
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Represents the outcome of local console command, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class LocalConsoleCommandResult
{
    /// <summary>
    /// Gets or sets the stable operation identifier used to identify or correlate this local console command instance with related application state.
    /// </summary>
    /// <value>The operation identifier value exposed by <see cref="LocalConsoleCommandResult"/>.</value>
    public Guid OperationId { get; set; }
    /// <summary>Gets or sets whether the process completed with exit code zero.</summary>
    /// <value>The succeeded value exposed by <see cref="LocalConsoleCommandResult"/>.</value>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets the exit code value that forms part of the local console command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The exit code value exposed by <see cref="LocalConsoleCommandResult"/>.</value>
    public int? ExitCode { get; set; }
    /// <summary>
    /// Gets or sets the shell value that forms part of the local console command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The shell value exposed by <see cref="LocalConsoleCommandResult"/>.</value>
    public string Shell { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the standard output value that forms part of the local console command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The standard output value exposed by <see cref="LocalConsoleCommandResult"/>.</value>
    public string StandardOutput { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the standard error value that forms part of the local console command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The standard error value exposed by <see cref="LocalConsoleCommandResult"/>.</value>
    public string StandardError { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the local console command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="LocalConsoleCommandResult"/>.</value>
    public string Status { get; set; } = string.Empty;
}

/// <summary>Identifies an operator-level signal that can be sent to one active LocalGPT console process.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LocalConsoleSignalKind
{
    /// <summary>Requests an interrupt comparable to SIGINT on Unix-like hosts.</summary>
    Interrupt,
    /// <summary>Requests graceful termination comparable to SIGTERM on Unix-like hosts.</summary>
    Terminate,
    /// <summary>Requests an immediate kill comparable to SIGKILL or a process-tree kill.</summary>
    Kill,
    /// <summary>Requests a hangup comparable to SIGHUP on Unix-like hosts.</summary>
    Hangup
}

/// <summary>Describes one shell backend that LocalGPT can use behind its own ASCII operator surface.</summary>
public sealed class LocalConsoleShellDescriptor
{
    /// <summary>
    /// Gets or sets the shell value that forms part of the local console shell state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The shell kind exposed by <see cref="LocalConsoleShellDescriptor"/>.</value>
    public LocalConsoleShellKind Shell { get; set; }
    /// <summary>
    /// Gets or sets the display name value that forms part of the local console shell state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name exposed by <see cref="LocalConsoleShellDescriptor"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the resolved executable used for this shell backend.</summary>
    /// <value>The executable exposed by <see cref="LocalConsoleShellDescriptor"/>.</value>
    public string Executable { get; set; } = string.Empty;
    /// <summary>Gets or sets whether this shell is the host-preferred default.</summary>
    /// <value>The default-shell state exposed by <see cref="LocalConsoleShellDescriptor"/>.</value>
    public bool IsDefault { get; set; }
    /// <summary>Gets or sets the operator signals supported for processes on the current operating system.</summary>
    /// <value>The supported signal collection exposed by <see cref="LocalConsoleShellDescriptor"/>.</value>
    public List<LocalConsoleSignalKind> SupportedSignals { get; set; } = [];
}

/// <summary>Represents one currently running command behind LocalGPT's ASCII operator surface.</summary>
public sealed class LocalConsoleOperationSnapshot
{
    /// <summary>
    /// Gets or sets the stable operation identifier used to identify or correlate this local console operation snapshot instance with related application state.
    /// </summary>
    /// <value>The operation identifier exposed by <see cref="LocalConsoleOperationSnapshot"/>.</value>
    public Guid OperationId { get; set; }
    /// <summary>
    /// Gets or sets the display name value that forms part of the local console operation snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name exposed by <see cref="LocalConsoleOperationSnapshot"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the operating-system process identifier when the process has started.</summary>
    /// <value>The process identifier exposed by <see cref="LocalConsoleOperationSnapshot"/>.</value>
    public int? ProcessId { get; set; }
    /// <summary>
    /// Gets or sets the shell value that forms part of the local console operation snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The shell exposed by <see cref="LocalConsoleOperationSnapshot"/>.</value>
    public LocalConsoleShellKind Shell { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the local console operation snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status exposed by <see cref="LocalConsoleOperationSnapshot"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the started UTC associated with this local console operation snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The start timestamp exposed by <see cref="LocalConsoleOperationSnapshot"/>.</value>
    public DateTimeOffset StartedUtc { get; set; }
}

/// <summary>Represents one line submitted to the reusable LocalGPT ASCII operator command layer.</summary>
public sealed class LocalConsoleOperatorRequest
{
    /// <summary>
    /// Gets or sets the input value that forms part of the local console operator state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The input text exposed by <see cref="LocalConsoleOperatorRequest"/>.</value>
    public string Input { get; set; } = string.Empty;
    /// <summary>Gets or sets the currently selected shell for ordinary command lines.</summary>
    /// <value>The shell exposed by <see cref="LocalConsoleOperatorRequest"/>.</value>
    public LocalConsoleShellKind Shell { get; set; } = LocalConsoleShellKind.Auto;
    /// <summary>Gets or sets the working directory used for ordinary command lines.</summary>
    /// <value>The working directory exposed by <see cref="LocalConsoleOperatorRequest"/>.</value>
    public string WorkingDirectory { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the human explicitly submitted this consequential line.</summary>
    /// <value>The confirmation state exposed by <see cref="LocalConsoleOperatorRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>Represents the immediate result of parsing one ASCII operator line.</summary>
public sealed class LocalConsoleOperatorResult
{
    /// <summary>
    /// Gets or sets a value indicating whether accepted applies to the local console operator state.
    /// </summary>
    /// <value>The accepted state exposed by <see cref="LocalConsoleOperatorResult"/>.</value>
    public bool Accepted { get; set; }
    /// <summary>Gets or sets the shell that should remain selected after the line is processed.</summary>
    /// <value>The selected shell exposed by <see cref="LocalConsoleOperatorResult"/>.</value>
    public LocalConsoleShellKind SelectedShell { get; set; } = LocalConsoleShellKind.Auto;
    /// <summary>Gets or sets the working directory retained by the current ASCII operator session.</summary>
    /// <value>The working directory exposed by <see cref="LocalConsoleOperatorResult"/>.</value>
    public string WorkingDirectory { get; set; } = string.Empty;
    /// <summary>Gets or sets a bounded human-readable response suitable for the ASCII control row.</summary>
    /// <value>The message exposed by <see cref="LocalConsoleOperatorResult"/>.</value>
    public string Message { get; set; } = string.Empty;
    /// <summary>Gets or sets the started operation identifier when an ordinary shell command was queued.</summary>
    /// <value>The operation identifier exposed by <see cref="LocalConsoleOperatorResult"/>.</value>
    public Guid? OperationId { get; set; }
}

/// <summary>Represents one GPU/device candidate used by the first-run hardware assistant.</summary>
public sealed class InitialSetupHardwareDevice
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this initial setup hardware device instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="InitialSetupHardwareDevice"/>.</value>
    public string Key { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>Gets or sets the provider endpoint used to resolve the physical host that owns this device.</summary>
    /// <value>The endpoint value exposed by <see cref="InitialSetupHardwareDevice"/>.</value>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>Gets or sets the normalized physical-host key when already known.</summary>
    /// <value>The host key value exposed by <see cref="InitialSetupHardwareDevice"/>.</value>
    public string HostKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the detected or reviewed CPU/model name for the physical host.</summary>
    /// <value>The CPU/model identity; it remains separate from accelerator rows.</value>
    public string CpuName { get; set; } = string.Empty;
    /// <summary>Gets or sets the user-reviewed CanIRun.ai hardware label when its catalog uses a different accelerator variant name.</summary>
    /// <value>The external hardware-catalog label; an empty value means use the local accelerator name.</value>
    public string CanIRunHardwareName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the name value that forms part of the initial setup hardware device state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="InitialSetupHardwareDevice"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the vendor value that forms part of the initial setup hardware device state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The vendor value exposed by <see cref="InitialSetupHardwareDevice"/>.</value>
    public string Vendor { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the dedicated vram gi b value that forms part of the initial setup hardware device state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The dedicated vram gi b value exposed by <see cref="InitialSetupHardwareDevice"/>.</value>
    public double? DedicatedVramGiB { get; set; }
    /// <summary>Gets or sets total system/unified memory in GiB for compatibility requests and reviewed host persistence.</summary>
    /// <value>Total system memory in GiB, or <see langword="null"/> when it has not been detected or entered.</value>
    public double? SystemMemoryGiB { get; set; }
    /// <summary>
    /// Gets or sets the source value that forms part of the initial setup hardware device state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source value exposed by <see cref="InitialSetupHardwareDevice"/>.</value>
    public string Source { get; set; } = "Manual";
    /// <summary>Gets or sets whether the device participates in recommendation and benchmark setup.</summary>
    /// <value>The selected value exposed by <see cref="InitialSetupHardwareDevice"/>.</value>
    public bool Selected { get; set; } = true;
    /// <summary>Retains the legacy CanIRun.ai device slug field for backwards-compatible request deserialization; 3.9+ hardware lookups use explicit hardware facts instead.</summary>
    /// <value>The legacy device slug value, which is no longer required by the setup UI.</value>
    public string CanIRunSlug { get; set; } = string.Empty;
}

/// <summary>Represents one model recommendation parsed from an explicitly requested CanIRun.ai JSON hardware recommendation response.</summary>
public sealed class CanIRunModelRecommendation
{
    /// <summary>
    /// Gets or sets the stable model identifier used to identify or correlate this can i run model recommendation instance with related application state.
    /// </summary>
    /// <value>The model identifier value exposed by <see cref="CanIRunModelRecommendation"/>.</value>
    public string ModelId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the model name value that forms part of the can i run model recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model name value exposed by <see cref="CanIRunModelRecommendation"/>.</value>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the grade value that forms part of the can i run model recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The grade value exposed by <see cref="CanIRunModelRecommendation"/>.</value>
    public string Grade { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the can i run model recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="CanIRunModelRecommendation"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the score value that forms part of the can i run model recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The score value exposed by <see cref="CanIRunModelRecommendation"/>.</value>
    public int Score { get; set; }
    /// <summary>Gets or sets the selected quantization name when exposed by the source page.</summary>
    /// <value>The quantization value exposed by <see cref="CanIRunModelRecommendation"/>.</value>
    public string Quantization { get; set; } = string.Empty;
    /// <summary>Gets or sets the selected quantization VRAM requirement in GiB when available.</summary>
    /// <value>The required vram gi b value exposed by <see cref="CanIRunModelRecommendation"/>.</value>
    public double? RequiredVramGiB { get; set; }
    /// <summary>Gets or sets the model provider/publisher reported by CanIRun.ai.</summary>
    /// <value>The publisher value exposed by <see cref="CanIRunModelRecommendation"/>.</value>
    public string Publisher { get; set; } = string.Empty;
    /// <summary>Gets or sets the exact Ollama model token when CanIRun.ai exposes provider-specific install metadata.</summary>
    /// <value>The Ollama model identifier, or an empty string when the response did not provide one.</value>
    public string OllamaModelId { get; set; } = string.Empty;
    /// <summary>Gets or sets the exact LM Studio catalog identifier when CanIRun.ai exposes provider-specific install metadata.</summary>
    /// <value>The LM Studio model identifier, or an empty string when the response did not provide one.</value>
    public string LmStudioModelId { get; set; } = string.Empty;
    /// <summary>Gets or sets the source hardware label retained in the legacy device-slug field for backwards-compatible serialization.</summary>
    /// <value>The reviewed accelerator name associated with this recommendation.</value>
    public string DeviceSlug { get; set; } = string.Empty;
    /// <summary>Gets or sets the local provider endpoint/physical-host route whose accelerator produced this compatibility lookup.</summary>
    /// <value>The LocalGPT endpoint associated with the source hardware row; it is never sent to CanIRun.ai.</value>
    public string DeviceEndpoint { get; set; } = string.Empty;
    /// <summary>Gets or sets the normalized local physical-host key associated with the source accelerator.</summary>
    /// <value>The LocalGPT host key used to keep recommendations from different machines separate.</value>
    public string HostKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source URL that identifies the network or application endpoint associated with this can i run model recommendation state.
    /// </summary>
    /// <value>The source URL value exposed by <see cref="CanIRunModelRecommendation"/>.</value>
    public string SourceUrl { get; set; } = string.Empty;
}

/// <summary>Represents one knowledge-backed AI provider installation profile for the current operating system.</summary>
public sealed class AiProviderBootstrapProfile
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this AI provider bootstrap profile instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="AiProviderBootstrapProfile"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the AI provider bootstrap profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="AiProviderBootstrapProfile"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the provider kind value that forms part of the AI provider bootstrap profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The provider kind value exposed by <see cref="AiProviderBootstrapProfile"/>.</value>
    public string ProviderKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the platform value that forms part of the AI provider bootstrap profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The platform value exposed by <see cref="AiProviderBootstrapProfile"/>.</value>
    public string Platform { get; set; } = string.Empty;
    /// <summary>Gets or sets the shell adapter used by the commands in this platform-specific profile.</summary>
    /// <value>The shell value exposed by <see cref="AiProviderBootstrapProfile"/>.</value>
    public LocalConsoleShellKind Shell { get; set; } = LocalConsoleShellKind.Auto;
    /// <summary>
    /// Gets or sets the endpoint that identifies the network or application endpoint associated with this AI provider bootstrap profile state.
    /// </summary>
    /// <value>The endpoint value exposed by <see cref="AiProviderBootstrapProfile"/>.</value>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>Gets or sets the knowledge/source URL shown to the user.</summary>
    /// <value>The source URL value exposed by <see cref="AiProviderBootstrapProfile"/>.</value>
    public string SourceUrl { get; set; } = string.Empty;
    /// <summary>Gets or sets the official provider model catalog URL shown independently from CanIRun.ai recommendations.</summary>
    /// <value>The provider-owned model catalog/search URL.</value>
    public string ModelCatalogUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the detect command value that forms part of the AI provider bootstrap profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The detect command value exposed by <see cref="AiProviderBootstrapProfile"/>.</value>
    public string DetectCommand { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the install command value that forms part of the AI provider bootstrap profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The install command value exposed by <see cref="AiProviderBootstrapProfile"/>.</value>
    public string InstallCommand { get; set; } = string.Empty;
    /// <summary>Gets or sets the knowledge-backed provider update command. An empty value falls back to the install command for backward-compatible profiles.</summary>
    /// <value>The provider update command exposed by <see cref="AiProviderBootstrapProfile"/>.</value>
    public string UpdateCommand { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the start command value that forms part of the AI provider bootstrap profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The start command value exposed by <see cref="AiProviderBootstrapProfile"/>.</value>
    public string StartCommand { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the list models command value that forms part of the AI provider bootstrap profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The list models command value exposed by <see cref="AiProviderBootstrapProfile"/>.</value>
    public string ListModelsCommand { get; set; } = string.Empty;
    /// <summary>Gets or sets the reviewable model-install command template containing {{model}}.</summary>
    /// <value>The install model command template value exposed by <see cref="AiProviderBootstrapProfile"/>.</value>
    public string InstallModelCommandTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets user-editable generic-model to provider-model aliases.</summary>
    /// <value>The model aliases value exposed by <see cref="AiProviderBootstrapProfile"/>.</value>
    public Dictionary<string, string> ModelAliases { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Represents one provider/model installation choice in the first-run setup assistant.</summary>
public sealed class InitialSetupModelChoice
{
    /// <summary>
    /// Gets or sets the stable recommendation identifier used to identify or correlate this initial setup model choice instance with related application state.
    /// </summary>
    /// <value>The recommendation identifier value exposed by <see cref="InitialSetupModelChoice"/>.</value>
    public string RecommendationId { get; set; } = string.Empty;
    /// <summary>Gets or sets the provider-specific model identifier used for installation.</summary>
    /// <value>The provider model identifier value exposed by <see cref="InitialSetupModelChoice"/>.</value>
    public string ProviderModelId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the initial setup model choice state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="InitialSetupModelChoice"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the provider-qualified LocalGPT selection key when the model is currently installed.</summary>
    /// <value>The selection key value exposed by <see cref="InitialSetupModelChoice"/>.</value>
    public string SelectionKey { get; set; } = string.Empty;
    /// <summary>Gets or sets whether LocalGPT has a provider-safe one-click installation mapping for this recommendation.</summary>
    /// <value><see langword="true"/> when the provider model identifier can be used for an explicit install action.</value>
    public bool CanInstall { get; set; } = true;
    /// <summary>Gets or sets a short mapping note when a recommendation is visible but not safely one-click installable.</summary>
    /// <value>The mapping status shown beside the recommendation.</value>
    public string MappingStatus { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the selected provider already exposes this model.</summary>
    /// <value>The is installed value exposed by <see cref="InitialSetupModelChoice"/>.</value>
    public bool IsInstalled { get; set; }
    /// <summary>Gets or sets whether the row originated from the selected provider's installed/runtime inventory.</summary>
    /// <value><see langword="true"/> for provider-discovered local models.</value>
    public bool IsProviderInventory { get; set; }
    /// <summary>Gets or sets whether the row originated from an explicit search of the selected provider's official public model catalog.</summary>
    /// <value><see langword="true"/> for provider-owned online catalog results.</value>
    public bool IsProviderCatalogEntry { get; set; }
    /// <summary>Gets or sets whether CanIRun.ai identified this model as a hardware-fit discovery for the reviewed host.</summary>
    /// <value><see langword="true"/> when attributed CanIRun.ai recommendation data is attached.</value>
    public bool IsHardwareRecommended { get; set; }
    /// <summary>Gets or sets whether the selected provider can safely re-run its install/pull operation to check or refresh an installed model.</summary>
    /// <value><see langword="true"/> when an explicit provider-safe update/check action is available.</value>
    public bool CanCheckUpdate { get; set; }
    /// <summary>Gets or sets the official provider catalog URL for discovering additional installable models outside CanIRun.ai.</summary>
    /// <value>The provider-owned model-detail URL when exact, otherwise a provider-owned search/catalog URL.</value>
    public string ProviderCatalogUrl { get; set; } = string.Empty;
    /// <summary>Gets or sets whether <see cref="ProviderCatalogUrl"/> identifies an exact provider model page rather than a broader catalog/search page.</summary>
    /// <value><see langword="true"/> when the provider link resolves directly to this provider model identifier.</value>
    public bool IsProviderCatalogUrlExact { get; set; }
    /// <summary>
    /// Gets or sets the recommendation score value that forms part of the initial setup model choice state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recommendation score value exposed by <see cref="InitialSetupModelChoice"/>.</value>
    public int RecommendationScore { get; set; }
    /// <summary>
    /// Gets or sets the recommendation grade value that forms part of the initial setup model choice state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recommendation grade value exposed by <see cref="InitialSetupModelChoice"/>.</value>
    public string RecommendationGrade { get; set; } = string.Empty;
    /// <summary>Gets or sets the attributed CanIRun.ai model-detail URL for this recommendation.</summary>
    /// <value>The source URL retained with the recommendation.</value>
    public string SourceUrl { get; set; } = string.Empty;
    /// <summary>Gets or sets the recommended quantization returned by CanIRun.ai when available.</summary>
    /// <value>The recommended quantization label.</value>
    public string Quantization { get; set; } = string.Empty;
    /// <summary>Gets or sets the estimated memory requirement for the recommended quantization.</summary>
    /// <value>The required memory in GiB, or <see langword="null"/> when unavailable.</value>
    public double? RequiredVramGiB { get; set; }
    /// <summary>Carries the CanIRun.ai compatibility classification displayed beside this provider-mapped model choice.</summary>
    /// <value>The recommendation status label.</value>
    public string RecommendationStatus { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the user selected the model for installation/benchmark setup.</summary>
    /// <value>The selected value exposed by <see cref="InitialSetupModelChoice"/>.</value>
    public bool Selected { get; set; }
}


/// <summary>Represents one explicit provider-catalog resolution attempt for a hardware-fit recommendation.</summary>
public sealed class InitialSetupProviderCatalogResolution
{
    /// <summary>Gets or sets the unique conservative provider match when one was found.</summary>
    /// <value>The provider-safe matched choice, or <see langword="null"/> when the result is ambiguous or absent.</value>
    public InitialSetupModelChoice? Match { get; set; }
    /// <summary>Gets or sets the bounded provider-owned catalog candidates inspected for this explicit resolution action.</summary>
    /// <value>The provider catalog candidates retained for user review.</value>
    public List<InitialSetupModelChoice> Candidates { get; set; } = [];
    /// <summary>Gets or sets the bounded provider search query derived from the recommendation identity.</summary>
    /// <value>The provider catalog search query used for this resolution attempt.</value>
    public string Query { get; set; } = string.Empty;
}

/// <summary>
/// Represents an initial setup assistant snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class InitialSetupAssistantSnapshot
{
    /// <summary>
    /// Gets or sets the hardware collection maintained or exposed by this initial setup assistant snapshot instance for downstream processing.
    /// </summary>
    /// <value>The hardware value exposed by <see cref="InitialSetupAssistantSnapshot"/>.</value>
    public List<InitialSetupHardwareDevice> Hardware { get; set; } = [];
    /// <summary>Gets or sets knowledge-backed provider installation profiles available on this platform.</summary>
    /// <value>The provider profiles value exposed by <see cref="InitialSetupAssistantSnapshot"/>.</value>
    public List<AiProviderBootstrapProfile> ProviderProfiles { get; set; } = [];
    /// <summary>
    /// Gets or sets the installed models collection maintained or exposed by this initial setup assistant snapshot instance for downstream processing.
    /// </summary>
    /// <value>The installed models value exposed by <see cref="InitialSetupAssistantSnapshot"/>.</value>
    public List<ProviderModelReference> InstalledModels { get; set; } = [];
    /// <summary>Gets or sets the strongest installed provider-qualified model keys recommended by the shared benchmark reviewer policy for curator/reviewer roles.</summary>
    /// <value>The recommended curator model keys value exposed by <see cref="InitialSetupAssistantSnapshot"/>.</value>
    public List<string> RecommendedCuratorModelKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets the platform value that forms part of the initial setup assistant snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The platform value exposed by <see cref="InitialSetupAssistantSnapshot"/>.</value>
    public string Platform { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the legacy first-run guide was already marked reviewed.</summary>
    /// <value>The is onboarding completed value exposed by <see cref="InitialSetupAssistantSnapshot"/>.</value>
    public bool IsOnboardingCompleted { get; set; }
    /// <summary>Gets or sets whether at least one installed provider-qualified model is available to run the AI-guided setup Council.</summary>
    /// <value><see langword="true"/> when the maintained initial-setup Council can be started locally.</value>
    public bool CanStartAiGuidedSetup { get; set; }
    /// <summary>Gets or sets the maintained Chat route that starts the AI-guided initial-setup Council when a local model is available.</summary>
    /// <value>The local application route for the initial-setup Council; no external URL is produced.</value>
    public string AiGuidedSetupRoute { get; set; } = "/chat?team=initial-setup-assistant&starter=initial-setup-council-start&autoStartCouncil=true&newCouncil=true";
}

/// <summary>Represents a request to create a user-owned hardware-curated initial benchmark team.</summary>
public sealed class CreateInitialBenchmarkTeamRequest
{
    /// <summary>Gets or sets the provider-qualified model keys selected for the new team.</summary>
    /// <value>The model selection keys value exposed by <see cref="CreateInitialBenchmarkTeamRequest"/>.</value>
    public List<string> ModelSelectionKeys { get; set; } = [];
    /// <summary>Gets or sets the ordered subset of stronger provider-qualified models that should own curator/reviewer roles.</summary>
    /// <value>The preferred curator model keys value exposed by <see cref="CreateInitialBenchmarkTeamRequest"/>.</value>
    public List<string> PreferredCuratorModelKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets the display name value that forms part of the create initial benchmark team state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="CreateInitialBenchmarkTeamRequest"/>.</value>
    public string DisplayName { get; set; } = "Hardware-curated initial benchmark";
    /// <summary>Gets or sets whether the exact creation was confirmed by the user.</summary>
    /// <value>The user confirmed value exposed by <see cref="CreateInitialBenchmarkTeamRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Stores LocalGPT-owned launch defaults for the local Ollama runtime without changing independently configured remote Ollama hosts.</summary>
public sealed class OllamaRuntimeManagementOptions
{
    /// <summary>Gets or sets the directory passed to Ollama through <c>OLLAMA_MODELS</c>; an empty value keeps the provider default.</summary>
    /// <value>The optional model-store directory used when LocalGPT starts Ollama.</value>
    public string ModelDirectory { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the bind address that identifies the network or application endpoint associated with this Ollama runtime management state.
    /// </summary>
    /// <value>The server bind address; loopback is the safe default.</value>
    public string BindAddress { get; set; } = "127.0.0.1";
    /// <summary>Gets or sets the TCP port used to build <c>OLLAMA_HOST</c>.</summary>
    /// <value>The Ollama server port.</value>
    public int Port { get; set; } = 11434;
    /// <summary>Gets or sets the default Ollama context length; zero keeps the provider default.</summary>
    /// <value>The context length in tokens, or zero for provider-managed behavior.</value>
    public int ContextLengthTokens { get; set; }
    /// <summary>Gets or sets the Ollama keep-alive value; an empty value keeps the provider default.</summary>
    /// <value>A provider duration such as <c>5m</c>, <c>0</c>, or <c>-1</c>.</value>
    public string KeepAlive { get; set; } = string.Empty;
    /// <summary>Gets or sets the maximum simultaneously loaded Ollama models; zero keeps the provider default.</summary>
    /// <value>The maximum loaded model count, or zero for provider-managed behavior.</value>
    public int MaxLoadedModels { get; set; }
    /// <summary>Gets or sets the maximum parallel requests per loaded Ollama model; zero keeps the provider default.</summary>
    /// <value>The per-model parallel-request limit, or zero for provider-managed behavior.</value>
    public int ParallelRequests { get; set; }
    /// <summary>Gets or sets the maximum Ollama request queue length; zero keeps the provider default.</summary>
    /// <value>The queue length, or zero for provider-managed behavior.</value>
    public int MaxQueue { get; set; }
    /// <summary>Gets or sets whether LocalGPT-started Ollama runs with cloud features disabled.</summary>
    /// <value><see langword="true"/> to set <c>OLLAMA_NO_CLOUD=1</c>.</value>
    public bool DisableCloud { get; set; }
}

/// <summary>Stores LocalGPT-owned LM Studio CLI/server defaults while leaving the LM Studio model directory under LM Studio's supported My Models workflow.</summary>
public sealed class LmStudioRuntimeManagementOptions
{
    /// <summary>Gets or sets the address used by <c>lms server start --bind</c>.</summary>
    /// <value>The server bind address; loopback is the safe default.</value>
    public string BindAddress { get; set; } = "127.0.0.1";
    /// <summary>Gets or sets the port used by <c>lms server start --port</c>.</summary>
    /// <value>The LM Studio local-server port.</value>
    public int Port { get; set; } = 1234;
    /// <summary>Gets or sets whether LocalGPT asks the LM Studio server to enable CORS.</summary>
    /// <value><see langword="true"/> when CORS should be enabled during a LocalGPT-started server session.</value>
    public bool EnableCors { get; set; }
    /// <summary>Gets or sets the default context length used by LocalGPT's LM Studio load action; zero lets LM Studio decide.</summary>
    /// <value>The context length in tokens, or zero for automatic behavior.</value>
    public int ContextLengthTokens { get; set; }
    /// <summary>Gets or sets the GPU offload value accepted by <c>lms load --gpu</c>.</summary>
    /// <value><c>auto</c>, <c>off</c>, <c>max</c>, or a fractional value between zero and one.</value>
    public string GpuOffload { get; set; } = "auto";
    /// <summary>Gets or sets the idle TTL used by LocalGPT's LM Studio load action; zero leaves TTL unspecified.</summary>
    /// <value>The idle time-to-live in seconds, or zero for provider-managed behavior.</value>
    public int TtlSeconds { get; set; }
}

/// <summary>Describes one installed local-provider model together with disk and memory lifecycle capabilities exposed by the provider.</summary>
public sealed class ProviderManagedModelInfo
{
    /// <summary>Gets or sets the provider-native model key used for lifecycle operations.</summary>
    /// <value>The provider model key.</value>
    public string ModelId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the provider managed model info state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model display name.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the size bytes value that forms part of the provider managed model info state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model size in bytes when known.</value>
    public long? SizeBytes { get; set; }
    /// <summary>
    /// Gets or sets the parameter size that quantifies the associated provider managed model info data.
    /// </summary>
    /// <value>A value such as <c>7B</c> when available.</value>
    public string ParameterSize { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the architecture value that forms part of the provider managed model info state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model architecture when available.</value>
    public string Architecture { get; set; } = string.Empty;
    /// <summary>Gets or sets the provider-reported maximum context length when available from machine-readable inventory.</summary>
    /// <value>The maximum context length in tokens, or <see langword="null"/> when the provider does not report it.</value>
    public int? MaxContextLengthTokens { get; set; }
    /// <summary>Gets or sets whether the provider currently reports this model as loaded in memory.</summary>
    /// <value><see langword="true"/> when the model is loaded.</value>
    public bool IsLoaded { get; set; }
    /// <summary>Gets or sets whether LocalGPT can unload this model through a documented provider operation.</summary>
    /// <value><see langword="true"/> when unload is supported.</value>
    public bool CanUnload { get; set; }
    /// <summary>Gets or sets whether LocalGPT can permanently remove this model through a documented provider operation.</summary>
    /// <value><see langword="true"/> when permanent deletion is supported.</value>
    public bool CanDelete { get; set; }
    /// <summary>Gets or sets an optional provider-relative path reported for the downloaded model.</summary>
    /// <value>The provider-relative path; LocalGPT does not use this value as implicit deletion authority.</value>
    public string ProviderPath { get; set; } = string.Empty;
}

/// <summary>Captures one provider-management refresh used by the DevExpress runtime and disk-management workbench.</summary>
public sealed class ProviderRuntimeManagementSnapshot
{
    /// <summary>
    /// Gets or sets the stable profile key used to identify or correlate this provider runtime management snapshot instance with related application state.
    /// </summary>
    /// <value>The provider profile key.</value>
    public string ProfileKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the provider name value that forms part of the provider runtime management snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The provider display name.</value>
    public string ProviderName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether Ollama applies to the provider runtime management snapshot state.
    /// </summary>
    /// <value><see langword="true"/> for an Ollama profile.</value>
    public bool IsOllama { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether lm studio applies to the provider runtime management snapshot state.
    /// </summary>
    /// <value><see langword="true"/> for an LM Studio profile.</value>
    public bool IsLmStudio { get; set; }
    /// <summary>Gets or sets the effective or provider-managed model directory shown to the user.</summary>
    /// <value>The model-directory description or path.</value>
    public string ModelDirectory { get; set; } = string.Empty;
    /// <summary>Gets or sets whether LocalGPT may persist the model-directory setting for this provider.</summary>
    /// <value><see langword="true"/> when the directory is directly configurable through LocalGPT.</value>
    public bool ModelDirectoryEditable { get; set; }
    /// <summary>
    /// Gets or sets the model directory guidance used by this provider runtime management snapshot instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The storage-management guidance displayed by the workbench.</value>
    public string ModelDirectoryGuidance { get; set; } = string.Empty;
    /// <summary>Gets or sets the summed provider-reported disk usage for downloaded models.</summary>
    /// <value>The aggregate model size in bytes.</value>
    public long InstalledModelBytes { get; set; }
    /// <summary>Gets or sets available bytes on the filesystem that owns the model directory when LocalGPT can resolve it safely.</summary>
    /// <value>The available bytes, or <see langword="null"/> when the provider owns directory resolution.</value>
    public long? DiskFreeBytes { get; set; }
    /// <summary>Gets or sets total bytes on the filesystem that owns the model directory when LocalGPT can resolve it safely.</summary>
    /// <value>The total bytes, or <see langword="null"/> when the provider owns directory resolution.</value>
    public long? DiskTotalBytes { get; set; }
    /// <summary>Gets or sets LocalGPT's database-backed default maximum output tokens.</summary>
    /// <value>The LocalGPT default output-token budget.</value>
    public int DefaultMaxOutputTokens { get; set; }
    /// <summary>Gets or sets LocalGPT's database-backed default context-token budget.</summary>
    /// <value>The LocalGPT default context-token budget.</value>
    public int DefaultContextTokens { get; set; }
    /// <summary>Gets or sets the detached Ollama launch settings shown in the workbench.</summary>
    /// <value>The Ollama runtime settings.</value>
    public OllamaRuntimeManagementOptions Ollama { get; set; } = new();
    /// <summary>Gets or sets the detached LM Studio load/server settings shown in the workbench.</summary>
    /// <value>The LM Studio runtime settings.</value>
    public LmStudioRuntimeManagementOptions LmStudio { get; set; } = new();
    /// <summary>
    /// Gets or sets the models collection maintained or exposed by this provider runtime management snapshot instance for downstream processing.
    /// </summary>
    /// <value>The downloaded model collection.</value>
    public List<ProviderManagedModelInfo> Models { get; set; } = [];
    /// <summary>Gets or sets a bounded status note produced during inventory refresh.</summary>
    /// <value>The provider-management status text.</value>
    public string Status { get; set; } = string.Empty;
}

/// <summary>Describes one provider model action result that may carry resource-estimation text in addition to success state.</summary>
public sealed class ProviderModelManagementResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded applies to the provider model management state.
    /// </summary>
    /// <value><see langword="true"/> when the operation succeeded.</value>
    public bool Succeeded { get; set; }
    /// <summary>Gets or sets bounded human-readable provider output intended for the management UI.</summary>
    /// <value>The action result text.</value>
    public string Message { get; set; } = string.Empty;
}

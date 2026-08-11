namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents an aicore options.
    /// </summary>
    public class AICoreOptions
    {
        /// <summary>
        /// Stores aicore.
        /// </summary>
        public const string AICore = "AICore";

        /// <summary>
        /// Gets or sets ollama core.
        /// </summary>
        public OllamaCoreOptions OllamaCore { get; set; } = new();
        /// <summary>
        /// Gets or sets ollama cores.
        /// </summary>
        public List<OllamaCoreOptions> OllamaCores { get; set; } = new(); // Additional independently reachable Ollama hosts/preferred models
        /// <summary>
        /// Gets or sets open aiservice core.
        /// </summary>
        public OpenAIServiceCoreOptions OpenAIServiceCore { get; set; } = new(); // Azure OpenAI
        /// <summary>
        /// Gets or sets chat gptlocal core.
        /// </summary>
        public ChatGPTLocalCoreOptions ChatGPTLocalCore { get; set; } = new();   // Primary OpenAI-compatible (LM Studio/vLLM/etc.)
        /// <summary>
        /// Gets or sets chat gptlocal cores.
        /// </summary>
        public List<ChatGPTLocalCoreOptions> ChatGPTLocalCores { get; set; } = new(); // Additional independently reachable OpenAI-compatible hosts/models
        /// <summary>
        /// Gets or sets open aicore.
        /// </summary>
        public OpenAICompatOptions OpenAICore { get; set; } = new();             // OpenAI cloud (api.openai.com)
    }

    /// <summary>
    /// Represents a chat gptlocal core options.
    /// </summary>
    public class ChatGPTLocalCoreOptions
    {
        /// <summary>
        /// Stores chat gptlocal core.
        /// </summary>
        public const string ChatGPTLocalCore = "ChatGPTLocalCore";
        /// <summary>
        /// Gets or sets endpoint.
        /// </summary>
        public string Endpoint { get; set; } = "http://localhost:11434/v1";
        /// <summary>
        /// Gets or sets API key.
        /// </summary>
        public string ApiKey { get; set; } = "local-no-key";
        /// <summary>
        /// Gets or sets model name.
        /// </summary>
        public string ModelName { get; set; } = "gpt-oss:20b";
        /// <summary>
        /// Gets or sets auto start server.
        /// </summary>
        public bool AutoStartServer { get; set; } = false;
        /// <summary>
        /// Gets or sets python environment.
        /// </summary>
        public string? PythonEnvironment { get; set; }
        /// <summary>
        /// Gets or sets start script.
        /// </summary>
        public string? StartScript { get; set; }  // e.g. path to run_gpt_oss_server.py
        /// <summary>
        /// Gets or sets working dir.
        /// </summary>
        public string? WorkingDir { get; set; }   // directory where model + script live
        /// <summary>
        /// Gets or sets start command.
        /// </summary>
        public string? StartCommand { get; set; }
        /// <summary>
        /// Gets or sets health timeout seconds.
        /// </summary>
        public int HealthTimeoutSeconds { get; set; } = 45;
    }

    /// <summary>
    /// Represents an open aiservice core options.
    /// </summary>
    public class OpenAIServiceCoreOptions // Azure OpenAI
    {
        /// <summary>
        /// Stores open aiservice core.
        /// </summary>
        public const string OpenAIServiceCore = "OpenAIServiceCore";
        /// <summary>
        /// Gets or sets endpoint.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets key.
        /// </summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets deployment name.
        /// </summary>
        public string DeploymentName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents an open aicompat options.
    /// </summary>
    public class OpenAICompatOptions // OpenAI cloud (or any OpenAI-compatible)
    {
        /// <summary>
        /// Stores open aicore.
        /// </summary>
        public const string OpenAICore = "OpenAICore";
        /// <summary>
        /// Gets or sets endpoint.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets API key.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets model name.
        /// </summary>
        public string ModelName { get; set; } = "gpt-4o-mini";
    }

    /// <summary>
    /// Represents an ollama core options.
    /// </summary>
    public class OllamaCoreOptions
    {
        /// <summary>
        /// Stores ollama core.
        /// </summary>
        public const string OllamaCore = "OllamaCore";
        /// <summary>
        /// Gets or sets URI.
        /// </summary>
        public string Uri { get; set; } = "http://localhost:11434";
        /// <summary>
        /// Gets or sets model name.
        /// </summary>
        public string ModelName { get; set; } = "gpt-oss:20b";
        /// <summary>
        /// Gets or sets response protocol.
        /// </summary>
        public ChatResponseProtocol ResponseProtocol { get; set; } = ChatResponseProtocol.Auto;
    }

    /// <summary>
    /// Represents a local ai host discovery result.
    /// </summary>
    public sealed class LocalAiHostDiscoveryResult
    {
        /// <summary>
        /// Gets or sets provider.
        /// </summary>
        public string Provider { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets endpoint.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets is reachable.
        /// </summary>
        public bool IsReachable { get; set; }
        /// <summary>
        /// Gets or sets status.
        /// </summary>
        public string Status { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets models.
        /// </summary>
        public List<LocalAiModelInfo> Models { get; set; } = new();
    }

    /// <summary>
    /// Represents a local ai model info.
    /// </summary>
    public sealed class LocalAiModelInfo
    {
        /// <summary>
        /// Gets or sets name.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets is loaded.
        /// </summary>
        public bool IsLoaded { get; set; }
        /// <summary>
        /// Gets or sets details.
        /// </summary>
        public string? Details { get; set; }
    }
}

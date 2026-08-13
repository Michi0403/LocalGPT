namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Carries the configurable AI core settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    public class AICoreOptions
    {
        /// <summary>
        /// Defines the AI core constant used by <see cref="AICoreOptions"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const string AICore = "AICore";

        /// <summary>
        /// Gets or sets the Ollama core value that forms part of the AI core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The Ollama core value exposed by <see cref="AICoreOptions"/>.</value>
        public OllamaCoreOptions OllamaCore { get; set; } = new();
        /// <summary>
        /// Gets or sets the Ollama cores collection maintained or exposed by this AI core instance for downstream processing.
        /// </summary>
        /// <value>The Ollama cores value exposed by <see cref="AICoreOptions"/>.</value>
        public List<OllamaCoreOptions> OllamaCores { get; set; } = new(); // Additional independently reachable Ollama hosts/preferred models
        /// <summary>
        /// Gets or sets the OpenAI service core value that forms part of the AI core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The OpenAI service core value exposed by <see cref="AICoreOptions"/>.</value>
        public OpenAIServiceCoreOptions OpenAIServiceCore { get; set; } = new(); // Azure OpenAI
        /// <summary>
        /// Gets or sets the ChatGPT local core value that forms part of the AI core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The ChatGPT local core value exposed by <see cref="AICoreOptions"/>.</value>
        public ChatGPTLocalCoreOptions ChatGPTLocalCore { get; set; } = new();   // Primary OpenAI-compatible (LM Studio/vLLM/etc.)
        /// <summary>
        /// Gets or sets the ChatGPT local cores collection maintained or exposed by this AI core instance for downstream processing.
        /// </summary>
        /// <value>The ChatGPT local cores value exposed by <see cref="AICoreOptions"/>.</value>
        public List<ChatGPTLocalCoreOptions> ChatGPTLocalCores { get; set; } = new(); // Additional independently reachable OpenAI-compatible hosts/models
        /// <summary>
        /// Gets or sets the OpenAI core value that forms part of the AI core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The OpenAI core value exposed by <see cref="AICoreOptions"/>.</value>
        public OpenAICompatOptions OpenAICore { get; set; } = new();             // OpenAI cloud (api.openai.com)
    }

    /// <summary>
    /// Carries the configurable ChatGPT local core settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    public class ChatGPTLocalCoreOptions
    {
        /// <summary>
        /// Defines the ChatGPT local core constant used by <see cref="ChatGPTLocalCoreOptions"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const string ChatGPTLocalCore = "ChatGPTLocalCore";
        /// <summary>
        /// Gets or sets the endpoint that identifies the network or application endpoint associated with this ChatGPT local core state.
        /// </summary>
        /// <value>The endpoint value exposed by <see cref="ChatGPTLocalCoreOptions"/>.</value>
        public string Endpoint { get; set; } = "http://localhost:11434/v1";
        /// <summary>
        /// Gets or sets the stable API key used to identify or correlate this ChatGPT local core instance with related application state.
        /// </summary>
        /// <value>The API key value exposed by <see cref="ChatGPTLocalCoreOptions"/>.</value>
        public string ApiKey { get; set; } = "local-no-key";
        /// <summary>
        /// Gets or sets the model name value that forms part of the ChatGPT local core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The model name value exposed by <see cref="ChatGPTLocalCoreOptions"/>.</value>
        public string ModelName { get; set; } = "gpt-oss:20b";
        /// <summary>
        /// Gets or sets a value indicating whether auto start server applies to the ChatGPT local core state.
        /// </summary>
        /// <value>The auto start server value exposed by <see cref="ChatGPTLocalCoreOptions"/>.</value>
        public bool AutoStartServer { get; set; } = false;
        /// <summary>
        /// Gets or sets the python environment value that forms part of the ChatGPT local core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The python environment value exposed by <see cref="ChatGPTLocalCoreOptions"/>.</value>
        public string? PythonEnvironment { get; set; }
        /// <summary>
        /// Gets or sets the start script value that forms part of the ChatGPT local core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The start script value exposed by <see cref="ChatGPTLocalCoreOptions"/>.</value>
        public string? StartScript { get; set; }  // e.g. path to run_gpt_oss_server.py
        /// <summary>
        /// Gets or sets the working dir value that forms part of the ChatGPT local core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The working dir value exposed by <see cref="ChatGPTLocalCoreOptions"/>.</value>
        public string? WorkingDir { get; set; }   // directory where model + script live
        /// <summary>
        /// Gets or sets the start command value that forms part of the ChatGPT local core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The start command value exposed by <see cref="ChatGPTLocalCoreOptions"/>.</value>
        public string? StartCommand { get; set; }
        /// <summary>
        /// Gets or sets the health timeout seconds value that forms part of the ChatGPT local core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The health timeout seconds value exposed by <see cref="ChatGPTLocalCoreOptions"/>.</value>
        public int HealthTimeoutSeconds { get; set; } = 45;
    }

    /// <summary>
    /// Carries the configurable OpenAI service core settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    public class OpenAIServiceCoreOptions // Azure OpenAI
    {
        /// <summary>
        /// Defines the OpenAI service core constant used by <see cref="OpenAIServiceCoreOptions"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const string OpenAIServiceCore = "OpenAIServiceCore";
        /// <summary>
        /// Gets or sets the endpoint that identifies the network or application endpoint associated with this OpenAI service core state.
        /// </summary>
        /// <value>The endpoint value exposed by <see cref="OpenAIServiceCoreOptions"/>.</value>
        public string Endpoint { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the stable key used to identify or correlate this OpenAI service core instance with related application state.
        /// </summary>
        /// <value>The key value exposed by <see cref="OpenAIServiceCoreOptions"/>.</value>
        public string Key { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the deployment name value that forms part of the OpenAI service core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The deployment name value exposed by <see cref="OpenAIServiceCoreOptions"/>.</value>
        public string DeploymentName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Carries the configurable OpenAI compat settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    public class OpenAICompatOptions // OpenAI cloud (or any OpenAI-compatible)
    {
        /// <summary>
        /// Defines the OpenAI core constant used by <see cref="OpenAICompatOptions"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const string OpenAICore = "OpenAICore";
        /// <summary>
        /// Gets or sets the endpoint that identifies the network or application endpoint associated with this OpenAI compat state.
        /// </summary>
        /// <value>The endpoint value exposed by <see cref="OpenAICompatOptions"/>.</value>
        public string Endpoint { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the stable API key used to identify or correlate this OpenAI compat instance with related application state.
        /// </summary>
        /// <value>The API key value exposed by <see cref="OpenAICompatOptions"/>.</value>
        public string ApiKey { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the model name value that forms part of the OpenAI compat state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The model name value exposed by <see cref="OpenAICompatOptions"/>.</value>
        public string ModelName { get; set; } = "gpt-4o-mini";
    }

    /// <summary>
    /// Carries the configurable Ollama core settings used to control the associated application behavior without hard-coding policy in consumers.
    /// </summary>
    public class OllamaCoreOptions
    {
        /// <summary>
        /// Defines the Ollama core constant used by <see cref="OllamaCoreOptions"/> so callers and internal logic share the same stable value.
        /// </summary>
        public const string OllamaCore = "OllamaCore";
        /// <summary>
        /// Gets or sets the URI that identifies the network or application endpoint associated with this Ollama core state.
        /// </summary>
        /// <value>The URI value exposed by <see cref="OllamaCoreOptions"/>.</value>
        public string Uri { get; set; } = "http://localhost:11434";
        /// <summary>
        /// Gets or sets the model name value that forms part of the Ollama core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The model name value exposed by <see cref="OllamaCoreOptions"/>.</value>
        public string ModelName { get; set; } = "gpt-oss:20b";
        /// <summary>
        /// Gets or sets the response protocol value that forms part of the Ollama core state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The response protocol value exposed by <see cref="OllamaCoreOptions"/>.</value>
        public ChatResponseProtocol ResponseProtocol { get; set; } = ChatResponseProtocol.Auto;
    }

    /// <summary>
    /// Represents the outcome of local AI host discovery, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    public sealed class LocalAiHostDiscoveryResult
    {
        /// <summary>
        /// Gets or sets the provider value that forms part of the local AI host discovery state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The provider value exposed by <see cref="LocalAiHostDiscoveryResult"/>.</value>
        public string Provider { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the endpoint that identifies the network or application endpoint associated with this local AI host discovery state.
        /// </summary>
        /// <value>The endpoint value exposed by <see cref="LocalAiHostDiscoveryResult"/>.</value>
        public string Endpoint { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets a value indicating whether reachable applies to the local AI host discovery state.
        /// </summary>
        /// <value>The is reachable value exposed by <see cref="LocalAiHostDiscoveryResult"/>.</value>
        public bool IsReachable { get; set; }
        /// <summary>
        /// Gets or sets the status value that forms part of the local AI host discovery state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The status value exposed by <see cref="LocalAiHostDiscoveryResult"/>.</value>
        public string Status { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the models collection maintained or exposed by this local AI host discovery instance for downstream processing.
        /// </summary>
        /// <value>The models value exposed by <see cref="LocalAiHostDiscoveryResult"/>.</value>
        public List<LocalAiModelInfo> Models { get; set; } = new();
    }

    /// <summary>
    /// Represents a local AI model info application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public sealed class LocalAiModelInfo
    {
        /// <summary>
        /// Gets or sets the name value that forms part of the local AI model info state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The name value exposed by <see cref="LocalAiModelInfo"/>.</value>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets a value indicating whether loaded applies to the local AI model info state.
        /// </summary>
        /// <value>The is loaded value exposed by <see cref="LocalAiModelInfo"/>.</value>
        public bool IsLoaded { get; set; }
        /// <summary>
        /// Gets or sets the details value that forms part of the local AI model info state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The details value exposed by <see cref="LocalAiModelInfo"/>.</value>
        public string? Details { get; set; }
    }
}

namespace LocalGPT.BusinessObjects
{
    public class AICoreOptions
    {
        public const string AICore = "AICore";

        public OllamaCoreOptions? OllamaCore { get; set; }
        public OpenAIServiceCoreOptions? OpenAIServiceCore { get; set; } // Azure OpenAI
        public ChatGPTLocalCoreOptions? ChatGPTLocalCore { get; set; }   // Local OpenAI-compatible (vLLM/LM Studio/etc.)
        public OpenAICompatOptions? OpenAICore { get; set; }             // OpenAI cloud (api.openai.com)
    }

    public class ChatGPTLocalCoreOptions
    {
        public const string ChatGPTLocalCore = "ChatGPTLocalCore";
        public string Endpoint { get; set; } = "http://localhost:8080/";
        public string ApiKey { get; set; } = "local-key";
        public string ModelName { get; set; } = "gpt-oss-20b";
        public bool AutoStartServer { get; set; } = false;
        public string? PythonEnvironment { get; set; }
        public string? StartScript { get; set; }  // e.g. path to run_gpt_oss_server.py
        public string? WorkingDir { get; set; }   // directory where model + script live
        public string? StartCommand { get; set; }
        public int HealthTimeoutSeconds { get; set; } = 45;
    }

    public class OpenAIServiceCoreOptions // Azure OpenAI
    {
        public const string OpenAIServiceCore = "OpenAIServiceCore";
        public string Endpoint { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = string.Empty;
    }

    public class OpenAICompatOptions // OpenAI cloud (or any OpenAI-compatible)
    {
        public const string OpenAICore = "OpenAICore";
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = "gpt-4o-mini";
    }

    public class OllamaCoreOptions
    {
        public const string OllamaCore = "OllamaCore";
        public string Uri { get; set; } = "http://localhost:11434";
        public string ModelName { get; set; } = "llama3.1";
    }
}

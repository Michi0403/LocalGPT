namespace LocalGPT.BusinessObjects
{
    public class AICoreOptions
    {
        public const string AICore = "AICore";

        public OllamaCoreOptions? OllamaCore { get; set; }
        public OpenAIServiceCoreOptions? OpenAIServiceCore { get; set; }
        public ChatGPTLocalCoreOptions? ChatGPTLocalCore { get; set; }
    }

    public class ChatGPTLocalCoreOptions
    {
        public const string ChatGPTLocalCore = "ChatGPTLocalCore";
    }

    public class OpenAIServiceCoreOptions
    {
        public const string OpenAIServiceCore = "OpenAIServiceCore";
        public string Endpoint { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = string.Empty;
    }

    public class OllamaCoreOptions
    {
        public const string OllamaCore = "OllamaCore";
        public string Uri { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
    }
}

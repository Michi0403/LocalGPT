using Azure;
using Azure.AI.OpenAI;
using LocalGPT.BusinessObjects;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
namespace LocalGPT.Services
{
    public interface IChatClientFactory
    {
        CompositeChatClient BuildFrom(AICoreOptions options);
    }

    public class ChatClientFactory : IChatClientFactory
    {
        public CompositeChatClient BuildFrom(AICoreOptions options)
        {
            var sessions = new List<ChatClientSession>();

            // Azure OpenAI (Azure.AI.OpenAI)
            if (options.OpenAIServiceCore is { Endpoint.Length: > 0, Key.Length: > 0, DeploymentName.Length: > 0 } az)
            {
                var azureClient = new AzureOpenAIClient(new Uri(az.Endpoint), new AzureKeyCredential(az.Key))
                    .GetChatClient(az.DeploymentName)
                    .AsIChatClient();

                sessions.Add(new ChatClientSession(azureClient, $"Azure OpenAI — {az.DeploymentName}"));
            }

            // OpenAI cloud (OpenAI SDK)
            if (options.OpenAICore is { ApiKey.Length: > 0, ModelName.Length: > 0 } openai)
            {
                var oai = new OpenAIClient(openai.ApiKey); // default base: https://api.openai.com/v1
                var modelChat = oai.GetChatClient(openai.ModelName).AsIChatClient();

                sessions.Add(new ChatClientSession(modelChat, $"OpenAI — {openai.ModelName}"));
            }

            // Ollama (Microsoft.Extensions.AI.Ollama)
            if (options.OllamaCore is { Uri.Length: > 0, ModelName.Length: > 0 } ollama)
            {
                var ollamaChat = new OllamaChatClient(new Uri(ollama.Uri), ollama.ModelName);
                sessions.Add(new ChatClientSession(ollamaChat, $"Ollama — {ollama.ModelName}"));
            }
            // Local OpenAI-compatible (vLLM / LM Studio / text-gen-webui OpenAI API)
            if (options.ChatGPTLocalCore is { Endpoint.Length: > 0, ApiKey.Length: > 0, ModelName.Length: > 0 } loc)
            {
                // IMPORTANT: OpenAIClientOptions expects the base host (no trailing /v1).
                // If your UI stores ".../v1", strip it before passing to Endpoint.
                var endpoint = loc.Endpoint.TrimEnd('/');
                if (endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                    endpoint = endpoint[..^3]; // remove "/v1"

                var localClient = new OpenAIClient(
                    new ApiKeyCredential(loc.ApiKey),
                    new OpenAIClientOptions { Endpoint = new Uri(endpoint) }
                );

                var localChat = localClient.GetChatClient(loc.ModelName).AsIChatClient();
                sessions.Add(new ChatClientSession(localChat, $"Local — {loc.ModelName}"));
            }
            if (sessions.Count == 0)
                throw new InvalidOperationException("No AI providers configured.");

            return new CompositeChatClient(sessions.ToArray());
        }
    }
}

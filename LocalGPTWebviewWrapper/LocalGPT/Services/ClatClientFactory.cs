using Azure;
using Azure.AI.OpenAI;
using LocalGPT.BusinessObjects;
using LocalGPT.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
namespace LocalGPT.Services
{
    public interface IChatClientFactory
    {
        CompositeChatClient Build();
    }

    public class ChatClientFactory(ILogger<ChatClientFactory> logger, IOptionsMonitor<BusinessObjects.ConfigurationRoot> optionsRoot) : IChatClientFactory
    {
        public CompositeChatClient Build()
        {
            try
            {
                AICoreOptions options = optionsRoot.CurrentValue.AICore;
                var sessions = new List<ChatClientSession>();

                logger.LogInformation($"Started Buildfrom {options.ToJsonString()}");
                // Azure OpenAI (Azure.AI.OpenAI)
                if (options.OpenAIServiceCore is { Endpoint.Length: > 0, Key.Length: > 0, DeploymentName.Length: > 0 } az)
                {

                    logger.LogInformation($"Found OpenAIServiceCore {options.OpenAIServiceCore.ToJsonString()}");
                    var azureClient = new AzureOpenAIClient(new Uri(az.Endpoint), new AzureKeyCredential(az.Key))
                        .GetChatClient(az.DeploymentName)
                        .AsIChatClient();

                    sessions.Add(new ChatClientSession(azureClient, $"Azure OpenAI — {az.DeploymentName}"));
                }

                // OpenAI cloud (OpenAI SDK)
                if (options.OpenAICore is { ApiKey.Length: > 0, ModelName.Length: > 0 } openai)
                {
                    logger.LogInformation($"Found OpenAICore {options.OpenAICore.ToJsonString()}");
                    var oai = new OpenAIClient(openai.ApiKey); // default base: https://api.openai.com/v1
                    var modelChat = oai.GetChatClient(openai.ModelName).AsIChatClient();

                    sessions.Add(new ChatClientSession(modelChat, $"OpenAI — {openai.ModelName}"));
                }

                // Ollama (Microsoft.Extensions.AI.Ollama)
                if (options.OllamaCore is { Uri.Length: > 0, ModelName.Length: > 0 } ollama)
                {
                    logger.LogInformation($"Found OllamaCore {options.OllamaCore.ToJsonString()}");
                    var ollamaChat = new OllamaChatClient(new Uri(ollama.Uri), ollama.ModelName);
                    sessions.Add(new ChatClientSession(ollamaChat, $"Ollama — {ollama.ModelName}"));
                }
                // Local OpenAI-compatible (vLLM / LM Studio / text-gen-webui OpenAI API)
                if (options.ChatGPTLocalCore is { Endpoint.Length: > 0, ApiKey.Length: > 0, ModelName.Length: > 0 } loc)
                {
                    logger.LogInformation($"Found ChatGPTLocalCore {options.ChatGPTLocalCore.ToJsonString()}");
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

                return new CompositeChatClient(logger,sessions.ToArray());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IChatClientFactory Buildfrom {ex.ToString()}");
                throw;
            }
           
        }
    }
}

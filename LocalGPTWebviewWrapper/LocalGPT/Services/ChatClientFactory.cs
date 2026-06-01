using Azure;
using Azure.AI.OpenAI;
using LocalGPT.BusinessObjects;
using LocalGPT.Extensions;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using System.ClientModel.Primitives;
namespace LocalGPT.Services
{

    public class ChatClientFactory(
          ILogger<ChatClientFactory> logger,
          ILoggerFactory loggerFactory,
          IOptionsMonitor<BusinessObjects.ConfigurationRoot> optionsRoot,
          IAiFeatureReportService featureReportService,
          IAiContextBootstrapService bootstrapService,
          ICouncilKnowledgeService knowledgeService
      ) : IChatClientFactory
    {
        public CompositeChatClient Build()
        {
            try
            {
                var options = optionsRoot.CurrentValue.AICore ?? new AICoreOptions();
                var sessions = new List<ChatClientSession>();

                logger.LogInformation("🔧 Building chat clients from configuration: {Json}", options.ToJsonString());

                // --- Ollama (Microsoft.Extensions.AI.Ollama) ---
                foreach (var ollama in GetConfiguredOllamaProviders(options))
                {
                    logger.LogInformation("⚙️ Found Ollama configuration: {Json}", ollama.ToJsonString());

                    var ollamaChat = new OllamaThinkingChatClient(
                        ollama,
                        keepAlive: "2m",
                        contextLength: 4096,
                        timeout: TimeSpan.FromMinutes(10),
                        numGpu: null);

                    sessions.Add(new ChatClientSession(
                        new LoggingChatClient(ollamaChat, loggerFactory.CreateLogger("AI.Ollama")),
                        $"Ollama — {ollama.ModelName}"
                    ));
                }

                // --- Azure OpenAI (Azure.AI.OpenAI) ---
                if (options.OpenAIServiceCore is { Endpoint.Length: > 0, Key.Length: > 0, DeploymentName.Length: > 0 } az)
                {
                    logger.LogInformation("⚙️ Found Azure OpenAI configuration: {Json}", az.ToJsonString());

                    var azureOptions = new AzureOpenAIClientOptions
                    {
                        ClientLoggingOptions = new ClientLoggingOptions
                        {
                            EnableLogging = true,
                            EnableMessageLogging = true,
                            EnableMessageContentLogging = true,
                            LoggerFactory = loggerFactory
                        }
                    };

                    var azureClient = new AzureOpenAIClient(new Uri(az.Endpoint), new AzureKeyCredential(az.Key), azureOptions)
                        .GetChatClient(az.DeploymentName)
                        .AsIChatClient();

                    sessions.Add(new ChatClientSession(
                        new LoggingChatClient(azureClient, loggerFactory.CreateLogger("AI.AzureOpenAI")),
                        $"Azure OpenAI — {az.DeploymentName}"
                    ));
                }

                // --- OpenAI cloud (OpenAI SDK) ---
                if (options.OpenAICore is { ModelName.Length: > 0 } openai && HasRealApiKey(openai.ApiKey))
                {
                    logger.LogInformation("⚙️ Found OpenAI configuration: {Json}", openai.ToJsonString());

                    // Allow custom endpoint (use default if empty)
                    var configString = openai.Endpoint?.TrimEnd('/');
                    var endpoint = string.IsNullOrWhiteSpace( configString)? "https://api.openai.com/v1" : configString;

                    var oai = new OpenAIClient(
                        new ApiKeyCredential(openai.ApiKey),
                        new OpenAIClientOptions
                        {
                            Endpoint = new Uri(endpoint,uriKind: UriKind.Absolute),
                            ClientLoggingOptions = new ClientLoggingOptions
                            {
                                EnableLogging = true,
                                EnableMessageLogging = true,
                                EnableMessageContentLogging = true,
                                LoggerFactory = loggerFactory
                            }
                        });

                    var modelChat = oai.GetChatClient(openai.ModelName).AsIChatClient();

                    sessions.Add(new ChatClientSession(
                        new LoggingChatClient(modelChat, loggerFactory.CreateLogger("AI.OpenAI")),
                        $"OpenAI — {openai.ModelName}"
                    ));
                }

                // --- Local OpenAI-compatible (LM Studio / vLLM / text-gen-webui) ---
                if (options.ChatGPTLocalCore is { Endpoint.Length: > 0, ApiKey.Length: > 0, ModelName.Length: > 0 } loc)
                {
                    logger.LogInformation("⚙️ Found Local ChatGPT configuration: {Json}", loc.ToJsonString());

                    var endpoint = loc.Endpoint.TrimEnd('/');
                    if (endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                        endpoint = endpoint[..^3]; // strip trailing /v1

                    var localClient = new OpenAIClient(
                        new ApiKeyCredential(loc.ApiKey),
                        new OpenAIClientOptions
                        {
                            Endpoint = new Uri(endpoint, uriKind: UriKind.Absolute),
                            ClientLoggingOptions = new ClientLoggingOptions
                            {
                                EnableLogging = true,
                                EnableMessageLogging = true,
                                EnableMessageContentLogging = true,
                                LoggerFactory = loggerFactory
                            }
                        });

                    var localChat = localClient.GetChatClient(loc.ModelName).AsIChatClient();

                    sessions.Add(new ChatClientSession(
                        new LoggingChatClient(localChat, loggerFactory.CreateLogger("AI.LocalOpenAI")),
                        $"Local — {loc.ModelName}"
                    ));
                }

                if (sessions.Count == 0)
                    throw new InvalidOperationException("❌ No AI providers configured. Check appsettings.json or Installation page.");

                return new CompositeChatClient(logger, featureReportService, bootstrapService, knowledgeService, sessions.ToArray());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "💥 ChatClientFactory.Build failed: {Message}", ex.Message);
                throw;
            }
        }

        private static IEnumerable<OllamaCoreOptions> GetConfiguredOllamaProviders(AICoreOptions options)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (options.OllamaCore is { Uri.Length: > 0, ModelName.Length: > 0 } primary)
            {
                seen.Add($"{primary.Uri.TrimEnd('/')}|{primary.ModelName}");
                yield return primary;
            }

            foreach (var ollama in options.OllamaCores.Where(o => !string.IsNullOrWhiteSpace(o.Uri) && !string.IsNullOrWhiteSpace(o.ModelName)))
            {
                if (seen.Add($"{ollama.Uri.TrimEnd('/')}|{ollama.ModelName}"))
                    yield return ollama;
            }
        }

        private static bool HasRealApiKey(string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return false;

            var trimmed = apiKey.Trim();
            return trimmed != "---" && !trimmed.Equals("local-key", StringComparison.OrdinalIgnoreCase);
        }
    }
}

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
using System.ServiceModel.Channels;
using System.Net.Http.Headers;
using System.Text.Json;
namespace LocalGPT.Services
{

    public class ChatClientFactory(
          ILogger<ChatClientFactory> logger,
          ILoggerFactory loggerFactory,
          IOptionsMonitor<BusinessObjects.ConfigurationRoot> optionsRoot,
          IAiFeatureReportService featureReportService,
          IAiContextBootstrapService bootstrapService,
          ICouncilKnowledgeService knowledgeService,
          IChatUploadWorkspaceService chatUploadWorkspaces,
          IPromptConfigService promptConfigService,
          IVariableStoreService variableStoreService,
          IDxAiFunctionRegistry functionRegistry,
          IChatResponseFormatterFactory formatterFactory,
          IChatProtocolResolver protocolResolver
      ,
        CouncilRuntimeService councilRuntime,
        CouncilTextService councilText) : IChatClientFactory
    {
        public CompositeChatClient Build()
        {
            try
            {
                var options = optionsRoot.CurrentValue.AICore ?? new AICoreOptions();
                var sessions = new List<ChatClientSession>();

                logger.LogInformation("Building configured chat provider sessions.");

                // --- Ollama (Microsoft.Extensions.AI.Ollama) ---
                foreach (var ollama in councilRuntime.GetConfiguredOllamaProviders(options, logger))
                {
                    logger.LogInformation("Found Ollama-compatible provider at {Endpoint} for model {Model}.", ollama.Uri, ollama.ModelName);

                    var ollamaChat = new OllamaThinkingChatClient(
                        ollama,
                        logger,
                        councilRuntime,
                        keepAlive: "2m",
                        contextLength: 65536,
                        timeout: TimeSpan.FromMinutes(30),
                        numGpu: null,
                        formatterFactory: formatterFactory,
                        protocolResolver: protocolResolver,
                        promptConfigService: promptConfigService,
                        functionRegistry: functionRegistry);

                    sessions.Add(new ChatClientSession(
                        new LoggingChatClient(ollamaChat, loggerFactory.CreateLogger("AI.Ollama")),
                        $"Ollama — {ollama.ModelName}", "Ollama", ollama.ModelName, ollama.Uri
                    ));
                }

                // --- Azure OpenAI (Azure.AI.OpenAI) ---
                if (options.OpenAIServiceCore is { Endpoint.Length: > 0, Key.Length: > 0, DeploymentName.Length: > 0 } az)
                {
                    logger.LogInformation("Found Azure OpenAI provider for deployment {Deployment}.", az.DeploymentName);

                    var azureOptions = new AzureOpenAIClientOptions
                    {
                        ClientLoggingOptions = new ClientLoggingOptions
                        {
                            EnableLogging = true,
                            EnableMessageLogging = false,
                            EnableMessageContentLogging = false,
                            LoggerFactory = loggerFactory
                        }
                    };

                    var azureClient = new AzureOpenAIClient(new Uri(az.Endpoint), new AzureKeyCredential(az.Key), azureOptions)
                        .GetChatClient(az.DeploymentName)
                        .AsIChatClient();

                    sessions.Add(new ChatClientSession(
                        new LoggingChatClient(azureClient, loggerFactory.CreateLogger("AI.AzureOpenAI")),
                        $"Azure OpenAI — {az.DeploymentName}", "Azure OpenAI", az.DeploymentName, az.Endpoint
                    ));
                }

                // --- OpenAI cloud (OpenAI SDK) ---
                if (options.OpenAICore is { ModelName.Length: > 0 } openai && councilRuntime.HasRealApiKey(openai.ApiKey, logger))
                {
                    logger.LogInformation("Found OpenAI-compatible cloud provider for model {Model}.", openai.ModelName);

                    // Allow custom endpoint (use default if empty)
                    var configString = openai.Endpoint?.TrimEnd('/');
                    var endpoint = string.IsNullOrWhiteSpace(configString) ? "https://api.openai.com/v1" : configString;

                    var oai = new OpenAIClient(
                        new ApiKeyCredential(openai.ApiKey),
                        new OpenAIClientOptions
                        {
                            Endpoint = new Uri(endpoint, uriKind: UriKind.Absolute),
                            ClientLoggingOptions = new ClientLoggingOptions
                            {
                                EnableLogging = true,
                                EnableMessageLogging = false,
                                EnableMessageContentLogging = false,
                                LoggerFactory = loggerFactory
                            }
                        });

                    var modelChat = oai.GetChatClient(openai.ModelName).AsIChatClient();

                    sessions.Add(new ChatClientSession(
                        new LoggingChatClient(modelChat, loggerFactory.CreateLogger("AI.OpenAI")),
                        $"OpenAI — {openai.ModelName}", "OpenAI", openai.ModelName, endpoint
                    ));
                }

                // --- Local OpenAI-compatible (LM Studio / vLLM / text-gen-webui) ---
                if (options.ChatGPTLocalCore is { Endpoint.Length: > 0 } loc)
                {
                    // 1. Define candidate endpoints: configured endpoint first, followed by standard local fallbacks.
                    var candidateEndpoints = new[]
                    {
        loc.Endpoint,
        "http://localhost:11434/v1", // Standard Ollama OpenAI-compatible port
        "http://127.0.0.1:11434/v1",
        "http://localhost:1234/v1",  // Standard LM Studio OpenAI-compatible port
        "http://127.0.0.1:1234/v1"
    }
                    .Where(ep => !string.IsNullOrWhiteSpace(ep))
                    .Select(NormalizeOpenAiCompatibleEndpoint)
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                    string? activeEndpoint = null;
                    string? resolvedModel = null;

                    // 2. Iterate through endpoints until a reachable provider with an active model is found
                    foreach (var endpoint in candidateEndpoints)
                    {
                        var model = ResolveOpenAiCompatibleModel(endpoint, loc.ModelName, loc.ApiKey, logger);
                        if (!string.IsNullOrWhiteSpace(model))
                        {
                            activeEndpoint = endpoint;
                            resolvedModel = model;
                            break; // Stop at the first working endpoint
                        }
                    }

                    // 3. Register the client session if any candidate succeeded, or log the fallback failure
                    if (!string.IsNullOrWhiteSpace(activeEndpoint) && !string.IsNullOrWhiteSpace(resolvedModel))
                    {
                        logger.LogInformation("Found reachable local OpenAI-compatible provider at {Endpoint} for model {Model}.", activeEndpoint, resolvedModel);

                        var localClient = new OpenAIClient(
                            new ApiKeyCredential(string.IsNullOrWhiteSpace(loc.ApiKey) ? "local-no-key" : loc.ApiKey),
                            new OpenAIClientOptions
                            {
                                Endpoint = new Uri(activeEndpoint, UriKind.Absolute),
                                ClientLoggingOptions = new ClientLoggingOptions
                                {
                                    EnableLogging = true,
                                    EnableMessageLogging = false,
                                    EnableMessageContentLogging = false,
                                    LoggerFactory = loggerFactory
                                }
                            });

                        var localChat = localClient.GetChatClient(resolvedModel).AsIChatClient();
                        sessions.Add(new ChatClientSession(
                            new LoggingChatClient(localChat, loggerFactory.CreateLogger("AI.LocalOpenAI")),
                            $"LM Studio / OpenAI-compatible — {resolvedModel}", "LM Studio / OpenAI-compatible", resolvedModel, activeEndpoint
                        ));
                    }
                    else
                    {
                        logger.LogInformation("The configured local endpoint ({Endpoint}) and common fallbacks (Ollama: 11434, LM Studio: 1234) are offline or expose no models; none were added to the active chat selector.", loc.Endpoint);
                    }
                }

                if (sessions.Count == 0)
                    throw new InvalidOperationException("❌ No AI providers configured. Check appsettings.json or Installation page.");

                return new CompositeChatClient(
                    logger,
                    featureReportService,
                    bootstrapService,
                    knowledgeService,
                    chatUploadWorkspaces,
                    promptConfigService,
                    variableStoreService,
                    councilRuntime,
                    councilText,
                    sessions.ToArray());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "💥 ChatClientFactory.Build failed: {Message}", ex.Message);
                throw;
            }
        }

        private static string? ResolveOpenAiCompatibleModel(string endpoint, string? configuredModel, string? apiKey, ILogger logger)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                if (!string.IsNullOrWhiteSpace(apiKey))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                using var response = client.GetAsync(endpoint.TrimEnd('/') + "/models").GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode) return null;
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                using var document = JsonDocument.Parse(body);
                if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) return null;
                var models = data.EnumerateArray()
                    .Select(item => item.TryGetProperty("id", out var id) ? id.GetString() : null)
                    .Where(model => !string.IsNullOrWhiteSpace(model))
                    .Cast<string>()
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (models.Length == 0) return null;
                var configured = configuredModel?.Trim();
                return models.FirstOrDefault(model => string.Equals(model, configured, StringComparison.OrdinalIgnoreCase))
                    ?? models[0];
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
            {
                logger.LogDebug(exception, "Local OpenAI-compatible model discovery failed for {Endpoint}.", endpoint);
                return null;
            }
        }

        private static string NormalizeOpenAiCompatibleEndpoint(string value)
        {
            var endpoint = value.Trim().TrimEnd('/');
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
                throw new InvalidOperationException("The local OpenAI-compatible endpoint is not a valid absolute URI.");
            if (string.IsNullOrWhiteSpace(uri.AbsolutePath) || uri.AbsolutePath == "/")
                endpoint += "/v1";
            return endpoint;
        }
    }
}
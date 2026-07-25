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
                        $"Ollama — {ollama.ModelName}"
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
                        $"Azure OpenAI — {az.DeploymentName}"
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
                        $"OpenAI — {openai.ModelName}"
                    ));
                }

                // --- Local OpenAI-compatible (LM Studio / vLLM / text-gen-webui) ---
                if (options.ChatGPTLocalCore is { Endpoint.Length: > 0, ModelName.Length: > 0 } loc)
                {
                    logger.LogInformation("Found local OpenAI-compatible provider at {Endpoint} for model {Model}.", loc.Endpoint, loc.ModelName);

                    var endpoint = loc.Endpoint.TrimEnd('/');
                    if (endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                        endpoint = endpoint[..^3]; // strip trailing /v1

                    var localClient = new OpenAIClient(
                        new ApiKeyCredential(string.IsNullOrWhiteSpace(loc.ApiKey) ? "local-no-key" : loc.ApiKey),
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

                    var localChat = localClient.GetChatClient(loc.ModelName).AsIChatClient();

                    sessions.Add(new ChatClientSession(
                        new LoggingChatClient(localChat, loggerFactory.CreateLogger("AI.LocalOpenAI")),
                        $"Local — {loc.ModelName}"
                    ));
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
    }
}
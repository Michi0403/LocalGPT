using Azure;
using Azure.AI.OpenAI;
using LocalGPT.BusinessObjects;
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

    /// <summary>
    /// Creates configured chat client instances from the application's current dependencies and runtime settings.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="loggerFactory">Logger factory dependency used by the chat client workflow to provide the corresponding application capability.</param>
    /// <param name="optionsRoot">Business objects.configuration root dependency used by the chat client workflow to provide the corresponding application capability.</param>
    /// <param name="featureReportService">Ai feature report service dependency used by the chat client workflow to provide the corresponding application capability.</param>
    /// <param name="bootstrapService">Ai context bootstrap service dependency used by the chat client workflow to provide the corresponding application capability.</param>
    /// <param name="knowledgeService">Council knowledge service dependency used by the chat client workflow to provide the corresponding application capability.</param>
    /// <param name="chatUploadWorkspaces">Chat upload workspace service dependency used by the chat client workflow to provide the corresponding application capability.</param>
    /// <param name="promptConfigService">Prompt config service dependency used by the chat client workflow to provide the corresponding application capability.</param>
    /// <param name="variableStoreService">Variable store service dependency used by the chat client workflow to provide the corresponding application capability.</param>
    /// <param name="systemVariables">System variable definition service dependency used by the chat client workflow to provide the corresponding application capability.</param>
    /// <param name="functionRegistry">Devexpress ai function registry dependency used by the chat client workflow to provide the corresponding application capability.</param>
    /// <param name="functionCallRecovery">Devexpress ai function call recovery service dependency used by the chat client workflow to provide the corresponding application capability.</param>
    /// <param name="formatterFactory">Chat response formatter factory dependency used by the chat client workflow to provide the corresponding application capability.</param>
    /// <param name="protocolResolver">Chat protocol resolver dependency used by the chat client workflow to provide the corresponding application capability.</param>
    /// <param name="councilRuntime">Council runtime service dependency used by the chat client workflow to provide the corresponding application capability.</param>
    /// <param name="councilText">Council text service dependency used by the chat client workflow to provide the corresponding application capability.</param>
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
          ISystemVariableDefinitionService systemVariables,
          IDxAiFunctionRegistry functionRegistry,
          IDxAiFunctionCallRecoveryService functionCallRecovery,
          IChatResponseFormatterFactory formatterFactory,
          IChatProtocolResolver protocolResolver
      ,
        CouncilRuntimeService councilRuntime,
        CouncilTextService councilText) : IChatClientFactory
    {
        /// <summary>
        /// Performs build using the configuration and dependencies owned by <see cref="ChatClientFactory"/>.
        /// </summary>
        /// <returns>The composite chat client produced by the operation.</returns>
        public CompositeChatClient Build()
        {
            try
            {
                var options = optionsRoot.CurrentValue.AICore ?? new AICoreOptions();
                var sessions = new List<ChatClientSession>();
                var configuredOllamaEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                logger.LogInformation("Building configured chat provider sessions.");

                // --- Ollama (Microsoft.Extensions.AI.Ollama) ---
                foreach (var ollama in councilRuntime.GetConfiguredOllamaProviders(options, logger))
                {
                    logger.LogInformation("Found Ollama-compatible provider at {Endpoint} for model {Model}.", ollama.Uri, ollama.ModelName);
                    configuredOllamaEndpoints.Add(NormalizeProviderIdentity(ollama.Uri));

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
                        functionRegistry: functionRegistry,
                        functionCallRecovery: functionCallRecovery);

                    sessions.Add(new ChatClientSession(
                        /// <summary>
                        /// Runs the logging chat client operation.
                        /// </summary>
                        new LoggingChatClient(ollamaChat, loggerFactory.CreateLogger("AI.Ollama")),
                        /// <summary>
                        /// Runs the provider model identity operation.
                        /// </summary>
                        new ProviderModelIdentity().CreateSelectionKey("Ollama", ollama.Uri, ollama.ModelName), "Ollama", ollama.ModelName, ollama.Uri
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
                        /// <summary>
                        /// Runs the logging chat client operation.
                        /// </summary>
                        new LoggingChatClient(azureClient, loggerFactory.CreateLogger("AI.AzureOpenAI")),
                        /// <summary>
                        /// Runs the provider model identity operation.
                        /// </summary>
                        new ProviderModelIdentity().CreateSelectionKey("Azure OpenAI", az.Endpoint, az.DeploymentName), "Azure OpenAI", az.DeploymentName, az.Endpoint
                    ));
                }

                // --- OpenAI cloud (OpenAI SDK) ---
                if (options.OpenAICore is { ModelName.Length: > 0 } openai && councilRuntime.HasRealApiKey(openai.ApiKey, logger))
                {
                    logger.LogInformation("Found OpenAI-compatible cloud provider for model {Model}.", openai.ModelName);

                    // Allow custom endpoint (use default if empty)
                    var configString = openai.Endpoint?.TrimEnd('/');
                    var endpoint = string.IsNullOrWhiteSpace(configString)
                        ? "https://api.openai.com/v1"
                        : NormalizeOpenAiCompatibleEndpoint(configString);

                    var oai = new OpenAIClient(
                        /// <summary>
                        /// Runs the API key credential operation.
                        /// </summary>
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
                        /// <summary>
                        /// Runs the logging chat client operation.
                        /// </summary>
                        new LoggingChatClient(modelChat, loggerFactory.CreateLogger("AI.OpenAI")),
                        /// <summary>
                        /// Runs the provider model identity operation.
                        /// </summary>
                        new ProviderModelIdentity().CreateSelectionKey("OpenAI", endpoint, openai.ModelName), "OpenAI", openai.ModelName, endpoint
                    ));
                }

                // --- OpenAI-compatible hosts (LM Studio / vLLM / text-gen-webui; local or remote) ---
                foreach (var loc in EnumerateOpenAiCompatibleProviders(options))
                {
                    var configuredEndpoint = NormalizeOpenAiCompatibleEndpoint(loc.Endpoint);
                    var configuredIdentity = NormalizeProviderIdentity(loc.Endpoint);
                    if (configuredOllamaEndpoints.Contains(configuredIdentity))
                    {
                        logger.LogInformation(
                            "Skipping duplicate OpenAI-compatible registration for {Endpoint}; the same provider authority is already registered through native Ollama.",
                            loc.Endpoint);
                        continue;
                    }

                    var resolvedModel = ResolveOpenAiCompatibleModel(configuredEndpoint, loc.ModelName, loc.ApiKey, logger);
                    if (string.IsNullOrWhiteSpace(resolvedModel))
                    {
                        logger.LogInformation(
                            "Configured OpenAI-compatible endpoint {Endpoint} is offline or exposes no models; it was not added to the active chat selector.",
                            loc.Endpoint);
                        continue;
                    }

                    var runtimeApiKey = !string.IsNullOrWhiteSpace(loc.ApiKey) ? loc.ApiKey : "local-no-key";
                    var localClient = new OpenAIClient(
                        /// <summary>
                        /// Runs the API key credential operation.
                        /// </summary>
                        new ApiKeyCredential(runtimeApiKey),
                        new OpenAIClientOptions
                        {
                            Endpoint = new Uri(configuredEndpoint, UriKind.Absolute),
                            ClientLoggingOptions = new ClientLoggingOptions
                            {
                                EnableLogging = true,
                                EnableMessageLogging = false,
                                EnableMessageContentLogging = false,
                                LoggerFactory = loggerFactory
                            }
                        });

                    var localChat = localClient.GetChatClient(resolvedModel).AsIChatClient();
                    var providerName = GetLocalProviderName(configuredEndpoint);
                    sessions.Add(new ChatClientSession(
                        /// <summary>
                        /// Runs the logging chat client operation.
                        /// </summary>
                        new LoggingChatClient(localChat, loggerFactory.CreateLogger("AI.LocalOpenAI")),
                        /// <summary>
                        /// Runs the provider model identity operation.
                        /// </summary>
                        new ProviderModelIdentity().CreateSelectionKey(providerName, configuredEndpoint, resolvedModel),
                        providerName, resolvedModel, configuredEndpoint));
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
                    systemVariables,
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

        /// <summary>
        /// Performs enumerate OpenAI compatible providers using the configuration and dependencies owned by <see cref="ChatClientFactory"/>.
        /// </summary>
        /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        private IReadOnlyList<ChatGPTLocalCoreOptions> EnumerateOpenAiCompatibleProviders(AICoreOptions options)
        {
            try
            {
                var results = new List<ChatGPTLocalCoreOptions>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                void Add(ChatGPTLocalCoreOptions? item)
                {
                    if (item is null || string.IsNullOrWhiteSpace(item.Endpoint))
                        return;
                    var normalized = NormalizeOpenAiCompatibleEndpoint(item.Endpoint);
                    if (seen.Add(normalized))
                        results.Add(item);
                }

                Add(options.ChatGPTLocalCore);
                foreach (var item in options.ChatGPTLocalCores ?? [])
                    Add(item);
                // Preserve the historical local LM Studio auto-discovery while allowing any number of configured remote hosts.
                Add(new ChatGPTLocalCoreOptions
                {
                    Endpoint = "http://127.0.0.1:1234/v1",
                    ApiKey = "local-no-key",
                    ModelName = string.Empty,
                    AutoStartServer = false
                });

                return results;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Enumerating configured OpenAI-compatible providers failed.");
                throw;
            }
        }

        /// <summary>
        /// Resolves OpenAI compatible model using the configuration and dependencies owned by <see cref="ChatClientFactory"/>.
        /// </summary>
        /// <param name="endpoint">Endpoint value supplied to the chat client operation and used when producing its result.</param>
        /// <param name="configuredModel">Configured model value supplied to the chat client operation and used when producing its result.</param>
        /// <param name="apiKey">Api key value supplied to the chat client operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        private string? ResolveOpenAiCompatibleModel(string endpoint, string? configuredModel, string? apiKey, ILogger logger)
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

        /// <summary>
        /// Retrieves local provider name using the configuration and dependencies owned by <see cref="ChatClientFactory"/>.
        /// </summary>
        /// <param name="endpoint">Endpoint value supplied to the chat client operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string GetLocalProviderName(string endpoint) {
    try
    {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && uri.Port == 1234
                ? "LM Studio"
                : "Local OpenAI-compatible";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ChatClientFactory)}.{nameof(GetLocalProviderName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ChatClientFactory)}.{nameof(GetLocalProviderName)} failed.");
        throw;
    }
}

        /// <summary>
        /// Normalizes provider identity using the configuration and dependencies owned by <see cref="ChatClientFactory"/>.
        /// </summary>
        /// <param name="value">Value value supplied to the chat client operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string NormalizeProviderIdentity(string value)
        {
    try
    {
                var endpoint = NormalizeOpenAiCompatibleEndpoint(value);
                var uri = new Uri(endpoint, UriKind.Absolute);
                var host = string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ? "127.0.0.1" : uri.Host;
                return $"{uri.Scheme}://{host}:{uri.Port}";
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ChatClientFactory)}.{nameof(NormalizeProviderIdentity)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ChatClientFactory)}.{nameof(NormalizeProviderIdentity)} failed.");
        throw;
    }
}

        /// <summary>
        /// Normalizes OpenAI compatible endpoint using the configuration and dependencies owned by <see cref="ChatClientFactory"/>.
        /// </summary>
        /// <param name="value">Value value supplied to the chat client operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string NormalizeOpenAiCompatibleEndpoint(string value)
        {
    try
    {
                var endpoint = value.Trim().TrimEnd('/');
                if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
                    throw new InvalidOperationException("The local OpenAI-compatible endpoint is not a valid absolute URI.");
                var builder = new UriBuilder(uri);
                if (string.Equals(builder.Host, "localhost", StringComparison.OrdinalIgnoreCase))
                    builder.Host = "127.0.0.1";
                if (string.IsNullOrWhiteSpace(builder.Path) || builder.Path == "/")
                    builder.Path = "/v1";
                return builder.Uri.ToString().TrimEnd('/');
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ChatClientFactory)}.{nameof(NormalizeOpenAiCompatibleEndpoint)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ChatClientFactory)}.{nameof(NormalizeOpenAiCompatibleEndpoint)} failed.");
        throw;
    }
}
    }
}

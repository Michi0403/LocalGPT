using Azure;
using Azure.AI.OpenAI;
using LocalGPT.BusinessObjects;
using ConfigurationRoot = LocalGPT.BusinessObjects.ConfigurationRoot;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Collections.Concurrent;

namespace LocalGPT.Services;

/// <summary>
/// Discovers and opens provider-qualified model sessions. A model name is never treated as a globally
/// unique address: provider kind and endpoint are always retained with it.
/// </summary>
/// <param name="optionsRoot">Configuration root dependency used by the provider model runtime workflow to provide the corresponding application capability.</param>
/// <param name="loggerFactory">Logger factory dependency used by the provider model runtime workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
/// <param name="councilRuntime">Council runtime service dependency used by the provider model runtime workflow to provide the corresponding application capability.</param>
/// <param name="formatterFactory">Chat response formatter factory dependency used by the provider model runtime workflow to provide the corresponding application capability.</param>
/// <param name="protocolResolver">Chat protocol resolver dependency used by the provider model runtime workflow to provide the corresponding application capability.</param>
/// <param name="promptConfigService">Prompt config service dependency used by the provider model runtime workflow to provide the corresponding application capability.</param>
/// <param name="functionRegistry">Devexpress ai function registry dependency used by the provider model runtime workflow to provide the corresponding application capability.</param>
/// <param name="functionCallRecovery">Devexpress ai function call recovery service dependency used by the provider model runtime workflow to provide the corresponding application capability.</param>
public sealed class ProviderModelRuntimeService(
    IOptionsMonitor<ConfigurationRoot> optionsRoot,
    ILoggerFactory loggerFactory,
    ILogger<ProviderModelRuntimeService> logger,
    CouncilRuntimeService councilRuntime,
    IChatResponseFormatterFactory formatterFactory,
    IChatProtocolResolver protocolResolver,
    IPromptConfigService promptConfigService,
    IDxAiFunctionRegistry functionRegistry,
    IDxAiFunctionCallRecoveryService functionCallRecovery) : IProviderModelRuntimeService
{
    /// <summary>
    /// Stores the in-memory reference cache collection maintained internally by <see cref="ProviderModelRuntimeService"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<string, ProviderModelReference> referenceCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Retrieves candidates as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<MultiModelCouncilModelCandidate>> GetCandidatesAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            var options = optionsRoot.CurrentValue.AICore ?? new AICoreOptions();
            var candidates = new Dictionary<string, MultiModelCouncilModelCandidate>(StringComparer.OrdinalIgnoreCase);
            var ollamaOptions = EnumerateOllama(options).ToList();

            foreach (var ollama in ollamaOptions)
            {
                if (string.IsNullOrWhiteSpace(ollama.ModelName))
                    continue;
                var endpoint = NormalizeOllamaEndpoint(ollama.Uri);
                AddCandidate(candidates, new MultiModelCouncilModelCandidate(
                    ollama.ModelName.Trim(), "Ollama", endpoint,
                    IsInstalled: false, IsConfigured: true, IsLoaded: false,
                    Details: "Configured endpoint-qualified Ollama model.",
                    ProviderKind: ProviderModelKinds.Ollama,
                    IsLocal: IsLocalEndpoint(endpoint),
                    SupportsBenchmark: true));
            }

            // Start every independent provider-host probe before awaiting any of them. A slow or
            // unreachable remote endpoint must not serialize discovery and make an InteractiveServer
            // page appear frozen while unrelated providers are healthy.
            var ollamaProbeTasks = EnumerateOllamaProbeEndpoints(options)
                .Select(endpoint => (Endpoint: endpoint, Task: ProbeOllamaAsync(endpoint, cancellationToken)))
                .ToList();
            var openAiProbeTasks = EnumerateOpenAiCompatible(options)
                .Select(local =>
                {
                    var configuredEndpoint = NormalizeOpenAiEndpoint(local.Endpoint);
                    var providerName = GetLocalProviderName(configuredEndpoint);
                    return (
                        Local: local,
                        Endpoint: configuredEndpoint,
                        ProviderName: providerName,
                        Task: ProbeOpenAiCompatibleAsync(
                            providerName,
                            configuredEndpoint,
                            local.ApiKey,
                            ProviderModelKinds.OpenAICompatible,
                            isLocal: IsLocalEndpoint(configuredEndpoint),
                            cancellationToken));
                })
                .ToList();

            // Discovery is host-oriented, not primary-slot-oriented. Keep the historical loopback
            // Ollama endpoint discoverable even when the user's primary Ollama binding is remote,
            // and probe every configured endpoint once regardless of how many preferred models it owns.
            foreach (var probe in ollamaProbeTasks)
            {
                foreach (var discovered in await probe.Task.ConfigureAwait(false))
                    AddCandidate(candidates, discovered);
            }

            foreach (var probe in openAiProbeTasks)
            {
                // Native Ollama and its OpenAI-compatible /v1 surface are deliberately separate
                // provider identities. Do not suppress one merely because both share host/port.
                // Council selection keys already include provider + endpoint + model, so both can
                // coexist without same-name ambiguity.
                var discovered = await probe.Task.ConfigureAwait(false);
                foreach (var candidate in discovered)
                    AddCandidate(candidates, candidate);

                if (!string.IsNullOrWhiteSpace(probe.Local.ModelName))
                {
                    AddCandidate(candidates, new MultiModelCouncilModelCandidate(
                        probe.Local.ModelName.Trim(), probe.ProviderName, probe.Endpoint,
                        IsInstalled: discovered.Any(item => item.ModelName.Equals(probe.Local.ModelName, StringComparison.OrdinalIgnoreCase)),
                        IsConfigured: true,
                        IsLoaded: false,
                        Details: "Configured OpenAI-compatible model.",
                        ProviderKind: ProviderModelKinds.OpenAICompatible,
                        IsLocal: IsLocalEndpoint(probe.Endpoint),
                        SupportsBenchmark: true));
                }
            }

            if (options.OpenAICore is { ModelName.Length: > 0 } openAi && HasRealApiKey(openAi.ApiKey))
            {
                var endpoint = NormalizeOpenAiEndpoint(string.IsNullOrWhiteSpace(openAi.Endpoint)
                    ? "https://api.openai.com/v1"
                    : openAi.Endpoint);
                AddCandidate(candidates, new MultiModelCouncilModelCandidate(
                    openAi.ModelName.Trim(), "OpenAI", endpoint,
                    IsInstalled: false, IsConfigured: true, IsLoaded: false,
                    Details: "Configured OpenAI cloud model. Connectivity is verified when the model is benchmarked or called.",
                    ProviderKind: ProviderModelKinds.OpenAI,
                    IsLocal: false,
                    SupportsBenchmark: true));
            }

            if (options.OpenAIServiceCore is { Endpoint.Length: > 0, Key.Length: > 0, DeploymentName.Length: > 0 } azure)
            {
                AddCandidate(candidates, new MultiModelCouncilModelCandidate(
                    azure.DeploymentName.Trim(), "Azure OpenAI", azure.Endpoint.TrimEnd('/'),
                    IsInstalled: false, IsConfigured: true, IsLoaded: false,
                    Details: "Configured Azure OpenAI deployment. Connectivity is verified when the deployment is benchmarked or called.",
                    ProviderKind: ProviderModelKinds.AzureOpenAI,
                    IsLocal: false,
                    SupportsBenchmark: true));
            }

            foreach (var candidate in candidates.Values)
                Remember(candidate.ToReference());

            return candidates.Values
                .OrderByDescending(candidate => candidate.IsInstalled)
                .ThenByDescending(candidate => candidate.IsConfigured)
                .ThenByDescending(candidate => candidate.IsLoaded)
                .ThenBy(candidate => candidate.Provider)
                .ThenBy(candidate => candidate.ModelName)
                .ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(GetCandidatesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(GetCandidatesAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs resolve as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="selectionOrModelName">Selection or model name value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The provider model reference produced by the operation.</returns>
    public async Task<ProviderModelReference> ResolveAsync(string selectionOrModelName, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(selectionOrModelName);
            var requested = selectionOrModelName.Trim();
            if (referenceCache.TryGetValue(requested, out var cached))
                return cached;
            var candidates = await GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
            var exact = candidates.FirstOrDefault(candidate =>
                candidate.SelectionKey.Equals(requested, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                var resolved = exact.ToReference();
                referenceCache[resolved.SelectionKey] = resolved;
                return resolved;
            }

            var byModel = candidates
                .Where(candidate => candidate.ModelName.Equals(requested, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (byModel.Count == 1)
            {
                var resolved = byModel[0].ToReference();
                referenceCache[resolved.SelectionKey] = resolved;
                return resolved;
            }
            if (byModel.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Model name '{requested}' is exposed by multiple providers. Select the provider-qualified model entry instead of using the bare model name.");
            }
            if (new ProviderModelIdentity().LooksProviderQualified(requested))
            {
                throw new KeyNotFoundException(
                    $"The provider-qualified model '{requested}' is no longer available. Refresh provider models instead of falling back to another provider.");
            }

            var options = optionsRoot.CurrentValue.AICore ?? new AICoreOptions();
            var fallbackEndpoint = NormalizeOllamaEndpoint(options.OllamaCore?.Uri ?? "http://127.0.0.1:11434");
            var fallback = new ProviderModelReference
            {
                ProviderKind = ProviderModelKinds.Ollama,
                ProviderName = "Ollama",
                Endpoint = fallbackEndpoint,
                ModelName = requested,
                IsLocal = true,
                IsConfigured = false,
                IsReachable = false,
                SupportsBenchmark = true,
                Details = "Legacy bare model name resolved through the primary Ollama provider."
            };
            referenceCache[fallback.SelectionKey] = fallback;
            return fallback;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(ResolveAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(ResolveAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs remember as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="model">Model value supplied to the provider model runtime operation and used when producing its result.</param>
    public void Remember(ProviderModelReference model)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentException.ThrowIfNullOrWhiteSpace(model.ProviderKind);
            ArgumentException.ThrowIfNullOrWhiteSpace(model.ProviderName);
            ArgumentException.ThrowIfNullOrWhiteSpace(model.Endpoint);
            ArgumentException.ThrowIfNullOrWhiteSpace(model.ModelName);
            referenceCache[model.SelectionKey] = model;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(Remember)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(Remember)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs from session as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <returns>The provider model reference produced by the operation.</returns>
    public ProviderModelReference FromSession(ChatClientSession session)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(session);
            return new ProviderModelReference
            {
                ProviderKind = InferProviderKind(session.Provider, session.Endpoint),
                ProviderName = session.Provider,
                Endpoint = session.Endpoint,
                ModelName = session.ModelName,
                IsLocal = IsLocalEndpoint(session.Endpoint),
                IsConfigured = true,
                IsReachable = true,
                SupportsBenchmark = true,
                Details = "Active Chat session."
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(FromSession)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(FromSession)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates chat client as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="model">Model value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="keepAlive">Keep alive value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="maxContextTokens">Max context tokens value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="timeout">Timeout value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="ollamaNumGpu">Ollama num gpu value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="enableAutomaticTools">Value indicating whether enable automatic tools should apply to this operation.</param>
    /// <param name="throwOnFailure">Value indicating whether throw on failure should apply to this operation.</param>
    /// <param name="automaticFunctionAllowList">Optional exact registered-function allow-list for provider-native automatic tools.</param>
    /// <returns>The i chat client produced by the operation.</returns>
    public IChatClient CreateChatClient(
        ProviderModelReference model,
        string keepAlive,
        int maxContextTokens,
        TimeSpan timeout,
        int? ollamaNumGpu,
        bool enableAutomaticTools = true,
        bool throwOnFailure = false,
        IReadOnlyCollection<string>? automaticFunctionAllowList = null)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(model);
            var options = optionsRoot.CurrentValue.AICore ?? new AICoreOptions();

            if (model.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase))
            {
                return new LoggingChatClient(
                    new OllamaThinkingChatClient(
                        new OllamaCoreOptions { Uri = NormalizeOllamaEndpoint(model.Endpoint), ModelName = model.ModelName },
                        logger,
                        councilRuntime,
                        keepAlive,
                        maxContextTokens,
                        timeout,
                        ollamaNumGpu,
                        formatterFactory,
                        protocolResolver,
                        promptConfigService,
                        functionRegistry,
                        functionCallRecovery,
                        enableAutomaticTools,
                        throwOnFailure,
                        automaticFunctionAllowList),
                    loggerFactory.CreateLogger($"AI.Ollama.{model.StableId}"));
            }

            if (model.ProviderKind.Equals(ProviderModelKinds.AzureOpenAI, StringComparison.OrdinalIgnoreCase))
            {
                var azure = options.OpenAIServiceCore;
                if (azure is null || string.IsNullOrWhiteSpace(azure.Key) || string.IsNullOrWhiteSpace(azure.Endpoint))
                    throw new InvalidOperationException("Azure OpenAI credentials are not configured.");
                EnsureCredentialEndpointMatch(model.Endpoint, azure.Endpoint, "Azure OpenAI");
                var client = new AzureOpenAIClient(
                        new Uri(new ProviderModelIdentity().NormalizeEndpoint(azure.Endpoint), UriKind.Absolute),
                        new AzureKeyCredential(azure.Key),
                        new AzureOpenAIClientOptions
                        {
                            ClientLoggingOptions = CreateLoggingOptions()
                        })
                    .GetChatClient(model.ModelName)
                    .AsIChatClient();
                return new LoggingChatClient(client, loggerFactory.CreateLogger($"AI.AzureOpenAI.{model.StableId}"));
            }

            if (!model.ProviderKind.Equals(ProviderModelKinds.OpenAI, StringComparison.OrdinalIgnoreCase)
                && !model.ProviderKind.Equals(ProviderModelKinds.OpenAICompatible, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException($"Provider kind '{model.ProviderKind}' is not supported by the provider-qualified runtime.");
            }

            var endpoint = NormalizeOpenAiEndpoint(model.Endpoint);
            string apiKey;
            if (model.ProviderKind.Equals(ProviderModelKinds.OpenAI, StringComparison.OrdinalIgnoreCase))
            {
                var configuredOpenAi = options.OpenAICore;
                if (configuredOpenAi is null || !HasRealApiKey(configuredOpenAi.ApiKey))
                    throw new InvalidOperationException("The OpenAI API key is not configured.");
                var configuredEndpoint = NormalizeOpenAiEndpoint(string.IsNullOrWhiteSpace(configuredOpenAi.Endpoint)
                    ? "https://api.openai.com/v1"
                    : configuredOpenAi.Endpoint);
                EnsureCredentialEndpointMatch(endpoint, configuredEndpoint, "OpenAI");
                apiKey = configuredOpenAi.ApiKey;
            }
            else
            {
                var configuredLocal = EnumerateOpenAiCompatible(options)
                    .FirstOrDefault(item => NormalizeOpenAiEndpoint(item.Endpoint).Equals(endpoint, StringComparison.OrdinalIgnoreCase));
                var isLoopbackFallback = Uri.TryCreate(endpoint, UriKind.Absolute, out var localUri) && localUri.IsLoopback;
                if (configuredLocal is null && !isLoopbackFallback)
                {
                    throw new InvalidOperationException(
                        "The OpenAI-compatible endpoint is neither configured nor a loopback provider discovered on this machine.");
                }
                // Credentials are endpoint-owned; never forward one configured host's key to another host.
                apiKey = configuredLocal is not null && !string.IsNullOrWhiteSpace(configuredLocal.ApiKey)
                    ? configuredLocal.ApiKey
                    : "local-no-key";
            }

            var openAiClient = new global::OpenAI.OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions
                {
                    Endpoint = new Uri(endpoint, UriKind.Absolute),
                    ClientLoggingOptions = CreateLoggingOptions()
                });
            var chat = openAiClient.GetChatClient(model.ModelName).AsIChatClient();
            return new LoggingChatClient(chat, loggerFactory.CreateLogger($"AI.OpenAICompatible.{model.StableId}"));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(CreateChatClient)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(CreateChatClient)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates session as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="model">Model value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The chat client session produced by the operation.</returns>
    public Task<ChatClientSession> CreateSessionAsync(ProviderModelReference model, CancellationToken cancellationToken = default)
    {
    try
    {
            cancellationToken.ThrowIfCancellationRequested();
            var client = CreateChatClient(model, "2m", 65536, TimeSpan.FromMinutes(30), null);
            return Task.FromResult(new ChatClientSession(
                client,
                model.SelectionKey,
                model.ProviderName,
                model.ModelName,
                model.Endpoint));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(CreateSessionAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(CreateSessionAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates logging options as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The client logging options produced by the operation.</returns>
    private ClientLoggingOptions CreateLoggingOptions() {
    try
    {
        return new()
    {
        EnableLogging = true,
        EnableMessageLogging = false,
        EnableMessageContentLogging = false,
        LoggerFactory = loggerFactory
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(CreateLoggingOptions)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(CreateLoggingOptions)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs enumerate OpenAI compatible as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<ChatGPTLocalCoreOptions> EnumerateOpenAiCompatible(AICoreOptions options)
    {
        try
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var providers = new List<ChatGPTLocalCoreOptions>();

            void Add(ChatGPTLocalCoreOptions? item)
            {
                if (item is null || string.IsNullOrWhiteSpace(item.Endpoint))
                    return;
                var normalized = NormalizeOpenAiEndpoint(item.Endpoint);
                if (seen.Add(normalized))
                    providers.Add(item);
            }

            Add(options.ChatGPTLocalCore);
            foreach (var item in options.ChatGPTLocalCores ?? [])
                Add(item);
            // Preserve historical local LM Studio discovery while allowing configured remote hosts in parallel.
            Add(new ChatGPTLocalCoreOptions
            {
                Endpoint = "http://127.0.0.1:1234/v1",
                ApiKey = "local-no-key",
                ModelName = string.Empty,
                AutoStartServer = false
            });
            return providers;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Enumerating configured OpenAI-compatible providers failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs enumerate Ollama as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<OllamaCoreOptions> EnumerateOllama(AICoreOptions options)
    {
        try
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var providers = new List<OllamaCoreOptions>();

            void Add(OllamaCoreOptions? item)
            {
                if (item is null || string.IsNullOrWhiteSpace(item.Uri))
                    return;
                var endpoint = NormalizeOllamaEndpoint(item.Uri);
                var modelName = item.ModelName?.Trim() ?? string.Empty;
                if (seen.Add($"{endpoint}|{modelName}"))
                    providers.Add(item);
            }

            Add(options.OllamaCore);
            foreach (var item in options.OllamaCores ?? [])
                Add(item);
            return providers;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Enumerating configured endpoint-qualified Ollama provider bindings failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs enumerate Ollama probe endpoints as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<string> EnumerateOllamaProbeEndpoints(AICoreOptions options)
    {
        try
        {
            var endpoints = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(string? endpoint)
            {
                if (string.IsNullOrWhiteSpace(endpoint))
                    return;
                var normalized = NormalizeOllamaEndpoint(endpoint);
                if (seen.Add(normalized))
                    endpoints.Add(normalized);
            }

            Add(options.OllamaCore?.Uri);
            foreach (var item in options.OllamaCores ?? [])
                Add(item.Uri);

            // A local Ollama has always been a LocalGPT discovery convention. It stays a probe
            // candidate even when a remote host is the configured primary provider.
            Add("http://127.0.0.1:11434");
            return endpoints;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Enumerating Ollama discovery endpoints failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs probe Ollama as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="endpoint">Endpoint value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<IReadOnlyList<MultiModelCouncilModelCandidate>> ProbeOllamaAsync(string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(endpoint), Timeout = TimeSpan.FromSeconds(10) };
            using var tagsResponse = await http.GetAsync("/api/tags", cancellationToken).ConfigureAwait(false);
            tagsResponse.EnsureSuccessStatusCode();
            using var tags = JsonDocument.Parse(await tagsResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            var running = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var psResponse = await http.GetAsync("/api/ps", cancellationToken).ConfigureAwait(false);
                if (psResponse.IsSuccessStatusCode)
                {
                    using var ps = JsonDocument.Parse(await psResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                    if (ps.RootElement.TryGetProperty("models", out var runningModels) && runningModels.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in runningModels.EnumerateArray())
                        {
                            if (item.TryGetProperty("name", out var name) && !string.IsNullOrWhiteSpace(name.GetString()))
                                running.Add(name.GetString()!);
                        }
                    }
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
            {
                logger.LogDebug(exception, "Ollama running-model discovery was unavailable for {Endpoint}.", endpoint);
            }

            var results = new List<MultiModelCouncilModelCandidate>();
            if (tags.RootElement.TryGetProperty("models", out var models) && models.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in models.EnumerateArray())
                {
                    var name = item.TryGetProperty("name", out var nameProperty) ? nameProperty.GetString() : null;
                    if (string.IsNullOrWhiteSpace(name))
                        continue;
                    var details = item.TryGetProperty("details", out var detailProperty)
                        ? detailProperty.ToString()
                        : string.Empty;
                    results.Add(new MultiModelCouncilModelCandidate(
                        name.Trim(), "Ollama", endpoint,
                        IsInstalled: true, IsConfigured: false, IsLoaded: running.Contains(name),
                        Details: details,
                        ProviderKind: ProviderModelKinds.Ollama,
                        IsLocal: IsLocalEndpoint(endpoint),
                        SupportsBenchmark: true));
                }
            }
            return results;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogDebug(exception, "Ollama model discovery failed for {Endpoint}.", endpoint);
            return [];
        }
    }

    /// <summary>
    /// Performs probe OpenAI compatible as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="providerName">Provider name value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="endpoint">Endpoint value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="apiKey">Api key value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="providerKind">Provider kind value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="isLocal">Value indicating whether is local should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<IReadOnlyList<MultiModelCouncilModelCandidate>> ProbeOpenAiCompatibleAsync(
        string providerName,
        string endpoint,
        string? apiKey,
        string providerKind,
        bool isLocal,
        CancellationToken cancellationToken)
    {
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(endpoint + "/"), Timeout = TimeSpan.FromSeconds(10) };
            if (!string.IsNullOrWhiteSpace(apiKey))
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            using var response = await http.GetAsync("models", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return [];
            return data.EnumerateArray()
                .Select(item => item.TryGetProperty("id", out var id) ? id.GetString() : null)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => new MultiModelCouncilModelCandidate(
                    name!.Trim(), providerName, endpoint,
                    IsInstalled: true, IsConfigured: false, IsLoaded: false,
                    Details: "Discovered through the OpenAI-compatible /models endpoint.",
                    ProviderKind: providerKind,
                    IsLocal: isLocal,
                    SupportsBenchmark: true))
                .ToList();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or UriFormatException)
        {
            logger.LogDebug(exception, "OpenAI-compatible model discovery failed for {Endpoint}.", endpoint);
            return [];
        }
    }

    /// <summary>
    /// Adds candidate as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="candidates">Multi model council model candidate dependency used by the provider model runtime workflow to provide the corresponding application capability.</param>
    /// <param name="candidate">Candidate value supplied to the provider model runtime operation and used when producing its result.</param>
    private void AddCandidate(
        IDictionary<string, MultiModelCouncilModelCandidate> candidates,
        MultiModelCouncilModelCandidate candidate)
    {
    try
    {
            var key = candidate.SelectionKey;
            if (!candidates.TryGetValue(key, out var existing))
            {
                candidates[key] = candidate;
                return;
            }
            candidates[key] = candidate with
            {
                IsInstalled = existing.IsInstalled || candidate.IsInstalled,
                IsConfigured = existing.IsConfigured || candidate.IsConfigured,
                IsLoaded = existing.IsLoaded || candidate.IsLoaded,
                Details = string.Join(" ", new[] { existing.Details, candidate.Details }.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct())
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(AddCandidate)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(AddCandidate)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes Ollama endpoint as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="endpoint">Endpoint value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeOllamaEndpoint(string endpoint)
    {
    try
    {
            var normalized = new ProviderModelIdentity().NormalizeEndpoint(endpoint);
            if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
                throw new InvalidOperationException("The Ollama endpoint is not a valid absolute URI.");
            var builder = new UriBuilder(uri) { Path = string.Empty, Query = string.Empty, Fragment = string.Empty };
            return builder.Uri.ToString().TrimEnd('/');
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(NormalizeOllamaEndpoint)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(NormalizeOllamaEndpoint)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes OpenAI endpoint as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="endpoint">Endpoint value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeOpenAiEndpoint(string endpoint)
    {
    try
    {
            var normalized = new ProviderModelIdentity().NormalizeEndpoint(endpoint);
            if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
                throw new InvalidOperationException("The OpenAI-compatible endpoint is not a valid absolute URI.");
            var builder = new UriBuilder(uri);
            if (string.IsNullOrWhiteSpace(builder.Path) || builder.Path == "/")
                builder.Path = "/v1";
            return builder.Uri.ToString().TrimEnd('/');
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(NormalizeOpenAiEndpoint)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(NormalizeOpenAiEndpoint)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves local provider name as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="endpoint">Endpoint value supplied to the provider model runtime operation and used when producing its result.</param>
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
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(GetLocalProviderName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(GetLocalProviderName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Ensures credential endpoint match as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="requestedEndpoint">Requested endpoint value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="configuredEndpoint">Configured endpoint value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="providerName">Provider name value supplied to the provider model runtime operation and used when producing its result.</param>
    private void EnsureCredentialEndpointMatch(string requestedEndpoint, string configuredEndpoint, string providerName)
    {
    try
    {
            var identity = new ProviderModelIdentity();
            var requested = identity.NormalizeEndpoint(requestedEndpoint);
            var configured = identity.NormalizeEndpoint(configuredEndpoint);
            if (!requested.Equals(configured, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"The {providerName} model route does not match the configured credential endpoint. Refresh provider models before retrying.");
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(EnsureCredentialEndpointMatch)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(EnsureCredentialEndpointMatch)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether real API key as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="apiKey">Api key value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool HasRealApiKey(string? apiKey) {
    try
    {
        return !string.IsNullOrWhiteSpace(apiKey)
        && !apiKey.Contains("YOUR_", StringComparison.OrdinalIgnoreCase)
        && !apiKey.Contains("replace", StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(HasRealApiKey)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(HasRealApiKey)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether local endpoint as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="endpoint">Endpoint value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsLocalEndpoint(string endpoint) {
    try
    {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
        && (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(IsLocalEndpoint)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(IsLocalEndpoint)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs infer provider kind as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="provider">Provider value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="endpoint">Endpoint value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string InferProviderKind(string provider, string endpoint)
    {
    try
    {
            if (provider.Contains("Azure", StringComparison.OrdinalIgnoreCase))
                return ProviderModelKinds.AzureOpenAI;
            if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
                return ProviderModelKinds.OpenAI;
            if (provider.Contains("Ollama", StringComparison.OrdinalIgnoreCase))
                return ProviderModelKinds.Ollama;
            return ProviderModelKinds.OpenAICompatible;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(InferProviderKind)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelRuntimeService)}.{nameof(InferProviderKind)} failed.");
        throw;
    }
}
}

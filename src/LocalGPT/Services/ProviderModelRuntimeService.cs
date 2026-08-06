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
public sealed class ProviderModelRuntimeService(
    IOptionsMonitor<ConfigurationRoot> optionsRoot,
    ILoggerFactory loggerFactory,
    ILogger<ProviderModelRuntimeService> logger,
    CouncilRuntimeService councilRuntime,
    IChatResponseFormatterFactory formatterFactory,
    IChatProtocolResolver protocolResolver,
    IPromptConfigService promptConfigService,
    IDxAiFunctionRegistry functionRegistry) : IProviderModelRuntimeService
{
    private readonly ConcurrentDictionary<string, ProviderModelReference> referenceCache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<MultiModelCouncilModelCandidate>> GetCandidatesAsync(CancellationToken cancellationToken = default)
    {
        var options = optionsRoot.CurrentValue.AICore ?? new AICoreOptions();
        var candidates = new Dictionary<string, MultiModelCouncilModelCandidate>(StringComparer.OrdinalIgnoreCase);
        var ollamaOptions = EnumerateOllama(options).ToList();
        var ollamaAuthorities = ollamaOptions
            .Select(item => GetAuthority(NormalizeOllamaEndpoint(item.Uri)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var ollama in ollamaOptions)
        {
            var endpoint = NormalizeOllamaEndpoint(ollama.Uri);
            AddCandidate(candidates, new MultiModelCouncilModelCandidate(
                ollama.ModelName.Trim(), "Ollama", endpoint,
                IsInstalled: false, IsConfigured: true, IsLoaded: false,
                Details: "Configured Ollama model.",
                ProviderKind: ProviderModelKinds.Ollama,
                IsLocal: true,
                SupportsBenchmark: true));

            foreach (var discovered in await ProbeOllamaAsync(endpoint, cancellationToken).ConfigureAwait(false))
                AddCandidate(candidates, discovered);
        }

        if (options.ChatGPTLocalCore is { Endpoint.Length: > 0 } local)
        {
            var configuredEndpoint = NormalizeOpenAiEndpoint(local.Endpoint);
            var localEndpoints = new[]
            {
                configuredEndpoint,
                NormalizeOpenAiEndpoint("http://127.0.0.1:1234/v1")
            }
            .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var endpoint in localEndpoints)
            {
                // Ollama exposes an OpenAI-compatible facade at /v1. Do not present the same host twice:
                // its native Ollama address is the authoritative identity because it also supports load/unload controls.
                if (ollamaAuthorities.Contains(GetAuthority(endpoint)))
                    continue;

                var providerName = GetLocalProviderName(endpoint);
                var discoveryApiKey = endpoint.Equals(configuredEndpoint, StringComparison.OrdinalIgnoreCase)
                    ? local.ApiKey
                    : null;
                var discovered = await ProbeOpenAiCompatibleAsync(
                    providerName,
                    endpoint,
                    discoveryApiKey,
                    ProviderModelKinds.OpenAICompatible,
                    isLocal: true,
                    cancellationToken).ConfigureAwait(false);
                foreach (var candidate in discovered)
                    AddCandidate(candidates, candidate);

                if (endpoint.Equals(configuredEndpoint, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(local.ModelName))
                {
                    AddCandidate(candidates, new MultiModelCouncilModelCandidate(
                        local.ModelName.Trim(), providerName, endpoint,
                        IsInstalled: discovered.Any(item => item.ModelName.Equals(local.ModelName, StringComparison.OrdinalIgnoreCase)),
                        IsConfigured: true,
                        IsLoaded: false,
                        Details: "Configured local OpenAI-compatible model.",
                        ProviderKind: ProviderModelKinds.OpenAICompatible,
                        IsLocal: true,
                        SupportsBenchmark: true));
                }
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

    public async Task<ProviderModelReference> ResolveAsync(string selectionOrModelName, CancellationToken cancellationToken = default)
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

    public void Remember(ProviderModelReference model)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.ProviderKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.ProviderName);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.Endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.ModelName);
        referenceCache[model.SelectionKey] = model;
    }

    public ProviderModelReference FromSession(ChatClientSession session)
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

    public IChatClient CreateChatClient(
        ProviderModelReference model,
        string keepAlive,
        int maxContextTokens,
        TimeSpan timeout,
        int? ollamaNumGpu,
        bool enableAutomaticTools = true,
        bool throwOnFailure = false)
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
                    enableAutomaticTools,
                    throwOnFailure),
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
            var configuredLocal = options.ChatGPTLocalCore;
            var configuredEndpoint = configuredLocal is { Endpoint.Length: > 0 }
                ? NormalizeOpenAiEndpoint(configuredLocal.Endpoint)
                : string.Empty;
            var isConfiguredEndpoint = endpoint.Equals(configuredEndpoint, StringComparison.OrdinalIgnoreCase);
            var isLoopbackFallback = Uri.TryCreate(endpoint, UriKind.Absolute, out var localUri) && localUri.IsLoopback;
            if (!isConfiguredEndpoint && !isLoopbackFallback)
            {
                throw new InvalidOperationException(
                    "The local OpenAI-compatible endpoint is neither the configured endpoint nor a loopback provider discovered on this machine.");
            }
            // Never forward a configured local-provider credential to an unrelated fallback endpoint.
            apiKey = isConfiguredEndpoint && !string.IsNullOrWhiteSpace(configuredLocal?.ApiKey)
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

    public Task<ChatClientSession> CreateSessionAsync(ProviderModelReference model, CancellationToken cancellationToken = default)
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

    private ClientLoggingOptions CreateLoggingOptions() => new()
    {
        EnableLogging = true,
        EnableMessageLogging = false,
        EnableMessageContentLogging = false,
        LoggerFactory = loggerFactory
    };

    private IReadOnlyList<OllamaCoreOptions> EnumerateOllama(AICoreOptions options)
    {
        try
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var providers = new List<OllamaCoreOptions>();
            if (options.OllamaCore is { Uri.Length: > 0, ModelName.Length: > 0 } primary
                && seen.Add($"{NormalizeOllamaEndpoint(primary.Uri)}|{primary.ModelName.Trim()}"))
            {
                providers.Add(primary);
            }
            foreach (var item in options.OllamaCores.Where(item => !string.IsNullOrWhiteSpace(item.Uri) && !string.IsNullOrWhiteSpace(item.ModelName)))
            {
                if (seen.Add($"{NormalizeOllamaEndpoint(item.Uri)}|{item.ModelName.Trim()}"))
                    providers.Add(item);
            }
            return providers;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Enumerating configured Ollama providers failed.");
            throw;
        }
    }

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
                        IsLocal: true,
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

    private void AddCandidate(
        IDictionary<string, MultiModelCouncilModelCandidate> candidates,
        MultiModelCouncilModelCandidate candidate)
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

    private string NormalizeOllamaEndpoint(string endpoint)
    {
        var normalized = new ProviderModelIdentity().NormalizeEndpoint(endpoint);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("The Ollama endpoint is not a valid absolute URI.");
        var builder = new UriBuilder(uri) { Path = string.Empty, Query = string.Empty, Fragment = string.Empty };
        return builder.Uri.ToString().TrimEnd('/');
    }

    private string NormalizeOpenAiEndpoint(string endpoint)
    {
        var normalized = new ProviderModelIdentity().NormalizeEndpoint(endpoint);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("The OpenAI-compatible endpoint is not a valid absolute URI.");
        var builder = new UriBuilder(uri);
        if (string.IsNullOrWhiteSpace(builder.Path) || builder.Path == "/")
            builder.Path = "/v1";
        return builder.Uri.ToString().TrimEnd('/');
    }

    private string GetAuthority(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            return endpoint.Trim().TrimEnd('/');
        var builder = new UriBuilder(uri) { Path = string.Empty, Query = string.Empty, Fragment = string.Empty };
        return builder.Uri.ToString().TrimEnd('/');
    }

    private string GetLocalProviderName(string endpoint) =>
        Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && uri.Port == 1234
            ? "LM Studio"
            : "Local OpenAI-compatible";

    private void EnsureCredentialEndpointMatch(string requestedEndpoint, string configuredEndpoint, string providerName)
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

    private bool HasRealApiKey(string? apiKey) =>
        !string.IsNullOrWhiteSpace(apiKey)
        && !apiKey.Contains("YOUR_", StringComparison.OrdinalIgnoreCase)
        && !apiKey.Contains("replace", StringComparison.OrdinalIgnoreCase);

    private bool IsLocalEndpoint(string endpoint) =>
        Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
        && (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase));

    private string InferProviderKind(string provider, string endpoint)
    {
        if (provider.Contains("Azure", StringComparison.OrdinalIgnoreCase))
            return ProviderModelKinds.AzureOpenAI;
        if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            return ProviderModelKinds.OpenAI;
        if (provider.Contains("Ollama", StringComparison.OrdinalIgnoreCase))
            return ProviderModelKinds.Ollama;
        return ProviderModelKinds.OpenAICompatible;
    }
}

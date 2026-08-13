using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Represents an AI connectivity probe application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
/// <param name="aiDiscovery">Ai discovery service dependency used by the AI connectivity probe workflow to provide the corresponding application capability.</param>
/// <param name="councilText">Council text service dependency used by the AI connectivity probe workflow to provide the corresponding application capability.</param>
/// <param name="optionsRoot">Options root value supplied to the AI connectivity probe operation and used when producing its result.</param>
public sealed class AiConnectivityProbe(ILogger<AiConnectivityProbe> logger,
        AiDiscoveryService aiDiscovery,
        CouncilTextService councilText,
        Microsoft.Extensions.Options.IOptionsMonitor<global::LocalGPT.BusinessObjects.ConfigurationRoot> optionsRoot) : IAiConnectivityProbe
{
    /// <summary>
    /// Performs test azure for <see cref="AiConnectivityProbe"/>, keeping the operation consistent with the state and invariants of the surrounding AI connectivity probe workflow.
    /// </summary>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The bool ok string message produced by the operation.</returns>
    public async Task<(bool ok, string message)> TestAzureAsync(OpenAIServiceCoreOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint) || string.IsNullOrWhiteSpace(options.Key))
            return (false, "Missing endpoint or key.");

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(options.Endpoint) };
            http.DefaultRequestHeaders.Add("api-key", options.Key);
            return await aiDiscovery.GetAsync(http, "/", cancellationToken, logger).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Azure connectivity test failed for endpoint host {EndpointHost}.", GetEndpointHost(options.Endpoint));
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Performs test OpenAI for <see cref="AiConnectivityProbe"/>, keeping the operation consistent with the state and invariants of the surrounding AI connectivity probe workflow.
    /// </summary>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The bool ok string message produced by the operation.</returns>
    public async Task<(bool ok, string message)> TestOpenAIAsync(OpenAICompatOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            return (false, "Missing API key.");

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("https://api.openai.com/v1/") };
            http.DefaultRequestHeaders.Authorization =
                /// <summary>
                /// Runs the authentication header value operation.
                /// </summary>
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            return await aiDiscovery.GetAsync(http, "models", cancellationToken, logger).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OpenAI connectivity test failed.");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Performs test Ollama for <see cref="AiConnectivityProbe"/>, keeping the operation consistent with the state and invariants of the surrounding AI connectivity probe workflow.
    /// </summary>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The bool ok string message produced by the operation.</returns>
    public async Task<(bool ok, string message)> TestOllamaAsync(OllamaCoreOptions options, CancellationToken cancellationToken)
    {
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(options.Uri) };
            return await aiDiscovery.GetAsync(http, "/api/tags", cancellationToken, logger).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ollama connectivity test failed for endpoint host {EndpointHost}.", GetEndpointHost(options.Uri));
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Performs test local OpenAI compat for <see cref="AiConnectivityProbe"/>, keeping the operation consistent with the state and invariants of the surrounding AI connectivity probe workflow.
    /// </summary>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The bool ok string message produced by the operation.</returns>
    public async Task<(bool ok, string message)> TestLocalOpenAICompatAsync(ChatGPTLocalCoreOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = councilText.NormalizeOpenAIEndpoint(options.Endpoint, logger);
            using var http = new HttpClient { BaseAddress = new Uri(endpoint) };
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                http.DefaultRequestHeaders.Authorization =
                    /// <summary>
                    /// Runs the authentication header value operation.
                    /// </summary>
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            }

            return await aiDiscovery.GetAsync(http, "/v1/models", cancellationToken, logger).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Local OpenAI-compatible connectivity test failed for endpoint host {EndpointHost}.", GetEndpointHost(options.Endpoint));
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Attempts to start local for <see cref="AiConnectivityProbe"/>, keeping the operation consistent with the state and invariants of the surrounding AI connectivity probe workflow.
    /// </summary>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The bool ok string message produced by the operation.</returns>
    public Task<(bool ok, string message)> TryStartLocalAsync(ChatGPTLocalCoreOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!options.AutoStartServer)
            return Task.FromResult((false, "Local server auto-start is disabled in configuration."));

        logger.LogWarning(
            "Local server auto-start was requested for endpoint host {EndpointHost}, but unrestricted shell launch is disabled. Start the provider manually or add a bounded launcher service.",
            GetEndpointHost(options.Endpoint));
        return Task.FromResult((false,
            "Automatic shell launch is disabled. Start the local provider manually; unrestricted StartCommand execution is no longer permitted."));
    }

    /// <summary>
    /// Discovers local hosts for <see cref="AiConnectivityProbe"/>, keeping the operation consistent with the state and invariants of the surrounding AI connectivity probe workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<LocalAiHostDiscoveryResult>> DiscoverLocalHostsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var options = optionsRoot.CurrentValue.AICore ?? new AICoreOptions();
            var probes = new List<Task<LocalAiHostDiscoveryResult>>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddOllama(string? endpoint)
            {
                if (!TryNormalizeAuthority(endpoint, out var normalized) || !seen.Add($"ollama|{normalized}"))
                    return;
                probes.Add(aiDiscovery.ProbeOllamaAsync(normalized, cancellationToken, logger));
            }

            void AddOpenAiCompatible(string provider, string? endpoint)
            {
                if (!TryNormalizeAuthority(endpoint, out var normalized) || !seen.Add($"openai|{normalized}"))
                    return;
                probes.Add(aiDiscovery.ProbeOpenAICompatibleAsync(provider, normalized, cancellationToken, logger));
            }

            // Configured endpoints are authoritative. Local defaults remain discovery candidates only;
            // they must never rewrite a configured remote provider merely because localhost responds.
            AddOllama(options.OllamaCore?.Uri);
            foreach (var configured in options.OllamaCores ?? [])
                AddOllama(configured.Uri);

            AddOpenAiCompatible("OpenAI-compatible", options.ChatGPTLocalCore?.Endpoint);
            foreach (var configured in options.ChatGPTLocalCores ?? [])
                AddOpenAiCompatible("OpenAI-compatible", configured.Endpoint);

            AddOllama("http://localhost:11434");
            AddOpenAiCompatible("LM Studio", "http://localhost:1234");
            AddOpenAiCompatible("Local OpenAI-compatible", "http://localhost:8080");

            return probes.Count == 0
                ? []
                : await Task.WhenAll(probes).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Configured/local AI host discovery failed.");
            return [];
        }
    }

    /// <summary>
    /// Attempts to normalize authority for <see cref="AiConnectivityProbe"/>, keeping the operation consistent with the state and invariants of the surrounding AI connectivity probe workflow.
    /// </summary>
    /// <param name="endpoint">Endpoint value supplied to the AI connectivity probe operation and used when producing its result.</param>
    /// <param name="normalized">Normalized value supplied to the AI connectivity probe operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool TryNormalizeAuthority(string? endpoint, out string normalized)
    {
        try
        {
            normalized = string.Empty;
            if (!Uri.TryCreate(endpoint?.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return false;

            // Provider identity canonicalization is shared with Chat/Council configuration so aliases such as
            // localhost and 127.0.0.1 can never become two discovery/provider cards for the same Ollama host.
            var canonicalEndpoint = new ProviderModelIdentity().NormalizeEndpoint(uri.GetLeftPart(UriPartial.Authority));
            if (!Uri.TryCreate(canonicalEndpoint, UriKind.Absolute, out var canonicalUri))
                return false;

            var builder = new UriBuilder(canonicalUri.Scheme, canonicalUri.Host, canonicalUri.Port);
            normalized = builder.Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI endpoint normalization failed while enumerating configured hosts.");
            normalized = string.Empty;
            return false;
        }
    }

    /// <summary>
    /// Retrieves endpoint host for <see cref="AiConnectivityProbe"/>, keeping the operation consistent with the state and invariants of the surrounding AI connectivity probe workflow.
    /// </summary>
    /// <param name="endpoint">Endpoint value supplied to the AI connectivity probe operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string GetEndpointHost(string? endpoint) {
    try
    {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? uri.Host : "invalid-or-unset";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(AiConnectivityProbe)}.{nameof(GetEndpointHost)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(AiConnectivityProbe)}.{nameof(GetEndpointHost)} failed.");
        throw;
    }
}
}

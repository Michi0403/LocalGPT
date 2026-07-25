using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class AiConnectivityProbe(ILogger<AiConnectivityProbe> logger) : IAiConnectivityProbe
{
    public async Task<(bool ok, string message)> TestAzureAsync(OpenAIServiceCoreOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint) || string.IsNullOrWhiteSpace(options.Key))
            return (false, "Missing endpoint or key.");

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(options.Endpoint) };
            http.DefaultRequestHeaders.Add("api-key", options.Key);
            return await HttpAIStaticsGeneral.GetAsync(http, "/", cancellationToken, logger).ConfigureAwait(false);
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

    public async Task<(bool ok, string message)> TestOpenAIAsync(OpenAICompatOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            return (false, "Missing API key.");

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("https://api.openai.com/v1/") };
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            return await HttpAIStaticsGeneral.GetAsync(http, "models", cancellationToken, logger).ConfigureAwait(false);
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

    public async Task<(bool ok, string message)> TestOllamaAsync(OllamaCoreOptions options, CancellationToken cancellationToken)
    {
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(options.Uri) };
            return await HttpAIStaticsGeneral.GetAsync(http, "/api/tags", cancellationToken, logger).ConfigureAwait(false);
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

    public async Task<(bool ok, string message)> TestLocalOpenAICompatAsync(ChatGPTLocalCoreOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = CouncilChatStringFunctions.NormalizeOpenAIEndpoint(options.Endpoint, logger);
            using var http = new HttpClient { BaseAddress = new Uri(endpoint) };
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            }

            return await HttpAIStaticsGeneral.GetAsync(http, "/v1/models", cancellationToken, logger).ConfigureAwait(false);
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

    public async Task<IReadOnlyList<LocalAiHostDiscoveryResult>> DiscoverLocalHostsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var probes = new[]
            {
                HttpAIStaticsGeneral.ProbeOllamaAsync("http://localhost:11434", cancellationToken, logger),
                HttpAIStaticsGeneral.ProbeOpenAICompatibleAsync("LM Studio", "http://localhost:1234", cancellationToken, logger),
                HttpAIStaticsGeneral.ProbeOpenAICompatibleAsync("Local OpenAI-compatible", "http://localhost:8080", cancellationToken, logger)
            };

            return await Task.WhenAll(probes).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Local AI host discovery failed.");
            return [];
        }
    }

    private static string GetEndpointHost(string? endpoint) =>
        Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? uri.Host : "invalid-or-unset";
}

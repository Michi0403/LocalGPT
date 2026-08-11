using LocalGPT.BusinessObjects;
using System.Text.Json;

namespace LocalGPT.Services
{
    /// <summary>
    /// Provides ai discovery service operations.
    /// </summary>
    public sealed class AiDiscoveryService(
        CouncilRuntimeService runtime,
        CouncilTextService text,
        LocalGptCatalogService catalog,
        ILogger<AiDiscoveryService> serviceLogger)
    {
        private readonly CouncilRuntimeService _runtime = runtime;
        private readonly CouncilTextService _text = text;
        private readonly LocalGptCatalogService _catalog = catalog;

        /// <summary>
        /// Gets async.
        /// </summary>
        public async Task<(bool ok, string msg)> GetAsync(HttpClient http, string path, CancellationToken ct, ILogger<AiConnectivityProbe> logger)
        {
            try
            {
                serviceLogger.LogInformation("AI discovery GET operation started for host {EndpointHost}; request content was omitted.", http.BaseAddress?.Host ?? "invalid-or-unset");
                using var res = await http.GetAsync(path, ct).ConfigureAwait(false);
                var body = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                serviceLogger.LogInformation("AI discovery GET operation completed for host {EndpointHost} with status {StatusCode}.", http.BaseAddress?.Host ?? "invalid-or-unset", (int)res.StatusCode);
                return (res.IsSuccessStatusCode, $"{(int)res.StatusCode} {res.ReasonPhrase}: {body}");
            }
            catch (OperationCanceledException exception) when (ct.IsCancellationRequested)
            {
                serviceLogger.LogInformation(exception, "AI discovery operation was cancelled by the caller.");
                throw;
            }
            catch (OperationCanceledException ex)
            {
                logger.LogInformation(
                    $"AI connectivity request to {{EndpointHost}} did not answer before the configured timeout.",
                    http.BaseAddress?.Host ?? "invalid-or-unset");
                return (false, ex.Message);
            }
            catch (HttpRequestException ex)
            {
                logger.LogInformation(
                    $"AI connectivity request to {{EndpointHost}} could not establish a connection.",
                    http.BaseAddress?.Host ?? "invalid-or-unset");
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Unexpected error in GetAsync for host {{EndpointHost}} and path {{Path}}.", http.BaseAddress?.Host ?? "invalid-or-unset", path);
                return (false, ex.Message);
            }
        }
        /// <summary>
        /// Creates discovery client.
        /// </summary>
        public HttpClient CreateDiscoveryClient(string endpoint, ILogger<AiConnectivityProbe> logger)
        {
            try
            {
                return new HttpClient
                {
                    BaseAddress = new Uri(endpoint.TrimEnd('/')),
                    Timeout = TimeSpan.FromSeconds(3)
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create a discovery client for endpoint {Endpoint}.", endpoint);
                throw new InvalidOperationException("The configured AI endpoint is not a valid absolute URI.", ex);
            }
        }
        /// <summary>
        /// Runs the probe open aicompatible async operation.
        /// </summary>
        public async Task<LocalAiHostDiscoveryResult> ProbeOpenAICompatibleAsync(string provider, string endpoint, CancellationToken ct, ILogger<AiConnectivityProbe> logger)
        {
            var result = new LocalAiHostDiscoveryResult
            {
                Provider = provider,
                Endpoint = endpoint
            };

            try
            {
                serviceLogger.LogInformation("AI provider discovery started for {Provider} at host {EndpointHost}.", provider, GetEndpointHost(endpoint));
                using var http = CreateDiscoveryClient(endpoint, logger);
                using var response = await http.GetAsync("/v1/models", ct).ConfigureAwait(false); 
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    result.Status = $"{(int)response.StatusCode} {response.ReasonPhrase}";
                    return result;
                }

                result.IsReachable = true;
                var models = JsonSerializer.Deserialize<OpenAIModelsResponse>(body, _catalog.JsonOptions)?.Data ?? new();
                result.Models = models
                    .Where(m => !string.IsNullOrWhiteSpace(m.Id))
                    .Select(m => new LocalAiModelInfo
                    {
                        Name = m.Id,
                        IsLoaded = true,
                        Details = provider
                    })
                    .ToList();

                result.Status = result.Models.Count == 0
                    ? $"{provider} is reachable, but returned no models."
                    : $"Found {result.Models.Count} {provider} model(s).";
            }
            catch (OperationCanceledException exception) when (ct.IsCancellationRequested)
            {
                serviceLogger.LogInformation(exception, "AI discovery operation was cancelled by the caller.");
                throw;
            }
            catch (OperationCanceledException ex)
            {
                var endpointHost = Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri)
                    ? endpointUri.Host
                    : "invalid-or-unset";
                logger.LogInformation(
                    $"Optional local AI provider {{Provider}} at host {{EndpointHost}} did not answer before the configured discovery timeout.",
                    provider,
                    endpointHost);
                result.Status = ex.Message;
            }
            catch (HttpRequestException ex)
            {
                var endpointHost = Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri)
                    ? endpointUri.Host
                    : "invalid-or-unset";
                logger.LogInformation(
                    $"Optional local AI provider {{Provider}} at host {{EndpointHost}} is not currently reachable.",
                    provider,
                    endpointHost);
                result.Status = ex.Message;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Unexpected error while probing provider {{Provider}} at endpoint {{Endpoint}}.", provider, endpoint);
                result.Status = ex.Message;
            }

            serviceLogger.LogInformation("AI provider discovery completed for {Provider}; reachable={IsReachable}; models={ModelCount}.", provider, result.IsReachable, result.Models.Count);
            return result;

        }

        /// <summary>
        /// Runs the probe ollama async operation.
        /// </summary>
        public async Task<LocalAiHostDiscoveryResult> ProbeOllamaAsync(string endpoint, CancellationToken ct, ILogger<AiConnectivityProbe> logger)
        {
            var result = new LocalAiHostDiscoveryResult
            {
                Provider = "Ollama",
                Endpoint = endpoint
            };

            try
            {
                serviceLogger.LogInformation("Ollama discovery started at host {EndpointHost}.", GetEndpointHost(endpoint));
                using var http = CreateDiscoveryClient(endpoint, logger);
                using var tagsResponse = await http.GetAsync("/api/tags", ct).ConfigureAwait(false);
                var tagsBody = await tagsResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                if (!tagsResponse.IsSuccessStatusCode)
                {
                    result.Status = $"{(int)tagsResponse.StatusCode} {tagsResponse.ReasonPhrase}";
                    return result;
                }

                result.IsReachable = true;
                var installed = JsonSerializer.Deserialize<OllamaTagsResponse>(tagsBody, _catalog.JsonOptions)?.Models ?? new();
                foreach (var model in installed)
                {
                    var name = _runtime.FirstText(logger, model.Model, model.Name);
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    result.Models.Add(new LocalAiModelInfo
                    {
                        Name = name,
                        Details = _text.BuildOllamaDetails(model.Details, logger)
                    });
                }

                using var psResponse = await http.GetAsync("/api/ps", ct).ConfigureAwait(false);
                if (psResponse.IsSuccessStatusCode)
                {
                    var psBody = await psResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    var loaded = JsonSerializer.Deserialize<OllamaTagsResponse>(psBody, _catalog.JsonOptions)?.Models ?? new();
                    var loadedNames = loaded
                        .Select(m => _runtime.FirstText(logger, m.Model, m.Name))
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (var model in result.Models)
                        model.IsLoaded = loadedNames.Contains(model.Name);

                    foreach (var loadedModel in loaded)
                    {
                        var name = _runtime.FirstText(logger, loadedModel.Model, loadedModel.Name);
                        if (!string.IsNullOrWhiteSpace(name) &&
                            result.Models.All(m => !string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase)))
                        {
                            result.Models.Add(new LocalAiModelInfo
                            {
                                Name = name,
                                IsLoaded = true,
                                Details = _text.BuildOllamaDetails(loadedModel.Details, logger)
                            });
                        }
                    }
                }

                result.Status = result.Models.Count == 0
                    ? "Ollama is reachable, but no models are installed yet."
                    : $"Found {result.Models.Count} Ollama model(s).";
            }
            catch (OperationCanceledException exception) when (ct.IsCancellationRequested)
            {
                serviceLogger.LogInformation(exception, "AI discovery operation was cancelled by the caller.");
                throw;
            }
            catch (OperationCanceledException ex)
            {
                var endpointHost = Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri)
                    ? endpointUri.Host
                    : "invalid-or-unset";
                logger.LogInformation(
                    $"Optional Ollama discovery at host {{EndpointHost}} did not answer before the configured discovery timeout.",
                    endpointHost);
                result.Status = ex.Message;
            }
            catch (HttpRequestException ex)
            {
                var endpointHost = Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri)
                    ? endpointUri.Host
                    : "invalid-or-unset";
                logger.LogInformation(
                    $"Optional Ollama discovery at host {{EndpointHost}} is not currently reachable.",
                    endpointHost);
                result.Status = ex.Message;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Unexpected error while probing Ollama endpoint {{Endpoint}}.", endpoint);
                result.Status = ex.Message;
            }

            serviceLogger.LogInformation("Ollama discovery completed; reachable={IsReachable}; models={ModelCount}.", result.IsReachable, result.Models.Count);
            return result;
        }

        /// <summary>
        /// Gets endpoint host.
        /// </summary>
        private string GetEndpointHost(string endpoint) {
    try
    {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? uri.Host : "invalid-or-unset";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(AiDiscoveryService)}.{nameof(GetEndpointHost)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(AiDiscoveryService)}.{nameof(GetEndpointHost)} failed.");
        throw;
    }
}
    }
}

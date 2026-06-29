using LocalGPT.BusinessObjects;
using LocalGPT.Services;
using System.Text.Json;
using static LocalGPT.Extensions.PlainStatics.GlobalVariableSlopCollectionToRemove;

namespace LocalGPT.Extensions.PlainStatics
{
    public static class HttpAIStaticsGeneral
    {
        public static async Task<(bool ok, string msg)> GetAsync(HttpClient http, string path, CancellationToken ct, ILogger<AiConnectivityProbe> logger)
        {
            try
            {
                using var res = await http.GetAsync(path, ct).ConfigureAwait(false);
                var body = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return (res.IsSuccessStatusCode, $"{(int)res.StatusCode} {res.ReasonPhrase}: {body}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetAsync http {http.ToString()} path {path?.ToString()}");
                return (false, ex.Message);
            }
        }
        public static HttpClient? CreateDiscoveryClient(string endpoint, ILogger<AiConnectivityProbe> logger)
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
                logger.LogError(ex, $"Error in CreateDiscoveryClient endpoint {endpoint.ToString()}");
                return null;
            }
        }
        public static async Task<LocalAiHostDiscoveryResult> ProbeOpenAICompatibleAsync(string provider, string endpoint, CancellationToken ct, ILogger<AiConnectivityProbe> logger)
        {
            var result = new LocalAiHostDiscoveryResult
            {
                Provider = provider,
                Endpoint = endpoint
            };

            try
            {
                using var http = CreateDiscoveryClient(endpoint, logger);
                using var response = await http.GetAsync("/v1/models", ct).ConfigureAwait(false); 
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    result.Status = $"{(int)response.StatusCode} {response.ReasonPhrase}";
                    return result;
                }

                result.IsReachable = true;
                var models = JsonSerializer.Deserialize<OpenAIModelsResponse>(body, JsonOptions)?.Data ?? new();
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ProbeOpenAICompatibleAsync provider {provider.ToString()} endpoint {endpoint?.ToString()}");
                result.Status = ex.Message;
            }

            return result;

        }

        public static async Task<LocalAiHostDiscoveryResult> ProbeOllamaAsync(string endpoint, CancellationToken ct, ILogger<AiConnectivityProbe> logger)
        {
            var result = new LocalAiHostDiscoveryResult
            {
                Provider = "Ollama",
                Endpoint = endpoint
            };

            try
            {
                using var http = CreateDiscoveryClient(endpoint, logger);
                using var tagsResponse = await http.GetAsync("/api/tags", ct).ConfigureAwait(false);
                var tagsBody = await tagsResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                if (!tagsResponse.IsSuccessStatusCode)
                {
                    result.Status = $"{(int)tagsResponse.StatusCode} {tagsResponse.ReasonPhrase}";
                    return result;
                }

                result.IsReachable = true;
                var installed = JsonSerializer.Deserialize<OllamaTagsResponse>(tagsBody, JsonOptions)?.Models ?? new();
                foreach (var model in installed)
                {
                    var name = CouncilChatStaticsGeneral.FirstText(logger, model.Model, model.Name);
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    result.Models.Add(new LocalAiModelInfo
                    {
                        Name = name,
                        Details = CouncilChatStringFunctions.BuildOllamaDetails(model.Details, logger)
                    });
                }

                using var psResponse = await http.GetAsync("/api/ps", ct).ConfigureAwait(false);
                if (psResponse.IsSuccessStatusCode)
                {
                    var psBody = await psResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    var loaded = JsonSerializer.Deserialize<OllamaTagsResponse>(psBody, JsonOptions)?.Models ?? new();
                    var loadedNames = loaded
                        .Select(m => CouncilChatStaticsGeneral.FirstText(logger, m.Model, m.Name))
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (var model in result.Models)
                        model.IsLoaded = loadedNames.Contains(model.Name);

                    foreach (var loadedModel in loaded)
                    {
                        var name = CouncilChatStaticsGeneral.FirstText(logger, loadedModel.Model, loadedModel.Name);
                        if (!string.IsNullOrWhiteSpace(name) &&
                            result.Models.All(m => !string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase)))
                        {
                            result.Models.Add(new LocalAiModelInfo
                            {
                                Name = name,
                                IsLoaded = true,
                                Details = CouncilChatStringFunctions.BuildOllamaDetails(loadedModel.Details, logger)
                            });
                        }
                    }
                }

                result.Status = result.Models.Count == 0
                    ? "Ollama is reachable, but no models are installed yet."
                    : $"Found {result.Models.Count} Ollama model(s).";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ProbeOllamaAsync endpoint {endpoint?.ToString()}");
                result.Status = ex.Message;
            }

            return result;
        }
    }
}

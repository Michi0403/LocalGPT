using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services
{
    public class AiConnectivityProbe : IAiConnectivityProbe
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        private static async Task<(bool ok, string msg)> GetAsync(HttpClient http, string path, CancellationToken ct)
        {
            try
            {
                using var res = await http.GetAsync(path, ct);
                var body = await res.Content.ReadAsStringAsync(ct);
                return (res.IsSuccessStatusCode, $"{(int)res.StatusCode} {res.ReasonPhrase}: {body}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool ok, string message)> TestAzureAsync(OpenAIServiceCoreOptions o, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(o.Endpoint) || string.IsNullOrWhiteSpace(o.Key))
                return (false, "Missing endpoint or key.");

            try
            {
                var http = new HttpClient { BaseAddress = new Uri(o.Endpoint) };
                http.DefaultRequestHeaders.Add("api-key", o.Key);
                return await GetAsync(http, "/", ct);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool ok, string message)> TestOpenAIAsync(OpenAICompatOptions o, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(o.ApiKey))
                return (false, "Missing API key.");

            var http = new HttpClient { BaseAddress = new Uri("https://api.openai.com/v1/") };
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", o.ApiKey);
            return await GetAsync(http, "models", ct);
        }

        public async Task<(bool ok, string message)> TestOllamaAsync(OllamaCoreOptions o, CancellationToken ct)
        {
            var http = new HttpClient { BaseAddress = new Uri(o.Uri) };
            return await GetAsync(http, "/api/tags", ct);
        }

        public async Task<(bool ok, string message)> TestLocalOpenAICompatAsync(ChatGPTLocalCoreOptions o, CancellationToken ct)
        {
            var endpoint = NormalizeOpenAIEndpoint(o.Endpoint);
            var http = new HttpClient { BaseAddress = new Uri(endpoint) };
            if (!string.IsNullOrWhiteSpace(o.ApiKey))
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", o.ApiKey);

            return await GetAsync(http, "/v1/models", ct);
        }

        public async Task<(bool ok, string message)> TryStartLocalAsync(ChatGPTLocalCoreOptions o, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(o.StartCommand))
                return (false, "StartCommand not set.");

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + o.StartCommand,
                    WorkingDirectory = string.IsNullOrWhiteSpace(o.WorkingDir) ? null : o.WorkingDir,
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                System.Diagnostics.Process.Start(psi);

                var started = false;
                var deadline = DateTime.UtcNow.AddSeconds(Math.Max(5, o.HealthTimeoutSeconds));
                while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
                {
                    var (ok, _) = await TestLocalOpenAICompatAsync(o, ct);
                    if (ok)
                    {
                        started = true;
                        break;
                    }

                    await Task.Delay(1000, ct);
                }

                return started ? (true, "Local server is responding.") : (false, "Local server did not respond in time.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<IReadOnlyList<LocalAiHostDiscoveryResult>> DiscoverLocalHostsAsync(CancellationToken ct)
        {
            var probes = new[]
            {
                ProbeOllamaAsync("http://localhost:11434", ct),
                ProbeOpenAICompatibleAsync("LM Studio", "http://localhost:1234", ct),
                ProbeOpenAICompatibleAsync("Local OpenAI-compatible", "http://localhost:8080", ct)
            };

            return await Task.WhenAll(probes);
        }

        private static async Task<LocalAiHostDiscoveryResult> ProbeOllamaAsync(string endpoint, CancellationToken ct)
        {
            var result = new LocalAiHostDiscoveryResult
            {
                Provider = "Ollama",
                Endpoint = endpoint
            };

            try
            {
                using var http = CreateDiscoveryClient(endpoint);
                using var tagsResponse = await http.GetAsync("/api/tags", ct);
                var tagsBody = await tagsResponse.Content.ReadAsStringAsync(ct);
                if (!tagsResponse.IsSuccessStatusCode)
                {
                    result.Status = $"{(int)tagsResponse.StatusCode} {tagsResponse.ReasonPhrase}";
                    return result;
                }

                result.IsReachable = true;
                var installed = JsonSerializer.Deserialize<OllamaTagsResponse>(tagsBody, JsonOptions)?.Models ?? new();
                foreach (var model in installed)
                {
                    var name = FirstText(model.Model, model.Name);
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    result.Models.Add(new LocalAiModelInfo
                    {
                        Name = name,
                        Details = BuildOllamaDetails(model.Details)
                    });
                }

                using var psResponse = await http.GetAsync("/api/ps", ct);
                if (psResponse.IsSuccessStatusCode)
                {
                    var psBody = await psResponse.Content.ReadAsStringAsync(ct);
                    var loaded = JsonSerializer.Deserialize<OllamaTagsResponse>(psBody, JsonOptions)?.Models ?? new();
                    var loadedNames = loaded
                        .Select(m => FirstText(m.Model, m.Name))
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (var model in result.Models)
                        model.IsLoaded = loadedNames.Contains(model.Name);

                    foreach (var loadedModel in loaded)
                    {
                        var name = FirstText(loadedModel.Model, loadedModel.Name);
                        if (!string.IsNullOrWhiteSpace(name) &&
                            result.Models.All(m => !string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase)))
                        {
                            result.Models.Add(new LocalAiModelInfo
                            {
                                Name = name,
                                IsLoaded = true,
                                Details = BuildOllamaDetails(loadedModel.Details)
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
                result.Status = ex.Message;
            }

            return result;
        }

        private static async Task<LocalAiHostDiscoveryResult> ProbeOpenAICompatibleAsync(string provider, string endpoint, CancellationToken ct)
        {
            var result = new LocalAiHostDiscoveryResult
            {
                Provider = provider,
                Endpoint = endpoint
            };

            try
            {
                using var http = CreateDiscoveryClient(endpoint);
                using var response = await http.GetAsync("/v1/models", ct);
                var body = await response.Content.ReadAsStringAsync(ct);
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
                result.Status = ex.Message;
            }

            return result;
        }

        private static HttpClient CreateDiscoveryClient(string endpoint)
        {
            return new HttpClient
            {
                BaseAddress = new Uri(endpoint.TrimEnd('/')),
                Timeout = TimeSpan.FromSeconds(3)
            };
        }

        private static string NormalizeOpenAIEndpoint(string endpoint)
        {
            var normalized = endpoint.Trim().TrimEnd('/');
            return normalized.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
                ? normalized[..^3]
                : normalized;
        }

        private static string FirstText(params string?[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
        }

        private static string? BuildOllamaDetails(OllamaModelDetails? details)
        {
            if (details is null)
                return null;

            var parts = new[] { details.Family, details.ParameterSize, details.QuantizationLevel }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            var text = string.Join(", ", parts);
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private sealed class OllamaTagsResponse
        {
            public List<OllamaModelEntry> Models { get; set; } = new();
        }

        private sealed class OllamaModelEntry
        {
            public string? Name { get; set; }
            public string? Model { get; set; }
            public OllamaModelDetails? Details { get; set; }
        }

        private sealed class OllamaModelDetails
        {
            public string? Family { get; set; }
            public string? ParameterSize { get; set; }
            public string? QuantizationLevel { get; set; }
        }

        private sealed class OpenAIModelsResponse
        {
            public List<OpenAIModelEntry> Data { get; set; } = new();
        }

        private sealed class OpenAIModelEntry
        {
            public string Id { get; set; } = string.Empty;
        }
    }
}

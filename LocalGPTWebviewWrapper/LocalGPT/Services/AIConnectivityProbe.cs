using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using System.ServiceModel.Channels;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace LocalGPT.Services
{
    public class AiConnectivityProbe(ILogger<AiConnectivityProbe> logger) : IAiConnectivityProbe
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        private static async Task<(bool ok, string msg)> GetAsync(HttpClient http, string path, CancellationToken ct, ILogger<AiConnectivityProbe> logger)
        {
            try
            {
                using var res = await http.GetAsync(path, ct);
                var body = await res.Content.ReadAsStringAsync(ct);
                return (res.IsSuccessStatusCode, $"{(int)res.StatusCode} {res.ReasonPhrase}: {body}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetAsync http {http.ToString()} path {path?.ToString()}");
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
                return await GetAsync(http, "/", ct, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TestAzureAsync o {o.ToString()}");
                return (false, ex.Message);
            }
        }

        public async Task<(bool ok, string message)> TestOpenAIAsync(OpenAICompatOptions o, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(o.ApiKey))
                    return (false, "Missing API key.");

                var http = new HttpClient { BaseAddress = new Uri("https://api.openai.com/v1/") };
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", o.ApiKey);
                return await GetAsync(http, "models", ct,logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TestOpenAIAsync o {o.ToString()}");
                return (false, ex.Message);
            }

        }

        public async Task<(bool ok, string message)> TestOllamaAsync(OllamaCoreOptions o, CancellationToken ct)
        {
            try
            {
                var http = new HttpClient { BaseAddress = new Uri(o.Uri) };
                return await GetAsync(http, "/api/tags", ct,logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TestOllamaAsync o {o.ToString()}");
                return (false, ex.Message);
            }
        }

        public async Task<(bool ok, string message)> TestLocalOpenAICompatAsync(ChatGPTLocalCoreOptions o, CancellationToken ct)
        {
            try
            {
                var endpoint = CouncilChatStringFunctions.NormalizeOpenAIEndpoint(o.Endpoint, logger);
                var http = new HttpClient { BaseAddress = new Uri(endpoint) };
                if (!string.IsNullOrWhiteSpace(o.ApiKey))
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", o.ApiKey);

                return await GetAsync(http, "/v1/models", ct,logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TestLocalOpenAICompatAsync o {o.ToString()}");
                return (false, ex.Message);
            }

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
                logger.LogError(ex, $"Error in TryStartLocalAsync o {o.ToString()}");
                return (false, ex.Message);
            }
        }
        public async Task<IReadOnlyList<LocalAiHostDiscoveryResult>> DiscoverLocalHostsAsync(CancellationToken ct)
        {
            try
            {
                var probes = new[]
       {
                HttpAIStaticsGeneral.ProbeOllamaAsync("http://localhost:11434", ct,logger),
                HttpAIStaticsGeneral.ProbeOpenAICompatibleAsync("LM Studio", "http://localhost:1234", ct,logger),
                HttpAIStaticsGeneral.ProbeOpenAICompatibleAsync("Local OpenAI-compatible", "http://localhost:8080", ct,logger)
            };

                return await Task.WhenAll(probes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in DiscoverLocalHostsAsync");
                return new List<LocalAiHostDiscoveryResult>();
            }
        }

    }
}

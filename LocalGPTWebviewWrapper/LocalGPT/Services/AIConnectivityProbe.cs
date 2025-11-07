using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{

    public class AiConnectivityProbe : IAiConnectivityProbe
    {
        private static async Task<(bool ok, string msg)> GetAsync(HttpClient http, string path, CancellationToken ct)
        {
            try
            {
                using var res = await http.GetAsync(path, ct);
                var body = await res.Content.ReadAsStringAsync(ct);
                return (res.IsSuccessStatusCode, $"{(int)res.StatusCode} {res.ReasonPhrase}: {body}");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool ok, string message)> TestAzureAsync(OpenAIServiceCoreOptions o, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(o.Endpoint) || string.IsNullOrWhiteSpace(o.Key)) return (false, "Missing endpoint or key.");
            try
            {
                var http = new HttpClient { BaseAddress = new Uri(o.Endpoint) };
                http.DefaultRequestHeaders.Add("api-key", o.Key);
                // Azure OpenAI model list (varies by deployment); a health hit is enough:
                return await GetAsync(http, "/", ct);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool ok, string message)> TestOpenAIAsync(OpenAICompatOptions o, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(o.ApiKey)) return (false, "Missing API key.");
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
            var http = new HttpClient { BaseAddress = new Uri(o.Endpoint) };
            if (!string.IsNullOrWhiteSpace(o.ApiKey))
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", o.ApiKey);
            return await GetAsync(http, "models", ct);
        }

        public async Task<(bool ok, string message)> TryStartLocalAsync(ChatGPTLocalCoreOptions o, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(o.StartCommand)) return (false, "StartCommand not set.");
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

                // Wait for health
                var http = new HttpClient { BaseAddress = new Uri(o.Endpoint) };
                var started = false;
                var deadline = DateTime.UtcNow.AddSeconds(Math.Max(5, o.HealthTimeoutSeconds));
                while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
                {
                    var (ok, _) = await TestLocalOpenAICompatAsync(o, ct);
                    if (ok) { started = true; break; }
                    await Task.Delay(1000, ct);
                }
                return started ? (true, "Local server is responding.") : (false, "Local server did not respond in time.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
    }
}

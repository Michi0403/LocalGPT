using System.Net.Http.Json;
using System.Collections.Concurrent;
using System.Text.Json;

namespace LocalGPT.Services
{
    /// <summary>
    /// Reads the Ollama-advertised model context for diagnostic guidance without overriding the caller's configured num_ctx.
    /// </summary>
    public sealed partial class CouncilRuntimeService
    {
        /// <summary>Process-local cache of the model maximum context per provider host and model; only successful lookups are stored.</summary>
        private readonly ConcurrentDictionary<string, int> ollamaModelMaximumContextTokens = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns the advertised context length Ollama reports for a model through <c>/api/show</c> (<c>model_info</c> key ending in the
        /// runtime value <c>OllamaModelContextLengthKeySuffix</c>). The answer is cached per host and model for the process.
        /// </summary>
        /// <param name="http">HTTP client of the calling chat client; its base address selects the Ollama host.</param>
        /// <param name="modelName">Ollama model name.</param>
        /// <param name="logger">Logger of the calling chat client.</param>
        /// <param name="cancellationToken">Cancellation token of the calling request.</param>
        /// <returns>The model maximum context in tokens, or <see langword="null"/> when it is unknown (lookup failed or no key found).</returns>
        public async Task<int?> OllamaThinkingChatClientResolveModelMaximumContextAsync(
            HttpClient http,
            string modelName,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            try
            {
                if (http.BaseAddress is null || string.IsNullOrWhiteSpace(modelName))
                    return null;
                var cacheKey = $"{http.BaseAddress.GetLeftPart(UriPartial.Authority).TrimEnd('/')}|{modelName.Trim()}";
                if (ollamaModelMaximumContextTokens.TryGetValue(cacheKey, out var cached))
                    return cached;

                var suffix = runtimePolicy.GetString("OllamaModelContextLengthKeySuffix");
                var timeoutSeconds = runtimePolicy.GetInt("OllamaModelDetailsLookupTimeoutSeconds");
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/show")
                {
                    Content = JsonContent.Create(new Dictionary<string, string> { ["model"] = modelName })
                };
                using var response = await http.SendAsync(request, linked.Token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogInformation(
                        "Ollama did not return model details for {Model} (HTTP {Status}); its maximum context is unknown and the configured context is used.",
                        modelName, (int)response.StatusCode);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(linked.Token).ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: linked.Token).ConfigureAwait(false);
                if (!document.RootElement.TryGetProperty("model_info", out var modelInfo) || modelInfo.ValueKind != JsonValueKind.Object)
                    return null;
                foreach (var property in modelInfo.EnumerateObject())
                {
                    if (!property.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ||
                        !property.Value.TryGetInt32(out var contextLength) ||
                        contextLength <= 0)
                        continue;
                    ollamaModelMaximumContextTokens[cacheKey] = contextLength;
                    return contextLength;
                }

                return null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not read the maximum context of Ollama model {Model}; it is treated as unknown and the configured context is used.", modelName);
                return null;
            }
        }
    }
}

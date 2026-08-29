using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Executes local OCR through an Ollama-compatible vision model. Operational ceilings are owned by the persisted
/// LocalGPT runtime policy; the shipped policy is permissive and lets the local operator opt into tighter limits.
/// </summary>
/// <param name="options">Options containing the configured local AI providers.</param>
/// <param name="runtimePolicy">Database-backed operator policy used for OCR resource and timeout limits.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class LocalVisionOcrService(
    IOptionsMonitor<LocalGPT.BusinessObjects.ConfigurationRoot> options,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<LocalVisionOcrService> logger) : ILocalVisionOcrService
{
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="LocalVisionOcrService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Recognizes visible text in the supplied browser-rendered image.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local vision OCR result produced by the operation.</returns>
    public async Task<LocalVisionOcrResult> RecognizeAsync(LocalVisionOcrRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var imageBase64 = ReadImageBase64(request.ImageDataUrl, out var mediaType);
            var provider = ResolveProvider(request.ModelName);
            var configuredTimeoutSeconds = runtimePolicy.GetInt(LocalGptRuntimeValue.LocalVisionRequestTimeoutSeconds);
            using var client = new HttpClient
            {
                BaseAddress = new Uri(provider.Uri.TrimEnd('/') + "/", UriKind.Absolute),
                Timeout = configuredTimeoutSeconds <= 0
                    ? System.Threading.Timeout.InfiniteTimeSpan
                    : TimeSpan.FromSeconds(configuredTimeoutSeconds)
            };

            var minimumOutputTokens = Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.LocalVisionMinimumOutputTokens));
            var maximumOutputTokens = Math.Max(minimumOutputTokens, runtimePolicy.GetInt(LocalGptRuntimeValue.LocalVisionMaximumOutputTokens));
            var requestedOutputTokens = request.MaximumOutputTokens <= 0 ? maximumOutputTokens : request.MaximumOutputTokens;
            var payload = new OllamaChatRequest
            {
                Model = provider.ModelName,
                Stream = false,
                KeepAlive = "2m",
                Messages =
                [
                    new OllamaChatMessage
                    {
                        Role = "user",
                        Content = string.IsNullOrWhiteSpace(request.Prompt)
                            ? "Recognize all visible text in this image. Preserve reading order and line breaks. Return only the recognized text; mark uncertain fragments with [?]."
                            : request.Prompt.Trim(),
                        Images = [imageBase64]
                    }
                ],
                Options = new OllamaRequestOptions
                {
                    NumPredict = Math.Clamp(requestedOutputTokens, minimumOutputTokens, maximumOutputTokens),
                    Temperature = 0
                }
            };

            using var response = await client.PostAsJsonAsync("api/chat", payload, JsonOptions, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var maximumDiagnosticCharacters = Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.LocalVisionMaximumDiagnosticCharacters));
                throw new InvalidOperationException($"The configured local OCR model returned HTTP {(int)response.StatusCode}: {Trim(body, maximumDiagnosticCharacters)}");
            }

            var parsed = JsonSerializer.Deserialize<OllamaChatResponse>(body, JsonOptions)
                ?? throw new JsonException("The local OCR response was empty.");
            var text = parsed.Message?.Content?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("The configured local OCR model returned no recognized text.");

            logger.LogInformation(
                "Completed local 1-Wire OCR with model {ModelName}; media type {MediaType}; output length {Length}.",
                provider.ModelName,
                mediaType,
                text.Length);
            return new LocalVisionOcrResult
            {
                Text = text,
                ModelName = provider.ModelName,
                ProviderUri = provider.Uri,
                MediaType = mediaType,
                NeedsHumanReview = true
            };
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Cancelled local 1-Wire OCR at the caller's request.");
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or HttpRequestException or JsonException or FormatException or UriFormatException)
        {
            logger.LogError(ex, "Local 1-Wire OCR failed for requested model {RequestedModel}.", request.ModelName);
            throw;
        }
    }

    /// <summary>
    /// Resolves provider as part of the local vision OCR service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="requestedModel">Requested model value supplied to the local vision OCR operation and used when producing its result.</param>
    /// <returns>The Ollama core options produced by the operation.</returns>
    private OllamaCoreOptions ResolveProvider(string? requestedModel)
    {
        try
        {
            var ai = options.CurrentValue.AICore ?? new AICoreOptions();
            var configured = new[] { ai.OllamaCore }
                .Concat(ai.OllamaCores ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item.Uri) && !string.IsNullOrWhiteSpace(item.ModelName))
                .ToList();
            if (configured.Count == 0)
                throw new InvalidOperationException("No Ollama-compatible provider is configured for local OCR.");

            if (!string.IsNullOrWhiteSpace(requestedModel))
            {
                var exact = configured.FirstOrDefault(item => string.Equals(item.ModelName, requestedModel.Trim(), StringComparison.OrdinalIgnoreCase));
                if (exact is not null)
                    return exact;
            }

            return configured.FirstOrDefault(item =>
                       item.ModelName.Contains("ocr", StringComparison.OrdinalIgnoreCase) ||
                       item.ModelName.Contains("vision", StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidOperationException(
                       "No OCR/vision model is configured. Add DeepSeek OCR or another Ollama-compatible vision model in LocalGPT settings.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Service method {Service}.{Method} failed.", nameof(LocalVisionOcrService), nameof(ResolveProvider));
            throw;
        }
    }

    /// <summary>
    /// Reads image base64 as part of the local vision OCR service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="dataUrl">Data url value supplied to the local vision OCR operation and used when producing its result.</param>
    /// <param name="mediaType">Media type value supplied to the local vision OCR operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ReadImageBase64(string dataUrl, out string mediaType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dataUrl))
                throw new ArgumentException("imageDataUrl is required.");
            var comma = dataUrl.IndexOf(',');
            if (!dataUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ||
                comma <= 0 ||
                !dataUrl[..comma].Contains(";base64", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("imageDataUrl must be a base64 image data URL.");

            var separator = dataUrl.IndexOf(';');
            if (separator <= 5 || separator >= comma)
                throw new ArgumentException("imageDataUrl contains an invalid media type.");

            mediaType = dataUrl[5..separator];
            var encoded = dataUrl[(comma + 1)..];
            var bytes = Convert.FromBase64String(encoded);
            var maximumImageBytes = Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.LocalVisionMaximumImageBytes));
            if (bytes.Length == 0 || bytes.Length > maximumImageBytes)
                throw new ArgumentException($"The OCR image must be between 1 byte and the configured LocalVisionMaximumImageBytes value ({maximumImageBytes} bytes).");
            return Convert.ToBase64String(bytes);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Service method {Service}.{Method} failed.", nameof(LocalVisionOcrService), nameof(ReadImageBase64));
            throw;
        }
    }

    /// <summary>
    /// Performs trim as part of the local vision OCR service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the local vision OCR operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the local vision OCR operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Trim(string value, int maximum)
    {
        try
        {
            if (maximum <= 0 || value.Length <= maximum)
                return value;
            return value[..maximum] + "…";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Service method {Service}.{Method} failed.", nameof(LocalVisionOcrService), nameof(Trim));
            throw;
        }
    }
}

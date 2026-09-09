using System.Globalization;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Posts explicitly approved hardware facts to CanIRun.ai's JSON recommendation API and converts the bounded response into LocalGPT recommendations.</summary>
/// <param name="httpClientFactory">Creates the redirect-disabled HTTP client dedicated to the optional CanIRun.ai lookup.</param>
/// <param name="runtimePolicy">Database-backed operator runtime policy.</param>
/// <param name="logger">Writes lookup diagnostics without copying request hardware values or response bodies into logs.</param>
public sealed class CanIRunHardwareRecommendationService(
    IHttpClientFactory httpClientFactory,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<CanIRunHardwareRecommendationService> logger) : ICanIRunHardwareRecommendationService
{
    /// <summary>Stores the fixed JSON media type used for the optional CanIRun.ai request.</summary>
    private const string JsonMediaType = "application/json";
    /// <summary>Stores the fixed HTTPS JSON recommendation endpoint used only after explicit user opt-in.</summary>
    private readonly Uri recommendationUri = new("https://www.canirun.ai/api/recommend", UriKind.Absolute);

    /// <summary>Posts one explicitly approved hardware profile to the maintained CanIRun.ai recommendation endpoint.</summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<CanIRunModelRecommendation>> GetRecommendationsAsync(
        InitialSetupHardwareDevice device,
        bool userConfirmedWebLookup,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmedWebLookup)
                throw new InvalidOperationException("CanIRun.ai lookup requires explicit user opt-in for this web request.");
            ArgumentNullException.ThrowIfNull(device);
            var hardwareName = string.IsNullOrWhiteSpace(device.CanIRunHardwareName) ? device.Name : device.CanIRunHardwareName;
            if (string.IsNullOrWhiteSpace(hardwareName) && string.IsNullOrWhiteSpace(device.CpuName))
                throw new ArgumentException("A reviewed accelerator or CPU name is required for CanIRun.ai recommendations.", nameof(device));
            if (device.SystemMemoryGiB is not > 0)
                throw new ArgumentException("Total system / unified memory is required for CanIRun.ai recommendations. Detect it locally or enter the reviewed GiB value first.", nameof(device));

            ValidateCanIRunUri(recommendationUri);
            var hardware = new Dictionary<string, object?>
            {
                ["ramGb"] = Math.Round(device.SystemMemoryGiB.Value, 2, MidpointRounding.AwayFromZero)
            };
            if (!string.IsNullOrWhiteSpace(hardwareName))
            {
                var gpu = new Dictionary<string, object?>
                {
                    ["name"] = Bound(hardwareName, 240)
                };
                if (device.DedicatedVramGiB is > 0)
                    gpu["vramGb"] = Math.Round(device.DedicatedVramGiB.Value, 2, MidpointRounding.AwayFromZero);
                hardware["gpu"] = gpu;
            }
            if (!string.IsNullOrWhiteSpace(device.CpuName))
                hardware["cpu"] = new Dictionary<string, object?> { ["name"] = Bound(device.CpuName, 240) };

            var payload = new Dictionary<string, object?>
            {
                ["hardware"] = hardware
            };

            var client = httpClientFactory.CreateClient("LocalGPTCanIRun");
            var requestJson = JsonSerializer.Serialize(payload);
            using var response = await SendRecommendationRequestAsync(client, requestJson, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (json.Length > Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.CanIRunMaximumPageCharacters)))
                throw new InvalidDataException("The CanIRun.ai response exceeded LocalGPT's bounded response size.");

            using var document = JsonDocument.Parse(json);
            var recommendations = ParseRecommendations(document.RootElement, hardwareName, recommendationUri.ToString(), cancellationToken);
            var result = recommendations
                .Where(item => !string.IsNullOrWhiteSpace(item.ModelId))
                .GroupBy(item => item.ModelId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(item => item.Score).First())
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.ModelName, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.CanIRunMaximumRecommendations)))
                .ToList();
            logger.LogInformation("Loaded {RecommendationCount} CanIRun.ai JSON recommendation(s) for one user-approved hardware profile.", result.Count);
            return result;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "CanIRun.ai lookup was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "CanIRun.ai lookup failed; hardware facts and response content were omitted from logs.");
            throw;
        }
    }

    /// <summary>Posts the approved JSON payload while following only bounded HTTPS redirects that stay on CanIRun.ai.</summary>
    /// <param name="client">Redirect-disabled HTTP client used by the optional web lookup.</param>
    /// <param name="requestJson">Serialized approved hardware payload.</param>
    /// <param name="cancellationToken">Cancellation token for the web request.</param>
    /// <returns>The final HTTP response; the caller owns disposal.</returns>
    private async Task<HttpResponseMessage> SendRecommendationRequestAsync(HttpClient client, string requestJson, CancellationToken cancellationToken)
    {
        try
        {
            var currentUri = recommendationUri;
            for (var redirectCount = 0; redirectCount <= 2; redirectCount++)
            {
                ValidateCanIRunUri(currentUri);
                using var request = new HttpRequestMessage(HttpMethod.Post, currentUri)
                {
                    Content = new StringContent(requestJson, Encoding.UTF8, JsonMediaType)
                };
                request.Headers.UserAgent.ParseAdd("LocalGPT/4.0.3 (+offline-first; explicit-user-opt-in; source-credit-canirun.ai)");
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if ((int)response.StatusCode is < 300 or >= 400)
                    return response;

                var location = response.Headers.Location;
                if (location is null)
                {
                    response.Dispose();
                    throw new InvalidOperationException("CanIRun.ai returned a redirect without a destination.");
                }
                var redirectedUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
                ValidateCanIRunUri(redirectedUri);
                response.Dispose();
                if (redirectCount >= 2)
                    throw new InvalidOperationException("CanIRun.ai exceeded LocalGPT's bounded redirect limit.");
                currentUri = redirectedUri;
            }

            throw new InvalidOperationException("CanIRun.ai redirect handling did not produce a response.");
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "CanIRun.ai request redirect handling was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "CanIRun.ai request redirect handling failed; request payload was omitted.");
            throw;
        }
    }

    /// <summary>Builds the legacy editable CanIRun.ai slug without performing a network request.</summary>
    /// <inheritdoc />
    public string SuggestDeviceSlug(string hardwareName)
    {
        try
        {
            var value = hardwareName?.Trim().ToLowerInvariant() ?? string.Empty;
            foreach (var token in new[] { "advanced micro devices", "amd", "radeon", "nvidia", "geforce", "graphics", "gpu" })
                value = value.Replace(token, " ", StringComparison.OrdinalIgnoreCase);
            var chars = value.Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray();
            var slug = new string(chars);
            while (slug.Contains("--", StringComparison.Ordinal))
                slug = slug.Replace("--", "-", StringComparison.Ordinal);
            return slug.Trim('-');
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Suggesting a legacy CanIRun.ai device slug failed.");
            throw;
        }
    }

    /// <summary>Finds recommendation objects in the public JSON response while tolerating harmless response-envelope changes.</summary>
    /// <param name="root">Root JSON element returned by CanIRun.ai.</param>
    /// <param name="deviceName">Reviewed accelerator name associated with the request.</param>
    /// <param name="sourceUrl">Attributed API source URL stored with each recommendation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop parsing.</param>
    /// <returns>The bounded recommendation candidates extracted from the response.</returns>
    private List<CanIRunModelRecommendation> ParseRecommendations(JsonElement root, string deviceName, string sourceUrl, CancellationToken cancellationToken)
    {
        try
        {
            var candidates = new List<JsonElement>();
            CollectRecommendationObjects(root, candidates, depth: 0, cancellationToken);
            var result = new List<CanIRunModelRecommendation>();
            foreach (var candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = ParseRecommendation(candidate, deviceName, sourceUrl);
                if (item is null)
                    continue;
                result.Add(item);
            }
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing the CanIRun.ai JSON recommendation response failed; response content was omitted.");
            throw;
        }
    }

    /// <summary>Collects object candidates from common recommendation envelopes without binding LocalGPT to one presentation shape.</summary>
    /// <param name="element">Current JSON element being traversed.</param>
    /// <param name="result">Bounded destination collection for object candidates.</param>
    /// <param name="depth">Current recursion depth used by the response-envelope safety bound.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop traversal.</param>
    private void CollectRecommendationObjects(JsonElement element, List<JsonElement> result, int depth, CancellationToken cancellationToken)
    {
        try
        {
            if (depth > 6 || result.Count >= 512)
                return;
            cancellationToken.ThrowIfCancellationRequested();
            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                        result.Add(item);
                    CollectRecommendationObjects(item, result, depth + 1, cancellationToken);
                }
                return;
            }
            if (element.ValueKind != JsonValueKind.Object)
                return;
            foreach (var property in element.EnumerateObject())
            {
                if (property.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Object)
                    CollectRecommendationObjects(property.Value, result, depth + 1, cancellationToken);
            }
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Collecting CanIRun.ai recommendation candidates was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Collecting CanIRun.ai recommendation candidates failed; response content was omitted.");
            throw;
        }
    }

    /// <summary>Normalizes one JSON object into the stable LocalGPT recommendation shape when it contains a model identity.</summary>
    /// <param name="candidate">Candidate JSON object discovered in the response.</param>
    /// <param name="deviceName">Reviewed accelerator name associated with the request.</param>
    /// <param name="sourceUrl">Attributed API source URL stored with the normalized recommendation.</param>
    /// <returns>The normalized recommendation, or <see langword="null"/> when the object is not a model recommendation.</returns>
    private CanIRunModelRecommendation? ParseRecommendation(JsonElement candidate, string deviceName, string sourceUrl)
    {
        try
        {
            var model = TryGetObject(candidate, "model");
            var compatibility = TryGetObject(candidate, "compatibility");
            var modelId = FirstString(candidate, "modelId", "model_id", "modelSlug", "slug");
            if (string.IsNullOrWhiteSpace(modelId) && model is { } modelElement)
                modelId = FirstString(modelElement, "modelId", "model_id", "modelSlug", "slug", "id");
            if (string.IsNullOrWhiteSpace(modelId))
                modelId = FirstString(candidate, "id");
            if (string.IsNullOrWhiteSpace(modelId))
                return null;

            var modelName = FirstString(candidate, "modelName", "model_name", "displayName");
            if (string.IsNullOrWhiteSpace(modelName) && model is { } namedModel)
                modelName = FirstString(namedModel, "name", "displayName", "modelName", "id");
            if (string.IsNullOrWhiteSpace(modelName))
                modelName = FirstString(candidate, "name");
            var grade = FirstString(candidate, "grade", "tier", "rating");
            if (string.IsNullOrWhiteSpace(grade) && compatibility is { } compatibilityElement)
                grade = FirstString(compatibilityElement, "grade", "tier", "rating");
            var status = FirstString(candidate, "status", "runStatus", "compatibilityStatus", "verdict");
            if (string.IsNullOrWhiteSpace(status) && compatibility is { } statusCompatibility)
                status = FirstString(statusCompatibility, "status", "verdict", "label");
            var score = FirstInt(candidate, "score", "compatibilityScore", "rankScore");
            if (score == 0 && compatibility is { } scoreCompatibility)
                score = FirstInt(scoreCompatibility, "score", "compatibilityScore", "rankScore");
            var quantization = FirstString(candidate, "quantization", "quant", "recommendedQuantization", "selectedQuantization");
            var requiredVram = FirstDouble(candidate, "requiredVramGb", "requiredVRAMGb", "vramGb", "memoryGb", "memoryRequiredGb");
            var publisher = FirstString(candidate, "publisher", "provider", "organization", "author");
            if (string.IsNullOrWhiteSpace(publisher) && model is { } publisherModel)
                publisher = FirstString(publisherModel, "publisher", "provider", "organization", "author");

            if (TryGetObject(candidate, "quantization") is { } quantizationObject)
            {
                if (string.IsNullOrWhiteSpace(quantization))
                    quantization = FirstString(quantizationObject, "name", "id", "format");
                requiredVram ??= FirstDouble(quantizationObject, "vramGb", "requiredVramGb", "memoryGb");
            }
            if (TryGetObject(candidate, "selectedQuantization") is { } selectedQuantization)
            {
                if (string.IsNullOrWhiteSpace(quantization))
                    quantization = FirstString(selectedQuantization, "name", "id", "format");
                requiredVram ??= FirstDouble(selectedQuantization, "vramGb", "requiredVramGb", "memoryGb");
            }
            if (TryGetObject(candidate, "recommendedQuantization") is { } recommendedQuantization)
            {
                if (string.IsNullOrWhiteSpace(quantization))
                    quantization = FirstString(recommendedQuantization, "name", "id", "format");
                requiredVram ??= FirstDouble(recommendedQuantization, "vramGb", "requiredVramGb", "memoryGb");
            }

            var ollamaModelId = FirstString(candidate, "ollamaModelId", "ollama_model_id", "ollamaModel", "ollama_model", "ollama");
            if (string.IsNullOrWhiteSpace(ollamaModelId) && model is { } ollamaModel)
                ollamaModelId = FirstString(ollamaModel, "ollamaModelId", "ollama_model_id", "ollamaModel", "ollama_model", "ollama");
            if (string.IsNullOrWhiteSpace(ollamaModelId) && TryGetObject(candidate, "ollama") is { } ollamaObject)
                ollamaModelId = FirstString(ollamaObject, "modelId", "model", "name", "id", "command");
            ollamaModelId = NormalizeOllamaModelId(ollamaModelId);

            var lmStudioModelId = FirstString(candidate, "lmStudioModelId", "lmstudioModelId", "lm_studio_model_id", "lmStudioModel", "lmstudioModel", "lmstudio");
            if (string.IsNullOrWhiteSpace(lmStudioModelId) && model is { } lmStudioModel)
                lmStudioModelId = FirstString(lmStudioModel, "lmStudioModelId", "lmstudioModelId", "lm_studio_model_id", "lmStudioModel", "lmstudioModel", "lmstudio");
            if (string.IsNullOrWhiteSpace(lmStudioModelId) && TryGetObject(candidate, "lmstudio") is { } lmStudioObject)
                lmStudioModelId = FirstString(lmStudioObject, "modelId", "model", "name", "id", "command");
            lmStudioModelId = NormalizeLmStudioModelId(lmStudioModelId);

            return new CanIRunModelRecommendation
            {
                ModelId = Bound(modelId, 240),
                ModelName = Bound(string.IsNullOrWhiteSpace(modelName) ? modelId : modelName, 240),
                Grade = Bound(grade, 32),
                Status = Bound(status, 80),
                Score = score,
                Quantization = Bound(quantization, 64),
                RequiredVramGiB = requiredVram,
                Publisher = Bound(publisher, 120),
                OllamaModelId = Bound(ollamaModelId, 240),
                LmStudioModelId = Bound(lmStudioModelId, 240),
                DeviceSlug = Bound(deviceName, 240),
                SourceUrl = BuildModelSourceUrl(modelId, sourceUrl)
            };
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException)
        {
            logger.LogDebug(exception, "Ignored one unrecognized CanIRun.ai recommendation object.");
            return null;
        }
    }

    /// <summary>Extracts one safe Ollama model token from an API scalar that may contain a raw token or an <c>ollama run/pull</c> command.</summary>
    /// <param name="value">CanIRun.ai scalar value associated with Ollama metadata.</param>
    /// <returns>The provider token, or an empty string when the scalar does not expose one safely.</returns>
    private string NormalizeOllamaModelId(string value)
    {
        try
        {
            var candidate = value?.Trim() ?? string.Empty;
            if (candidate.Length == 0)
                return string.Empty;
            var tokens = candidate.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length >= 3
                && tokens[0].Equals("ollama", StringComparison.OrdinalIgnoreCase)
                && (tokens[1].Equals("run", StringComparison.OrdinalIgnoreCase) || tokens[1].Equals("pull", StringComparison.OrdinalIgnoreCase)))
                candidate = tokens[2];
            else if (tokens.Length != 1)
                return string.Empty;
            return candidate.Trim('`', '"', '\'');
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Ignoring unrecognized CanIRun.ai Ollama model metadata.");
            return string.Empty;
        }
    }

    /// <summary>Extracts one safe LM Studio catalog identifier from a raw ID or an <c>lms get</c> command.</summary>
    /// <param name="value">CanIRun.ai scalar value associated with LM Studio metadata.</param>
    /// <returns>The provider token, or an empty string when the scalar does not expose one safely.</returns>
    private string NormalizeLmStudioModelId(string value)
    {
        try
        {
            var candidate = value?.Trim() ?? string.Empty;
            if (candidate.Length == 0)
                return string.Empty;
            var tokens = candidate.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length >= 3
                && tokens[0].Equals("lms", StringComparison.OrdinalIgnoreCase)
                && tokens[1].Equals("get", StringComparison.OrdinalIgnoreCase))
                candidate = tokens[2];
            else if (tokens.Length != 1)
                return string.Empty;
            candidate = candidate.Trim('`', '"', '\'');
            return candidate.Length <= 240 && candidate.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.' or '/' or '@')
                ? candidate
                : string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Ignoring unrecognized CanIRun.ai LM Studio model metadata.");
            return string.Empty;
        }
    }

    /// <summary>Builds an attributed model-detail URL when the recommendation ID is a safe CanIRun.ai path segment.</summary>
    /// <param name="modelId">CanIRun.ai model identifier.</param>
    /// <param name="fallbackSourceUrl">Canonical recommendation API URL used when no safe detail URL can be produced.</param>
    /// <returns>The attributed source URL retained with the recommendation.</returns>
    private string BuildModelSourceUrl(string modelId, string fallbackSourceUrl)
    {
        try
        {
            var candidate = modelId?.Trim() ?? string.Empty;
            if (candidate.Length > 0 && candidate.Length <= 240 && candidate.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.'))
                return $"https://www.canirun.ai/model/{candidate}";
            return fallbackSourceUrl;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Building a CanIRun.ai model-detail source URL failed.");
            return fallbackSourceUrl;
        }
    }

    /// <summary>Returns a case-insensitive nested object property when present.</summary>
    /// <param name="element">JSON object that may contain the requested property.</param>
    /// <param name="name">Property name to match without case sensitivity.</param>
    /// <returns>The nested object element, or <see langword="null"/> when absent or not an object.</returns>
    private JsonElement? TryGetObject(JsonElement element, string name)
    {
        try
        {
            if (element.ValueKind != JsonValueKind.Object)
                return null;
            foreach (var property in element.EnumerateObject())
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.Object)
                    return property.Value;
            return null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a CanIRun.ai nested recommendation object failed; response content was omitted.");
            throw;
        }
    }

    /// <summary>Returns the first matching scalar property as text.</summary>
    /// <param name="element">JSON object containing candidate scalar properties.</param>
    /// <param name="names">Ordered property names to try.</param>
    /// <returns>The first scalar text value, or an empty string when none match.</returns>
    private string FirstString(JsonElement element, params string[] names)
    {
        try
        {
            if (element.ValueKind != JsonValueKind.Object)
                return string.Empty;
            foreach (var name in names)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (!property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (property.Value.ValueKind == JsonValueKind.String)
                        return property.Value.GetString() ?? string.Empty;
                    if (property.Value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                        return property.Value.ToString();
                }
            }
            return string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a CanIRun.ai scalar text property failed; response content was omitted.");
            throw;
        }
    }

    /// <summary>Returns the first matching integer property using invariant parsing.</summary>
    /// <param name="element">JSON object containing candidate integer properties.</param>
    /// <param name="names">Ordered property names to try.</param>
    /// <returns>The first parsed integer value, or zero when none match.</returns>
    private int FirstInt(JsonElement element, params string[] names)
    {
        try
        {
            foreach (var name in names)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (!property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out var value))
                        return value;
                    if (property.Value.ValueKind == JsonValueKind.String && int.TryParse(property.Value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                        return value;
                }
            }
            return 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a CanIRun.ai integer property failed; response content was omitted.");
            throw;
        }
    }

    /// <summary>Returns the first matching floating-point property using invariant parsing.</summary>
    /// <param name="element">JSON object containing candidate floating-point properties.</param>
    /// <param name="names">Ordered property names to try.</param>
    /// <returns>The first parsed floating-point value, or <see langword="null"/> when none match.</returns>
    private double? FirstDouble(JsonElement element, params string[] names)
    {
        try
        {
            foreach (var name in names)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (!property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDouble(out var value))
                        return value;
                    if (property.Value.ValueKind == JsonValueKind.String && double.TryParse(property.Value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        return value;
                }
            }
            return null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a CanIRun.ai floating-point property failed; response content was omitted.");
            throw;
        }
    }

    /// <summary>Enforces the fixed HTTPS allowlist for the optional CanIRun.ai request.</summary>
    /// <param name="uri">Request URI to validate before the external call.</param>
    private void ValidateCanIRunUri(Uri uri)
    {
        try
        {
            if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("CanIRun.ai lookups require HTTPS.");
            if (!uri.Host.Equals("canirun.ai", StringComparison.OrdinalIgnoreCase)
                && !uri.Host.Equals("www.canirun.ai", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("CanIRun.ai lookup host is not allowlisted.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "CanIRun.ai request URI validation failed.");
            throw;
        }
    }

    /// <summary>Trims and bounds one externally sourced text value before it enters the stable recommendation model.</summary>
    /// <param name="value">Externally sourced text to normalize.</param>
    /// <param name="maximum">Maximum number of characters retained.</param>
    /// <returns>The trimmed value truncated to the requested bound.</returns>
    private string Bound(string? value, int maximum)
    {
        try
        {
            var text = value?.Trim() ?? string.Empty;
            return text.Length <= maximum ? text : text[..maximum];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bounding a CanIRun.ai recommendation value failed; response content was omitted.");
            throw;
        }
    }

}

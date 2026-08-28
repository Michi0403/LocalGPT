using System.Globalization;
using System.Net;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Fetches explicitly approved CanIRun.ai device pages and converts their public model-card metadata into bounded LocalGPT recommendations.</summary>
/// <param name="httpClientFactory">Creates the redirect-disabled HTTP client dedicated to the optional CanIRun.ai lookup.</param>
/// <param name="regexPatterns">Provides database-backed parsers for model cards and HTML data attributes.</param>
/// <param name="logger">Writes bounded lookup diagnostics without copying page bodies into logs.</param>
public sealed class CanIRunHardwareRecommendationService(
    IHttpClientFactory httpClientFactory,
    IRegexPatternService regexPatterns,
    ILogger<CanIRunHardwareRecommendationService> logger) : ICanIRunHardwareRecommendationService
{
    /// <summary>
    /// Defines the maximum page characters constant used by <see cref="CanIRunHardwareRecommendationService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaximumPageCharacters = 4_000_000;
    /// <summary>
    /// Defines the maximum recommendations constant used by <see cref="CanIRunHardwareRecommendationService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaximumRecommendations = 96;

    /// <summary>Fetches one explicitly approved CanIRun.ai device page and parses its public recommendation cards.</summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<CanIRunModelRecommendation>> GetRecommendationsAsync(
        string deviceSlug,
        bool userConfirmedWebLookup,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmedWebLookup)
                throw new InvalidOperationException("CanIRun.ai lookup requires explicit user opt-in for this web request.");
            var slug = NormalizeSlug(deviceSlug);
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("A CanIRun.ai device slug is required.", nameof(deviceSlug));

            var uri = new Uri($"https://www.canirun.ai/device/{Uri.EscapeDataString(slug)}/", UriKind.Absolute);
            ValidateCanIRunUri(uri);
            var client = httpClientFactory.CreateClient("LocalGPTCanIRun");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("LocalGPT/3.4.7 (+offline-first; explicit-user-opt-in; source-credit-canirun.ai)");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if ((int)response.StatusCode is >= 300 and < 400)
                throw new InvalidOperationException("CanIRun.ai redirects are not followed automatically. Review the configured device slug.");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (html.Length > MaximumPageCharacters)
                throw new InvalidDataException("The CanIRun.ai response exceeded LocalGPT's bounded page size.");

            var cardRegex = await regexPatterns.GetRegexAsync("builtin.canirun-model-card-pattern").ConfigureAwait(false)
                ?? throw new InvalidOperationException("The CanIRun.ai model-card regex is unavailable.");
            var attributeRegex = await regexPatterns.GetRegexAsync("builtin.html-data-attribute-pattern").ConfigureAwait(false)
                ?? throw new InvalidOperationException("The HTML data-attribute regex is unavailable.");
            var recommendations = new List<CanIRunModelRecommendation>();
            foreach (System.Text.RegularExpressions.Match card in cardRegex.Matches(html))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var attributes = ParseAttributes(card.Groups["attrs"].Value, attributeRegex);
                if (!attributes.TryGetValue("model-id", out var modelId) || string.IsNullOrWhiteSpace(modelId))
                    continue;
                var selectedQuantIndex = ParseInt(attributes.GetValueOrDefault("selected-quant"), -1);
                var (quantization, vram) = ParseSelectedQuant(attributes.GetValueOrDefault("quants"), selectedQuantIndex);
                recommendations.Add(new CanIRunModelRecommendation
                {
                    ModelId = Bound(modelId, 240),
                    ModelName = Bound(attributes.GetValueOrDefault("model-name") ?? modelId, 240),
                    Grade = Bound(attributes.GetValueOrDefault("grade") ?? string.Empty, 16),
                    Status = Bound(attributes.GetValueOrDefault("status") ?? string.Empty, 48),
                    Score = ParseInt(attributes.GetValueOrDefault("score"), 0),
                    Quantization = Bound(quantization, 48),
                    RequiredVramGiB = vram,
                    Publisher = Bound(attributes.GetValueOrDefault("provider") ?? string.Empty, 120),
                    DeviceSlug = slug,
                    SourceUrl = uri.ToString()
                });
                if (recommendations.Count >= MaximumRecommendations)
                    break;
            }

            var result = recommendations
                .GroupBy(item => item.ModelId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(item => item.Score).First())
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.ModelName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            logger.LogInformation("Loaded {RecommendationCount} CanIRun.ai recommendation card(s) for user-approved device slug {DeviceSlug}.", result.Count, slug);
            return result;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "CanIRun.ai lookup was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "CanIRun.ai lookup failed; response content was omitted from logs.");
            throw;
        }
    }

    /// <summary>Builds an editable CanIRun.ai device slug from a local GPU display name without performing a network request.</summary>
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
            logger.LogError(exception, "Suggesting a CanIRun.ai device slug failed.");
            throw;
        }
    }

    /// <summary>
    /// Parses attributes as part of the can i run hardware recommendation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="attributeText">Attribute text value supplied to the can i run hardware recommendation operation and used when producing its result.</param>
    /// <param name="attributeRegex">Attribute regex value supplied to the can i run hardware recommendation operation and used when producing its result.</param>
    /// <returns>The dictionary string string produced by the operation.</returns>
    private Dictionary<string, string> ParseAttributes(string attributeText, System.Text.RegularExpressions.Regex attributeRegex)
    {
        try
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Text.RegularExpressions.Match match in attributeRegex.Matches(attributeText))
            {
                var name = match.Groups["name"].Value;
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                values[name] = WebUtility.HtmlDecode(match.Groups["value"].Value);
            }
            return values;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing CanIRun.ai HTML attributes failed; attribute text was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Parses selected quant as part of the can i run hardware recommendation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="json">Json value supplied to the can i run hardware recommendation operation and used when producing its result.</param>
    /// <param name="selectedIndex">Selected index value supplied to the can i run hardware recommendation operation and used when producing its result.</param>
    /// <returns>The string quantization double vram gi b produced by the operation.</returns>
    private (string Quantization, double? VramGiB) ParseSelectedQuant(string? json, int selectedIndex)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
                return (string.Empty, null);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
                return (string.Empty, null);
            var index = Math.Clamp(selectedIndex, 0, document.RootElement.GetArrayLength() - 1);
            var quant = document.RootElement[index];
            var name = quant.TryGetProperty("name", out var nameValue) ? nameValue.GetString() ?? string.Empty : string.Empty;
            double? vram = quant.TryGetProperty("vramGB", out var vramValue) && vramValue.TryGetDouble(out var parsed) ? parsed : null;
            return (name, vram);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Ignored malformed CanIRun.ai quantization metadata; JSON content was omitted.");
            return (string.Empty, null);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing CanIRun.ai quantization metadata failed; JSON content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes slug as part of the can i run hardware recommendation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the can i run hardware recommendation operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeSlug(string value)
    {
        try
        {
            var slug = (value ?? string.Empty).Trim().Trim('/').ToLowerInvariant();
            if (slug.Length > 120 || slug.Any(character => !(char.IsLetterOrDigit(character) || character == '-')))
                throw new ArgumentException("CanIRun.ai device slugs may contain only letters, numbers and hyphens.", nameof(value));
            return slug;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing CanIRun.ai device slug failed.");
            throw;
        }
    }

    /// <summary>
    /// Validates can i run URI as part of the can i run hardware recommendation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="uri">Uri value supplied to the can i run hardware recommendation operation and used when producing its result.</param>
    private void ValidateCanIRunUri(Uri uri)
    {
        try
        {
            if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("CanIRun.ai lookups require HTTPS.");
            if (!uri.Host.Equals("www.canirun.ai", StringComparison.OrdinalIgnoreCase)
                && !uri.Host.Equals("canirun.ai", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("CanIRun.ai lookup host is not allowlisted.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Validating CanIRun.ai lookup URI failed.");
            throw;
        }
    }

    /// <summary>
    /// Parses int as part of the can i run hardware recommendation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the can i run hardware recommendation operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the can i run hardware recommendation operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int ParseInt(string? value, int fallback)
    {
        try
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing bounded CanIRun.ai integer metadata failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs bound as part of the can i run hardware recommendation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the can i run hardware recommendation operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the can i run hardware recommendation operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Bound(string value, int maximum)
    {
        try
        {
            var text = value?.Trim() ?? string.Empty;
            return text.Length <= maximum ? text : text[..maximum];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bounding CanIRun.ai text metadata failed; text was omitted from logs.");
            throw;
        }
    }
}

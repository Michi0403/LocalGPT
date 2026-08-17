using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services;

/// <summary>Resolves user-authored Remote Control interpolation tokens through scoped LocalGPT services.</summary>
/// <param name="regex">Shared regular-expression compiler and timeout policy.</param>
/// <param name="variables">Database-backed LocalGPT variable store.</param>
/// <param name="jsonText">JSON text policy service.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
public sealed class RemoteControlTemplateService(
    IRegexCompilationService regex,
    IVariableStoreService variables,
    IJsonTextService jsonText,
    ILogger<RemoteControlTemplateService> logger) : IRemoteControlTemplateService
{
    /// <summary>
    /// Stores the internal token pattern state used by <see cref="RemoteControlTemplateService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Regex _tokenPattern = regex.Compile(@"\{\{(?<expression>[^{}]{1,256})\}\}", "c", TimeSpan.FromSeconds(2), nameof(RemoteControlTemplateService));

    /// <summary>
    /// Performs resolve as part of the remote control template service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<string> ResolveAsync(
        string template,
        RemoteControlPayload? payload,
        IReadOnlyDictionary<string, RemoteControlPipelineStepResult>? steps = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;
            var matches = _tokenPattern.Matches(template);
            if (matches.Count == 0) return template;

            var replacements = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match match in matches)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var token = match.Value;
                if (replacements.ContainsKey(token)) continue;
                var expression = match.Groups["expression"].Value.Trim();
                replacements[token] = await ResolveExpressionAsync(expression, payload, steps, cancellationToken).ConfigureAwait(false);
            }

            var result = template;
            foreach (var replacement in replacements)
                result = result.Replace(replacement.Key, replacement.Value, StringComparison.Ordinal);
            return result;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Remote Control template interpolation was cancelled.");
            else
                logger.LogError(exception, "Remote Control template interpolation failed; template content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs select JSON as part of the remote control template service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public JsonElement SelectJson(JsonElement value, string selector)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selector) || selector.Trim() == "$") return value.Clone();
            var normalized = selector.Trim();
            if (normalized.StartsWith("$.", StringComparison.Ordinal)) normalized = normalized[2..];
            else if (normalized.StartsWith('$')) normalized = normalized[1..];
            if (string.IsNullOrWhiteSpace(normalized)) return value.Clone();

            var current = value;
            foreach (var segment in SplitSelector(normalized))
            {
                if (segment.IsArrayIndex)
                {
                    if (current.ValueKind != JsonValueKind.Array || segment.ArrayIndex < 0 || segment.ArrayIndex >= current.GetArrayLength())
                        throw new KeyNotFoundException($"JSON selector '{selector}' could not resolve array index {segment.ArrayIndex}.");
                    current = current[segment.ArrayIndex];
                    continue;
                }

                if (current.ValueKind != JsonValueKind.Object || !TryGetPropertyIgnoreCase(current, segment.PropertyName, out var next))
                    throw new KeyNotFoundException($"JSON selector '{selector}' could not resolve property '{segment.PropertyName}'.");
                current = next;
            }
            return current.Clone();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Remote Control JSON selection failed for selector {Selector}; payload content was omitted.", selector);
            throw;
        }
    }

    /// <summary>
    /// Parses selected JSON as part of the remote control template service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public JsonElement? ParseSelectedJson(string content, string contentType, RemoteControlResponseFormat format, string selector)
    {
        try
        {
            if (format is RemoteControlResponseFormat.Text or RemoteControlResponseFormat.Xml) return null;
            var trimmed = content.AsSpan().TrimStart();
            var looksJson = contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
                (!trimmed.IsEmpty && (trimmed[0] == '{' || trimmed[0] == '[' || trimmed[0] == '"'));
            if (format == RemoteControlResponseFormat.Auto && !looksJson) return null;
            if (format == RemoteControlResponseFormat.Json || looksJson)
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.String)
                {
                    var nested = root.GetString();
                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        var nestedTrimmed = nested.AsSpan().TrimStart();
                        if (!nestedTrimmed.IsEmpty && (nestedTrimmed[0] == '{' || nestedTrimmed[0] == '['))
                        {
                            using var nestedDocument = JsonDocument.Parse(nested);
                            root = nestedDocument.RootElement.Clone();
                        }
                    }
                }
                return string.IsNullOrWhiteSpace(selector) ? root.Clone() : SelectJson(root, selector);
            }
            return null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Remote Control response parsing failed; payload content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Resolves expression as part of the remote control template service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="expression">Expression value supplied to the remote control template operation and used when producing its result.</param>
    /// <param name="payload">Payload value supplied to the remote control template operation and used when producing its result.</param>
    /// <param name="steps">Remote control pipeline step result dependency used by the remote control template workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> ResolveExpressionAsync(
        string expression,
        RemoteControlPayload? payload,
        IReadOnlyDictionary<string, RemoteControlPipelineStepResult>? steps,
        CancellationToken cancellationToken)
    {
        try
        {
            var textMode = expression.StartsWith("text:", StringComparison.OrdinalIgnoreCase);
            var core = textMode ? expression[5..].Trim() : expression.Trim();
            string raw;

            if (core.Equals("payload", StringComparison.OrdinalIgnoreCase))
            {
                raw = payload?.Json is JsonElement payloadJson
                    ? payloadJson.GetRawText()
                    : jsonText.Serialize(payload?.RawText ?? string.Empty);
            }
            else if (core.StartsWith("payload.", StringComparison.OrdinalIgnoreCase))
            {
                if (payload?.Json is not JsonElement payloadJson)
                    throw new InvalidOperationException("The current Remote Control payload is not JSON, so a payload path cannot be resolved.");
                raw = SelectJson(payloadJson, core[8..]).GetRawText();
            }
            else if (core.StartsWith("step:", StringComparison.OrdinalIgnoreCase))
            {
                var stepExpression = core[5..].Trim();
                var separator = stepExpression.IndexOf('.', StringComparison.Ordinal);
                var stepKey = separator < 0 ? stepExpression : stepExpression[..separator].Trim();
                var selector = separator < 0 ? string.Empty : stepExpression[(separator + 1)..].Trim();
                if (steps is null || !steps.TryGetValue(stepKey, out var step))
                    throw new KeyNotFoundException($"Remote Control step result '{stepKey}' is not available.");
                var stepJson = JsonSerializer.SerializeToElement(step.Value, step.Value?.GetType() ?? typeof(object));
                raw = string.IsNullOrWhiteSpace(selector) ? stepJson.GetRawText() : SelectJson(stepJson, selector).GetRawText();
            }
            else if (core.StartsWith("var:", StringComparison.OrdinalIgnoreCase))
            {
                var variableName = core[4..].Trim();
                ArgumentException.ThrowIfNullOrWhiteSpace(variableName);
                var value = await variables.GetAsync<string>(variableName, cancellationToken).ConfigureAwait(false);
                raw = jsonText.Serialize(value);
            }
            else if (core.Equals("connector.key", StringComparison.OrdinalIgnoreCase))
            {
                raw = jsonText.Serialize(payload?.ConnectorKey ?? string.Empty);
            }
            else
            {
                throw new InvalidDataException($"Unsupported Remote Control template token '{{{{{expression}}}}}'.");
            }

            if (!textMode) return raw;
            if (TryUnwrapJsonString(raw, out var plain)) return jsonText.EscapeStringValue(plain);
            return jsonText.EscapeStringValue(raw);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Resolving Remote Control template expression was cancelled.");
            else
                logger.LogError(exception, "Resolving Remote Control template expression failed; expression content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs split selector as part of the remote control template service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="selector">Selector value supplied to the remote control template operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<SelectorSegment> SplitSelector(string selector)
    {
        try
        {
            var result = new List<SelectorSegment>();
            foreach (var rawSegment in selector.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var remaining = rawSegment;
                var bracket = remaining.IndexOf('[', StringComparison.Ordinal);
                if (bracket < 0)
                {
                    result.Add(new SelectorSegment(remaining, -1, false));
                    continue;
                }

                var property = remaining[..bracket];
                if (!string.IsNullOrWhiteSpace(property)) result.Add(new SelectorSegment(property, -1, false));
                while (bracket >= 0)
                {
                    var close = remaining.IndexOf(']', bracket + 1);
                    if (close < 0 || !int.TryParse(remaining[(bracket + 1)..close], out var index))
                        throw new InvalidDataException($"Invalid array index in JSON selector '{selector}'.");
                    result.Add(new SelectorSegment(string.Empty, index, true));
                    bracket = remaining.IndexOf('[', close + 1);
                }
            }
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Splitting a Remote Control JSON selector failed for {Selector}.", selector);
            throw;
        }
    }

    /// <summary>
    /// Attempts to retrieve property ignore case as part of the remote control template service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the remote control template operation and used when producing its result.</param>
    /// <param name="propertyName">Property name value supplied to the remote control template operation and used when producing its result.</param>
    /// <param name="result">Result value supplied to the remote control template operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool TryGetPropertyIgnoreCase(JsonElement value, string propertyName, out JsonElement result)
    {
        try
        {
            if (value.TryGetProperty(propertyName, out result)) return true;
            foreach (var property in value.EnumerateObject())
            {
                if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase)) continue;
                result = property.Value;
                return true;
            }
            result = default;
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving a Remote Control JSON property failed; payload content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Attempts to unwrap JSON string as part of the remote control template service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="raw">Raw value supplied to the remote control template operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the remote control template operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool TryUnwrapJsonString(string raw, out string value)
    {
        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                value = document.RootElement.GetString() ?? string.Empty;
                return true;
            }
            value = string.Empty;
            return false;
        }
        catch (JsonException)
        {
            value = string.Empty;
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unwrapping a Remote Control JSON string failed; content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Represents a selector segment helper type nested within <see cref="RemoteControlTemplateService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="PropertyName">Property name value supplied to the remote control template operation and used when producing its result.</param>
    /// <param name="ArrayIndex">Array index value supplied to the remote control template operation and used when producing its result.</param>
    /// <param name="IsArrayIndex">Value indicating whether array index should apply to this operation.</param>
    private readonly record struct SelectorSegment(string PropertyName, int ArrayIndex, bool IsArrayIndex);
}

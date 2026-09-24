using System.Diagnostics;
using System.Net;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates provider model benchmark behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class ProviderModelBenchmarkService
    {
    /// <summary>Attempts to parse the first complete JSON object contained in untrusted benchmark output without treating malformed model text as an application error.</summary>
    /// <param name="value">Untrusted provider response text.</param>
    /// <param name="document">The parsed first complete JSON object when successful.</param>
    /// <returns><see langword="true"/> when a complete JSON object was found and parsed; otherwise <see langword="false"/>.</returns>
    private bool TryParseFirstJsonObject(string value, out JsonDocument? document)
    {
        document = null;
        try
        {
            var normalized = value ?? string.Empty;
            for (var decodePass = 0; decodePass < 2; decodePass++)
            {
                var decoded = WebUtility.HtmlDecode(normalized);
                if (string.Equals(decoded, normalized, StringComparison.Ordinal))
                    break;
                normalized = decoded;
            }

            var searchOffset = 0;
            while (searchOffset < normalized.Length)
            {
                var start = normalized.IndexOf('{', searchOffset);
                if (start < 0)
                    break;

                if (!LooksLikeJsonObjectStart(normalized, start))
                {
                    searchOffset = start + 1;
                    continue;
                }

                if (!TryExtractBalancedJsonObject(normalized, start, out var candidate, out var nextOffset))
                {
                    searchOffset = start + 1;
                    continue;
                }

                try
                {
                    document = JsonDocument.Parse(
                        candidate,
                        new JsonDocumentOptions
                        {
                            CommentHandling = JsonCommentHandling.Skip,
                            AllowTrailingCommas = true
                        });
                    return document.RootElement.ValueKind == JsonValueKind.Object;
                }
                catch (JsonException)
                {
                    // The benchmark output is untrusted model text. Only attempt parsing after a complete,
                    // balanced JSON-shaped carrier was found so ordinary prose/code never creates a
                    // JsonReaderException storm in the debugger. A malformed balanced candidate is skipped
                    // as benchmark evidence and scanning resumes after that carrier.
                    document?.Dispose();
                    document = null;
                    searchOffset = Math.Max(nextOffset, start + 1);
                }
            }
            return false;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unexpected failure while scanning untrusted provider output for JSON; model output content was omitted.");
            return false;
        }
    }

    /// <summary>Checks whether an opening brace is plausibly the beginning of a JSON object rather than a brace in prose or source code.</summary>
    private bool LooksLikeJsonObjectStart(string value, int start)
    {
    try
    {
            for (var index = start + 1; index < value.Length; index++)
            {
                var character = value[index];
                if (char.IsWhiteSpace(character))
                    continue;
                return character is '"' or '}';
            }
            return false;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(LooksLikeJsonObjectStart)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(LooksLikeJsonObjectStart)} failed.");
        throw;
    }
}

    /// <summary>Extracts one complete brace-balanced JSON-shaped object without invoking the throwing JSON parser on incomplete model output.</summary>
    private bool TryExtractBalancedJsonObject(string value, int start, out string candidate, out int nextOffset)
    {
    try
    {
            candidate = string.Empty;
            nextOffset = Math.Min(value.Length, start + 1);
            var depth = 0;
            var inString = false;
            var escaped = false;

            for (var index = start; index < value.Length; index++)
            {
                var character = value[index];
                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }
                    if (character == '\\')
                    {
                        escaped = true;
                        continue;
                    }
                    if (character == '"')
                        inString = false;
                    continue;
                }

                if (character == '"')
                {
                    inString = true;
                    continue;
                }
                if (character == '{')
                {
                    depth++;
                    continue;
                }
                if (character != '}')
                    continue;

                depth--;
                if (depth < 0)
                    return false;
                if (depth != 0)
                    continue;

                nextOffset = index + 1;
                candidate = value[start..nextOffset];
                return true;
            }

            return false;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(TryExtractBalancedJsonObject)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(TryExtractBalancedJsonObject)} failed.");
        throw;
    }
}

    /// <summary>Parses the first complete JSON object for internal reviewer contracts that require structured data.</summary>
    /// <param name="value">Untrusted reviewer response text.</param>
    /// <returns>The parsed JSON object.</returns>
    private JsonDocument ParseFirstJsonObject(string value)
    {
        try
        {
            if (TryParseFirstJsonObject(value, out var document) && document is not null)
                return document;
            throw new JsonException("No complete JSON object was returned.");
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing a required internal benchmark-review JSON object failed; response content was omitted.");
            throw;
        }
    }

    /// <summary>Detects a narrow class of generic role/capability refusals that are invalid for maintained text-only benchmark assignments.</summary>
    /// <param name="value">Visible provider response.</param>
    /// <returns><see langword="true"/> when the response is a generic AI capability/non-performance refusal rather than an attempted answer.</returns>
    private bool LooksLikeGenericCapabilityRefusal(string value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var normalized = value.Trim();
            if (normalized.Length > 1600)
                return false;
            var lower = normalized.ToLowerInvariant();
            if (lower.Contains("safety", StringComparison.Ordinal) ||
                lower.Contains("harmful", StringComparison.Ordinal) ||
                lower.Contains("illegal", StringComparison.Ordinal))
                return false;
            return lower.Contains("as an ai", StringComparison.Ordinal) &&
                       (lower.Contains("cannot", StringComparison.Ordinal) || lower.Contains("can't", StringComparison.Ordinal) || lower.Contains("do not have", StringComparison.Ordinal)) ||
                   lower.Contains("don't have the capability", StringComparison.Ordinal) ||
                   lower.Contains("do not have the capability", StringComparison.Ordinal) ||
                   lower.Contains("cannot execute tasks", StringComparison.Ordinal) ||
                   lower.Contains("cannot participate in benchmarking", StringComparison.Ordinal) ||
                   lower.Contains("please provide the task", StringComparison.Ordinal) ||
                   lower.Contains("please provide instructions", StringComparison.Ordinal);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Detecting generic benchmark role refusal failed; response content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs estimate tokens as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int EstimateTokens(string value) {
    try
    {
        return Math.Max(1, (int)Math.Ceiling(value.Length / 4d));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(EstimateTokens)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(EstimateTokens)} failed.");
        throw;
    }
}
    /// <summary>
    /// Reads double as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="property">Property value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double ReadDouble(JsonElement root, string property, double fallback) {
    try
    {
        return root.TryGetProperty(property, out var value) && value.TryGetDouble(out var number)
            ? Math.Clamp(number, 0d, 100d)
            : fallback;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(ReadDouble)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(ReadDouble)} failed.");
        throw;
    }
}
    /// <summary>
    /// Reads int as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="property">Property value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int ReadInt(JsonElement root, string property, int fallback) {
    try
    {
        return root.TryGetProperty(property, out var value) && value.TryGetInt32(out var number) ? number : fallback;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(ReadInt)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(ReadInt)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs clamp to supported step as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="minimum">Minimum value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int ClampToSupportedStep(int value, int minimum, int maximum)
    {
    try
    {
            var clamped = Math.Clamp(value, minimum, maximum);
            var step = clamped >= 8192 ? 1024 : clamped >= 2048 ? 512 : 128;
            return Math.Clamp((int)Math.Round(clamped / (double)step) * step, minimum, maximum);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(ClampToSupportedStep)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(ClampToSupportedStep)} failed.");
        throw;
    }
}

    /// <summary>
    /// Represents a benchmark task helper type nested within <see cref="ProviderModelBenchmarkService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="Name">Name value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="Prompt">Prompt value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="ExpectedTokens">String dependency used by the provider model benchmark workflow to provide the corresponding application capability.</param>
    /// <param name="ExpectJson">Value indicating whether expect JSON should apply to this operation.</param>
    /// <param name="ExpectedSectionCount">Number of numbered answer sections expected from a composite assignment.</param>
    /// <param name="RequireEmbeddedJsonObject">Whether at least one complete embedded JSON object is required by the task contract.</param>
    /// <param name="EnforceRoleExecution">Whether a narrow generic capability refusal receives one bounded same-role retry.</param>
    private sealed record BenchmarkTask(
        string Name,
        string Prompt,
        IReadOnlyList<string> ExpectedTokens,
        bool ExpectJson = false,
        int ExpectedSectionCount = 0,
        bool RequireEmbeddedJsonObject = false,
        bool EnforceRoleExecution = false);
    /// <summary>
    /// Represents a benchmark profile helper type nested within <see cref="ProviderModelBenchmarkService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="Name">Name value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="ContextTokens">Context tokens value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="OutputTokens">Output tokens value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="OllamaNumGpu">Ollama num gpu value supplied to the provider model benchmark operation and used when producing its result.</param>
    private sealed record BenchmarkProfile(string Name, int ContextTokens, int OutputTokens, int? OllamaNumGpu);

    }
}

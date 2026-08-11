using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services;

/// <summary>
/// Recovers structured DX function calls emitted as assistant text by provider runtimes that do not
/// populate their native tool-call field. Function names are resolved exclusively against the live
/// DI-backed registry; this service never invents or hardcodes callable operations.
/// </summary>
public sealed class DxAiFunctionCallRecoveryService(
    IDxAiFunctionRegistry registry,
    ICouncilTextPatternDataService textPatterns,
    ILogger<DxAiFunctionCallRecoveryService> logger) : IDxAiFunctionCallRecoveryService
{
    /// <summary>
    /// Runs the recover operation.
    /// </summary>
    public DxAiFunctionTextRecoveryResult Recover(string content, bool automaticInvocation = true)
    {
    try
    {
            var result = new DxAiFunctionTextRecoveryResult { VisibleContent = content ?? string.Empty };
            if (string.IsNullOrWhiteSpace(content))
                return result;

            var decoded = WebUtility.HtmlDecode(content).Trim();
            var candidates = new List<(string Json, string Format, bool WholeCarrier)>();

            var tagged = textPatterns.CouncilDxFunctionCallPattern.Match(decoded);
            if (tagged.Success && tagged.Groups["json"].Success)
                candidates.Add((tagged.Groups["json"].Value, "localgpt-dx-call", tagged.Index == 0 && tagged.Length == decoded.Length));

            var fenced = TryUnwrapFence(decoded);
            if (fenced is not null)
                candidates.Add((fenced, "json-fence", true));

            if (decoded.StartsWith('{') || decoded.StartsWith('['))
                candidates.Add((decoded, "json", true));

            foreach (var candidate in candidates)
            {
                var recovered = ParseCandidate(candidate.Json, candidate.Format, automaticInvocation);
                if (recovered.Count == 0)
                    continue;

                result.Recognized = true;
                result.Calls.AddRange(recovered);
                result.SuppressRecoveredContent = candidate.WholeCarrier;
                result.VisibleContent = candidate.WholeCarrier
                    ? string.Empty
                    : textPatterns.CouncilDxFunctionCallPattern.Replace(content, string.Empty).Trim();
                logger.LogInformation("Recovered {FunctionCount} textual DX function call(s) from {SourceFormat}; raw arguments were omitted from logs.", recovered.Count, candidate.Format);
                return result;
            }

            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(Recover)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(Recover)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the looks like structured function call operation.
    /// </summary>
    public bool LooksLikeStructuredFunctionCall(string content)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(content))
                return false;
            var probe = WebUtility.HtmlDecode(content).TrimStart();
            if (probe.StartsWith('{') || probe.StartsWith('[') || probe.StartsWith("<localgpt-dx-call", StringComparison.OrdinalIgnoreCase))
                return true;
            if (!probe.StartsWith("```", StringComparison.Ordinal))
                return false;

            var firstLineEnd = probe.IndexOf('\n');
            var fenceHeader = firstLineEnd < 0 ? probe : probe[..firstLineEnd];
            return fenceHeader.Equals("```json", StringComparison.OrdinalIgnoreCase) ||
                   fenceHeader.Equals("```tool", StringComparison.OrdinalIgnoreCase) ||
                   fenceHeader.Equals("```function", StringComparison.OrdinalIgnoreCase);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(LooksLikeStructuredFunctionCall)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(LooksLikeStructuredFunctionCall)} failed.");
        throw;
    }
}

    /// <summary>
    /// Parses candidate.
    /// </summary>
    private List<RecoveredDxAiFunctionCall> ParseCandidate(string json, string format, bool automaticInvocation)
    {
    try
    {
            try
            {
                using var document = JsonDocument.Parse(json);
                var calls = new List<RecoveredDxAiFunctionCall>();
                ReadElement(document.RootElement, format, automaticInvocation, calls);
                return calls;
            }
            catch (JsonException)
            {
                return [];
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(ParseCandidate)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(ParseCandidate)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads element.
    /// </summary>
    private void ReadElement(JsonElement element, string format, bool automaticInvocation, List<RecoveredDxAiFunctionCall> calls)
    {
    try
    {
            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                    ReadElement(item, format, automaticInvocation, calls);
                return;
            }
            if (element.ValueKind != JsonValueKind.Object)
                return;

            if (element.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in toolCalls.EnumerateArray())
                    ReadElement(item, format, automaticInvocation, calls);
                return;
            }
            if (element.TryGetProperty("function", out var function) && function.ValueKind == JsonValueKind.Object)
            {
                ReadNamedCall(function, format, automaticInvocation, calls);
                return;
            }
            ReadNamedCall(element, format, automaticInvocation, calls);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(ReadElement)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(ReadElement)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads named call.
    /// </summary>
    private void ReadNamedCall(JsonElement element, string format, bool automaticInvocation, List<RecoveredDxAiFunctionCall> calls)
    {
    try
    {
            var suppliedName = ReadString(element, "name") ?? ReadString(element, "functionName");
            if (string.IsNullOrWhiteSpace(suppliedName))
                return;

            var registryName = ResolveRegistryName(suppliedName, automaticInvocation);
            if (registryName is null)
                return;

            var arguments = ReadArguments(element);
            calls.Add(new RecoveredDxAiFunctionCall
            {
                FunctionName = registryName,
                TransportName = suppliedName,
                Arguments = arguments,
                SourceFormat = format
            });
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(ReadNamedCall)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(ReadNamedCall)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves registry name.
    /// </summary>
    private string? ResolveRegistryName(string suppliedName, bool automaticInvocation)
    {
    try
    {
            var normalized = suppliedName.Trim();
            return registry.GetFunctions()
                .Where(function => function.AvailableToAi && function.SupportsDirectInvocation)
                .Where(function => !automaticInvocation ||
                    (function.RequiresHumanConfirmation
                        ? function.SupportsDeferredApprovalRequest
                        : function.SupportsAutomaticInvocation && (function.IsReadOnly || function.IsCoordinationOnly)))
                .FirstOrDefault(function =>
                    function.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
                    ToTransportName(function.Name).Equals(normalized, StringComparison.OrdinalIgnoreCase))
                ?.Name;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(ResolveRegistryName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(ResolveRegistryName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads arguments.
    /// </summary>
    private JsonElement ReadArguments(JsonElement element)
    {
    try
    {
            if (!element.TryGetProperty("arguments", out var arguments) && !element.TryGetProperty("parameters", out arguments))
                return JsonSerializer.SerializeToElement(new { });
            if (arguments.ValueKind == JsonValueKind.String)
            {
                try
                {
                    using var nested = JsonDocument.Parse(arguments.GetString() ?? "{}");
                    return nested.RootElement.Clone();
                }
                catch (JsonException)
                {
                    return JsonSerializer.SerializeToElement(new { });
                }
            }
            return arguments.Clone();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(ReadArguments)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(ReadArguments)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads string.
    /// </summary>
    private string? ReadString(JsonElement element, string propertyName) {
    try
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(ReadString)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(ReadString)} failed.");
        throw;
    }
}

    /// <summary>
    /// Attempts to unwrap fence.
    /// </summary>
    private string? TryUnwrapFence(string content)
    {
    try
    {
            if (!content.StartsWith("```", StringComparison.Ordinal))
                return null;
            var firstLine = content.IndexOf('\n');
            var lastFence = content.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLine < 0 || lastFence <= firstLine)
                return null;
            return content[(firstLine + 1)..lastFence].Trim();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(TryUnwrapFence)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(TryUnwrapFence)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the to transport name operation.
    /// </summary>
    private string ToTransportName(string registryName)
    {
    try
    {
            var chars = registryName.Select(character => char.IsLetterOrDigit(character) || character == '_' ? character : '_').ToArray();
            return new string(chars);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(ToTransportName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCallRecoveryService)}.{nameof(ToTransportName)} failed.");
        throw;
    }
}
}

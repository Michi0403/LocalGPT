using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services.Formatting;
using Microsoft.Extensions.AI;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.Services;

/// <summary>
/// Represents an Ollama thinking chat application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed partial class OllamaThinkingChatClient
{
    /// <summary>
    /// Performs append automatic tool results for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="conversation">Conversation value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="toolCalls">Ollama tool call dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>User-visible function-result trace fragments produced while appending tool messages.</returns>
    private async Task<IReadOnlyList<string>> AppendAutomaticToolResultsAsync(
        List<OllamaChatMessage> conversation,
        IReadOnlyList<OllamaToolCall> toolCalls,
        CancellationToken cancellationToken)
    {
        try
        {
            var traces = new List<string>(toolCalls.Count);
            var missingToolWarningLogged = false;
            foreach (var call in toolCalls)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var registryName = ResolveRegistryFunctionName(call.Function.Name);
                DxAiFunctionInvocationResult result;

                if (functionRegistry is null || registryName is null)
                {
                    if (!missingToolWarningLogged)
                    {
                        // A registered function can still be absent from this request's
                        // advertised/tool-policy surface. Record which boundary rejected
                        // it without logging parameters, files, or tool result content.
                        var requestedName = call.Function.Name ?? string.Empty;
                        requestedName = requestedName.Replace('\r', ' ').Replace('\n', ' ');
                        if (requestedName.Length > 128) requestedName = requestedName[..128];
                        logger.LogWarning(
                            "Ollama tool name {RequestedTool} was not resolvable for model {Model}; registryPresent={RegistryPresent}, automaticToolsEnabled={AutomaticToolsEnabled}, policyListCount={PolicyListCount}. This is tool exposure/resolution, not proof of a missing workspace file.",
                            requestedName, model, functionRegistry is not null, automaticToolsEnabled,
                            automaticFunctionAllowList?.Count ?? -1);
                        missingToolWarningLogged = true;
                    }
                    result = new DxAiFunctionInvocationResult
                    {
                        FunctionName = call.Function.Name,
                        Status = "NotFound",
                        Error = "The requested automatic function is not registered.",
                        Succeeded = false
                    };
                }
                else
                {
                    result = await functionRegistry.InvokeAsync(
                        registryName,
                        new DxAiFunctionInvocationRequest
                        {
                            Parameters = NormalizeArguments(call.Function.Arguments),
                            AutomaticInvocation = true,
                            UserConfirmed = false,
                            RequestedBy = $"Ollama:{model}",
                            ConversationId = sessionContext?.ConversationId,
                            ProjectId = sessionContext?.ProjectId,
                            ProjectVersionId = sessionContext?.ProjectVersionId,
                            ApplicationVersion = sessionContext?.ApplicationVersion ?? string.Empty
                        },
                        cancellationToken).ConfigureAwait(false);
                }

                var serializedResult = SerializeToolResult(result);
                conversation.Add(new OllamaChatMessage
                {
                    Role = "tool",
                    ToolName = call.Function.Name,
                    Content = serializedResult
                });
                traces.Add(BuildOllamaFunctionResultTrace(call.Function.Name, result, serializedResult));
            }

            return traces;
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(AppendAutomaticToolResultsAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(AppendAutomaticToolResultsAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Builds durable user-visible markup for an Ollama function request so direct chat, Council streams and saved sessions retain the exact call.
    /// </summary>
    /// <param name="call">Ollama function call requested by the provider.</param>
    /// <returns>Controlled chat markup containing the function name and normalized arguments.</returns>
    private string BuildOllamaFunctionCallTrace(OllamaToolCall call)
    {
        try
        {
            var functionName = string.IsNullOrWhiteSpace(call.Function.Name) ? "(unnamed)" : call.Function.Name.Trim();
            var arguments = NormalizeArguments(call.Function.Arguments).GetRawText();
            return $"<details class=\"council-step\" open><summary>Function call · {System.Net.WebUtility.HtmlEncode(functionName)}</summary>\n\n{councilRuntime.FormatUserVisibleCodePayload(arguments, logger)}\n\n</details>\n\n";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not format a user-visible Ollama function-call trace.");
            return "<details class=\"council-step\" open><summary>Function call</summary>\n\nThe provider requested a function, but its trace payload could not be formatted.\n\n</details>\n\n";
        }
    }

    /// <summary>
    /// Builds durable user-visible markup for an Ollama function result so function execution evidence survives chat persistence.
    /// </summary>
    /// <param name="functionName">Provider function name associated with the result.</param>
    /// <param name="result">Function registry result containing status and success information.</param>
    /// <param name="serializedResult">Bounded serialized function result returned to the model.</param>
    /// <returns>Controlled chat markup containing the function status and result payload.</returns>
    private string BuildOllamaFunctionResultTrace(
        string functionName,
        DxAiFunctionInvocationResult result,
        string serializedResult)
    {
        try
        {
            var displayName = string.IsNullOrWhiteSpace(functionName) ? "(unnamed)" : functionName.Trim();
            var status = string.IsNullOrWhiteSpace(result.Status)
                ? (result.Succeeded ? "Succeeded" : "Failed")
                : result.Status.Trim();
            return $"<details class=\"council-step\" open><summary>Function result · {System.Net.WebUtility.HtmlEncode(displayName)} · {System.Net.WebUtility.HtmlEncode(status)}</summary>\n\n{councilRuntime.FormatUserVisibleCodePayload(serializedResult, logger)}\n\n</details>\n\n";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not format a user-visible Ollama function-result trace for {FunctionName}.", functionName);
            return "<details class=\"council-step\" open><summary>Function result</summary>\n\nThe function completed, but its trace payload could not be formatted.\n\n</details>\n\n";
        }
    }

    /// <summary>Ensures the Ollama transport receives one unambiguous tool name for every registered automatic function.</summary>
    /// <param name="functions">Canonical registered functions selected by the configured policy.</param>
    private void ValidateUniqueAutomaticToolNames(IReadOnlyList<DxaichatFunctionInfo> functions)
    {
        try
        {
            var duplicates = functions
                .GroupBy(function => ToOllamaToolName(function.Name), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Select(function => function.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                .Select(group => $"{group.Key}: {string.Join(", ", group.Select(function => function.Name))}")
                .ToList();
            if (duplicates.Count > 0)
                throw new InvalidOperationException($"Automatic DXFunction transport names are ambiguous: {string.Join("; ", duplicates)}");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Validating unique Ollama automatic-tool transport names failed for model {Model}.", model);
            throw;
        }
    }

    /// <summary>
    /// Resolves registry function name for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="toolName">Tool name value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string? ResolveRegistryFunctionName(string toolName) {
    try
    {
        var functions = GetAutomaticFunctions();
        ValidateUniqueAutomaticToolNames(functions);
        // First resolve exactly what this request advertised. Strict step/team lists
        // must never escape this branch, including when a model invents a tool name.
        var directMatch = functions.FirstOrDefault(function =>
            function.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase) ||
            ToOllamaToolName(function.Name).Equals(toolName, StringComparison.OrdinalIgnoreCase));
        if (directMatch is not null)
            return directMatch.Name;

        // A NORMAL Council step publishes preferred tools plus the DXFunction broker.
        // The preferences are not a permission boundary: an eligible tool already
        // known to the model can still be resolved directly, without requiring it to
        // be advertised a second time. This is deliberately disabled for strict
        // lists, which have no broker entrypoints, and for disabled function access.
        if (automaticFunctionAllowList is null ||
            !automaticFunctionAllowList.Contains("dxai.tools.invoke") ||
            functionRegistry is null)
            return null;

        var eligible = functionRegistry.GetFunctions()
            .Where(function => function.AvailableToAi &&
                function.SupportsDirectInvocation && function.SupportsAutomaticInvocation)
            .ToList();
        var canonical = eligible.FirstOrDefault(function =>
            function.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
        if (canonical is not null)
            return canonical.Name;

        // A sanitized transport name can be ambiguous. Never dispatch it if more
        // than one canonical name maps to the same model-facing spelling.
        var transportMatches = eligible
            .Where(function => ToOllamaToolName(function.Name).Equals(toolName, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToList();
        return transportMatches.Count == 1 ? transportMatches[0].Name : null;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ResolveRegistryFunctionName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ResolveRegistryFunctionName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs to Ollama tool name for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="registryName">Registry name value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ToOllamaToolName(string registryName)
    {
    try
    {
            var builder = new StringBuilder(registryName.Length);
            foreach (var character in registryName)
                builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
            return builder.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToOllamaToolName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToOllamaToolName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds parameters schema for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="function">Function value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The JSON element produced by the operation.</returns>
    private JsonElement BuildParametersSchema(DxaichatFunctionInfo function)
    {
        try
        {
            using var document = JsonDocument.Parse(function.ParameterSchemaJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new JsonException("DXAIFunction parameter schema must be a JSON object.");
            return root.Clone();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "DXAIFunction {FunctionName} has invalid parameter schema metadata; an empty object schema will be used.", function.Name);
            return JsonSerializer.SerializeToElement(new { type = "object", properties = new { } });
        }
    }

    /// <summary>
    /// Normalizes arguments for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="arguments">Arguments value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The JSON element produced by the operation.</returns>
    private JsonElement NormalizeArguments(JsonElement arguments) {
    try
    {
        return arguments.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
        ? JsonSerializer.SerializeToElement(new { })
        : arguments.Clone();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(NormalizeArguments)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(NormalizeArguments)} failed.");
        throw;
    }
}

    /// <summary>Retains only completed workspace-read cursor metadata in a truncated function result; file contents stay bounded by the transport limit.</summary>
    /// <param name="result">Actual registered function result, not untrusted assistant text.</param>
    /// <returns>Source path and read cursor evidence, or null for non-file functions.</returns>
    private object? GetWorkspaceReadTransportMetadata(DxAiFunctionInvocationResult result)
    {
        try
        {
            if (!string.Equals(result.FunctionName, "chat.upload_workspace_file", StringComparison.OrdinalIgnoreCase) ||
                !result.Succeeded || result.Value is null)
                return null;
            var value = JsonSerializer.SerializeToElement(result.Value, jsonOptions);
            if (value.ValueKind != JsonValueKind.Object ||
                !value.TryGetProperty("workspaceName", out var workspace) ||
                !value.TryGetProperty("relativePath", out var path) ||
                !value.TryGetProperty("kind", out var kind) ||
                !value.TryGetProperty("characterOffset", out var offset) ||
                !value.TryGetProperty("charactersReturned", out var returned) ||
                !value.TryGetProperty("hasMore", out var hasMore) ||
                workspace.ValueKind != JsonValueKind.String || path.ValueKind != JsonValueKind.String ||
                kind.ValueKind != JsonValueKind.String ||
                offset.ValueKind != JsonValueKind.Number || returned.ValueKind != JsonValueKind.Number ||
                (hasMore.ValueKind != JsonValueKind.True && hasMore.ValueKind != JsonValueKind.False))
                return null;
            return new
            {
                WorkspaceName = workspace.GetString(), RelativePath = path.GetString(),
                Kind = kind.GetString(),
                CharacterOffset = offset.GetInt64(), CharactersReturned = returned.GetInt64(),
                HasMore = hasMore.GetBoolean()
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not retain source-read progress metadata for a bounded tool result; no file content was logged.");
            return null;
        }
    }

    /// <summary>
    /// Performs serialize tool result for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="result">Result value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SerializeToolResult(DxAiFunctionInvocationResult result)
    {
        try
        {
            var json = JsonSerializer.Serialize(result, jsonOptions);
            var maximumCharacters = MaxToolResultCharacters;
            if (json.Length <= maximumCharacters)
                return json;

            // The provider tool-message contract is exactly one JSON value. Cutting serialized JSON
            // at a character boundary and appending a second object corrupts that contract (and
            // generates first-chance JsonReaderExceptions in the Council's visible trace).
            // Preserve the configured ceiling by transporting an explicitly incomplete JSON prefix
            // as a *string*, rather than presenting the prefix as a complete function receipt.
            // Small, provenance-bearing read metadata survives large CONTENT truncation. It is
            // derived from the actual registered function result, not from the model's prose.
            var readProgress = GetWorkspaceReadTransportMetadata(result);
            var minimumMarker = JsonSerializer.Serialize(new { Truncated = true }, jsonOptions);
            var lower = 0;
            var upper = Math.Min(json.Length, maximumCharacters);
            var retainedCharacters = 0;
            string? boundedResult = null;
            while (lower <= upper)
            {
                var midpoint = lower + (upper - lower) / 2;
                var prefixLength = midpoint;
                if (prefixLength > 0 && prefixLength < json.Length &&
                    char.IsHighSurrogate(json[prefixLength - 1]) && char.IsLowSurrogate(json[prefixLength]))
                {
                    prefixLength--;
                }

                var candidate = JsonSerializer.Serialize(new
                {
                    Truncated = true,
                    result.FunctionName,
                    result.Status,
                    result.Succeeded,
                    OriginalCharacters = json.Length,
                    ReadProgress = readProgress,
                    SerializedResultPrefix = json[..prefixLength]
                }, jsonOptions);

                if (candidate.Length <= maximumCharacters)
                {
                    boundedResult = candidate;
                    retainedCharacters = prefixLength;
                    lower = midpoint + 1;
                }
                else
                {
                    upper = midpoint - 1;
                }
            }

            if (boundedResult is not null)
            {
                logger.LogWarning(
                    "Ollama function {FunctionName} returned {OriginalCharacters} serialized characters, exceeding the configured {MaximumCharacters}-character tool transport ceiling. A valid JSON truncation envelope with {RetainedCharacters} prefix characters was sent; request a smaller result or page for complete evidence.",
                    result.FunctionName, json.Length, maximumCharacters, retainedCharacters);
                return boundedResult;
            }

            // Even an empty full-metadata envelope cannot fit an extremely small operator limit.
            // Prefer the minimal valid truncation marker over malformed JSON or invented success.
            logger.LogWarning(
                "Ollama function {FunctionName} has a tool-result ceiling of {MaximumCharacters} characters, too small for the JSON truncation envelope (minimum marker {MinimumCharacters}). Returning the minimal valid truncation marker.",
                result.FunctionName, maximumCharacters, minimumMarker.Length);
            return minimumMarker;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Serializing an Ollama tool result was canceled.");
            else
                logger.LogError(exception, "Serializing an Ollama tool result failed.");
            throw;
        }
    }

}

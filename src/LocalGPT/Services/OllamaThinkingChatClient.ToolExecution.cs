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
            foreach (var call in toolCalls)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var registryName = ResolveRegistryFunctionName(call.Function.Name);
                DxAiFunctionInvocationResult result;

                if (functionRegistry is null || registryName is null)
                {
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
                            RequestedBy = $"Ollama:{model}"
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
            return $"<details class=\"council-step\" open><summary>Function call · {System.Net.WebUtility.HtmlEncode(functionName)}</summary>\n\n<pre>{System.Net.WebUtility.HtmlEncode(arguments)}</pre>\n\n</details>\n\n";
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
            return $"<details class=\"council-step\" open><summary>Function result · {System.Net.WebUtility.HtmlEncode(displayName)} · {System.Net.WebUtility.HtmlEncode(status)}</summary>\n\n<pre>{System.Net.WebUtility.HtmlEncode(serializedResult)}</pre>\n\n</details>\n\n";
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
        return functions
            .FirstOrDefault(function => ToOllamaToolName(function.Name).Equals(toolName, StringComparison.OrdinalIgnoreCase))
            ?.Name;
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
            return json.Length <= MaxToolResultCharacters
                ? json
                : json[..MaxToolResultCharacters] + "\n{\"truncated\":true}";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(SerializeToolResult)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(SerializeToolResult)} failed.");
        throw;
    }
}

}

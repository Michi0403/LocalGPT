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
    /// Builds automatic tools for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<OllamaToolDefinition>? BuildAutomaticTools()
    {
    try
    {
            if (!automaticToolsEnabled)
                return null;

            if (functionRegistry is null)
            {
                logger.LogWarning("Ollama model {Model} has no DXFunction registry, so native tool metadata cannot be attached.", model);
                return null;
            }

            var functions = GetAutomaticFunctions();
            if (functions.Count == 0)
            {
                logger.LogWarning("Ollama model {Model} has no policy-approved automatic DXFunctions to attach.", model);
                return null;
            }

            // Ollama receives transport-safe tool names rather than canonical DXFunction names. Reject any
            // collision before native metadata is sent so two registry functions can never masquerade as one tool.
            ValidateUniqueAutomaticToolNames(functions);
            logger.LogInformation("Attaching {FunctionCount} policy-approved automatic DXFunctions to Ollama model {Model}.", functions.Count, model);
            return functions.Select(function => new OllamaToolDefinition
            {
                Function = new OllamaToolFunctionDefinition
                {
                    Name = ToOllamaToolName(function.Name),
                    Description = $"{function.Purpose} Parameters: {function.Parameters} Safety: {function.SafetyNotes}",
                    Parameters = BuildParametersSchema(function)
                }
            }).ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(BuildAutomaticTools)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(BuildAutomaticTools)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves automatic functions for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<DxaichatFunctionInfo> GetAutomaticFunctions() {
    try
    {
        return functionRegistry?
        .GetFunctions()
        .Where(function => function.AvailableToAi &&
                           (automaticFunctionAllowList is null || automaticFunctionAllowList.Contains(function.Name)) &&
                           function.SupportsDirectInvocation &&
                           (function.RequiresHumanConfirmation
                               ? function.SupportsDeferredApprovalRequest
                               : function.SupportsAutomaticInvocation &&
                                 (function.IsReadOnly || function.IsCoordinationOnly)))
        .OrderBy(function => function.Name, StringComparer.OrdinalIgnoreCase)
        .ToList() ?? [];
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(GetAutomaticFunctions)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(GetAutomaticFunctions)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs to Ollama tool calls for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="calls">Recovered devexpress ai function call dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<OllamaToolCall> ToOllamaToolCalls(IEnumerable<RecoveredDxAiFunctionCall> calls) {
    try
    {
        return calls.Select(call => new OllamaToolCall
        {
            Function = new OllamaToolFunctionCall
            {
                Name = ToTransportToolName(call.FunctionName),
                Arguments = call.Arguments.Clone()
            }
        }).ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToOllamaToolCalls)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToOllamaToolCalls)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs to transport tool name for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="registryName">Registry name value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ToTransportToolName(string registryName)
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
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToTransportToolName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToTransportToolName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs deduplicate tool calls for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="toolCalls">Ollama tool call dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<OllamaToolCall> DeduplicateToolCalls(IEnumerable<OllamaToolCall> toolCalls)
    {
    try
    {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<OllamaToolCall>();
            foreach (var call in toolCalls)
            {
                var key = $"{call.Function.Name}\n{NormalizeArguments(call.Function.Arguments).GetRawText()}";
                if (seen.Add(key))
                    result.Add(call);
            }
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(DeduplicateToolCalls)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(DeduplicateToolCalls)} failed.");
        throw;
    }
}

}

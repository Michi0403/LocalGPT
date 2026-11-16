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
    /// Creates conversation for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="messages">Chat message dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<List<OllamaChatMessage>> CreateConversationAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
    try
    {
            var mappedMessages = messages
                .Select(ToOllamaMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message.Content))
                .ToList();

            if (protocolResolver.Resolve(providerOptions) == ChatResponseProtocol.Harmony)
            {
                var harmonyPrompt = await GetPromptAsync("HarmonyResponseProtocol", cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(harmonyPrompt))
                    AddSystemPrompt(mappedMessages, harmonyPrompt);
                else
                    logger.LogWarning("Harmony protocol is selected for model {Model}, but its database prompt is unavailable.", model);
            }

            return mappedMessages;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateConversationAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateConversationAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates request for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="messages">Ollama chat message dependency used by the Ollama thinking chat workflow to provide the corresponding application capability.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="stream">Value indicating whether stream should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>Prepared request or a context-fit failure for the owning Council and GoalExecutionService.</returns>
    private async Task<(OllamaChatRequest? Request, string? PreflightFailure)> CreateRequestAsync(
        IReadOnlyList<OllamaChatMessage> messages,
        ChatOptions? options,
        bool stream,
        CancellationToken cancellationToken)
    {
    try
    {
            cancellationToken.ThrowIfCancellationRequested();
            var requestMessages = messages.Select(CloneMessage).ToList();
            bool? think = councilRuntime.OllamaThinkingChatClientShouldSkipExplicitThinking(http.BaseAddress, model, logger) ? null : true;
            var tools = BuildAutomaticTools();
            var modelMaximumContext = await councilRuntime.OllamaThinkingChatClientResolveModelMaximumContextAsync(http, model, logger, cancellationToken).ConfigureAwait(false);
            // Ollama silently drops the oldest prompt content (system prompt and native tool schemas first) when
            // prompt + output exceed num_ctx, and thinking tokens count against num_predict. Size both per request.
            var budget = councilRuntime.OllamaThinkingChatClientResolveRequestBudget(
                contextLength,
                options?.MaxOutputTokens ?? 2048,
                think == true,
                MeasureRequestCharacters(requestMessages, tools),
                model,
                logger,
                modelMaximumContext);
            // Preserve the latest user request and every system instruction, and keep
            // the complete original chat in database storage. Older turns are only
            // removed from this cloned provider payload when the provider's actual
            // context limit cannot accommodate them. Keep native DX tools if possible.
            var reducedHistoryTurns = 0;
            while (budget.PromptExceedsMaximum)
            {
                var lastUserIndex = requestMessages.FindLastIndex(message =>
                    string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase));
                if (lastUserIndex <= 0)
                    break;
                var oldestUserIndex = -1;
                for (var index = 0; index < lastUserIndex; index++)
                {
                    if (string.Equals(requestMessages[index].Role, "user", StringComparison.OrdinalIgnoreCase))
                    {
                        oldestUserIndex = index;
                        break;
                    }
                }
                if (oldestUserIndex < 0)
                    break;
                var nextTurn = oldestUserIndex + 1;
                while (nextTurn < lastUserIndex &&
                    !string.Equals(requestMessages[nextTurn].Role, "user", StringComparison.OrdinalIgnoreCase))
                    nextTurn++;
                requestMessages.RemoveRange(oldestUserIndex, nextTurn - oldestUserIndex);
                reducedHistoryTurns++;
                budget = councilRuntime.OllamaThinkingChatClientResolveRequestBudget(
                    contextLength, options?.MaxOutputTokens ?? 2048, think == true,
                    MeasureRequestCharacters(requestMessages, tools), model, logger, modelMaximumContext);
            }
            if (reducedHistoryTurns > 0)
                logger.LogWarning(
                    "Ollama request for {Model} exceeded its effective context; omitted {Count} old conversation turn(s) from the provider payload only. System messages, the last user request and durable chat history are preserved.",
                    model, reducedHistoryTurns);

            // Native tools selected by a Council role are deliberate capabilities, not
            // dispensable padding. The goal observer can reduce the work and reassign an
            // eligible subagent; silently deleting its codegen/approval tools cannot.
            // Generic, unscoped chat still sheds the largest optional tool definitions.
            while (budget.PromptExceedsMaximum && automaticFunctionAllowList is null && tools is { Count: > 0 })
            {
                var largest = tools.MaxBy(tool => JsonSerializer.Serialize(tool, jsonOptions).Length)!;
                tools.Remove(largest);
                logger.LogWarning("Dropped native tool {Tool} from the {Model} request to fit its context window ({Remaining} tool(s) remain).",
                    largest.Function.Name, model, tools.Count);
                budget = councilRuntime.OllamaThinkingChatClientResolveRequestBudget(
                    contextLength, options?.MaxOutputTokens ?? 2048, think == true,
                    MeasureRequestCharacters(requestMessages, tools), model, logger, modelMaximumContext);
            }
            if (tools is { Count: 0 })
                tools = null;
            // Provider preflight owns only the fit check. It must neither throw a
            // routine context-capacity exception nor send a request known not to fit.
            // Return an observable failure to the existing Council -> GoalExecution path.
            if (budget.PromptExceedsMaximum)
            {
                var diagnostic =
                    $"OllamaContextBudgetExceeded;required={budget.RequiredContextTokens};" +
                    $"allowed={budget.NumCtx.GetValueOrDefault()};output={budget.NumPredict};" +
                    $"max={modelMaximumContext.GetValueOrDefault()}; " +
                    $"The request for {model} needs about {budget.RequiredContextTokens:N0} context tokens, " +
                    $"but at most {budget.NumCtx:N0} are allowed. " +
                    $"Configured num_ctx={contextLength?.ToString() ?? "provider default"}; " +
                    $"advertised model context={modelMaximumContext?.ToString() ?? "unknown"}.";
                logger.LogWarning(
                    "Ollama preflight cannot fit model {Model}: estimated {Required} tokens, " +
                    "effective context {Allowed}. Returning an observed failure to Council/GoalExecution.",
                    model, budget.RequiredContextTokens, budget.NumCtx);
                return (null, diagnostic);
            }
            return (new OllamaChatRequest
            {
                Model = model,
                Stream = stream,
                Think = think,
                KeepAlive = keepAlive,
                Messages = requestMessages,
                Tools = tools,
                Options = new OllamaRequestOptions
                {
                    NumPredict = budget.NumPredict,
                    NumCtx = budget.NumCtx,
                    NumGpu = numGpu,
                    Temperature = options?.Temperature
                }
            }, null);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateRequestAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateRequestAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Measures the characters Ollama renders into the prompt for this request: message text, previous thinking,
    /// tool-call arguments and the serialized native tool schemas.
    /// </summary>
    /// <param name="requestMessages">Messages sent with the request.</param>
    /// <param name="tools">Native tool definitions attached to the request, if any.</param>
    /// <returns>Total prompt characters used for the context estimate.</returns>
    private long MeasureRequestCharacters(IReadOnlyList<OllamaChatMessage> requestMessages, IReadOnlyList<OllamaToolDefinition>? tools)
    {
        try
        {
            long total = 0;
            foreach (var message in requestMessages)
            {
                total += message.Content?.Length ?? 0;
                total += message.Thinking?.Length ?? 0;
                total += message.ToolName?.Length ?? 0;
                if (message.ToolCalls is { Count: > 0 })
                    total += message.ToolCalls.Sum(call => (long)(call.Function?.Name?.Length ?? 0) +
                        (call.Function is null || call.Function.Arguments.ValueKind == JsonValueKind.Undefined
                            ? 0 : call.Function.Arguments.GetRawText().Length));
            }
            if (tools is { Count: > 0 })
                total += JsonSerializer.Serialize(tools, jsonOptions).Length;
            return total;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not measure the Ollama request size for model {Model}; the configured context is used unchanged.", model);
            return 0;
        }
    }

}

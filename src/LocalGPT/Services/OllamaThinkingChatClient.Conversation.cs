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
            var toolNames = new Dictionary<string, string>(StringComparer.Ordinal);
            var mappedMessages = messages
                .SelectMany(message => ToOllamaMessages(message, toolNames))
                .Where(message => !string.IsNullOrWhiteSpace(message.Content) ||
                    message.ToolCalls is { Count: > 0 } || message.Images is { Count: > 0 } ||
                    message.Role == "tool")
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
            // Only provider reasoning from completed, non-tool assistant turns is expendable.
            // Never remove a user's correction, a system instruction, a tool receipt or a
            // currently active tool transaction to make an otherwise oversized request fit.
            if (budget.PromptExceedsMaximum)
            {
                var lastUserIndex = requestMessages.FindLastIndex(message => message.Role == "user");
                var compactedTurns = 0;
                for (var index = 0; index < lastUserIndex && budget.PromptExceedsMaximum; index++)
                {
                    var message = requestMessages[index];
                    if (message.Role != "assistant" || string.IsNullOrWhiteSpace(message.Content) ||
                        string.IsNullOrEmpty(message.Thinking) || message.ToolCalls is { Count: > 0 })
                        continue;
                    message.Thinking = null;
                    compactedTurns++;
                    budget = councilRuntime.OllamaThinkingChatClientResolveRequestBudget(
                        contextLength, options?.MaxOutputTokens ?? 2048, think == true,
                        MeasureRequestCharacters(requestMessages, tools), model, logger, modelMaximumContext);
                }
                if (compactedTurns > 0)
                    logger.LogInformation("Compacted provider-only reasoning from {Count} completed assistant turns for {Model}; original conversation and tool evidence are unchanged.",
                        compactedTurns, model);
            }

            // A scoped tool list is part of the task contract. An unscoped request may
            // use the existing discovery broker instead of a large directory, but only
            // when discovery, preview and invocation are all available.
            if (budget.PromptExceedsMaximum && automaticFunctionAllowList is null && tools is { Count: > 0 })
            {
                var compactTools = GetDiscoveryBrokerTools(tools);
                if (compactTools is not null)
                {
                    tools = compactTools;
                    budget = councilRuntime.OllamaThinkingChatClientResolveRequestBudget(
                        contextLength, options?.MaxOutputTokens ?? 2048, think == true,
                        MeasureRequestCharacters(requestMessages, tools), model, logger, modelMaximumContext);
                }
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
                ContextCeiling = budget.NumCtx,
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
            // Measure the actual serialized message and schema shape, including roles,
            // separators, tool names, arguments and any image payloads. This is still
            // an estimate, not the provider's tokenizer or multimodal token count.
            var measurementOptions = new JsonSerializerOptions(jsonOptions)
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            long system = 0, user = 0, other = 0;
            foreach (var message in requestMessages)
            {
                var size = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(message, measurementOptions));
                if (message.Role == "system") system += size;
                else if (message.Role == "user") user += size;
                else other += size;
            }
            var schema = tools is { Count: > 0 } ? Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(tools, measurementOptions)) : 0;
            logger.LogInformation("Ollama payload size for {Model}: system {SystemChars}, user/task {UserChars}, assistant/tool history {OtherChars}, schemas {SchemaChars}, tools {ToolCount}; estimated decoded JSON UTF-8 sizes, no payload contents.",
                model, system, user, other, schema, tools?.Count ?? 0);
            return system + user + other + schema + requestMessages.Count + 2;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not measure the Ollama request size for model {Model}; an unmeasured request will not be sent.", model);
            throw;
        }
    }

}

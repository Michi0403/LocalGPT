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
    /// <returns>The Ollama chat request produced by the operation.</returns>
    private Task<OllamaChatRequest> CreateRequestAsync(
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
            // Ollama silently drops the oldest prompt content (system prompt and native tool schemas first) when
            // prompt + output exceed num_ctx, and thinking tokens count against num_predict. Size both per request.
            var budget = councilRuntime.OllamaThinkingChatClientResolveRequestBudget(
                contextLength,
                options?.MaxOutputTokens ?? 2048,
                think == true,
                MeasureRequestCharacters(requestMessages, tools),
                model,
                logger);
            return Task.FromResult(new OllamaChatRequest
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
            });
    
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

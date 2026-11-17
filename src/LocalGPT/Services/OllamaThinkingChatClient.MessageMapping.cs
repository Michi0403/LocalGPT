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
    /// Creates contents for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="response">Response value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<List<AIContent>> CreateContentsAsync(
        OllamaChatResponse response,
        CancellationToken cancellationToken)
    {
    try
    {
            var missingFinalAnswerNotice = await GetPromptAsync("MissingFinalAnswerNotice", cancellationToken).ConfigureAwait(false);
            var formatter = formatterFactory.Create(protocolResolver.Resolve(providerOptions), missingFinalAnswerNotice);
            var visible = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(response.Message?.Thinking))
            {
                foreach (var chunk in formatter.AppendThinking(response.Message.Thinking))
                    visible.Append(chunk);
            }

            if (!string.IsNullOrEmpty(response.Message?.Content))
            {
                foreach (var chunk in formatter.AppendContent(response.Message.Content))
                    visible.Append(chunk);
            }

            foreach (var chunk in formatter.Complete())
                visible.Append(chunk);

            return [new TextContent(visible.ToString())];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateContentsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateContentsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves prompt for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="key">Key value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> GetPromptAsync(string key, CancellationToken cancellationToken)
    {
    try
    {
            if (promptConfigService is null)
                return string.Empty;

            return await promptConfigService.GetPromptAsync(key, cancellationToken: cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(GetPromptAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(GetPromptAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs clone message for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The Ollama chat message produced by the operation.</returns>
    private OllamaChatMessage CloneMessage(OllamaChatMessage message) {
    try
    {
        return new()
    {
        Role = message.Role,
        Content = message.Content,
        Thinking = message.Thinking,
        ToolName = message.ToolName,
        Images = message.Images?.ToList(),
        ToolCalls = message.ToolCalls?.Select(call => new OllamaToolCall
        {
            Function = new OllamaToolFunctionCall
            {
                Name = call.Function.Name,
                Arguments = NormalizeArguments(call.Function.Arguments)
            }
        }).ToList()
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CloneMessage)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CloneMessage)} failed.");
        throw;
    }
}

    /// <summary>
    /// Maps a chat message and its structured function history into native Ollama messages without invoking any historical calls.
    /// </summary>
    /// <param name="message">The original chat message containing text or function content.</param>
    /// <param name="toolNames">Request-local mapping from function call identifiers to their wire names for result correlation.</param>
    /// <returns>Native messages in their original call/result order.</returns>
    private IReadOnlyList<OllamaChatMessage> ToOllamaMessages(ChatMessage message, Dictionary<string, string> toolNames)
    {
        try
        {
            var mapped = new List<OllamaChatMessage>();
            var native = new OllamaChatMessage
            {
                Role = message.Role == ChatRole.System ? "system" :
                    message.Role == ChatRole.Assistant ? "assistant" :
                    message.Role == ChatRole.Tool ? "tool" : "user",
                Content = message.Text ?? string.Empty
            };
            foreach (var call in message.Contents.OfType<FunctionCallContent>())
            {
                var name = ToOllamaToolName(call.Name);
                toolNames[call.CallId] = name;
                native.ToolCalls ??= [];
                native.ToolCalls.Add(new OllamaToolCall
                {
                    Function = new OllamaToolFunctionCall
                    {
                        Name = name,
                        Arguments = call.Arguments is null ? JsonSerializer.SerializeToElement(new { }) :
                            NormalizeArguments(JsonSerializer.SerializeToElement(call.Arguments))
                    }
                });
            }
            if (native.Content.Length > 0 || native.ToolCalls is { Count: > 0 } ||
                (message.Role == ChatRole.Tool && !message.Contents.OfType<FunctionResultContent>().Any()))
                mapped.Add(native);
            foreach (var result in message.Contents.OfType<FunctionResultContent>())
            {
                if (!toolNames.TryGetValue(result.CallId, out var name))
                {
                    // An isolated result cannot be associated with an invented tool name.
                    // Retain the evidence as a tool message; Ollama supports unnamed results.
                    logger.LogWarning("Ollama history for {Model} contains a function result without its original call.", model);
                }
                mapped.Add(new OllamaChatMessage
                {
                    Role = "tool",
                    ToolName = name,
                    Content = result.Result is string text ? text : JsonSerializer.Serialize(result.Result, jsonOptions)
                });
            }
            return mapped;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not map chat history for Ollama model {Model}.", model);
            throw;
        }
    }

    /// <summary>
    /// Adds system prompt for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="messages">Messages value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="prompt">Prompt value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    private void AddSystemPrompt(List<OllamaChatMessage> messages, string prompt)
    {
    try
    {
            if (messages.Count > 0 && messages[0].Role.Equals("system", StringComparison.OrdinalIgnoreCase))
            {
                if (!messages[0].Content.Contains(prompt, StringComparison.Ordinal))
                    messages[0].Content = $"{prompt}\n\n{messages[0].Content}";
                return;
            }

            messages.Insert(0, new OllamaChatMessage
            {
                Role = "system",
                Content = prompt
            });
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(AddSystemPrompt)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(AddSystemPrompt)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates streaming update for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="text">Text value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The chat response update produced by the operation.</returns>
    private ChatResponseUpdate CreateStreamingUpdate(string text)
    {
    try
    {
            var update = councilRuntime.OllamaThinkingChatClientCreateStreamingUpdate(text, logger);
            return update ?? new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(text)]);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateStreamingUpdate)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(CreateStreamingUpdate)} failed.");
        throw;
    }
}

}

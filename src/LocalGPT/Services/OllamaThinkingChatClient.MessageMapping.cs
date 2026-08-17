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
    /// Performs to Ollama message for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <returns>The Ollama chat message produced by the operation.</returns>
    private OllamaChatMessage ToOllamaMessage(ChatMessage message) {
    try
    {
        return new()
    {
        Role = message.Role == ChatRole.System
            ? "system"
            : message.Role == ChatRole.Assistant
                ? "assistant"
                : "user",
        Content = message.Text ?? string.Empty
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToOllamaMessage)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(ToOllamaMessage)} failed.");
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

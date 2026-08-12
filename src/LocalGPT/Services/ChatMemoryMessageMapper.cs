using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using System.Net;
using System.Collections;
using System.Reflection;

namespace LocalGPT.Services;

/// <summary>
/// Represents a chat memory message mapper.
/// </summary>
public sealed class ChatMemoryMessageMapper(
    CouncilTextService text,
    LocalGptCatalogService catalog,
    ILogger<ChatMemoryMessageMapper> logger) : IChatMemoryMessageMapper
{
    /// <summary>
    /// Builds title.
    /// </summary>
    public string BuildTitle(IReadOnlyList<BlazorChatMessage> messages)
    {
        try
        {
            var firstUserMessage = messages.FirstOrDefault(message => message.Role == ChatMessageRole.User)?.Content
                ?? messages.FirstOrDefault()?.Content
                ?? string.Empty;
            var title = catalog.WhitespacePattern
                .Replace(text.StripThinking(StripAttachmentPresentation(firstUserMessage), logger), " ")
                .Trim();

            if (string.IsNullOrWhiteSpace(title))
                return "New conversation";

            return title.Length <= 90 ? title : $"{title[..87].TrimEnd()}...";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not build a conversation title from {MessageCount} message(s).", messages.Count);
            return "New conversation";
        }
    }

    /// <summary>
    /// Ensures visible council prompt.
    /// </summary>
    public List<BlazorChatMessage> EnsureVisibleCouncilPrompt(
        ChatMemoryConversation conversation,
        List<BlazorChatMessage> messages)
    {
        try
        {
            if (messages.Count == 0 ||
                messages.Any(message => message.Role == ChatMessageRole.User && !string.IsNullOrWhiteSpace(message.Content)))
            {
                return messages;
            }

            if (!IsCouncilConversation(conversation, messages))
                return messages;

            var prompt = TryExtractPromptFromAssistantMessages(messages)
                ?? text.TryRecoverPromptFromTitle(conversation.Title, logger);
            if (string.IsNullOrWhiteSpace(prompt))
                return messages;

            messages.Insert(0, new BlazorChatMessage(
                ChatRole.User,
                prompt,
                /// <summary>
                /// Runs the list operation.
                /// </summary>
                new List<AIChatUploadFileInfo>()));
            return messages;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not ensure a visible council prompt for conversation {ConversationId}; message count {MessageCount}.", conversation.Id, messages.Count);
            return messages;
        }
    }

    /// <summary>
    /// Runs the to role name operation.
    /// </summary>
    public string ToRoleName(ChatMessageRole role) {
    try
    {
        return role switch
    {
        ChatMessageRole.Assistant => "assistant",
        ChatMessageRole.System => "system",
        ChatMessageRole.Error => "error",
        _ => "user"
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ChatMemoryMessageMapper)}.{nameof(ToRoleName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ChatMemoryMessageMapper)}.{nameof(ToRoleName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds durable message content and preserves the visible file names from native DXAiChat attachment metadata.
    /// </summary>
    public string BuildPersistedContent(BlazorChatMessage message)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(message);
            var content = message.Content ?? string.Empty;
            if (content.Contains("data-localgpt-restored-attachments", StringComparison.OrdinalIgnoreCase))
                return content;

            return text.BuildAttachmentPresentation(content, ExtractAttachmentNames(message));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not preserve DXAiChat attachment names while saving chat memory; message content was saved without attachment presentation.");
            return message.Content ?? string.Empty;
        }
    }

    /// <summary>
    /// Extracts native DXAiChat upload file names without binding LocalGPT to non-public DevExpress attachment members.
    /// </summary>
    private IReadOnlyList<string> ExtractAttachmentNames(BlazorChatMessage message)
    {
        try
        {
            var names = new List<string>();
            var properties = message.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                    continue;
                if (!property.Name.Contains("file", StringComparison.OrdinalIgnoreCase) &&
                    !property.Name.Contains("attachment", StringComparison.OrdinalIgnoreCase) &&
                    !(property.PropertyType.FullName?.Contains("AIChatUpload", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    continue;
                }

                if (property.GetValue(message) is not IEnumerable values)
                    continue;
                foreach (var value in values)
                {
                    if (value is null)
                        continue;
                    var valueType = value.GetType();
                    var nameProperty = valueType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .FirstOrDefault(candidate => candidate.PropertyType == typeof(string) &&
                            (candidate.Name.Equals("FileName", StringComparison.OrdinalIgnoreCase) ||
                             candidate.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
                             candidate.Name.Equals("DisplayName", StringComparison.OrdinalIgnoreCase)));
                    var name = nameProperty?.GetValue(value) as string;
                    if (!string.IsNullOrWhiteSpace(name))
                        names.Add(Path.GetFileName(name.Trim()));
                }
            }
            return names.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Native DXAiChat attachment metadata could not be inspected while persisting a message.");
            return [];
        }
    }

    /// <summary>
    /// Removes LocalGPT's restored-attachment presentation before deriving titles or other plain-text metadata.
    /// </summary>
    private string StripAttachmentPresentation(string content)
    {
        try
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;
            var start = content.IndexOf("<div class=\"localgpt-restored-attachments\"", StringComparison.OrdinalIgnoreCase);
            return start < 0 ? content : content[..start].TrimEnd();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not strip LocalGPT restored-attachment presentation from chat metadata text.");
            return content;
        }
    }

    /// <summary>
    /// Runs the to blazor chat message operation.
    /// </summary>
    public BlazorChatMessage ToBlazorChatMessage(ChatMemoryMessage message)
    {
        try
        {
            return new BlazorChatMessage(
                /// <summary>
                /// Runs the chat role operation.
                /// </summary>
                new ChatRole(message.Role),
                message.Content,
                /// <summary>
                /// Runs the list operation.
                /// </summary>
                new List<AIChatUploadFileInfo>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not convert a persisted chat message; message content was omitted from logs.");
            return new BlazorChatMessage(
                ChatRole.Assistant,
                message.Content ?? string.Empty,
                /// <summary>
                /// Runs the list operation.
                /// </summary>
                new List<AIChatUploadFileInfo>());
        }
    }

    /// <summary>
    /// Attempts to extract prompt from assistant messages.
    /// </summary>
    private string? TryExtractPromptFromAssistantMessages(IReadOnlyList<BlazorChatMessage> messages)
    {
    try
    {
            foreach (var message in messages)
            {
                var content = WebUtility.HtmlDecode(message.Content);
                var promptSection = text.TryFindCouncilPromptSection(content, logger);
                if (!string.IsNullOrWhiteSpace(promptSection))
                {
                    var fencedPrompt = catalog.CouncilPromptFencePattern.Match(promptSection);
                    if (fencedPrompt.Success)
                        return text.NormalizeRecoveredPrompt(fencedPrompt.Groups["prompt"].Value, logger);
                }

                var requestBlock = catalog.CouncilRequestBlockPattern.Match(content);
                if (requestBlock.Success)
                    return text.NormalizeRecoveredPrompt(requestBlock.Groups["prompt"].Value, logger);
            }

            return null;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ChatMemoryMessageMapper)}.{nameof(TryExtractPromptFromAssistantMessages)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ChatMemoryMessageMapper)}.{nameof(TryExtractPromptFromAssistantMessages)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether council conversation.
    /// </summary>
    private bool IsCouncilConversation(
        ChatMemoryConversation conversation,
        IReadOnlyList<BlazorChatMessage> messages)
    {
    try
    {
            return conversation.ProviderName.Contains("AI Council", StringComparison.OrdinalIgnoreCase) ||
                conversation.Title.Contains("AI Council request", StringComparison.OrdinalIgnoreCase) ||
                conversation.Title.Contains("Council members:", StringComparison.OrdinalIgnoreCase) ||
                messages.Any(message => message.Content.Contains("Council members:", StringComparison.OrdinalIgnoreCase));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ChatMemoryMessageMapper)}.{nameof(IsCouncilConversation)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ChatMemoryMessageMapper)}.{nameof(IsCouncilConversation)} failed.");
        throw;
    }
}
}

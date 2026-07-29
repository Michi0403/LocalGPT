using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using System.Net;

namespace LocalGPT.Services;

public sealed class ChatMemoryMessageMapper(
    CouncilTextService text,
    LocalGptCatalogService catalog,
    ILogger<ChatMemoryMessageMapper> logger) : IChatMemoryMessageMapper
{
    public string BuildTitle(IReadOnlyList<BlazorChatMessage> messages)
    {
        try
        {
            var firstUserMessage = messages.FirstOrDefault(message => message.Role == ChatMessageRole.User)?.Content
                ?? messages.FirstOrDefault()?.Content
                ?? string.Empty;
            var title = catalog.WhitespacePattern
                .Replace(text.StripThinking(firstUserMessage, logger), " ")
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
                new List<AIChatUploadFileInfo>()));
            return messages;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not ensure a visible council prompt for conversation {ConversationId}; message count {MessageCount}.", conversation.Id, messages.Count);
            return messages;
        }
    }

    public string ToRoleName(ChatMessageRole role) => role switch
    {
        ChatMessageRole.Assistant => "assistant",
        ChatMessageRole.System => "system",
        ChatMessageRole.Error => "error",
        _ => "user"
    };

    public BlazorChatMessage ToBlazorChatMessage(ChatMemoryMessage message)
    {
        try
        {
            return new BlazorChatMessage(
                new ChatRole(message.Role),
                message.Content,
                new List<AIChatUploadFileInfo>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not convert a persisted chat message; message content was omitted from logs.");
            return new BlazorChatMessage(
                ChatRole.Assistant,
                message.Content ?? string.Empty,
                new List<AIChatUploadFileInfo>());
        }
    }

    private string? TryExtractPromptFromAssistantMessages(IReadOnlyList<BlazorChatMessage> messages)
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

    private bool IsCouncilConversation(
        ChatMemoryConversation conversation,
        IReadOnlyList<BlazorChatMessage> messages)
    {
        return conversation.ProviderName.Contains("AI Council", StringComparison.OrdinalIgnoreCase) ||
            conversation.Title.Contains("AI Council request", StringComparison.OrdinalIgnoreCase) ||
            conversation.Title.Contains("Council members:", StringComparison.OrdinalIgnoreCase) ||
            messages.Any(message => message.Content.Contains("Council members:", StringComparison.OrdinalIgnoreCase));
    }
}

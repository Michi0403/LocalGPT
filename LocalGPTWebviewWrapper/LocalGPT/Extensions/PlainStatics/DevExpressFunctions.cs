using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using Microsoft.Extensions.AI;
using System.Net;

namespace LocalGPT.Extensions.PlainStatics
{
    public static class DevExpressFunctions
    {
        public static string BuildTitle(IReadOnlyList<BlazorChatMessage> messages, ILogger logger)
        {
            try
            {
                var firstUserMessage = messages.FirstOrDefault(message => message.Role == ChatMessageRole.User)?.Content
                ?? messages.First().Content;
                var title = GlobalVariableSlopCollectionToRemove.WhitespacePattern().Replace(CouncilChatStringFunctions.StripThinking(firstUserMessage,logger), " ").Trim();

                if (string.IsNullOrWhiteSpace(title))
                    return "New conversation";

                return title.Length <= 90 ? title : $"{title[..87].TrimEnd()}...";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildTitle messages {messages.ToString()}");
                return string.Empty;
            }
        }
        public static List<BlazorChatMessage> EnsureVisibleCouncilPrompt(
    ChatMemoryConversation conversation,
    List<BlazorChatMessage> messages, ILogger logger)
        {
            try
            {
                if (messages.Count == 0 ||
               messages.Any(message => message.Role == ChatMessageRole.User && !string.IsNullOrWhiteSpace(message.Content)))
                {
                    return messages;
                }

                if (!DevExpressFunctions.IsCouncilConversation(conversation, messages,logger))
                    return messages;

                var prompt = TryExtractPromptFromAssistantMessages(messages, logger)
                    ?? CouncilChatStringFunctions.TryRecoverPromptFromTitle(conversation.Title, logger);
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
                logger.LogError(ex, $"Error in EnsureVisibleCouncilPrompt conversation {conversation.ToString()} messages {messages.ToString()}");
                return new();
            }
        }
        public static string ToRoleName(ChatMessageRole role, ILogger logger)
        {
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToRoleName role {role.ToString()}");
                return string.Empty;
            }
        }
        public static string? TryExtractPromptFromAssistantMessages(IReadOnlyList<BlazorChatMessage> messages, ILogger logger)
        {
            try
            {
                foreach (var message in messages)
                {
                    var content = WebUtility.HtmlDecode(message.Content);
                    var promptSection = CouncilChatStringFunctions.TryFindCouncilPromptSection(content, logger);
                    if (!string.IsNullOrWhiteSpace(promptSection))
                    {
                        var fencedPrompt = GlobalVariableSlopCollectionToRemove.CouncilPromptFencePattern().Match(promptSection);
                        if (fencedPrompt.Success)
                            return CouncilChatStringFunctions.NormalizeRecoveredPrompt(fencedPrompt.Groups["prompt"].Value, logger);
                    }

                    var requestBlock = GlobalVariableSlopCollectionToRemove.CouncilRequestBlockPattern().Match(content);
                    if (requestBlock.Success)
                        return CouncilChatStringFunctions.NormalizeRecoveredPrompt(requestBlock.Groups["prompt"].Value, logger);
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TryExtractPromptFromAssistantMessages messages {messages.ToString()}");
                return null;
            }
        }
        public static BlazorChatMessage? ToBlazorChatMessage(ChatMemoryMessage message, ILogger logger)
        {
            try
            {
                return new BlazorChatMessage(new ChatRole(message.Role), message.Content, new List<AIChatUploadFileInfo>());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToBlazorChatMessage message {message}");
                return null;
            }
        }
        public static bool IsCouncilConversation(
    ChatMemoryConversation conversation,
    IReadOnlyList<BlazorChatMessage> messages, ILogger logger)
        {
            try
            {
                return conversation.ProviderName.Contains("AI Council", StringComparison.OrdinalIgnoreCase) ||
                conversation.Title.Contains("AI Council request", StringComparison.OrdinalIgnoreCase) ||
                conversation.Title.Contains("Council members:", StringComparison.OrdinalIgnoreCase) ||
                messages.Any(message => message.Content.Contains("Council members:", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsCouncilConversation conversation {conversation.ToString()} messages {messages.ToString()}");
                return new();
            }
        }
    }
}

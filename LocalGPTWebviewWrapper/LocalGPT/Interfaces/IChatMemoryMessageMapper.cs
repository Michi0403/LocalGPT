using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IChatMemoryMessageMapper
{
    string BuildTitle(IReadOnlyList<BlazorChatMessage> messages);

    List<BlazorChatMessage> EnsureVisibleCouncilPrompt(
        ChatMemoryConversation conversation,
        List<BlazorChatMessage> messages);

    string ToRoleName(ChatMessageRole role);

    BlazorChatMessage ToBlazorChatMessage(ChatMemoryMessage message);
}

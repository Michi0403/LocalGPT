using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the chat memory message mapper contract.
/// </summary>
public interface IChatMemoryMessageMapper
{
    /// <summary>
    /// Builds title.
    /// </summary>
    string BuildTitle(IReadOnlyList<BlazorChatMessage> messages);

    /// <summary>
    /// Ensures visible council prompt.
    /// </summary>
    List<BlazorChatMessage> EnsureVisibleCouncilPrompt(
        ChatMemoryConversation conversation,
        List<BlazorChatMessage> messages);

    /// <summary>
    /// Runs the to role name operation.
    /// </summary>
    string ToRoleName(ChatMessageRole role);

    /// <summary>
    /// Builds the durable message content, including a lightweight attachment presentation for native DXAiChat uploads.
    /// </summary>
    string BuildPersistedContent(BlazorChatMessage message);

    /// <summary>
    /// Runs the to blazor chat message operation.
    /// </summary>
    BlazorChatMessage ToBlazorChatMessage(ChatMemoryMessage message);
}

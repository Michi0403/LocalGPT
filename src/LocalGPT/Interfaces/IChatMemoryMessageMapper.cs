using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for chat memory message behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IChatMemoryMessageMapper
{
    /// <summary>
    /// Builds title while translating between the representations owned by the chat memory message mapping workflow.
    /// </summary>
    /// <param name="messages">Blazor chat message dependency used by the chat memory message workflow to provide the corresponding application capability.</param>
    /// <returns>The string produced by the operation.</returns>
    string BuildTitle(IReadOnlyList<BlazorChatMessage> messages);

    /// <summary>
    /// Ensures visible council prompt while translating between the representations owned by the chat memory message mapping workflow.
    /// </summary>
    /// <param name="conversation">Conversation value supplied to the chat memory message operation and used when producing its result.</param>
    /// <param name="messages">Messages value supplied to the chat memory message operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    List<BlazorChatMessage> EnsureVisibleCouncilPrompt(
        ChatMemoryConversation conversation,
        List<BlazorChatMessage> messages);

    /// <summary>
    /// Performs to role name while translating between the representations owned by the chat memory message mapping workflow.
    /// </summary>
    /// <param name="role">Role value supplied to the chat memory message operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string ToRoleName(ChatMessageRole role);

    /// <summary>
    /// Builds the durable message content, including a lightweight attachment presentation for native DXAiChat uploads.
    /// </summary>
    /// <param name="message">Message value supplied to the chat memory message operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string BuildPersistedContent(BlazorChatMessage message);

    /// <summary>
    /// Performs to blazor chat message while translating between the representations owned by the chat memory message mapping workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the chat memory message operation and used when producing its result.</param>
    /// <returns>The blazor chat message produced by the operation.</returns>
    BlazorChatMessage ToBlazorChatMessage(ChatMemoryMessage message);
}

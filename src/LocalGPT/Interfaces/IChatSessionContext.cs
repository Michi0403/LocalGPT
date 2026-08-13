using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for chat session context behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IChatSessionContext
{
    /// <summary>
    /// Gets the stable conversation identifier used to identify or correlate this chat session context instance with related application state.
    /// </summary>
    /// <value>The conversation identifier value exposed by <see cref="IChatSessionContext"/>.</value>
    Guid? ConversationId { get; }
    /// <summary>
    /// Gets the stable project identifier used to identify or correlate this chat session context instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="IChatSessionContext"/>.</value>
    Guid? ProjectId { get; }
    /// <summary>
    /// Gets the stable project version identifier used to identify or correlate this chat session context instance with related application state.
    /// </summary>
    /// <value>The project version identifier value exposed by <see cref="IChatSessionContext"/>.</value>
    Guid? ProjectVersionId { get; }
    /// <summary>
    /// Gets the application version value that forms part of the chat session context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The application version value exposed by <see cref="IChatSessionContext"/>.</value>
    string ApplicationVersion { get; }

    /// <summary>
    /// Performs snapshot for <see cref="IChatSessionContext"/>, keeping the operation consistent with the state and invariants of the surrounding chat session context workflow.
    /// </summary>
    /// <returns>The chat session context snapshot produced by the operation.</returns>
    ChatSessionContextSnapshot Snapshot();
    /// <summary>
    /// Sets conversation for <see cref="IChatSessionContext"/>, keeping the operation consistent with the state and invariants of the surrounding chat session context workflow.
    /// </summary>
    /// <param name="conversationId">Identifier of the conversation to use for this operation.</param>
    void SetConversation(Guid? conversationId);
    /// <summary>
    /// Sets project for <see cref="IChatSessionContext"/>, keeping the operation consistent with the state and invariants of the surrounding chat session context workflow.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="projectVersionId">Identifier of the project version to use for this operation.</param>
    void SetProject(Guid? projectId, Guid? projectVersionId);
    /// <summary>
    /// Performs restore for <see cref="IChatSessionContext"/>, keeping the operation consistent with the state and invariants of the surrounding chat session context workflow.
    /// </summary>
    /// <param name="snapshot">Snapshot value supplied to the chat session context operation and used when producing its result.</param>
    void Restore(ChatSessionContextSnapshot snapshot);
}

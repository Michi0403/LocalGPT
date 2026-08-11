using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the chat session context contract.
/// </summary>
public interface IChatSessionContext
{
    Guid? ConversationId { get; }
    Guid? ProjectId { get; }
    Guid? ProjectVersionId { get; }
    string ApplicationVersion { get; }

    /// <summary>
    /// Runs the snapshot operation.
    /// </summary>
    ChatSessionContextSnapshot Snapshot();
    /// <summary>
    /// Sets conversation.
    /// </summary>
    void SetConversation(Guid? conversationId);
    /// <summary>
    /// Sets project.
    /// </summary>
    void SetProject(Guid? projectId, Guid? projectVersionId);
    /// <summary>
    /// Runs the restore operation.
    /// </summary>
    void Restore(ChatSessionContextSnapshot snapshot);
}

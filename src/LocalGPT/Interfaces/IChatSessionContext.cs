using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IChatSessionContext
{
    Guid? ConversationId { get; }
    Guid? ProjectId { get; }
    Guid? ProjectVersionId { get; }
    string ApplicationVersion { get; }

    ChatSessionContextSnapshot Snapshot();
    void SetConversation(Guid? conversationId);
    void SetProject(Guid? projectId, Guid? projectVersionId);
    void Restore(ChatSessionContextSnapshot snapshot);
}

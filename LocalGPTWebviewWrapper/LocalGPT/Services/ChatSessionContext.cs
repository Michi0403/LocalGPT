using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class ChatSessionContext(ICustomVersion version) : IChatSessionContext
{
    public Guid? ConversationId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? ProjectVersionId { get; private set; }
    public string ApplicationVersion => version.Version;

    public ChatSessionContextSnapshot Snapshot() =>
        new(ConversationId, ProjectId, ProjectVersionId, ApplicationVersion);

    public void SetConversation(Guid? conversationId) => ConversationId = conversationId;

    public void SetProject(Guid? projectId, Guid? projectVersionId)
    {
        if (projectId is null)
            projectVersionId = null;
        ProjectId = projectId;
        ProjectVersionId = projectVersionId;
    }

    public void Restore(ChatSessionContextSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ConversationId = snapshot.ConversationId;
        ProjectId = snapshot.ProjectId;
        ProjectVersionId = snapshot.ProjectId is null ? null : snapshot.ProjectVersionId;
    }
}

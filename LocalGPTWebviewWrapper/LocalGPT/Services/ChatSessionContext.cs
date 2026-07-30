using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class ChatSessionContext(
    ICustomVersion version,
    ILogger<ChatSessionContext> logger) : IChatSessionContext
{
    public Guid? ConversationId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? ProjectVersionId { get; private set; }
    public string ApplicationVersion => version.Version;

    public ChatSessionContextSnapshot Snapshot()
    {
        logger.LogTrace("Captured the current LocalGPT chat session context.");
        return new(ConversationId, ProjectId, ProjectVersionId, ApplicationVersion);
    }

    public void SetConversation(Guid? conversationId)
    {
        ConversationId = conversationId;
        logger.LogTrace("Updated the active LocalGPT conversation context; identifier content was omitted.");
    }

    public void SetProject(Guid? projectId, Guid? projectVersionId)
    {
        if (projectId is null)
            projectVersionId = null;
        ProjectId = projectId;
        ProjectVersionId = projectVersionId;
        logger.LogTrace("Updated the active LocalGPT project context; identifier content was omitted.");
    }

    public void Restore(ChatSessionContextSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ConversationId = snapshot.ConversationId;
        ProjectId = snapshot.ProjectId;
        ProjectVersionId = snapshot.ProjectId is null ? null : snapshot.ProjectVersionId;
        logger.LogTrace("Restored a LocalGPT chat session context snapshot.");
    }
}

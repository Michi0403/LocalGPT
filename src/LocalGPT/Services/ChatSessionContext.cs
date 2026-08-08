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
    try
    {
            logger.LogTrace("Captured the current LocalGPT chat session context.");
            return new(ConversationId, ProjectId, ProjectVersionId, ApplicationVersion);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ChatSessionContext)}.{nameof(Snapshot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ChatSessionContext)}.{nameof(Snapshot)} failed.");
        throw;
    }
}

    public void SetConversation(Guid? conversationId)
    {
    try
    {
            ConversationId = conversationId;
            logger.LogTrace("Updated the active LocalGPT conversation context; identifier content was omitted.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ChatSessionContext)}.{nameof(SetConversation)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ChatSessionContext)}.{nameof(SetConversation)} failed.");
        throw;
    }
}

    public void SetProject(Guid? projectId, Guid? projectVersionId)
    {
    try
    {
            if (projectId is null)
                projectVersionId = null;
            ProjectId = projectId;
            ProjectVersionId = projectVersionId;
            logger.LogTrace("Updated the active LocalGPT project context; identifier content was omitted.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ChatSessionContext)}.{nameof(SetProject)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ChatSessionContext)}.{nameof(SetProject)} failed.");
        throw;
    }
}

    public void Restore(ChatSessionContextSnapshot snapshot)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(snapshot);
            ConversationId = snapshot.ConversationId;
            ProjectId = snapshot.ProjectId;
            ProjectVersionId = snapshot.ProjectId is null ? null : snapshot.ProjectVersionId;
            logger.LogTrace("Restored a LocalGPT chat session context snapshot.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ChatSessionContext)}.{nameof(Restore)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ChatSessionContext)}.{nameof(Restore)} failed.");
        throw;
    }
}
}

using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Represents a chat session context.
/// </summary>
public sealed class ChatSessionContext(
    ICustomVersion version,
    ILogger<ChatSessionContext> logger) : IChatSessionContext
{
    /// <summary>
    /// Gets or sets conversation identifier.
    /// </summary>
    public Guid? ConversationId { get; private set; }
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid? ProjectId { get; private set; }
    /// <summary>
    /// Gets or sets project version identifier.
    /// </summary>
    public Guid? ProjectVersionId { get; private set; }
    /// <summary>
    /// Gets or sets application version.
    /// </summary>
    public string ApplicationVersion => version.Version;

    /// <summary>
    /// Runs the snapshot operation.
    /// </summary>
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

    /// <summary>
    /// Sets conversation.
    /// </summary>
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

    /// <summary>
    /// Sets project.
    /// </summary>
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

    /// <summary>
    /// Runs the restore operation.
    /// </summary>
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

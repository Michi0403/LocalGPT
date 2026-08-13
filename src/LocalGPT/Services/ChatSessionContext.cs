using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Represents a chat session context application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="version">Custom version dependency used by the chat session context workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ChatSessionContext(
    ICustomVersion version,
    ILogger<ChatSessionContext> logger) : IChatSessionContext
{
    /// <summary>
    /// Gets or sets the stable conversation identifier used to identify or correlate this chat session context instance with related application state.
    /// </summary>
    /// <value>The conversation identifier value exposed by <see cref="ChatSessionContext"/>.</value>
    public Guid? ConversationId { get; private set; }
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this chat session context instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ChatSessionContext"/>.</value>
    public Guid? ProjectId { get; private set; }
    /// <summary>
    /// Gets or sets the stable project version identifier used to identify or correlate this chat session context instance with related application state.
    /// </summary>
    /// <value>The project version identifier value exposed by <see cref="ChatSessionContext"/>.</value>
    public Guid? ProjectVersionId { get; private set; }
    /// <summary>
    /// Gets the application version value that forms part of the chat session context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The application version value exposed by <see cref="ChatSessionContext"/>.</value>
    public string ApplicationVersion => version.Version;

    /// <summary>
    /// Performs snapshot for <see cref="ChatSessionContext"/>, keeping the operation consistent with the state and invariants of the surrounding chat session context workflow.
    /// </summary>
    /// <returns>The chat session context snapshot produced by the operation.</returns>
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
    /// Sets conversation for <see cref="ChatSessionContext"/>, keeping the operation consistent with the state and invariants of the surrounding chat session context workflow.
    /// </summary>
    /// <param name="conversationId">Identifier of the conversation to use for this operation.</param>
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
    /// Sets project for <see cref="ChatSessionContext"/>, keeping the operation consistent with the state and invariants of the surrounding chat session context workflow.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="projectVersionId">Identifier of the project version to use for this operation.</param>
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
    /// Performs restore for <see cref="ChatSessionContext"/>, keeping the operation consistent with the state and invariants of the surrounding chat session context workflow.
    /// </summary>
    /// <param name="snapshot">Snapshot value supplied to the chat session context operation and used when producing its result.</param>
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

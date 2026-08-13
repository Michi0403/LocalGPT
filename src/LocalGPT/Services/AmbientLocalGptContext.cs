using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Represents an ambient LocalGPT context application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="vocabulary">Local gpt vocabulary service dependency used by the ambient LocalGPT context workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class AmbientLocalGptContext(ILocalGptVocabularyService vocabulary,
    ILogger<AmbientLocalGptContext> logger)
    : IAmbientLocalGptContext, ILocalHumanInteractionContext, IHumanApprovalExecutionContext
{
    /// <summary>
    /// Stores the internal current holder state used by <see cref="AmbientLocalGptContext"/> while executing its surrounding workflow.
    /// </summary>
    private readonly AsyncLocal<AmbientLocalGptContextHolder?> CurrentHolder = new();
    /// <summary>
    /// Stores the internal fallback state used by <see cref="AmbientLocalGptContext"/> while executing its surrounding workflow.
    /// </summary>
    private readonly AmbientLocalGptContextSnapshot Fallback = new(
        "ambient-unset",
        vocabulary.Get().ActorSystem,
        "LocalGPT",
        Source: "AmbientFallback");

    /// <summary>
    /// Gets the current value that forms part of the ambient LocalGPT context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current value exposed by <see cref="AmbientLocalGptContext"/>.</value>
    public AmbientLocalGptContextSnapshot Current => CurrentHolder.Value?.Snapshot ?? Fallback;

    /// <summary>
    /// Performs push for <see cref="AmbientLocalGptContext"/>, keeping the operation consistent with the state and invariants of the surrounding ambient LocalGPT context workflow.
    /// </summary>
    /// <param name="snapshot">Snapshot value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <returns>The i disposable produced by the operation.</returns>
    private IDisposable Push(AmbientLocalGptContextSnapshot snapshot)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(snapshot);
            var prior = CurrentHolder.Value;
            CurrentHolder.Value = new AmbientLocalGptContextHolder(snapshot);
            var loggingScope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["LocalGptCorrelationId"] = snapshot.CorrelationId,
                ["LocalGptActorKind"] = snapshot.ActorKind,
                ["LocalGptAuthorityKind"] = snapshot.AuthorityKind,
                ["LocalGptCouncilRunId"] = snapshot.CouncilRunId,
                ["LocalGptCouncilRound"] = snapshot.CouncilRound,
                ["LocalGptCouncilPhase"] = snapshot.Phase,
                ["LocalGptApprovalRequestId"] = snapshot.ApprovalRequestId,
                ["LocalGptContextSource"] = snapshot.Source
            });
            return new AmbientLocalGptContextPopScope(holder => CurrentHolder.Value = holder, prior, loggingScope);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(AmbientLocalGptContext)}.{nameof(Push)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(AmbientLocalGptContext)}.{nameof(Push)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs push system for <see cref="AmbientLocalGptContext"/>, keeping the operation consistent with the state and invariants of the surrounding ambient LocalGPT context workflow.
    /// </summary>
    /// <param name="source">Source value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <returns>The i disposable produced by the operation.</returns>
    public IDisposable PushSystem(string source, string? correlationId = null) {
    try
    {
        return Push(new AmbientLocalGptContextSnapshot(
        NormalizeCorrelationId(correlationId),
        vocabulary.Get().ActorSystem,
        "LocalGPT",
        Source: Normalize(source, 160, "System")));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(AmbientLocalGptContext)}.{nameof(PushSystem)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(AmbientLocalGptContext)}.{nameof(PushSystem)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs push human interaction for <see cref="AmbientLocalGptContext"/>, keeping the operation consistent with the state and invariants of the surrounding ambient LocalGPT context workflow.
    /// </summary>
    /// <param name="humanProfileId">Identifier of the human profile to use for this operation.</param>
    /// <param name="displayName">Display name value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="councilRound">Council round value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <returns>The i disposable produced by the operation.</returns>
    public IDisposable PushHumanInteraction(
        Guid humanProfileId,
        string displayName,
        string source,
        string? correlationId = null,
        Guid? councilRunId = null,
        int councilRound = 0,
        string phase = "") {
    try
    {
        return Push(new AmbientLocalGptContextSnapshot(
            NormalizeCorrelationId(correlationId),
            vocabulary.Get().ActorHuman,
            Normalize(displayName, 120, "Human User"),
            vocabulary.Get().AuthorityHumanInteraction,
            humanProfileId,
            councilRunId,
            Math.Max(0, councilRound),
            Normalize(phase, 120),
            Source: Normalize(source, 160, "Local UI")));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(AmbientLocalGptContext)}.{nameof(PushHumanInteraction)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(AmbientLocalGptContext)}.{nameof(PushHumanInteraction)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs push human approval for <see cref="AmbientLocalGptContext"/>, keeping the operation consistent with the state and invariants of the surrounding ambient LocalGPT context workflow.
    /// </summary>
    /// <param name="humanProfileId">Identifier of the human profile to use for this operation.</param>
    /// <param name="displayName">Display name value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <param name="approvalRequestId">Identifier of the approval request to use for this operation.</param>
    /// <param name="source">Source value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="councilRound">Council round value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <returns>The i disposable produced by the operation.</returns>
    public IDisposable PushHumanApproval(
        Guid humanProfileId,
        string displayName,
        Guid approvalRequestId,
        string source,
        string correlationId,
        Guid? councilRunId = null,
        int councilRound = 0,
        string phase = "") {
    try
    {
        return Push(new AmbientLocalGptContextSnapshot(
            NormalizeCorrelationId(correlationId),
            vocabulary.Get().ActorHuman,
            Normalize(displayName, 120, "Human User"),
            vocabulary.Get().AuthorityHumanApproval,
            humanProfileId,
            councilRunId,
            Math.Max(0, councilRound),
            Normalize(phase, 120),
            approvalRequestId,
            Normalize(source, 160, "Human Collaboration Inbox")));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(AmbientLocalGptContext)}.{nameof(PushHumanApproval)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(AmbientLocalGptContext)}.{nameof(PushHumanApproval)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs push council for <see cref="AmbientLocalGptContext"/>, keeping the operation consistent with the state and invariants of the surrounding ambient LocalGPT context workflow.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="councilRound">Council round value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <returns>The i disposable produced by the operation.</returns>
    public IDisposable PushCouncil(
        Guid councilRunId,
        int councilRound,
        string phase,
        string? correlationId = null) {
    try
    {
        return Push(new AmbientLocalGptContextSnapshot(
            NormalizeCorrelationId(correlationId ?? councilRunId.ToString("N")),
            vocabulary.Get().ActorCouncil,
            "AI Council",
            CouncilRunId: councilRunId,
            CouncilRound: Math.Max(0, councilRound),
            Phase: Normalize(phase, 120),
            Source: "MultiModelCouncilService"));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(AmbientLocalGptContext)}.{nameof(PushCouncil)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(AmbientLocalGptContext)}.{nameof(PushCouncil)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes correlation identifier for <see cref="AmbientLocalGptContext"/>, keeping the operation consistent with the state and invariants of the surrounding ambient LocalGPT context workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeCorrelationId(string? value) {
    try
    {
        return Normalize(value, 180, Guid.NewGuid().ToString("N"));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(AmbientLocalGptContext)}.{nameof(NormalizeCorrelationId)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(AmbientLocalGptContext)}.{nameof(NormalizeCorrelationId)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs normalize for <see cref="AmbientLocalGptContext"/>, keeping the operation consistent with the state and invariants of the surrounding ambient LocalGPT context workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <param name="maxLength">Max length value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the ambient LocalGPT context operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Normalize(string? value, int maxLength, string fallback = "")
    {
    try
    {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return normalized[..Math.Min(normalized.Length, maxLength)];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(AmbientLocalGptContext)}.{nameof(Normalize)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(AmbientLocalGptContext)}.{nameof(Normalize)} failed.");
        throw;
    }
}


}

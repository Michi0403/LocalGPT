using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates human collaboration behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="vocabulary">Local gpt vocabulary service dependency used by the human collaboration workflow to provide the corresponding application capability.</param>
/// <param name="dbContextFactory">Local gpt memory database context dependency used by the human collaboration workflow to provide the corresponding application capability.</param>
/// <param name="ambientContext">Ambient local gpt context dependency used by the human collaboration workflow to provide the corresponding application capability.</param>
/// <param name="componentActivity">Component activity service dependency used by the human collaboration workflow to provide the corresponding application capability.</param>
/// <param name="runtimePolicy">Local gpt runtime policy data service dependency used by the human collaboration workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class HumanCollaborationService(ILocalGptVocabularyService vocabulary,
    
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IAmbientLocalGptContext ambientContext,
    IComponentActivityService componentActivity,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<HumanCollaborationService> logger) : IHumanCollaborationService
{
    /// <summary>
    /// Defines the max text length constant used by <see cref="HumanCollaborationService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaxTextLength = 1_000_000;
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to database gate state owned by <see cref="HumanCollaborationService"/>.
    /// </summary>
    private readonly SemaphoreSlim databaseGate = new(1, 1);
    /// <summary>
    /// Stores the in-memory active runs collection maintained internally by <see cref="HumanCollaborationService"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, HumanCouncilRunSnapshot> activeRuns = new();
    /// <summary>Tracks the single active-model consumer that claimed each direct user message for immediate interruption.</summary>
    private readonly ConcurrentDictionary<Guid, string> directUserMessageClaims = new();
    /// <summary>Tracks the owning run so process-local direct-message claims can be cleared deterministically.</summary>
    private readonly ConcurrentDictionary<Guid, Guid> directUserMessageRuns = new();
    /// <summary>Tracks the participant currently owning ordered live presentation for each Council run so a direct heartbeat interrupts the model the user is actually watching instead of an arbitrary parallel subscriber.</summary>
    private readonly ConcurrentDictionary<Guid, string> preferredDirectUserMessageConsumers = new();
    /// <summary>
    /// Stores the internal approval session identifier state used by <see cref="HumanCollaborationService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Guid approvalSessionId = Guid.NewGuid();

    /// <summary>
    /// Occurs when changed changes or completes in <see cref="HumanCollaborationService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action? Changed;
    /// <summary>
    /// Occurs when direct user message queued changes or completes in <see cref="HumanCollaborationService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action<HumanCouncilContribution>? DirectUserMessageQueued;

    /// <summary>
    /// Retrieves snapshot as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="includeResolved">Value indicating whether include resolved should apply to this operation.</param>
    /// <param name="take">Take value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human collaboration snapshot produced by the operation.</returns>
    public async Task<HumanCollaborationSnapshot> GetSnapshotAsync(
        bool includeResolved = true,
        int take = 80,
        CancellationToken cancellationToken = default)
    {
    try
    {
            using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var query = db.HumanCollaborationRequests.AsNoTracking();
            if (!includeResolved)
                query = query.Where(item => item.Status == vocabulary.Get().HumanStatusPending);

            var requests = await query
                .OrderBy(item => item.Status == vocabulary.Get().HumanStatusPending ? 0 : 1)
                .ThenByDescending(item => item.UpdatedAtUtc)
                .Take(Math.Clamp(take, 1, 200))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var contributions = await db.HumanCouncilContributions.AsNoTracking()
                .OrderByDescending(item => item.SubmittedAtUtc)
                .Take(30)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var profile = await db.HumanCouncilParticipantProfiles.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == runtimePolicy.GetGuid(LocalGptRuntimeValue.LocalHumanProfileId), cancellationToken)
                .ConfigureAwait(false)
                ?? new HumanCouncilParticipantProfile { Id = runtimePolicy.GetGuid(LocalGptRuntimeValue.LocalHumanProfileId) };

            return new HumanCollaborationSnapshot(
                profile,
                requests,
                activeRuns.Values.OrderBy(item => item.StartedAtUtc).ToList(),
                contributions);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetSnapshotAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetSnapshotAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs authorize or enqueue as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="directHumanConfirmation">Value indicating whether direct human confirmation should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human approval gate result produced by the operation.</returns>
    public async Task<HumanApprovalGateResult> AuthorizeOrEnqueueAsync(
        HumanApprovalRequestSpec request,
        bool directHumanConfirmation = false,
        CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.CorrelationId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.OperationKey);
            var requestKind = NormalizeRequestKind(request.RequestKind);

            var ambient = ambientContext.Current;
            if (directHumanConfirmation)
            {
                if (!ambient.IsTrustedHumanInteraction(vocabulary.Get()))
                {
                    logger.LogWarning(
                        "Rejected direct confirmation claim for operation {OperationKey}; no trusted local human UI context was active.",
                        Normalize(request.OperationKey, 180));
                }
                else
                {
                    logger.LogInformation(
                        "Trusted local human interaction directly confirmed operation {OperationKey}; payload details were omitted.",
                        Normalize(request.OperationKey, 180));
                    componentActivity.RecordInformation(
                        "HumanCollaboration",
                        "DirectConfirmation",
                        "The trusted local human UI directly confirmed a consequential operation.");
                    return new HumanApprovalGateResult(
                        true,
                        false,
                        ambient.ApprovalRequestId,
                        vocabulary.Get().HumanStatusApproved,
                        "Trusted local human confirmation accepted.",
                        CorrelationId: request.CorrelationId);
                }
            }

            await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var normalizedOperationKey = Normalize(request.OperationKey, 180);
                var normalizedCorrelationId = Normalize(request.CorrelationId, 180);
                var normalizedFingerprint = Normalize(request.ParameterFingerprint, 128);
                var candidateQuery = db.HumanCollaborationRequests
                    .Where(item => item.OperationKey == normalizedOperationKey && item.RequestKind == requestKind);
                candidateQuery = string.IsNullOrWhiteSpace(normalizedFingerprint)
                    ? candidateQuery.Where(item => item.CorrelationId == normalizedCorrelationId)
                    : candidateQuery.Where(item => item.ParameterFingerprint == normalizedFingerprint);
                var candidates = await candidateQuery
                    .OrderByDescending(item => item.UpdatedAtUtc)
                    .Take(24)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                var existing = candidates.FirstOrDefault(IsReusableDecision);

                if (existing is not null)
                {
                    if (existing.Status == vocabulary.Get().HumanStatusPending)
                    {
                        return new HumanApprovalGateResult(
                            false,
                            false,
                            existing.Id,
                            existing.Status,
                            "The exact operation is already waiting in the Human Collaboration Inbox.",
                            CorrelationId: existing.CorrelationId);
                    }

                    var resolvedStatus = existing.Status;
                    var declined = resolvedStatus == vocabulary.Get().HumanStatusDeclined;
                    if (existing.ConsumeApproval)
                    {
                        existing.Status = vocabulary.Get().HumanStatusConsumed;
                        existing.ConsumedAtUtc = DateTime.UtcNow;
                        existing.UpdatedAtUtc = DateTime.UtcNow;
                        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        NotifyChanged();
                        logger.LogInformation("Consumed saved human decision {RequestId} for operation {OperationKey}.", existing.Id, existing.OperationKey);
                    }
                    else
                    {
                        logger.LogInformation(
                            "Reused saved human decision {RequestId} for operation {OperationKey} under scope {ReuseScope}.",
                            existing.Id,
                            existing.OperationKey,
                            existing.ApprovalReuseScope);
                    }

                    return new HumanApprovalGateResult(
                        !declined,
                        declined,
                        existing.Id,
                        existing.Status,
                        declined
                            ? existing.ConsumeApproval
                                ? "The saved human decline was consumed for this exact operation."
                                : "The saved human decision declines this exact operation. Edit the decision in Approvals & team to change it."
                            : resolvedStatus == vocabulary.Get().HumanStatusAnswered
                                ? "The saved human-provided interaction value was reused for this exact operation."
                                : existing.ConsumeApproval
                                    ? "The saved human approval was consumed for this exact operation."
                                    : "The saved human approval was reused for this exact operation.",
                        existing.DecisionReason,
                        existing.CorrelationId,
                        existing.UserResponse);
                }

                if (requestKind != vocabulary.Get().HumanRequestApproval)
                {
                    var pendingCoordinationRequests = await db.HumanCollaborationRequests
                        .CountAsync(item => item.Status == vocabulary.Get().HumanStatusPending &&
                            item.RequestKind != vocabulary.Get().HumanRequestApproval &&
                            item.CouncilRunId == request.CouncilRunId, cancellationToken)
                        .ConfigureAwait(false);
                    if (pendingCoordinationRequests >= 12)
                    {
                        logger.LogWarning(
                            "Rejected additional human collaboration request for council run {CouncilRunId}; the pending coordination limit was reached.",
                            request.CouncilRunId);
                        return new HumanApprovalGateResult(
                            false,
                            false,
                            null,
                            "RequestLimitReached",
                            "The Human Collaboration Inbox already contains the maximum pending coordination questions for this run.",
                            CorrelationId: request.CorrelationId);
                    }
                }

                var questionScope = NormalizeQuestionScope(request.QuestionScope);
                var gateMode = NormalizeGateMode(request.GateMode, request.RequiredBeforeCompletion);
                var entity = new HumanCollaborationRequest
                {
                    CouncilRunId = request.CouncilRunId,
                    CorrelationId = normalizedCorrelationId,
                    OperationKey = normalizedOperationKey,
                    ParameterFingerprint = normalizedFingerprint,
                    RequestKind = requestKind,
                    Title = Normalize(request.Title, 240, "Human decision requested"),
                    Description = Normalize(request.Description, 2000),
                    RiskLevel = Normalize(request.RiskLevel, 40, "Medium"),
                    Source = Normalize(request.Source, 160),
                    RequestedBy = Normalize(request.RequestedBy, 160),
                    RequestedRole = Normalize(request.RequestedRole, 160),
                    QuestionScope = questionScope,
                    GateMode = gateMode,
                    TargetMembersText = NormalizeMultiline(request.TargetMembersText, 1600),
                    RequestedCouncilRound = Math.Max(0, request.RequestedCouncilRound),
                    RequestedCouncilPhase = Normalize(request.RequestedCouncilPhase, 120),
                    SuggestedResponsesText = NormalizeMultiline(request.SuggestedResponsesText, 1600),
                    ResponsePrompt = Normalize(request.ResponsePrompt, 500),
                    PrefillText = NormalizeMultiline(request.PrefillText, MaxTextLength),
                    EarliestCouncilRound = Math.Max(0, request.EarliestCouncilRound),
                    RequiredBeforeCompletion = gateMode == "Completion",
                    IsSensitive = request.IsSensitive,
                    AllowFreeText = request.AllowFreeText,
                    ApprovalReuseScope = GetDefaultReuseScope(requestKind, request.RiskLevel),
                    ConsumeApproval = GetDefaultConsumeApproval(requestKind, request.RiskLevel),
                    Status = vocabulary.Get().HumanStatusPending,
                    RequestedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                db.HumanCollaborationRequests.Add(entity);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                NotifyChanged();
                componentActivity.RecordWarning(
                    "HumanCollaboration",
                    "RequestQueued",
                    "A persistent human collaboration request is waiting in the main application inbox.");
                logger.LogInformation(
                    "Queued human collaboration request {RequestId} for operation {OperationKey}, scope {QuestionScope}, gate {GateMode}, risk {RiskLevel}, source {Source}; payload content was omitted.",
                    entity.Id,
                    entity.OperationKey,
                    entity.QuestionScope,
                    entity.GateMode,
                    entity.RiskLevel,
                    entity.Source);
                return new HumanApprovalGateResult(
                    false,
                    false,
                    entity.Id,
                    entity.Status,
                    "The operation was queued in the Human Collaboration Inbox.",
                    CorrelationId: entity.CorrelationId);
            }
            finally
            {
                databaseGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(AuthorizeOrEnqueueAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(AuthorizeOrEnqueueAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves request as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="requestId">Identifier of the request to use for this operation.</param>
    /// <param name="submission">Submission value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human collaboration request produced by the operation.</returns>
    public async Task<HumanCollaborationRequest?> ResolveRequestAsync(
        Guid requestId,
        HumanDecisionSubmission submission,
        CancellationToken cancellationToken = default)
    {
    try
    {
            EnsureTrustedHumanInteraction("save a collaboration decision");
            await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var request = await db.HumanCollaborationRequests
                    .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken)
                    .ConfigureAwait(false);
                if (request is null)
                    return null;

                var isApproval = request.RequestKind == vocabulary.Get().HumanRequestApproval;
                if (isApproval && submission.Approved is null)
                    throw new InvalidOperationException("Approval requests require an explicit approve or decline decision.");
                if (isApproval && !Enum.IsDefined(typeof(HumanApprovalReuseScope), submission.ReuseScope))
                    throw new InvalidOperationException("The selected approval reuse scope is invalid.");
                if (isApproval && submission.Approved == false && string.IsNullOrWhiteSpace(submission.Reason))
                    throw new InvalidOperationException("A decline reason is required so the LocalGPT team can adapt its next step.");
                if (!isApproval && string.IsNullOrWhiteSpace(submission.Response))
                    throw new InvalidOperationException("Feedback and guidance requests require a response.");

                var previousStatus = request.Status;
                request.UserResponse = NormalizeMultiline(submission.Response, MaxTextLength);
                request.DecisionReason = NormalizeMultiline(submission.Reason, 2000);
                request.DecisionBy = Normalize(ambientContext.Current.ActorDisplayName, 120, "Human User");
                request.DecisionByProfileId = ambientContext.Current.HumanProfileId ?? runtimePolicy.GetGuid(LocalGptRuntimeValue.LocalHumanProfileId);
                request.DecidedAtUtc = DateTime.UtcNow;
                request.UpdatedAtUtc = DateTime.UtcNow;
                request.ConsumedAtUtc = null;
                request.DecisionVersion = Math.Max(1, request.DecisionVersion + 1);
                request.ApprovalReuseScope = isApproval
                    ? submission.ReuseScope
                    : HumanApprovalReuseScope.ExactRequestOnce;
                request.ConsumeApproval = !isApproval || submission.ConsumeApproval;
                request.ApprovalSessionId = request.ApprovalReuseScope == HumanApprovalReuseScope.CurrentApplicationSession
                    ? approvalSessionId
                    : null;
                request.Status = isApproval
                    ? submission.Approved == true
                        ? vocabulary.Get().HumanStatusApproved
                        : vocabulary.Get().HumanStatusDeclined
                    : vocabulary.Get().HumanStatusAnswered;

                if (isApproval && submission.Approved == false && previousStatus != vocabulary.Get().HumanStatusDeclined)
                {
                    db.HumanCollaborationRequests.Add(new HumanCollaborationRequest
                    {
                        CouncilRunId = request.CouncilRunId,
                        CorrelationId = $"decline-feedback:{request.Id:N}:{request.DecisionVersion}",
                        OperationKey = "human.decline.feedback",
                        ParameterFingerprint = request.ParameterFingerprint,
                        RequestKind = vocabulary.Get().HumanRequestGuidance,
                        Title = $"Declined action feedback: {request.Title}",
                        Description = "The local human declined a guarded operation and supplied a reason so the team can adapt rather than retry the same action unchanged.",
                        RiskLevel = "Low",
                        Status = vocabulary.Get().HumanStatusAnswered,
                        Source = "Human Collaboration Inbox",
                        RequestedBy = request.DecisionBy,
                        RequestedRole = request.RequestedRole,
                        UserResponse = request.DecisionReason,
                        DecisionReason = "Security-decline feedback; context only, never approval.",
                        DecisionBy = request.DecisionBy,
                        DecisionByProfileId = request.DecisionByProfileId,
                        RequestedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow,
                        DecidedAtUtc = DateTime.UtcNow,
                        EarliestCouncilRound = Math.Max(0, request.EarliestCouncilRound),
                        RequiredBeforeCompletion = false,
                        IsSensitive = false,
                        AllowFreeText = false,
                        DecisionVersion = 1
                    });
                }

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                NotifyChanged();
                componentActivity.RecordInformation(
                    "HumanCollaboration",
                    previousStatus == vocabulary.Get().HumanStatusPending ? "RequestResolved" : "DecisionUpdated",
                    $"The local human saved a {request.RequestKind.ToLowerInvariant()} decision with status {request.Status} and version {request.DecisionVersion}.");
                logger.LogInformation(
                    "Human saved collaboration request {RequestId} decision version {DecisionVersion} with status {Status} and reuse scope {ReuseScope}; response content was omitted from logs.",
                    request.Id,
                    request.DecisionVersion,
                    request.Status,
                    request.ApprovalReuseScope);
                return request;
            }
            finally
            {
                databaseGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(ResolveRequestAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(ResolveRequestAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves profile as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human council participant profile produced by the operation.</returns>
    public async Task<HumanCouncilParticipantProfile> GetProfileAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await db.HumanCouncilParticipantProfiles.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == runtimePolicy.GetGuid(LocalGptRuntimeValue.LocalHumanProfileId), cancellationToken)
                .ConfigureAwait(false)
                ?? new HumanCouncilParticipantProfile { Id = runtimePolicy.GetGuid(LocalGptRuntimeValue.LocalHumanProfileId) };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetProfileAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetProfileAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Persists profile as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human council participant profile produced by the operation.</returns>
    public async Task<HumanCouncilParticipantProfile> SaveProfileAsync(
        HumanCouncilParticipantProfile profile,
        CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(profile);
            EnsureTrustedHumanInteraction("update the human council profile");
            await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var existing = await db.HumanCouncilParticipantProfiles
                    .SingleOrDefaultAsync(item => item.Id == runtimePolicy.GetGuid(LocalGptRuntimeValue.LocalHumanProfileId), cancellationToken)
                    .ConfigureAwait(false);
                if (existing is null)
                {
                    existing = new HumanCouncilParticipantProfile { Id = runtimePolicy.GetGuid(LocalGptRuntimeValue.LocalHumanProfileId) };
                    db.HumanCouncilParticipantProfiles.Add(existing);
                }

                existing.Id = runtimePolicy.GetGuid(LocalGptRuntimeValue.LocalHumanProfileId);
                existing.DisplayName = Normalize(profile.DisplayName, 120, "Human User");
                existing.RoleName = Normalize(profile.RoleName, 180, "Human collaborator");
                existing.Expertise = NormalizeMultiline(profile.Expertise, 2000);
                existing.WorkingStyle = NormalizeMultiline(profile.WorkingStyle, 1200);
                existing.IsEnabled = profile.IsEnabled;
                existing.ProfileVersion = Math.Max(1, existing.ProfileVersion + 1);
                existing.UpdatedAtUtc = DateTime.UtcNow;
                existing.UpdatedBy = Normalize(ambientContext.Current.ActorDisplayName, 120, "Human User");
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                SynchronizeActiveHumanMembership(existing);
                NotifyChanged();
                componentActivity.RecordInformation(
                    "HumanCollaboration",
                    "ProfileSaved",
                    "The local human council participant profile was updated through the main application UI.");
                logger.LogInformation("Human council participant profile version {ProfileVersion} was saved by the local human UI.", existing.ProfileVersion);
                return existing;
            }
            finally
            {
                databaseGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(SaveProfileAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(SaveProfileAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs queue contribution as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="content">Content value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human council contribution produced by the operation.</returns>
    public Task<HumanCouncilContribution> QueueContributionAsync(
        Guid councilRunId,
        string content,
        CancellationToken cancellationToken = default) {
    try
    {
        return QueueContributionCoreAsync(
            councilRunId,
            content,
            requireEnabledProfile: true,
            directUserMessage: false,
            cancellationToken: cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(QueueContributionAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(QueueContributionAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs queue user message as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="content">Content value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human council contribution produced by the operation.</returns>
    public Task<HumanCouncilContribution> QueueUserMessageAsync(
        Guid councilRunId,
        string content,
        CancellationToken cancellationToken = default) {
    try
    {
        return QueueContributionCoreAsync(
            councilRunId,
            content,
            requireEnabledProfile: false,
            directUserMessage: true,
            cancellationToken: cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(QueueUserMessageAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(QueueUserMessageAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Sets preferred direct user message consumer as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="consumerKey">Consumer key value supplied to the human collaboration operation and used when producing its result.</param>
    public void SetPreferredDirectUserMessageConsumer(Guid councilRunId, string consumerKey)
    {
        try
        {
            if (councilRunId == Guid.Empty || string.IsNullOrWhiteSpace(consumerKey))
                return;
            preferredDirectUserMessageConsumers[councilRunId] = consumerKey.Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Setting preferred direct-message consumer failed for Council run {CouncilRunId}.", councilRunId);
        }
    }

    /// <summary>
    /// Performs clear preferred direct user message consumer as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="consumerKey">Consumer key value supplied to the human collaboration operation and used when producing its result.</param>
    public void ClearPreferredDirectUserMessageConsumer(Guid councilRunId, string consumerKey)
    {
        try
        {
            if (councilRunId == Guid.Empty || string.IsNullOrWhiteSpace(consumerKey))
                return;
            if (preferredDirectUserMessageConsumers.TryGetValue(councilRunId, out var current) &&
                string.Equals(current, consumerKey.Trim(), StringComparison.Ordinal))
            {
                preferredDirectUserMessageConsumers.TryRemove(councilRunId, out _);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Clearing preferred direct-message consumer failed for Council run {CouncilRunId}.", councilRunId);
        }
    }

    /// <summary>
    /// Attempts to claim direct user message as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    /// <param name="contributionId">Identifier of the contribution to use for this operation.</param>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="consumerKey">Consumer key value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool TryClaimDirectUserMessage(Guid contributionId, Guid councilRunId, string consumerKey)
    {
        try
        {
            if (contributionId == Guid.Empty || councilRunId == Guid.Empty || string.IsNullOrWhiteSpace(consumerKey))
                return false;
            var normalizedConsumer = consumerKey.Trim();
            if (preferredDirectUserMessageConsumers.TryGetValue(councilRunId, out var preferredConsumer) &&
                !string.Equals(preferredConsumer, normalizedConsumer, StringComparison.Ordinal))
            {
                return false;
            }
            directUserMessageRuns[contributionId] = councilRunId;
            return directUserMessageClaims.TryAdd(contributionId, normalizedConsumer);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Claiming direct Council user message {ContributionId} failed.", contributionId);
            return false;
        }
    }

    /// <summary>
    /// Performs queue contribution core as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="content">Content value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="requireEnabledProfile">Value indicating whether require enabled profile should apply to this operation.</param>
    /// <param name="directUserMessage">Value indicating whether direct user message should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human council contribution produced by the operation.</returns>
    private async Task<HumanCouncilContribution> QueueContributionCoreAsync(
        Guid councilRunId,
        string content,
        bool requireEnabledProfile,
        bool directUserMessage,
        CancellationToken cancellationToken)
    {
    try
    {
            EnsureTrustedHumanInteraction(directUserMessage
                ? "add a direct user message to a running council"
                : "submit a human council contribution");
            var normalized = NormalizeMultiline(content, MaxTextLength);
            if (string.IsNullOrWhiteSpace(normalized))
                throw new InvalidOperationException("A Council user message cannot be empty.");

            var profile = await GetProfileAsync(cancellationToken).ConfigureAwait(false);
            if (requireEnabledProfile && !profile.IsEnabled)
                throw new InvalidOperationException("Enable the Human Council Participant profile before joining a run.");
            if (!activeRuns.TryGetValue(councilRunId, out var active))
                throw new InvalidOperationException("The selected council run is no longer active.");

            var contribution = new HumanCouncilContribution
            {
                CouncilRunId = councilRunId,
                HumanDisplayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? "Human User" : profile.DisplayName,
                HumanRole = directUserMessage
                    ? "Direct user message"
                    : profile.RoleName,
                Content = normalized,
                EarliestCouncilRound = directUserMessage
                    ? Math.Max(0, active.CurrentRound)
                    : Math.Max(1, active.CurrentRound + 1),
                Status = vocabulary.Get().ContributionQueued,
                EvaluationVerdict = vocabulary.Get().VerdictPending,
                SubmittedAtUtc = DateTime.UtcNow
            };

            using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            db.HumanCouncilContributions.Add(contribution);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            if (directUserMessage)
            {
                directUserMessageRuns[contribution.Id] = councilRunId;
                NotifyDirectUserMessageQueued(contribution);
            }
            NotifyChanged();
            componentActivity.RecordInformation(
                "HumanCollaboration",
                directUserMessage ? "CouncilUserMessageQueued" : "ContributionQueued",
                directUserMessage
                    ? "A direct user message was queued for immediate active-model interruption/resume and subsequent Council heartbeats."
                    : "A human contribution was queued for the next council heartbeat.");
            logger.LogInformation(
                directUserMessage
                    ? "Queued direct user message {ContributionId} for council run {CouncilRunId} at earliest round {EarliestRound}; content was omitted from logs."
                    : "Queued human contribution {ContributionId} for council run {CouncilRunId} at earliest round {EarliestRound}; content was omitted from logs.",
                contribution.Id,
                contribution.CouncilRunId,
                contribution.EarliestCouncilRound);
            return contribution;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(QueueContributionCoreAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(QueueContributionCoreAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads queued contributions as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="currentRound">Current round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<HumanCouncilContribution>> ReadQueuedContributionsAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                return await db.HumanCouncilContributions.AsNoTracking()
                    .Where(item => item.CouncilRunId == councilRunId &&
                        item.Status == vocabulary.Get().ContributionQueued &&
                        item.EarliestCouncilRound <= currentRound)
                    .OrderBy(item => item.SubmittedAtUtc)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                databaseGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(ReadQueuedContributionsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(ReadQueuedContributionsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs drain contributions as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="currentRound">Current round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<HumanCouncilContribution>> DrainContributionsAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var contributions = await db.HumanCouncilContributions
                    .Where(item => item.CouncilRunId == councilRunId &&
                        item.Status == vocabulary.Get().ContributionQueued &&
                        item.EarliestCouncilRound <= currentRound)
                    .OrderBy(item => item.SubmittedAtUtc)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                foreach (var contribution in contributions)
                {
                    contribution.Status = vocabulary.Get().ContributionInjected;
                    contribution.InjectedAtUtc = DateTime.UtcNow;
                }
                if (contributions.Count > 0)
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                foreach (var contribution in contributions)
                {
                    directUserMessageClaims.TryRemove(contribution.Id, out _);
                    directUserMessageRuns.TryRemove(contribution.Id, out _);
                }
                NotifyChanged();
                return contributions;
            }
            finally
            {
                databaseGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(DrainContributionsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(DrainContributionsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs mark contributions evaluated as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="afterRound">After round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="evaluation">Evaluation value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task MarkContributionsEvaluatedAsync(
        Guid councilRunId,
        int afterRound,
        string evaluation,
        CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var contributions = await db.HumanCouncilContributions
                    .Where(item => item.CouncilRunId == councilRunId && item.Status == vocabulary.Get().ContributionInjected)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (contributions.Count == 0)
                    return;

                var normalizedEvaluation = NormalizeMultiline(evaluation, MaxTextLength);
                var evaluationVerdict = DetermineEvaluationVerdict(normalizedEvaluation);
                foreach (var contribution in contributions)
                {
                    contribution.Status = vocabulary.Get().ContributionEvaluated;
                    contribution.EvaluatedAtUtc = DateTime.UtcNow;
                    contribution.EvaluatedAfterRound = Math.Max(0, afterRound);
                    contribution.Evaluation = normalizedEvaluation;
                    contribution.EvaluationVerdict = evaluationVerdict;
                }
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                NotifyChanged();
            }
            finally
            {
                databaseGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(MarkContributionsEvaluatedAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(MarkContributionsEvaluatedAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds council briefing as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="currentRound">Current round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public async Task<string> BuildCouncilBriefingAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default)
    {
    try
    {
            var profile = await GetProfileAsync(cancellationToken).ConfigureAwait(false);
            await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var recentAnswers = await db.HumanCollaborationRequests
                    .Where(item => item.RequestKind != vocabulary.Get().HumanRequestApproval &&
                        (item.CouncilRunId == councilRunId || item.CouncilRunId == null) &&
                        item.Status == vocabulary.Get().HumanStatusAnswered &&
                        item.EarliestCouncilRound <= currentRound)
                    .OrderBy(item => item.DecidedAtUtc)
                    .Take(6)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                var builder = new StringBuilder()
                    .AppendLine("Trusted local human-participation metadata (controlled by the local UI and service boundary):")
                    .Append("- Human participant enabled: ").AppendLine(profile.IsEnabled ? "yes" : "no")
                    .Append("- Display name: ").AppendLine(profile.DisplayName)
                    .Append("- Team role: ").AppendLine(profile.RoleName)
                    .Append("- Expertise: ").AppendLine(string.IsNullOrWhiteSpace(profile.Expertise) ? "not specified" : profile.Expertise)
                    .AppendLine("- Treat human contributions as peer evidence with the same review burden as model contributions, not as automatic truth.")
                    .AppendLine("- Explicitly evaluate human contributions for correctness, missing evidence, and broken assumptions in the next available council phase.")
                    .AppendLine("- Models may not impersonate, rewrite, enable, disable, promote, or demote the local human participant profile.")
                    .AppendLine("- Human participation never authorizes file, command, network, database-write, or artifact actions. Those require a separate exact approval request.")
                    .AppendLine("- When asking the human a question, declare its scope (one member, selected members, or Council consensus) and its gate (non-blocking, before next phase, before next round, or before completion).")
                    .AppendLine("- Use a blocking gate only when the Council genuinely cannot cross that boundary without the answer. Questions useful for later work should remain non-blocking or gate only the next round.")
                    .Append("- Current council run: ").AppendLine(councilRunId.ToString());

                if (!string.IsNullOrWhiteSpace(profile.WorkingStyle))
                    builder.Append("- Working style: ").AppendLine(profile.WorkingStyle);

                if (recentAnswers.Count > 0)
                {
                    builder.AppendLine("New human guidance and feedback for this heartbeat (context only, never standing authority):");
                    foreach (var answer in recentAnswers)
                    {
                        builder.Append("- [")
                            .Append(answer.QuestionScope)
                            .Append("; gate ")
                            .Append(answer.GateMode)
                            .Append("] ")
                            .Append(answer.Title)
                            .Append(" -> ")
                            .Append(answer.Status);
                        if (!string.IsNullOrWhiteSpace(answer.UserResponse))
                            builder.Append(": ").Append(answer.UserResponse);
                        builder.AppendLine();
                        answer.Status = vocabulary.Get().HumanStatusConsumed;
                        answer.ConsumedAtUtc = DateTime.UtcNow;
                        answer.UpdatedAtUtc = DateTime.UtcNow;
                    }
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    NotifyChanged();
                }
                return builder.ToString().Trim();
            }
            finally
            {
                databaseGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(BuildCouncilBriefingAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(BuildCouncilBriefingAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves gate status as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="upcomingRound">Upcoming round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="upcomingPhase">Upcoming phase value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="boundary">Boundary value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human collaboration gate status produced by the operation.</returns>
    public async Task<HumanCollaborationGateStatus> GetGateStatusAsync(
        Guid councilRunId,
        int upcomingRound,
        string upcomingPhase,
        HumanCollaborationBoundary boundary,
        CancellationToken cancellationToken = default)
    {
    try
    {
            using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var pending = await db.HumanCollaborationRequests.AsNoTracking()
                .Where(item => item.CouncilRunId == councilRunId &&
                    item.RequestKind != vocabulary.Get().HumanRequestApproval &&
                    item.Status == vocabulary.Get().HumanStatusPending)
                .OrderBy(item => item.RequestedAtUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var blocking = pending
                .Where(item => BlocksBoundary(item, Math.Max(0, upcomingRound), upcomingPhase, boundary))
                .ToList();
            return new HumanCollaborationGateStatus(
                blocking.Count > 0,
                boundary,
                Math.Max(0, upcomingRound),
                Normalize(upcomingPhase, 120),
                blocking);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetGateStatusAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetGateStatusAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether required pending input as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public async Task<bool> HasRequiredPendingInputAsync(Guid councilRunId, CancellationToken cancellationToken = default)
    {
    try
    {
            var gate = await GetGateStatusAsync(
                councilRunId,
                int.MaxValue,
                "Council completion",
                HumanCollaborationBoundary.Completion,
                cancellationToken).ConfigureAwait(false);
            return gate.IsBlocked;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(HasRequiredPendingInputAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(HasRequiredPendingInputAsync)} failed.");
        throw;
    }
}


    /// <summary>
    /// Performs synchronize active human membership as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the human collaboration operation and used when producing its result.</param>
    private void SynchronizeActiveHumanMembership(HumanCouncilParticipantProfile profile)
    {
    try
    {
            var humanMember = $"Human: {Normalize(profile.DisplayName, 120, "Human User")}";
            foreach (var pair in activeRuns.ToArray())
            {
                var current = pair.Value;
                var members = current.CouncilMembers
                    .Where(member => !member.StartsWith("Human:", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (profile.IsEnabled)
                    members.Add(humanMember);

                activeRuns[pair.Key] = current with
                {
                    CouncilMembers = members
                };
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(SynchronizeActiveHumanMembership)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(SynchronizeActiveHumanMembership)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs begin council run as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="members">String dependency used by the human collaboration workflow to provide the corresponding application capability.</param>
    public void BeginCouncilRun(Guid runId, IReadOnlyList<string> members)
    {
    try
    {
            activeRuns[runId] = new HumanCouncilRunSnapshot(runId, DateTime.UtcNow, 0, "Starting", members.ToList(), false);
            NotifyChanged();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(BeginCouncilRun)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(BeginCouncilRun)} failed.");
        throw;
    }
}

    /// <summary>
    /// Updates council run as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="currentRound">Current round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="isWaitingForFinalHumanInput">Value indicating whether is waiting for final human input should apply to this operation.</param>
    public void UpdateCouncilRun(Guid runId, int currentRound, string phase, bool isWaitingForFinalHumanInput = false)
    {
    try
    {
            if (!activeRuns.TryGetValue(runId, out var current))
                return;
            activeRuns[runId] = current with
            {
                CurrentRound = Math.Max(0, currentRound),
                Phase = Normalize(phase, 120),
                IsWaitingForFinalHumanInput = isWaitingForFinalHumanInput
            };
            NotifyChanged();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(UpdateCouncilRun)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(UpdateCouncilRun)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs end council run as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    public void EndCouncilRun(Guid runId)
    {
    try
    {
            activeRuns.TryRemove(runId, out _);
            preferredDirectUserMessageConsumers.TryRemove(runId, out _);
            foreach (var entry in directUserMessageRuns.Where(entry => entry.Value == runId).ToArray())
            {
                directUserMessageRuns.TryRemove(entry.Key, out _);
                directUserMessageClaims.TryRemove(entry.Key, out _);
            }
            NotifyChanged();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(EndCouncilRun)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(EndCouncilRun)} failed.");
        throw;
    }
}

    /// <summary>
    /// Ensures trusted human interaction as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="operation">Operation value supplied to the human collaboration operation and used when producing its result.</param>
    private void EnsureTrustedHumanInteraction(string operation)
    {
    try
    {
            if (!ambientContext.Current.IsTrustedHumanInteraction(vocabulary.Get()))
                throw new InvalidOperationException($"Only the trusted local human UI may {operation}.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(EnsureTrustedHumanInteraction)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(EnsureTrustedHumanInteraction)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs notify direct user message queued as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="contribution">Contribution value supplied to the human collaboration operation and used when producing its result.</param>
    private void NotifyDirectUserMessageQueued(HumanCouncilContribution contribution)
    {
        var listeners = DirectUserMessageQueued?.GetInvocationList()
            .Cast<Action<HumanCouncilContribution>>()
            .ToArray() ?? [];
        foreach (var listener in listeners)
        {
            try
            {
                listener(contribution);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "A direct Council user-message listener failed; remaining listeners will still be notified.");
            }
        }
    }

    /// <summary>
    /// Performs notify changed as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void NotifyChanged()
    {
        var listeners = Changed?.GetInvocationList().Cast<Action>().ToArray() ?? [];
        foreach (var listener in listeners)
        {
            try
            {
                listener();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "A Human Collaboration Inbox change listener failed; remaining listeners will still be notified.");
            }
        }
    }

    /// <summary>
    /// Performs determine evaluation verdict as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="evaluation">Evaluation value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DetermineEvaluationVerdict(string evaluation)
    {
    try
    {
            if (evaluation.Contains("Human peer assessment: Supported", StringComparison.OrdinalIgnoreCase))
                return vocabulary.Get().VerdictSupported;
            if (evaluation.Contains("Human peer assessment: Needs correction", StringComparison.OrdinalIgnoreCase))
                return vocabulary.Get().VerdictNeedsCorrection;
            if (evaluation.Contains("Human peer assessment: Mixed", StringComparison.OrdinalIgnoreCase))
                return vocabulary.Get().VerdictMixed;
            return vocabulary.Get().VerdictNotReviewed;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(DetermineEvaluationVerdict)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(DetermineEvaluationVerdict)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs blocks boundary as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="upcomingRound">Upcoming round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="upcomingPhase">Upcoming phase value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="boundary">Boundary value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool BlocksBoundary(
        HumanCollaborationRequest request,
        int upcomingRound,
        string upcomingPhase,
        HumanCollaborationBoundary boundary)
    {
    try
    {
            var gateMode = NormalizeGateMode(request.GateMode, request.RequiredBeforeCompletion);
            if (gateMode == "None")
                return false;
            if (boundary == HumanCollaborationBoundary.Completion)
                return gateMode == "NextPhase" ||
                    gateMode == "NextRound" ||
                    gateMode == "Completion";

            var movedToLaterRound = upcomingRound > request.RequestedCouncilRound;
            var movedToLaterPhase = movedToLaterRound ||
                (upcomingRound == request.RequestedCouncilRound &&
                 !string.Equals(Normalize(upcomingPhase, 120), request.RequestedCouncilPhase, StringComparison.OrdinalIgnoreCase));

            return gateMode switch
            {
                "NextPhase" => movedToLaterPhase,
                "NextRound" => movedToLaterRound,
                "Completion" => false,
                _ => false
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(BlocksBoundary)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(BlocksBoundary)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether reusable decision as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsReusableDecision(HumanCollaborationRequest request)
    {
    try
    {
            if (request.Status == vocabulary.Get().HumanStatusPending)
                return true;
            if (request.Status != vocabulary.Get().HumanStatusApproved &&
                request.Status != vocabulary.Get().HumanStatusAnswered &&
                request.Status != vocabulary.Get().HumanStatusDeclined)
                return false;

            return request.ApprovalReuseScope switch
            {
                HumanApprovalReuseScope.CurrentApplicationSession =>
                    request.ApprovalSessionId == approvalSessionId &&
                    (!request.ConsumeApproval || request.ConsumedAtUtc is null),
                HumanApprovalReuseScope.PersistentUntilChanged =>
                    !request.ConsumeApproval || request.ConsumedAtUtc is null,
                _ => request.ConsumedAtUtc is null
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(IsReusableDecision)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(IsReusableDecision)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves default reuse scope as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="requestKind">Request kind value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="riskLevel">Risk level value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>The human approval reuse scope produced by the operation.</returns>
    private HumanApprovalReuseScope GetDefaultReuseScope(string requestKind, string? riskLevel)
    {
    try
    {
            if (requestKind != vocabulary.Get().HumanRequestApproval)
                return HumanApprovalReuseScope.ExactRequestOnce;
            return IsHighImpactRisk(riskLevel)
                ? HumanApprovalReuseScope.ExactRequestOnce
                : HumanApprovalReuseScope.CurrentApplicationSession;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetDefaultReuseScope)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetDefaultReuseScope)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves default consume approval as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="requestKind">Request kind value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="riskLevel">Risk level value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetDefaultConsumeApproval(string requestKind, string? riskLevel) {
    try
    {
        return requestKind != vocabulary.Get().HumanRequestApproval || IsHighImpactRisk(riskLevel);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetDefaultConsumeApproval)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(GetDefaultConsumeApproval)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether high impact risk as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="riskLevel">Risk level value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsHighImpactRisk(string? riskLevel) {
    try
    {
        return string.Equals(riskLevel, "High", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(riskLevel, "Critical", StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(IsHighImpactRisk)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(IsHighImpactRisk)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes question scope as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeQuestionScope(string? value)
    {
    try
    {
            if (string.Equals(value, "Consensus", StringComparison.OrdinalIgnoreCase))
                return "Consensus";
            if (string.Equals(value, "SelectedMembers", StringComparison.OrdinalIgnoreCase))
                return "SelectedMembers";
            return "Member";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeQuestionScope)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeQuestionScope)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes gate mode as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="requiredBeforeCompletion">Value indicating whether required before completion should apply to this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeGateMode(string? value, bool requiredBeforeCompletion)
    {
    try
    {
            if (string.Equals(value, "NextPhase", StringComparison.OrdinalIgnoreCase))
                return "NextPhase";
            if (string.Equals(value, "NextRound", StringComparison.OrdinalIgnoreCase))
                return "NextRound";
            if (string.Equals(value, "Completion", StringComparison.OrdinalIgnoreCase) || requiredBeforeCompletion)
                return "Completion";
            return "None";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeGateMode)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeGateMode)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes request kind as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeRequestKind(string? value)
    {
    try
    {
            if (string.Equals(value, vocabulary.Get().HumanRequestFeedback, StringComparison.OrdinalIgnoreCase))
                return vocabulary.Get().HumanRequestFeedback;
            if (string.Equals(value, vocabulary.Get().HumanRequestGuidance, StringComparison.OrdinalIgnoreCase))
                return vocabulary.Get().HumanRequestGuidance;
            return vocabulary.Get().HumanRequestApproval;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeRequestKind)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeRequestKind)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs normalize as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="maxLength">Max length value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the human collaboration operation and used when producing its result.</param>
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
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(Normalize)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(Normalize)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes multiline as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="maxLength">Max length value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeMultiline(string? value, int maxLength)
    {
    try
    {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Trim();
            return normalized[..Math.Min(normalized.Length, maxLength)];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeMultiline)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HumanCollaborationService)}.{nameof(NormalizeMultiline)} failed.");
        throw;
    }
}
}

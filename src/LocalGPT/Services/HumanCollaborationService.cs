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
public sealed partial class HumanCollaborationService : IHumanCollaborationService
    {
        /// <summary>
        /// Stores the local GPT vocabulary service dependency used by <see cref="HumanCollaborationService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ILocalGptVocabularyService vocabulary;
        /// <summary>
        /// Stores the database context factory dependency used by <see cref="HumanCollaborationService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory;
        /// <summary>
        /// Stores the ambient local GPT context dependency used by <see cref="HumanCollaborationService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IAmbientLocalGptContext ambientContext;
        /// <summary>
        /// Stores the component activity service dependency used by <see cref="HumanCollaborationService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IComponentActivityService componentActivity;
        /// <summary>
        /// Stores the local GPT runtime policy data service dependency used by <see cref="HumanCollaborationService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ILocalGptRuntimePolicyDataService runtimePolicy;
        /// <summary>
        /// Stores the logger used by <see cref="HumanCollaborationService"/> to record operational diagnostics without coupling callers to logging details.
        /// </summary>
        private readonly ILogger<HumanCollaborationService> logger;

        /// <summary>Initializes the type with its dependency-injected collaborators.</summary>
        /// <param name="vocabulary">Injected dependency used by the HumanCollaborationService.</param>
        /// <param name="dbContextFactory">Injected dependency used by the HumanCollaborationService.</param>
        /// <param name="ambientContext">Injected dependency used by the HumanCollaborationService.</param>
        /// <param name="componentActivity">Injected dependency used by the HumanCollaborationService.</param>
        /// <param name="runtimePolicy">Injected dependency used by the HumanCollaborationService.</param>
        /// <param name="logger">Injected dependency used by the HumanCollaborationService.</param>
        public HumanCollaborationService(
            ILocalGptVocabularyService vocabulary,
            IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
            IAmbientLocalGptContext ambientContext,
            IComponentActivityService componentActivity,
            ILocalGptRuntimePolicyDataService runtimePolicy,
            ILogger<HumanCollaborationService> logger)
        {
            this.vocabulary = vocabulary;
            this.dbContextFactory = dbContextFactory;
            this.ambientContext = ambientContext;
            this.componentActivity = componentActivity;
            this.runtimePolicy = runtimePolicy;
            this.logger = logger;
        }

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
                    PrefillText = NormalizeMultiline(request.PrefillText, Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.HumanCollaborationMaximumTextLength))),
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
                request.UserResponse = NormalizeMultiline(submission.Response, Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.HumanCollaborationMaximumTextLength)));
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
}

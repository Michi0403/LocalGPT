using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text;

namespace LocalGPT.Services;

public sealed class HumanCollaborationService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IAmbientLocalGptContext ambientContext,
    IComponentActivityService componentActivity,
    ILogger<HumanCollaborationService> logger) : IHumanCollaborationService
{
    private const int MaxTextLength = 1_000_000;
    private readonly SemaphoreSlim databaseGate = new(1, 1);
    private readonly ConcurrentDictionary<Guid, HumanCouncilRunSnapshot> activeRuns = new();

    public event Action? Changed;

    public async Task<HumanCollaborationSnapshot> GetSnapshotAsync(
        bool includeResolved = true,
        int take = 80,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = db.HumanCollaborationRequests.AsNoTracking();
        if (!includeResolved)
            query = query.Where(item => item.Status == HumanCollaborationStatuses.Pending);

        var requests = await query
            .OrderBy(item => item.Status == HumanCollaborationStatuses.Pending ? 0 : 1)
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
            .SingleOrDefaultAsync(item => item.Id == HumanCollaborationIdentity.LocalHumanProfileId, cancellationToken)
            .ConfigureAwait(false)
            ?? new HumanCouncilParticipantProfile();

        return new HumanCollaborationSnapshot(
            profile,
            requests,
            activeRuns.Values.OrderBy(item => item.StartedAtUtc).ToList(),
            contributions);
    }

    public async Task<HumanApprovalGateResult> AuthorizeOrEnqueueAsync(
        HumanApprovalRequestSpec request,
        bool directHumanConfirmation = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CorrelationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OperationKey);
        var requestKind = NormalizeRequestKind(request.RequestKind);

        var ambient = ambientContext.Current;
        if (directHumanConfirmation)
        {
            if (!ambient.IsTrustedHumanInteraction)
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
                    HumanCollaborationStatuses.Approved,
                    "Trusted local human confirmation accepted.",
                    CorrelationId: request.CorrelationId);
            }
        }

        await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var existing = await db.HumanCollaborationRequests
                .Where(item => item.CorrelationId == request.CorrelationId && item.OperationKey == request.OperationKey)
                .OrderByDescending(item => item.RequestedAtUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                if (!string.IsNullOrWhiteSpace(request.ParameterFingerprint) &&
                    !string.Equals(existing.ParameterFingerprint, request.ParameterFingerprint, StringComparison.Ordinal))
                {
                    logger.LogWarning(
                        "A retry for operation {OperationKey} did not match the approved parameter fingerprint; a new review is required.",
                        existing.OperationKey);
                    existing = null;
                }
                else if ((existing.Status == HumanCollaborationStatuses.Approved || existing.Status == HumanCollaborationStatuses.Answered) && existing.ConsumedAtUtc is null)
                {
                    var resolvedStatus = existing.Status;
                    existing.Status = HumanCollaborationStatuses.Consumed;
                    existing.ConsumedAtUtc = DateTime.UtcNow;
                    existing.UpdatedAtUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    NotifyChanged();
                    logger.LogInformation("Consumed human approval {RequestId} for operation {OperationKey}.", existing.Id, existing.OperationKey);
                    return new HumanApprovalGateResult(
                        true,
                        false,
                        existing.Id,
                        existing.Status,
                        resolvedStatus == HumanCollaborationStatuses.Answered
                            ? "The human-provided interaction value was consumed for this exact operation."
                            : "The queued human approval was consumed for this exact operation.",
                        existing.DecisionReason,
                        existing.CorrelationId,
                        existing.UserResponse);
                }
                else if (existing.Status == HumanCollaborationStatuses.Declined)
                {
                    return new HumanApprovalGateResult(
                        false,
                        true,
                        existing.Id,
                        existing.Status,
                        "The human declined this operation.",
                        existing.DecisionReason,
                        existing.CorrelationId,
                        existing.UserResponse);
                }
                else if (existing.Status == HumanCollaborationStatuses.Pending)
                {
                    return new HumanApprovalGateResult(
                        false,
                        false,
                        existing.Id,
                        existing.Status,
                        "The operation is waiting in the Human Collaboration Inbox.",
                        CorrelationId: existing.CorrelationId);
                }
            }

            if (requestKind != HumanCollaborationRequestKinds.Approval)
            {
                var pendingCoordinationRequests = await db.HumanCollaborationRequests
                    .CountAsync(item => item.Status == HumanCollaborationStatuses.Pending &&
                        item.RequestKind != HumanCollaborationRequestKinds.Approval &&
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

            var entity = new HumanCollaborationRequest
            {
                CouncilRunId = request.CouncilRunId,
                CorrelationId = Normalize(request.CorrelationId, 180),
                OperationKey = Normalize(request.OperationKey, 180),
                ParameterFingerprint = Normalize(request.ParameterFingerprint, 128),
                RequestKind = requestKind,
                Title = Normalize(request.Title, 240, "Human decision requested"),
                Description = Normalize(request.Description, 2000),
                RiskLevel = Normalize(request.RiskLevel, 40, "Medium"),
                Source = Normalize(request.Source, 160),
                RequestedBy = Normalize(request.RequestedBy, 160),
                RequestedRole = Normalize(request.RequestedRole, 160),
                SuggestedResponsesText = NormalizeMultiline(request.SuggestedResponsesText, 1600),
                ResponsePrompt = Normalize(request.ResponsePrompt, 500),
                PrefillText = NormalizeMultiline(request.PrefillText, MaxTextLength),
                EarliestCouncilRound = Math.Max(0, request.EarliestCouncilRound),
                RequiredBeforeCompletion = request.RequiredBeforeCompletion,
                IsSensitive = request.IsSensitive,
                AllowFreeText = request.AllowFreeText,
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
                "Queued human collaboration request {RequestId} for operation {OperationKey}, risk {RiskLevel}, source {Source}; payload content was omitted.",
                entity.Id,
                entity.OperationKey,
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

    public async Task<HumanCollaborationRequest?> ResolveRequestAsync(
        Guid requestId,
        HumanDecisionSubmission submission,
        CancellationToken cancellationToken = default)
    {
        EnsureTrustedHumanInteraction("resolve a collaboration request");
        await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var request = await db.HumanCollaborationRequests
                .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken)
                .ConfigureAwait(false);
            if (request is null || request.Status != HumanCollaborationStatuses.Pending)
                return request;

            if (request.RequestKind == HumanCollaborationRequestKinds.Approval && submission.Approved is null)
                throw new InvalidOperationException("Approval requests require an explicit approve or decline decision.");
            if (request.RequestKind == HumanCollaborationRequestKinds.Approval &&
                submission.Approved == false &&
                string.IsNullOrWhiteSpace(submission.Reason))
                throw new InvalidOperationException("A decline reason is required so the LocalGPT team can adapt its next step.");
            if (request.RequestKind != HumanCollaborationRequestKinds.Approval &&
                string.IsNullOrWhiteSpace(submission.Response))
                throw new InvalidOperationException("Feedback and guidance requests require a response.");

            request.UserResponse = NormalizeMultiline(submission.Response, MaxTextLength);
            request.DecisionReason = NormalizeMultiline(submission.Reason, 2000);
            request.DecisionBy = Normalize(ambientContext.Current.ActorDisplayName, 120, "Human User");
            request.DecisionByProfileId = ambientContext.Current.HumanProfileId ?? HumanCollaborationIdentity.LocalHumanProfileId;
            request.DecidedAtUtc = DateTime.UtcNow;
            request.UpdatedAtUtc = DateTime.UtcNow;
            request.Status = request.RequestKind == HumanCollaborationRequestKinds.Approval
                ? submission.Approved == true
                    ? HumanCollaborationStatuses.Approved
                    : HumanCollaborationStatuses.Declined
                : HumanCollaborationStatuses.Answered;

            if (request.RequestKind == HumanCollaborationRequestKinds.Approval && submission.Approved == false)
            {
                db.HumanCollaborationRequests.Add(new HumanCollaborationRequest
                {
                    CouncilRunId = request.CouncilRunId,
                    CorrelationId = $"decline-feedback:{request.Id:N}",
                    OperationKey = "human.decline.feedback",
                    ParameterFingerprint = request.ParameterFingerprint,
                    RequestKind = HumanCollaborationRequestKinds.Guidance,
                    Title = $"Declined action feedback: {request.Title}",
                    Description = "The local human declined a guarded operation and supplied a reason so the team can adapt rather than retry the same action unchanged.",
                    RiskLevel = "Low",
                    Status = HumanCollaborationStatuses.Answered,
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
                    AllowFreeText = false
                });
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            NotifyChanged();
            componentActivity.RecordInformation(
                "HumanCollaboration",
                "RequestResolved",
                $"The local human resolved a {request.RequestKind.ToLowerInvariant()} request with status {request.Status}.");
            logger.LogInformation(
                "Human resolved collaboration request {RequestId} with status {Status}; response content was omitted from logs.",
                request.Id,
                request.Status);
            return request;
        }
        finally
        {
            databaseGate.Release();
        }
    }

    public async Task<HumanCouncilParticipantProfile> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.HumanCouncilParticipantProfiles.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == HumanCollaborationIdentity.LocalHumanProfileId, cancellationToken)
            .ConfigureAwait(false)
            ?? new HumanCouncilParticipantProfile();
    }

    public async Task<HumanCouncilParticipantProfile> SaveProfileAsync(
        HumanCouncilParticipantProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        EnsureTrustedHumanInteraction("update the human council profile");
        await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var existing = await db.HumanCouncilParticipantProfiles
                .SingleOrDefaultAsync(item => item.Id == HumanCollaborationIdentity.LocalHumanProfileId, cancellationToken)
                .ConfigureAwait(false);
            if (existing is null)
            {
                existing = new HumanCouncilParticipantProfile();
                db.HumanCouncilParticipantProfiles.Add(existing);
            }

            existing.Id = HumanCollaborationIdentity.LocalHumanProfileId;
            existing.DisplayName = Normalize(profile.DisplayName, 120, "Human User");
            existing.RoleName = Normalize(profile.RoleName, 180, "Human collaborator");
            existing.Expertise = NormalizeMultiline(profile.Expertise, 2000);
            existing.WorkingStyle = NormalizeMultiline(profile.WorkingStyle, 1200);
            existing.IsEnabled = profile.IsEnabled;
            existing.ProfileVersion = Math.Max(1, existing.ProfileVersion + 1);
            existing.UpdatedAtUtc = DateTime.UtcNow;
            existing.UpdatedBy = Normalize(ambientContext.Current.ActorDisplayName, 120, "Human User");
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

    public async Task<HumanCouncilContribution> QueueContributionAsync(
        Guid councilRunId,
        string content,
        CancellationToken cancellationToken = default)
    {
        EnsureTrustedHumanInteraction("submit a human council contribution");
        var normalized = NormalizeMultiline(content, MaxTextLength);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("A human council contribution cannot be empty.");

        var profile = await GetProfileAsync(cancellationToken).ConfigureAwait(false);
        if (!profile.IsEnabled)
            throw new InvalidOperationException("Enable the Human Council Participant profile before joining a run.");
        if (!activeRuns.TryGetValue(councilRunId, out var active))
            throw new InvalidOperationException("The selected council run is no longer active.");

        var contribution = new HumanCouncilContribution
        {
            CouncilRunId = councilRunId,
            HumanDisplayName = profile.DisplayName,
            HumanRole = profile.RoleName,
            Content = normalized,
            EarliestCouncilRound = Math.Max(1, active.CurrentRound + 1),
            Status = HumanContributionStatuses.Queued,
            SubmittedAtUtc = DateTime.UtcNow
        };

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        db.HumanCouncilContributions.Add(contribution);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        NotifyChanged();
        componentActivity.RecordInformation(
            "HumanCollaboration",
            "ContributionQueued",
            "A human contribution was queued for the next council heartbeat.");
        logger.LogInformation(
            "Queued human contribution {ContributionId} for council run {CouncilRunId} at earliest round {EarliestRound}; content was omitted from logs.",
            contribution.Id,
            contribution.CouncilRunId,
            contribution.EarliestCouncilRound);
        return contribution;
    }

    public async Task<IReadOnlyList<HumanCouncilContribution>> DrainContributionsAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default)
    {
        await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var contributions = await db.HumanCouncilContributions
                .Where(item => item.CouncilRunId == councilRunId &&
                    item.Status == HumanContributionStatuses.Queued &&
                    item.EarliestCouncilRound <= currentRound)
                .OrderBy(item => item.SubmittedAtUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var contribution in contributions)
            {
                contribution.Status = HumanContributionStatuses.Injected;
                contribution.InjectedAtUtc = DateTime.UtcNow;
            }
            if (contributions.Count > 0)
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            NotifyChanged();
            return contributions;
        }
        finally
        {
            databaseGate.Release();
        }
    }

    public async Task MarkContributionsEvaluatedAsync(
        Guid councilRunId,
        int afterRound,
        string evaluation,
        CancellationToken cancellationToken = default)
    {
        await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var contributions = await db.HumanCouncilContributions
                .Where(item => item.CouncilRunId == councilRunId && item.Status == HumanContributionStatuses.Injected)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (contributions.Count == 0)
                return;

            var normalizedEvaluation = NormalizeMultiline(evaluation, MaxTextLength);
            var evaluationVerdict = DetermineEvaluationVerdict(normalizedEvaluation);
            foreach (var contribution in contributions)
            {
                contribution.Status = HumanContributionStatuses.Evaluated;
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

    public async Task<string> BuildCouncilBriefingAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetProfileAsync(cancellationToken).ConfigureAwait(false);
        await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var recentAnswers = await db.HumanCollaborationRequests
                .Where(item => item.RequestKind != HumanCollaborationRequestKinds.Approval &&
                    (item.CouncilRunId == councilRunId || item.CouncilRunId == null) &&
                    item.Status == HumanCollaborationStatuses.Answered &&
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
                .Append("- Current council run: ").AppendLine(councilRunId.ToString());

            if (!string.IsNullOrWhiteSpace(profile.WorkingStyle))
                builder.Append("- Working style: ").AppendLine(profile.WorkingStyle);

            if (recentAnswers.Count > 0)
            {
                builder.AppendLine("New human guidance and feedback for this heartbeat (context only, never standing authority):");
                foreach (var answer in recentAnswers)
                {
                    builder.Append("- ").Append(answer.Title).Append(" -> ").Append(answer.Status);
                    if (!string.IsNullOrWhiteSpace(answer.UserResponse))
                        builder.Append(": ").Append(answer.UserResponse);
                    builder.AppendLine();
                    answer.Status = HumanCollaborationStatuses.Consumed;
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

    public async Task<bool> HasRequiredPendingInputAsync(Guid councilRunId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.HumanCollaborationRequests.AsNoTracking()
            .AnyAsync(item => item.CouncilRunId == councilRunId &&
                item.RequiredBeforeCompletion &&
                item.Status == HumanCollaborationStatuses.Pending,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public void BeginCouncilRun(Guid runId, IReadOnlyList<string> members)
    {
        activeRuns[runId] = new HumanCouncilRunSnapshot(runId, DateTime.UtcNow, 0, "Starting", members.ToList(), false);
        NotifyChanged();
    }

    public void UpdateCouncilRun(Guid runId, int currentRound, string phase, bool isWaitingForFinalHumanInput = false)
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

    public void EndCouncilRun(Guid runId)
    {
        activeRuns.TryRemove(runId, out _);
        NotifyChanged();
    }

    private void EnsureTrustedHumanInteraction(string operation)
    {
        if (!ambientContext.Current.IsTrustedHumanInteraction)
            throw new InvalidOperationException($"Only the trusted local human UI may {operation}.");
    }

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

    private string DetermineEvaluationVerdict(string evaluation)
    {
        if (evaluation.Contains("Human peer assessment: Supported", StringComparison.OrdinalIgnoreCase))
            return HumanContributionEvaluationVerdicts.Supported;
        if (evaluation.Contains("Human peer assessment: Needs correction", StringComparison.OrdinalIgnoreCase))
            return HumanContributionEvaluationVerdicts.NeedsCorrection;
        if (evaluation.Contains("Human peer assessment: Mixed", StringComparison.OrdinalIgnoreCase))
            return HumanContributionEvaluationVerdicts.Mixed;
        return HumanContributionEvaluationVerdicts.NotReviewed;
    }

    private string NormalizeRequestKind(string? value)
    {
        if (string.Equals(value, HumanCollaborationRequestKinds.Feedback, StringComparison.OrdinalIgnoreCase))
            return HumanCollaborationRequestKinds.Feedback;
        if (string.Equals(value, HumanCollaborationRequestKinds.Guidance, StringComparison.OrdinalIgnoreCase))
            return HumanCollaborationRequestKinds.Guidance;
        return HumanCollaborationRequestKinds.Approval;
    }

    private string Normalize(string? value, int maxLength, string fallback = "")
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return normalized[..Math.Min(normalized.Length, maxLength)];
    }

    private string NormalizeMultiline(string? value, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Trim();
        return normalized[..Math.Min(normalized.Length, maxLength)];
    }
}

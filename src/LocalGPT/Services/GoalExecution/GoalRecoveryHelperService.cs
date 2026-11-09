using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace LocalGPT.Services.GoalExecution;

/// <summary>
/// Goal execution's "let me do it for you" helper for running Councils. It decides how a failed member slot continues and how an
/// unavailable AI host is handled, so the original user goal keeps moving instead of stalling on one member or machine.
/// </summary>
/// <remarks>
/// Security boundary: every candidate is a member of the current run, and therefore runs on an AI host the run already uses.
/// The helper never adds a model, provider or host. Substituting a member of a paired role and excluding a host for the run are
/// offered to the local user through Human Collaboration; until the user decides, the run continues without blocking.
/// State is kept per Council run and evicted after <see cref="MaximumTrackedRuns"/> runs.
/// </remarks>
public sealed partial class GoalRecoveryHelperService : IGoalRecoveryHelperService
{
    /// <summary>Upper bound of Council runs whose recovery state is kept in this scope.</summary>
    private const int MaximumTrackedRuns = 32;

    /// <summary>Recovery state per Council run.</summary>
    private readonly ConcurrentDictionary<Guid, GoalRecoveryRunState> runs = new();
    /// <summary>Human Collaboration inbox used for paired-substitution and host-exclusion decisions.</summary>
    private readonly IHumanCollaborationService humanCollaboration;
    /// <summary>Retained Council runs used as member evidence.</summary>
    private readonly ICouncilSpoolerService councilSpooler;
    /// <summary>Provider runtime used for the bounded outage wait and the single reachability check.</summary>
    private readonly IProviderModelRuntimeService providerModels;
    /// <summary>Persisted runtime policy that owns the helper's bounds and texts.</summary>
    private readonly ILocalGptRuntimePolicyDataService runtimePolicy;
    /// <summary>Diagnostics for every helper decision.</summary>
    private readonly ILogger<GoalRecoveryHelperService> logger;

    /// <summary>Initializes the helper with its dependency-injected collaborators.</summary>
    /// <param name="humanCollaboration">Human Collaboration inbox used for decisions that belong to the local user.</param>
    /// <param name="councilSpooler">Retained Council runs used as member evidence.</param>
    /// <param name="providerModels">Provider runtime used for availability waits and reachability checks.</param>
    /// <param name="runtimePolicy">Persisted runtime policy that owns the helper's bounds and texts.</param>
    /// <param name="logger">Diagnostics for every helper decision.</param>
    public GoalRecoveryHelperService(
        IHumanCollaborationService humanCollaboration,
        ICouncilSpoolerService councilSpooler,
        IProviderModelRuntimeService providerModels,
        ILocalGptRuntimePolicyDataService runtimePolicy,
        ILogger<GoalRecoveryHelperService> logger)
    {
        this.humanCollaboration = humanCollaboration;
        this.councilSpooler = councilSpooler;
        this.providerModels = providerModels;
        this.runtimePolicy = runtimePolicy;
        this.logger = logger;
    }

    /// <summary>Gets the user-editable helper texts from the runtime policy.</summary>
    /// <value>The current helper texts.</value>
    private GoalRecoveryHelperTexts Texts => runtimePolicy.GetJson<GoalRecoveryHelperTexts>("GoalRecoveryHelperTextsJson");

    /// <summary>Gets the Council execution bounds from the runtime policy.</summary>
    /// <value>The current Council execution parameters.</value>
    private CouncilExecutionRuntimeParameters ExecutionParameters => runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>("CouncilExecutionRuntimeParametersJson");

    /// <inheritdoc />
    public async Task<GoalMemberRecoveryDecision> DecideMemberRecoveryAsync(GoalMemberRecoveryContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();
            var texts = Texts;
            var state = GetRunState(context.RunId);
            var failedHost = HostOf(context, context.FailedModel);
            var failedHostUsable = IsHostUsable(state, failedHost);

            if (context.SameMemberOnly)
            {
                return LogDecision(context, failedHostUsable
                    ? new GoalMemberRecoveryDecision
                    {
                        Action = GoalMemberRecoveryAction.RetrySameMember,
                        ModelName = context.FailedModel,
                        Reason = Render(texts.ReasonSameMemberPolicy, ("Model", context.FailedModel))
                    }
                    : new GoalMemberRecoveryDecision
                    {
                        Action = GoalMemberRecoveryAction.SkipSlot,
                        Reason = Render(texts.ReasonSameMemberHostDown, ("Model", context.FailedModel), ("Host", failedHost))
                    });
            }

            var sameMemberTried = context.AttemptedModels.Contains(context.FailedModel, StringComparer.OrdinalIgnoreCase);
            if (!sameMemberTried && failedHostUsable && context.FailureKind != GoalMemberFailureKind.HostUnreachable)
            {
                return LogDecision(context, new GoalMemberRecoveryDecision
                {
                    Action = GoalMemberRecoveryAction.RetrySameMember,
                    ModelName = context.FailedModel,
                    Reason = Render(texts.ReasonSameMemberFirst, ("Model", context.FailedModel))
                });
            }

            var evidence = BuildCandidateEvidence(context, state);
            var usable = evidence
                .Where(item => item.HostAvailable && IsSuitable(item))
                .Select((item, index) => (Evidence: item, Index: index))
                .OrderBy(item => item.Evidence.InRolePool ? 0 : 1)
                .ThenBy(item => string.Equals(item.Evidence.HostKey, failedHost, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenBy(item => item.Evidence.RunFailures)
                .ThenByDescending(item => SuccessScore(item.Evidence))
                .ThenBy(item => item.Index)
                .Select(item => item.Evidence)
                .ToList();

            if (usable.Count == 0)
            {
                return LogDecision(context, failedHostUsable && context.FailureKind != GoalMemberFailureKind.HostUnreachable
                    ? new GoalMemberRecoveryDecision
                    {
                        Action = GoalMemberRecoveryAction.RetrySameMember,
                        ModelName = context.FailedModel,
                        Reason = Render(texts.ReasonSameMemberAgain, ("Model", context.FailedModel)),
                        Evidence = evidence
                    }
                    : new GoalMemberRecoveryDecision
                    {
                        Action = GoalMemberRecoveryAction.SkipSlot,
                        Reason = Render(texts.ReasonNoCandidate, ("Model", context.FailedModel), ("Role", context.RoleName)),
                        Evidence = evidence
                    });
            }

            var chosen = usable[0];
            var evidenceText = DescribeEvidence(texts, chosen);
            var reason = chosen.InRolePool
                ? Render(texts.ReasonRolePoolMember, ("Model", context.FailedModel), ("Candidate", chosen.ModelName), ("Evidence", evidenceText), ("Role", context.RoleName))
                : Render(texts.ReasonOtherRoleMember, ("Model", context.FailedModel), ("Candidate", chosen.ModelName), ("Evidence", evidenceText), ("Role", context.RoleName));

            if (context.IsPairedRole)
            {
                var approval = await GetPairedSubstitutionApprovalAsync(context, state, chosen.ModelName, cancellationToken).ConfigureAwait(false);
                if (!approval.IsDecided)
                {
                    return LogDecision(context, new GoalMemberRecoveryDecision
                    {
                        Action = GoalMemberRecoveryAction.SkipSlot,
                        Reason = Render(texts.ReasonPairedPending, ("Model", context.FailedModel), ("Role", context.RoleName)),
                        ApprovalRequestId = approval.RequestId,
                        Evidence = usable
                    });
                }

                if (!approval.IsAuthorized)
                {
                    return LogDecision(context, new GoalMemberRecoveryDecision
                    {
                        Action = GoalMemberRecoveryAction.SkipSlot,
                        Reason = Render(texts.ReasonPairedDeclined, ("Model", context.FailedModel), ("Role", context.RoleName)),
                        ApprovalRequestId = approval.RequestId,
                        Evidence = usable
                    });
                }

                return LogDecision(context, new GoalMemberRecoveryDecision
                {
                    Action = GoalMemberRecoveryAction.UseOtherMember,
                    ModelName = chosen.ModelName,
                    Reason = reason,
                    SubstitutesPairedSlot = true,
                    FromOtherRole = !chosen.InRolePool,
                    ApprovalRequestId = approval.RequestId,
                    Evidence = usable
                });
            }

            return LogDecision(context, new GoalMemberRecoveryDecision
            {
                Action = GoalMemberRecoveryAction.UseOtherMember,
                ModelName = chosen.ModelName,
                Reason = reason,
                FromOtherRole = !chosen.InRolePool,
                Evidence = usable
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Goal recovery decision was canceled for run {RunId}.", context?.RunId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Goal recovery decision failed for run {RunId}, member {FailedModel}.", context?.RunId, context?.FailedModel);
            throw;
        }
    }

    /// <summary>Builds evidence for every run member that may answer for the failed slot: the role's members first, then the other members of the run.</summary>
    /// <param name="context">Failed slot and run members.</param>
    /// <param name="state">Recovery state of the run.</param>
    /// <returns>Evidence rows in configured order; the failed member, attempted members and reserved members are left out.</returns>
    private IReadOnlyList<GoalMemberEvidence> BuildCandidateEvidence(GoalMemberRecoveryContext context, GoalRecoveryRunState state)
    {
        try
        {
            // Security boundary: only members of this run are candidates; the role pool is intersected with them as well.
            var runMembers = context.RunParticipants
                .Where(member => !string.IsNullOrWhiteSpace(member))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var runMemberSet = runMembers.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var rolePool = context.RolePool
                .Where(member => runMemberSet.Contains(member))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var rolePoolSet = rolePool.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var excluded = context.AttemptedModels
                .Concat(context.ReservedByOtherRoles)
                .Append(context.FailedModel)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var history = GetHistory(context.RunId, state);

            return rolePool
                .Concat(runMembers.Where(member => !rolePoolSet.Contains(member)))
                .Where(member => !excluded.Contains(member))
                .Select(member =>
                {
                    var hostKey = HostOf(context, member);
                    var runSteps = context.RunSteps
                        .Where(step => string.Equals(step.ModelName, member, StringComparison.OrdinalIgnoreCase) && IsSameRole(step.Role, context.RoleName))
                        .ToList();
                    var historySteps = history
                        .Where(item => string.Equals(item.ModelName, member, StringComparison.OrdinalIgnoreCase) && IsSameRole(item.Role, context.RoleName))
                        .ToList();
                    return new GoalMemberEvidence
                    {
                        ModelName = member,
                        HostKey = hostKey,
                        InRolePool = rolePoolSet.Contains(member),
                        HostAvailable = IsHostUsable(state, hostKey),
                        RunSuccesses = runSteps.Count(step => string.IsNullOrWhiteSpace(step.Error) && !string.IsNullOrWhiteSpace(step.VisibleContent)),
                        RunFailures = runSteps.Count(step => !string.IsNullOrWhiteSpace(step.Error)),
                        HistorySuccesses = historySteps.Count(item => item.Succeeded),
                        HistoryFailures = historySteps.Count(item => item.Failed && !item.HostUnreachable)
                    };
                })
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building goal recovery evidence failed for run {RunId}, role {RoleName}.", context.RunId, context.RoleName);
            throw;
        }
    }

    /// <summary>
    /// Returns whether a candidate is suitable. Members of the failed role are suitable while their host is usable; a member of
    /// another role must not have failed in this role during the run and must not have failed more often than it succeeded in recent runs.
    /// </summary>
    /// <param name="evidence">Candidate evidence.</param>
    /// <returns><see langword="true"/> when the candidate may answer for the slot.</returns>
    private bool IsSuitable(GoalMemberEvidence evidence)
    {
        try
        {
            return evidence.InRolePool
                || (evidence.RunFailures == 0 && evidence.HistoryFailures <= evidence.HistorySuccesses);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Goal recovery suitability check failed for {ModelName}.", evidence?.ModelName);
            throw;
        }
    }

    /// <summary>Scores a candidate's observed reliability in the role; results of this run weigh twice as much as recent runs.</summary>
    /// <param name="evidence">Candidate evidence.</param>
    /// <returns>A smoothed success rate between 0 and 1; 0.5 without evidence.</returns>
    private double SuccessScore(GoalMemberEvidence evidence)
    {
        try
        {
            var successes = (2d * evidence.RunSuccesses) + evidence.HistorySuccesses;
            var failures = (2d * evidence.RunFailures) + evidence.HistoryFailures;
            return (successes + 1d) / (successes + failures + 2d);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Goal recovery score failed for {ModelName}.", evidence?.ModelName);
            throw;
        }
    }

    /// <summary>Renders the evidence summary of a chosen candidate.</summary>
    /// <param name="texts">Helper texts.</param>
    /// <param name="evidence">Candidate evidence.</param>
    /// <returns>The readable evidence summary.</returns>
    private string DescribeEvidence(GoalRecoveryHelperTexts texts, GoalMemberEvidence evidence)
    {
        try
        {
            if (evidence.RunSuccesses + evidence.RunFailures + evidence.HistorySuccesses + evidence.HistoryFailures == 0)
                return Render(texts.EvidenceNone);

            return Render(
                texts.EvidenceSummary,
                ("RunSuccesses", evidence.RunSuccesses.ToString(CultureInfo.InvariantCulture)),
                ("RunFailures", evidence.RunFailures.ToString(CultureInfo.InvariantCulture)),
                ("HistorySuccesses", evidence.HistorySuccesses.ToString(CultureInfo.InvariantCulture)),
                ("HistoryFailures", evidence.HistoryFailures.ToString(CultureInfo.InvariantCulture)));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Describing goal recovery evidence failed for {ModelName}.", evidence?.ModelName);
            throw;
        }
    }

    /// <summary>
    /// Returns the local user's run-scoped decision on letting other members answer for failed members of a paired role.
    /// The request is created once per role and run; a decision is remembered for the run so a consumed approval never creates a new card.
    /// </summary>
    /// <param name="context">Failed slot of the paired role.</param>
    /// <param name="state">Recovery state of the run.</param>
    /// <param name="candidate">Currently preferred substitute, shown in the request.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The remembered or current decision.</returns>
    private async Task<GoalRecoveryApprovalMemo> GetPairedSubstitutionApprovalAsync(
        GoalMemberRecoveryContext context,
        GoalRecoveryRunState state,
        string candidate,
        CancellationToken cancellationToken)
    {
        var roleDigest = Digest(context.RoleName);
        var operationKey = $"council.recovery.paired-substitution.{context.RunId:N}.{roleDigest}";
        var memo = state.Approvals.GetOrAdd(operationKey, _ => new GoalRecoveryApprovalMemo());
        try
        {
            if (memo.IsDecided)
                return memo;

            var texts = Texts;
            memo.Spec ??= new HumanApprovalRequestSpec(
                CorrelationId: $"council:{context.RunId:N}:paired-substitution:{roleDigest}",
                OperationKey: operationKey,
                Title: Render(texts.PairedSubstitutionTitle, ("Role", context.RoleName)),
                Description: Render(
                    texts.PairedSubstitutionDescription,
                    ("Model", context.FailedModel),
                    ("Role", context.RoleName),
                    ("PairedRole", context.PairedRoleName),
                    ("Partner", context.PairedParticipant),
                    ("Candidate", candidate)),
                RiskLevel: "Low",
                Source: nameof(GoalRecoveryHelperService),
                RequestedBy: texts.ApprovalRequestedBy,
                RequestedRole: texts.ApprovalRequestedRole,
                CouncilRunId: context.RunId,
                EarliestCouncilRound: 0,
                RequiredBeforeCompletion: false,
                IsSensitive: false,
                SuggestedResponsesText: texts.PairedSubstitutionSuggestedResponses,
                ResponsePrompt: texts.PairedSubstitutionResponsePrompt,
                AllowFreeText: true);
            var gate = await humanCollaboration.AuthorizeOrEnqueueAsync(memo.Spec, cancellationToken: cancellationToken).ConfigureAwait(false);
            ApplyApprovalGate(memo, gate);
            logger.LogInformation(
                "Goal recovery paired-substitution decision for run {RunId}, role {RoleName}: {Status} (request {RequestId}).",
                context.RunId,
                context.RoleName,
                gate.Status,
                gate.RequestId);
            return memo;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // A broken inbox must not stall the run: the slot stays empty and the failure remains visible.
            logger.LogWarning(exception, "Goal recovery could not read or queue the paired-substitution decision for run {RunId}, role {RoleName}; the slot stays empty for now.", context.RunId, context.RoleName);
            return memo;
        }
    }

    /// <summary>Records a Human Collaboration gate result in a run-scoped decision memo.</summary>
    /// <param name="memo">Decision memo.</param>
    /// <param name="gate">Gate result returned by the inbox.</param>
    private void ApplyApprovalGate(GoalRecoveryApprovalMemo memo, HumanApprovalGateResult gate)
    {
        try
        {
            memo.RequestId = gate.RequestId ?? memo.RequestId;
            if (gate.IsAuthorized || gate.IsDeclined)
            {
                memo.IsAuthorized = gate.IsAuthorized;
                memo.IsDecided = true;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Recording a goal recovery approval result failed.");
            throw;
        }
    }

    /// <summary>Logs a member recovery decision with its evidence and returns it unchanged.</summary>
    /// <param name="context">Failed slot.</param>
    /// <param name="decision">Decision to log.</param>
    /// <returns>The same decision.</returns>
    private GoalMemberRecoveryDecision LogDecision(GoalMemberRecoveryContext context, GoalMemberRecoveryDecision decision)
    {
        try
        {
            logger.LogInformation(
                "Goal recovery for run {RunId}, step {StepKey}, round {Round}, role {RoleName}, attempt {Attempt}: {FailedModel} ({FailureKind}) -> {Action} {ChosenModel}. {Reason}. Candidates: {Candidates}",
                context.RunId,
                context.WorkflowStepKey,
                context.Round,
                context.RoleName,
                context.AttemptNumber,
                context.FailedModel,
                context.FailureKind,
                decision.Action,
                decision.ModelName,
                decision.Reason,
                decision.Evidence.Count == 0
                    ? "none"
                    : string.Join("; ", decision.Evidence.Select(item =>
                        $"{item.ModelName} [{(item.InRolePool ? "role" : "run")}, host {item.HostKey}{(item.HostAvailable ? string.Empty : " unavailable")}, run {item.RunSuccesses}/{item.RunFailures}, history {item.HistorySuccesses}/{item.HistoryFailures}]")));
            return decision;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Logging a goal recovery decision failed for run {RunId}.", context?.RunId);
            throw;
        }
    }

    /// <summary>Returns the member outcomes of recent retained runs, read once per run.</summary>
    /// <param name="runId">Run that asks for evidence; it is left out of the history.</param>
    /// <param name="state">Recovery state of the run.</param>
    /// <returns>Retained member outcomes.</returns>
    private IReadOnlyList<CouncilMemberOutcome> GetHistory(Guid runId, GoalRecoveryRunState state)
    {
        try
        {
            lock (state.Sync)
            {
                if (state.History is null)
                {
                    var takeRuns = ExecutionParameters.MemberRecoveryHistoryRuns;
                    state.History = takeRuns <= 0
                        ? Array.Empty<CouncilMemberOutcome>()
                        : councilSpooler.GetMemberOutcomes(takeRuns, runId);
                    logger.LogDebug("Goal recovery read {OutcomeCount} retained member outcome(s) from up to {RunCount} recent run(s) for run {RunId}.", state.History.Count, takeRuns, runId);
                }

                return state.History;
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Goal recovery could not read retained Council history for run {RunId}; only this run's evidence is used.", runId);
            return Array.Empty<CouncilMemberOutcome>();
        }
    }

    /// <summary>Returns whether a step role belongs to the configured role, including recovery-suffixed role labels.</summary>
    /// <param name="stepRole">Role label of a step.</param>
    /// <param name="roleName">Configured role name.</param>
    /// <returns><see langword="true"/> when the step was produced for the role.</returns>
    private bool IsSameRole(string? stepRole, string roleName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(stepRole) || string.IsNullOrWhiteSpace(roleName))
                return false;
            return string.Equals(stepRole, roleName, StringComparison.OrdinalIgnoreCase)
                || stepRole.StartsWith(roleName + " (", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Goal recovery role comparison failed for role {RoleName}.", roleName);
            throw;
        }
    }

    /// <summary>Returns the AI host identity of a member from the caller-supplied host map.</summary>
    /// <param name="context">Failed slot with its host map.</param>
    /// <param name="member">Provider-qualified member.</param>
    /// <returns>The host key, or an empty string when the member is unknown.</returns>
    private string HostOf(GoalMemberRecoveryContext context, string member)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(member) && context.HostKeys.TryGetValue(member, out var hostKey)
                ? hostKey ?? string.Empty
                : string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Goal recovery host lookup failed for {ModelName}.", member);
            throw;
        }
    }

    /// <summary>Returns the recovery state of a run and evicts the oldest runs above <see cref="MaximumTrackedRuns"/>.</summary>
    /// <param name="runId">Council run identifier.</param>
    /// <returns>The run's recovery state.</returns>
    private GoalRecoveryRunState GetRunState(Guid runId)
    {
        try
        {
            var state = runs.GetOrAdd(runId, _ => new GoalRecoveryRunState());
            if (runs.Count > MaximumTrackedRuns)
            {
                foreach (var stale in runs
                             .Where(item => item.Key != runId)
                             .OrderBy(item => item.Value.CreatedAtUtc)
                             .Take(runs.Count - MaximumTrackedRuns)
                             .ToList())
                {
                    runs.TryRemove(stale.Key, out _);
                }
            }

            return state;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Goal recovery state lookup failed for run {RunId}.", runId);
            throw;
        }
    }

    /// <summary>Replaces <c>{Name}</c> placeholders of a configurable template.</summary>
    /// <param name="template">Template from the helper texts.</param>
    /// <param name="values">Placeholder names and values.</param>
    /// <returns>The rendered text.</returns>
    private string Render(string? template, params (string Name, string? Value)[] values)
    {
        try
        {
            var builder = new StringBuilder(template ?? string.Empty);
            foreach (var (name, value) in values)
                builder.Replace("{" + name + "}", value ?? string.Empty);
            return builder.ToString().Trim();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rendering a goal recovery text failed.");
            throw;
        }
    }

    /// <summary>Creates a short stable digest for operation keys.</summary>
    /// <param name="value">Value to digest.</param>
    /// <returns>Sixteen lowercase hexadecimal characters.</returns>
    private string Digest(string? value)
    {
        try
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes((value ?? string.Empty).Trim().ToLowerInvariant()));
            return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating a goal recovery digest failed.");
            throw;
        }
    }

    /// <summary>Recovery state of one Council run.</summary>
    private sealed class GoalRecoveryRunState
    {
        /// <summary>Gets the creation time used for eviction.</summary>
        /// <value>UTC creation time.</value>
        public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;
        /// <summary>Gets the lock for lazily loaded run data.</summary>
        /// <value>Lock object.</value>
        public object Sync { get; } = new();
        /// <summary>Gets the availability state per AI host.</summary>
        /// <value>Host states keyed by host identity.</value>
        public ConcurrentDictionary<string, GoalRecoveryHostState> Hosts { get; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Gets the remembered decisions per operation key.</summary>
        /// <value>Decision memos keyed by operation key.</value>
        public ConcurrentDictionary<string, GoalRecoveryApprovalMemo> Approvals { get; } = new(StringComparer.Ordinal);
        /// <summary>Gets or sets the retained member outcomes of recent runs.</summary>
        /// <value>Outcomes, or <see langword="null"/> before the first read.</value>
        public IReadOnlyList<CouncilMemberOutcome>? History { get; set; }
    }

    /// <summary>Run-scoped decision on a Human Collaboration request.</summary>
    private sealed class GoalRecoveryApprovalMemo
    {
        /// <summary>Gets or sets the exact request that was queued, reused for every later lookup.</summary>
        /// <value>The request spec, or <see langword="null"/> before the first lookup.</value>
        public HumanApprovalRequestSpec? Spec { get; set; }
        /// <summary>Gets or sets whether the user approved or declined.</summary>
        /// <value><see langword="true"/> once decided.</value>
        public bool IsDecided { get; set; }
        /// <summary>Gets or sets whether the decision approved the operation.</summary>
        /// <value><see langword="true"/> for an approval.</value>
        public bool IsAuthorized { get; set; }
        /// <summary>Gets or sets the inbox request identifier.</summary>
        /// <value>The request identifier, when known.</value>
        public Guid? RequestId { get; set; }
    }
}

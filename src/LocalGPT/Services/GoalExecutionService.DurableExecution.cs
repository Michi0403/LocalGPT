using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Durable subgoal execution and conservative restart/replan semantics above the existing Council.</summary>
public sealed partial class GoalExecutionService
{
    /// <summary>Captures a caller request into a recoverable, credential-free workflow snapshot.</summary>
    /// <param name="request">Caller-owned request.</param>
    /// <param name="plan">Validated goal plan.</param>
    /// <returns>Initial journal state.</returns>
    private GoalExecutionCheckpoint CreateCheckpoint(GoalExecutionRequest request, GoalExecutionResult plan)
    {
        var councilRequest = request.CouncilRequest;
        return new GoalExecutionCheckpoint
        {
            GoalId = councilRequest.RunId,
            GoalText = plan.GoalText,
            Prompt = councilRequest.Prompt,
            RequestedTeamKey = councilRequest.CouncilTeamKey,
            PreserveSelectedTeam = request.PreserveSelectedTeam,
            KeyaccountReviewEnabled = request.KeyaccountReviewEnabled,
            KeyaccountContinuationLimit = Math.Clamp(request.KeyaccountContinuationLimit > 0
                ? request.KeyaccountContinuationLimit : runtimePolicy.GetInt("GoalKeyaccountDefaultContinuations"), 0, 100),
            KeyaccountExpectedArtifacts = Math.Clamp(councilRequest.KeyaccountExpectedArtifacts, 0, 100000),
            ModelSelectionKeys = plan.SelectedModels.ToList(),
            RequiredFunctions = request.RequiredFunctions.ToList(),
            RequiredCapabilities = request.RequiredCapabilities.Concat(councilRequest.RequestedOrganicCapabilities)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            MaxRounds = councilRequest.MaxRounds,
            MaxOutputTokens = councilRequest.MaxOutputTokens,
            MaxContextTokens = councilRequest.MaxContextTokens,
            ModelRoutes = councilRequest.ModelRoutes.ToList(),
            MaxParallelModels = councilRequest.MaxParallelModels,
            ModelTimeoutSeconds = councilRequest.ModelTimeoutSeconds,
            ResourceLoadPercent = councilRequest.ResourceLoadPercent,
            AllowParallelHardwareRoads = councilRequest.AllowParallelHardwareRoads,
            OllamaKeepAlive = councilRequest.OllamaKeepAlive,
            OllamaNumGpu = councilRequest.OllamaNumGpu,
            IncludeMemory = councilRequest.IncludeMemory,
            SaveToMemory = councilRequest.SaveToMemory,
            UseChangeReviewWorkflow = councilRequest.UseChangeReviewWorkflow,
            ProjectTopicId = councilRequest.ProjectTopicId,
            ProjectRevisionId = councilRequest.ProjectRevisionId,
            OneWireCorrelationId = councilRequest.OneWireCorrelationId,
            UseOrganicCouncilWorkflow = councilRequest.UseOrganicCouncilWorkflow,
            AttachedWorkspaceNames = councilRequest.AttachedWorkspaceNames.ToList(),
            ProjectId = councilRequest.ProjectId,
            ExternalProjectContextJson = councilRequest.ExternalProjectContextJson,
            Steps = plan.PlanSteps.ToList(),
            PlannerSource = plan.PlannerSource,
            State = plan.MissingRequirements.Count == 0 ? GoalExecutionState.Running : GoalExecutionState.Blocked
        };
    }

    /// <summary>Reconstructs a goal without restoring ephemeral callbacks, credentials or prior human approvals.</summary>
    /// <param name="checkpoint">Committed goal state.</param>
    /// <returns>Safe request for the next Council subgoal.</returns>
    private GoalExecutionRequest RestoreRequest(GoalExecutionCheckpoint checkpoint) => new()
    {
        GoalText = checkpoint.GoalText,
        PreserveSelectedTeam = checkpoint.PreserveSelectedTeam,
        KeyaccountReviewEnabled = checkpoint.KeyaccountReviewEnabled,
        KeyaccountContinuationLimit = checkpoint.KeyaccountContinuationLimit,
        RequiredFunctions = checkpoint.RequiredFunctions.ToList(),
        RequiredCapabilities = checkpoint.RequiredCapabilities.ToList(),
        CouncilRequest = new MultiModelCouncilRequest
        {
            RunId = checkpoint.GoalId,
            ChatRunId = checkpoint.GoalId,
            Prompt = checkpoint.Prompt,
            GoalText = checkpoint.GoalText,
            KeyaccountReviewEnabled = checkpoint.KeyaccountReviewEnabled,
            KeyaccountContinuationLimit = checkpoint.KeyaccountContinuationLimit,
            KeyaccountExpectedArtifacts = checkpoint.KeyaccountExpectedArtifacts,
            CouncilTeamKey = checkpoint.RequestedTeamKey,
            MaxRounds = checkpoint.MaxRounds,
            MaxOutputTokens = checkpoint.MaxOutputTokens,
            MaxContextTokens = checkpoint.MaxContextTokens,
            ModelRoutes = checkpoint.ModelRoutes.ToList(),
            MaxParallelModels = checkpoint.MaxParallelModels,
            ModelTimeoutSeconds = checkpoint.ModelTimeoutSeconds,
            ResourceLoadPercent = checkpoint.ResourceLoadPercent,
            AllowParallelHardwareRoads = checkpoint.AllowParallelHardwareRoads,
            OllamaKeepAlive = checkpoint.OllamaKeepAlive,
            OllamaNumGpu = checkpoint.OllamaNumGpu,
            IncludeMemory = checkpoint.IncludeMemory,
            SaveToMemory = checkpoint.SaveToMemory,
            UseChangeReviewWorkflow = checkpoint.UseChangeReviewWorkflow,
            ProjectTopicId = checkpoint.ProjectTopicId,
            ProjectRevisionId = checkpoint.ProjectRevisionId,
            OneWireCorrelationId = checkpoint.OneWireCorrelationId,
            UseOrganicCouncilWorkflow = checkpoint.UseOrganicCouncilWorkflow,
            RequestedOrganicCapabilities = checkpoint.RequiredCapabilities.ToList(),
            AttachedWorkspaceNames = checkpoint.AttachedWorkspaceNames.ToList(),
            ProjectId = checkpoint.ProjectId,
            ExternalProjectContextJson = checkpoint.ExternalProjectContextJson
        }
    };

    /// <summary>Progresses past committed subgoals; never automatically replays interrupted or uncertain side effects.</summary>
    /// <param name="request">Current live caller, or a safe restored request.</param>
    /// <param name="checkpoint">Latest persisted state.</param>
    /// <param name="retryInterrupted">Explicit approval to retry potentially consequential interrupted Council work.</param>
    /// <param name="cancellationToken">Cancels execution and checkpoint writes.</param>
    /// <returns>Current goal outcome and last actual Council result if available.</returns>
    private async Task<GoalExecutionResult> ContinueAsync(GoalExecutionRequest request,
        GoalExecutionCheckpoint checkpoint, bool retryInterrupted, CancellationToken cancellationToken)
    {
        var outcome = new GoalExecutionResult
        {
            GoalId = checkpoint.GoalId,
            GoalText = checkpoint.GoalText,
            SelectedTeamKey = checkpoint.Steps.FirstOrDefault()?.TeamKey ?? checkpoint.RequestedTeamKey,
            SelectedModels = checkpoint.ModelSelectionKeys.ToList(),
            PlannerSource = checkpoint.PlannerSource,
            PlanSteps = checkpoint.Steps.ToList(),
            State = checkpoint.State,
            Checkpoint = checkpoint
        };
        if (checkpoint.State == GoalExecutionState.Completed || checkpoint.State == GoalExecutionState.NeedsVerification)
        {
            outcome.Evidence = checkpoint.Receipts.SelectMany(receipt => receipt.Evidence).ToList();
            outcome.CouncilResult = ReconstructResult(checkpoint);
            return outcome;
        }
        if ((checkpoint.State is GoalExecutionState.NeedsApproval or GoalExecutionState.Failed or GoalExecutionState.Interrupted)
            && !retryInterrupted)
        {
            outcome.State = GoalExecutionState.NeedsApproval;
            outcome.MissingRequirements.Add("GoalRecoveryRequiresExplicitApproval");
            return outcome;
        }
        if (checkpoint.State == GoalExecutionState.Blocked)
        {
            outcome.MissingRequirements.Add("ReplanAfterCapabilitiesBecomeAvailable");
            return outcome;
        }
        var interrupted = checkpoint.Receipts.FirstOrDefault(receipt =>
            receipt.State is GoalExecutionState.Interrupted or GoalExecutionState.Running or GoalExecutionState.NeedsApproval);
        if (interrupted is not null && !retryInterrupted)
        {
            checkpoint.State = GoalExecutionState.NeedsApproval;
            await ledger.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
            outcome.State = GoalExecutionState.NeedsApproval;
            outcome.MissingRequirements.Add("InterruptedCouncilStepRequiresExplicitRetryApproval");
            return outcome;
        }
        // Provider identities are resolved from the live runtime after restart, not from persisted endpoints.
        if (request.CouncilRequest.ModelSelections.Count == 0 && request.CouncilRequest.ModelNames.Count == 0)
        {
            var choices = new List<ProviderModelReference>();
            foreach (var modelKey in checkpoint.ModelSelectionKeys)
            {
                try { choices.Add(await providerModels.ResolveAsync(modelKey, cancellationToken).ConfigureAwait(false)); }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    logger.LogWarning(error, "Goal {GoalId} cannot resolve its previously selected model.", checkpoint.GoalId);
                    checkpoint.State = GoalExecutionState.Blocked;
                    await ledger.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
                    outcome.State = checkpoint.State;
                    outcome.MissingRequirements.Add("PreviouslySelectedModelUnavailable");
                    return outcome;
                }
            }
            request.CouncilRequest.ModelSelections = choices;
            request.CouncilRequest.ModelNames = choices.Select(choice => choice.ModelName).ToList();
        }
        var configuredTeams = await teams.GetTeamsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var liveTeams = configuredTeams.Select(team => team.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var directorySnapshot = await directory.SynchronizeAsync(cancellationToken).ConfigureAwait(false);
        var executableFunctions = directorySnapshot.Functions.Where(function => function.AvailableToAi &&
            !function.IsCoordinationOnly && (function.SupportsAutomaticInvocation || function.SupportsDirectInvocation || function.SupportsDeferredApprovalRequest))
            .Select(function => function.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        // Live 1-Wire capabilities are distinct from LocalGPT organic skill names.
        // A connected PublisherStudio advertises publisher.* keys in its peer capability catalog.
        var executableCapabilities = directorySnapshot.Skills.Where(skill => skill.IsEnabled && skill.IsOnline)
            .Select(skill => skill.Key)
            .Concat(wirePeers.GetPeers()
                .Where(peer => wireConnections.IsConnected(peer.PeerId))
                .SelectMany(peer => peer.Capabilities)
                .Where(capability => capability.IsEnabled && capability.IsOnline)
                .Select(capability => capability.Key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (checkpoint.RequiredFunctions.Any(function => !executableFunctions.Contains(function)) ||
            checkpoint.RequiredCapabilities.Any(capability => !executableCapabilities.Contains(capability)))
        {
            checkpoint.State = GoalExecutionState.Blocked;
            await ledger.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
            outcome.State = GoalExecutionState.Blocked;
            outcome.MissingRequirements.Add("RequiredExecutionCapabilitiesUnavailable");
            return outcome;
        }
        var originalRunId = request.CouncilRequest.RunId;
        request.CouncilRequest.ChatRunId ??= checkpoint.GoalId;
        var originalPrompt = request.CouncilRequest.Prompt;
        var originalTeam = request.CouncilRequest.CouncilTeamKey;
        var originalOutputBudget = request.CouncilRequest.MaxOutputTokens;
        var originalIncludeMemory = request.CouncilRequest.IncludeMemory;
        var originalCompactFlag = request.CouncilRequest.GoalRecoveryCompactContext;
        var originalContextBudget = request.CouncilRequest.MaxContextTokens;
        var originalLoad = request.CouncilRequest.ResourceLoadPercent;
        try
        {
            for (var stepIndex = 0; stepIndex < checkpoint.Steps.Count; stepIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var step = checkpoint.Steps[stepIndex];
                var developmentTeam = configuredTeams.Any(team => string.Equals(team.Key, step.TeamKey, StringComparison.OrdinalIgnoreCase) &&
                    (team.RequiredCompletedFunctions ?? []).Contains("council.development.artifact.verify", StringComparer.OrdinalIgnoreCase));
                var saved = checkpoint.Receipts.LastOrDefault(receipt => receipt.StepKey == step.Key);
                if (saved?.State == GoalExecutionState.Completed) continue;
                // Human-approved extension may proceed to a distinct later objective without
                // replaying an earlier unverifiable effect or pretending it was verified.
                if (saved?.State == GoalExecutionState.NeedsVerification &&
                    (checkpoint.KeyaccountContinuationApproved || IsApprovalGatedDeliveryGoal(checkpoint)) &&
                    stepIndex < checkpoint.Steps.Count - 1)
                    continue;
                if (!liveTeams.Contains(step.TeamKey))
                {
                    // A team removed before the next subgoal is safe to replan: no new Council call occurred.
                    if (await TryReplanRemainingAsync(request, checkpoint, stepIndex, liveTeams,
                        cancellationToken).ConfigureAwait(false))
                    {
                        outcome.PlanSteps = checkpoint.Steps.ToList();
                        stepIndex--;
                        continue;
                    }
                    checkpoint.State = GoalExecutionState.Blocked;
                    outcome.MissingRequirements.Add("PlannedCouncilTeamUnavailable");
                    break;
                }
                if (saved is not null && saved.State == GoalExecutionState.NeedsVerification)
                {
                    checkpoint.State = GoalExecutionState.NeedsVerification;
                    break;
                }
                if (saved is not null && saved.State == GoalExecutionState.Interrupted && !retryInterrupted)
                {
                    checkpoint.State = GoalExecutionState.NeedsApproval;
                    break;
                }
                // A retry is a NEW Council invocation with its own identity. Keep the earlier
                // uncertain/failed attempt as immutable history instead of overwriting evidence.
                var retrying = saved is not null &&
                    (saved.State is GoalExecutionState.Interrupted or GoalExecutionState.NeedsApproval or GoalExecutionState.Failed);
                var councilRunId = retrying ? Guid.NewGuid() : saved?.CouncilRunId
                    ?? (stepIndex == 0 && checkpoint.Receipts.Count == 0 ? checkpoint.GoalId : Guid.NewGuid());
                var receipt = retrying || saved is null
                    ? new GoalExecutionStepReceipt { StepKey = step.Key, CouncilRunId = councilRunId }
                    : saved;
                if (retrying || saved is null) checkpoint.Receipts.Add(receipt);
                receipt.State = GoalExecutionState.Running;
                receipt.UpdatedAtUtc = DateTime.UtcNow;
                checkpoint.State = GoalExecutionState.Running;
                await ledger.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false); // write-ahead record
                request.CouncilRequest.RunId = councilRunId;
                request.CouncilRequest.CouncilTeamKey = step.TeamKey;
                var recoveryStep = IsGoalContextRecoveryStep(checkpoint, step);
                request.CouncilRequest.Prompt = recoveryStep
                    ? RenderGoalContextRecoveryPrompt(checkpoint, step, stepIndex)
                    : checkpoint.Steps.Count == 1 && !step.Key.StartsWith("source-manifest-", StringComparison.Ordinal) ? originalPrompt :
                        RenderStepPrompt(checkpoint, step, stepIndex);
                request.CouncilRequest.MaxOutputTokens = recoveryStep
                    ? (step.Key.StartsWith(ContextFitStepPrefix, StringComparison.OrdinalIgnoreCase) ? originalOutputBudget : FitGoalRecoveryOutputBudget(checkpoint))
                    : originalOutputBudget;
                request.CouncilRequest.GoalRecoveryCompactContext = recoveryStep;
                if (recoveryStep &&
                    !string.Equals(runtimePolicy.GetString("GoalExecutionResourceMode"), "Manual", StringComparison.OrdinalIgnoreCase))
                {
                    // The full history remains durable. Only this recovered inference
                    // excludes unrelated memory to make space for registered DX tools.
                    request.CouncilRequest.IncludeMemory = false;
                    AnnounceGoalAgent(request, "GoalExecution AI · Context steward",
                        "Sending a bounded recovery work packet without replaying the old chat memory.");
                }
                await gameBootstrap.EnsureSessionAsync(request.CouncilRequest, cancellationToken).ConfigureAwait(false);
                MultiModelCouncilResult councilResult;
                try
                {
                    councilResult = await council.RunAsync(request.CouncilRequest, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    receipt.State = GoalExecutionState.Interrupted;
                    checkpoint.State = GoalExecutionState.Interrupted;
                    await ledger.SaveAsync(checkpoint, CancellationToken.None).ConfigureAwait(false);
                    throw;
                }
                catch (Exception error)
                {
                    logger.LogError(error, "Goal {GoalId} Council step {Step} failed.", checkpoint.GoalId, step.Key);
                    receipt.State = GoalExecutionState.Interrupted;
                    checkpoint.State = GoalExecutionState.NeedsApproval;
                    await ledger.SaveAsync(checkpoint, CancellationToken.None).ConfigureAwait(false);
                    outcome.MissingRequirements.Add("CouncilStepFailedWithUnknownEffects");
                    break;
                }
                receipt.Answer = councilResult.FinalAnswer;
                receipt.Evidence = ObserveCouncil(councilResult);
                var requiresDevelopmentArtifact = developmentTeam && councilResult.CoordinatedSourceWork;
                if (requiresDevelopmentArtifact)
                    checkpoint.KeyaccountExpectedArtifacts = Math.Max(1, checkpoint.KeyaccountExpectedArtifacts);
                var verifiedDevelopmentArtifact = requiresDevelopmentArtifact && councilResult.DevelopmentArtifactVerified &&
                    receipt.Evidence.Any(item => item.Kind == "CouncilArtifact" && item.Succeeded);
                var approvalGatedDelivery = IsApprovalGatedDeliveryGoal(checkpoint);
                if (approvalGatedDelivery)
                    receipt.Evidence.AddRange(await ObserveApprovalGatedRequestsAsync(councilResult.RunId, cancellationToken).ConfigureAwait(false));
                // Council's public result does not expose a complete invocation-by-invocation
                // write receipt. Without that proof, an enabled consequential function is a
                // possible side effect even when no artifact was returned.
                // GoalExecution, not the Ollama provider, owns the continuation decision.
                // Function *availability* is not evidence that a function executed. A Council
                // preflight rejection that contains no function/approval/artifact receipt is
                // safe to replan. All other ambiguous attempts retain the conservative guard.
                var confirmedSideEffectFreeContextFailure =
                    IsGoalContextPreflightFailureWithoutEffects(councilResult, receipt);
                var safeManifestRetry = requiresDevelopmentArtifact && councilResult.SourceManifestRetrySafe &&
                    councilResult.Artifacts.Count == 0 && councilResult.ChangeReview is null;
                receipt.ExternalEffectsPossible = !confirmedSideEffectFreeContextFailure && !safeManifestRetry &&
                    (councilResult.Artifacts.Count > 0 ||
                     checkpoint.RequiredFunctions.Count > 0 ||
                     directorySnapshot.Functions.Any(function => function.AvailableToAi && !function.IsCoordinationOnly &&
                         !function.IsReadOnly && (function.SupportsAutomaticInvocation || function.SupportsDirectInvocation)) ||
                     receipt.Evidence.Any(evidence => evidence.Kind == "VerifiedWorkspaceRead"));
                receipt.FailureSummary = councilResult.Steps.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Error))?.Error
                    ?? (string.IsNullOrWhiteSpace(councilResult.FinalAnswer) ? "EmptyCouncilResult" : string.Empty);
                // The Keyaccount Manager checks every Council subrun, regardless of its saved team workflow.
                // This is a read-only reviewer, not a replacement for tool or user approval receipts.
                GoalKeyaccountReview? keyaccountReview = null;
                if (checkpoint.KeyaccountReviewEnabled && !confirmedSideEffectFreeContextFailure)
                {
                    keyaccountReview = await ReviewKeyaccountCompletionAsync(request, checkpoint, step,
                        councilResult, receipt, cancellationToken).ConfigureAwait(false);
                    // Deterministic delivery evaluation for approval-gated 1-Wire goals: a missing delivery
                    // becomes the next objective instead of an unspecific human stop.
                    ApplyApprovalGatedDeliveryRule(checkpoint, keyaccountReview);
                    if (requiresDevelopmentArtifact && !verifiedDevelopmentArtifact)
                    {
                        keyaccountReview.Complete = false;
                        keyaccountReview.NeedsHumanReview = !safeManifestRetry;
                        keyaccountReview.Reason = runtimePolicy.GetString("GoalDevelopmentArtifactMissingNotice");
                        if (safeManifestRetry)
                            keyaccountReview.NextObjective = runtimePolicy.GetString("GoalDevelopmentManifestRetryObjective");
                    }
                    AnnounceGoalAgent(request, "GoalExecution AI · Keyaccount review", System.Text.Json.JsonSerializer.Serialize(keyaccountReview));
                    checkpoint.KeyaccountReviews.Add(keyaccountReview);
                    checkpoint.KeyaccountPendingObjective = keyaccountReview.Complete
                        ? string.Empty : keyaccountReview.NextObjective;
                }
                receipt.State = verifiedDevelopmentArtifact ? GoalExecutionState.Completed :
                    (requiresDevelopmentArtifact || string.IsNullOrWhiteSpace(councilResult.FinalAnswer) ||
                    councilResult.Steps.Any(item => !string.IsNullOrWhiteSpace(item.Error)))
                    ? GoalExecutionState.Failed
                    : councilResult.Artifacts.Count > 0 || checkpoint.RequiredFunctions.Count > 0 ||
                      checkpoint.RequiredCapabilities.Count > 0
                        ? GoalExecutionState.NeedsVerification : GoalExecutionState.Completed;
                receipt.UpdatedAtUtc = DateTime.UtcNow;
                await ledger.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false); // completion commit before next step
                outcome.CouncilResult = councilResult;
                if (receipt.State == GoalExecutionState.Failed)
                {
                    // A missing artifact is a failed engineering objective even if all model streams completed.
                    // Only a pre-mutation manifest failure may be retried as a new bounded Council attempt.
                    if (safeManifestRetry && checkpoint.KeyaccountContinuationsScheduled < checkpoint.KeyaccountContinuationLimit &&
                        stepIndex == checkpoint.Steps.Count - 1)
                    {
                        var recovery = await aiHelpEscalator.ReportAttemptAsync(new AiHelpAttemptReport
                        {
                            TaskId = $"goal:{checkpoint.GoalId:N}:source-manifest", AttemptId = councilResult.RunId,
                            Task = checkpoint.GoalText[..Math.Min(2000, checkpoint.GoalText.Length)], Action = "source manifest",
                            Status = "failed", CouncilRunId = councilResult.RunId,
                            Error = new AiHelpFailureEvidence { Code = "SourceManifestMissing", Message = runtimePolicy.GetString("GoalDevelopmentArtifactMissingNotice"), Source = "validator" }
                        }, cancellationToken).ConfigureAwait(false);
                        AnnounceGoalAgent(request, "Nurse · Source manifest recovery", System.Text.Json.JsonSerializer.Serialize(recovery));
                        if (recovery.Type is "try_alternative" or "retry_with_guidance")
                        {
                            // Replace only the unsuccessful plan step. Its failed receipt remains in the journal.
                            checkpoint.KeyaccountContinuationsScheduled++;
                            checkpoint.Steps[stepIndex] = new GoalExecutionPlanStep
                            {
                                Key = $"source-manifest-{checkpoint.KeyaccountContinuationsScheduled}", TeamKey = step.TeamKey,
                                Objective = runtimePolicy.GetString("GoalDevelopmentManifestRetryObjective") + "\n" + recovery.Instruction
                            };
                            outcome.PlanSteps = checkpoint.Steps.ToList();
                            await ledger.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
                            stepIndex--;
                            continue;
                        }
                    }
                    // The goal observer handles a recoverable context problem *before* the
                    // generic failed-step and human-approval branches. It can ask the existing
                    // semantic planner for a valid subagent/team decomposition, then fall back
                    // to one bounded, focused retry of the originally selected team.
                    if (confirmedSideEffectFreeContextFailure &&
                        await TryRecoverGoalContextFailureAsync(request, checkpoint, stepIndex,
                            liveTeams, cancellationToken).ConfigureAwait(false))
                    {
                        outcome.PlanSteps = checkpoint.Steps.ToList();
                        stepIndex--;
                        continue;
                    }
                    if (!requiresDevelopmentArtifact && !confirmedSideEffectFreeContextFailure && !receipt.ExternalEffectsPossible &&
                        await TryReplanRemainingAsync(request, checkpoint, stepIndex,
                            liveTeams, cancellationToken).ConfigureAwait(false))
                    {
                        outcome.PlanSteps = checkpoint.Steps.ToList();
                        stepIndex--; // retry only newly proposed remaining work; never replay committed effects
                        continue;
                    }
                    checkpoint.State = receipt.ExternalEffectsPossible ? GoalExecutionState.NeedsApproval : GoalExecutionState.Failed;
                    // Persist the terminal/approval-gated checkpoint before contacting an AI curator.
                    // A disconnect or cancelled model call must not leave this goal marked running.
                    await ledger.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
                    // Existing durable journal and explicit authorization remain authoritative.
                    // A central recovery report diagnoses the exhausted objective but never
                    // resumes an uncertain tool action or changes its approval requirement.
                    try
                    {
                        var taskId = $"goal:{checkpoint.GoalId:N}:{System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(step.Key)))}";
                        var objective = step.Objective ?? string.Empty;
                        var helpDecision = await aiHelpEscalator.ReportAttemptAsync(new AiHelpAttemptReport
                        {
                            TaskId = taskId,
                            AttemptId = receipt.CouncilRunId,
                            Task = checkpoint.GoalText[..Math.Min(2000, checkpoint.GoalText.Length)],
                            Action = objective.Length == 0 ? "Council goal step" : objective[..Math.Min(500, objective.Length)],
                            Status = "failed",
                            CouncilRunId = councilResult.RunId,
                            Error = new AiHelpFailureEvidence
                            {
                                Code = "GoalStepRecoveryExhausted",
                                Message = "The goal step did not produce verified completion after the allowed recovery paths.",
                                Source = "validator"
                            }
                        }, cancellationToken).ConfigureAwait(false);
                        logger.LogInformation("Goal {GoalId} exhausted step {StepKey}; AI help decision {DecisionType}, human request {HumanRequestId}.",
                            checkpoint.GoalId, step.Key, helpDecision.Type, helpDecision.HumanRequestId);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                    catch (Exception error)
                    {
                        logger.LogWarning(error, "AI help escalation unavailable after failed goal step {StepKey} in goal {GoalId}; the saved failure remains authoritative.",
                            step.Key, checkpoint.GoalId);
                    }
                    break;
                }
                if (receipt.State == GoalExecutionState.NeedsVerification && !approvalGatedDelivery)
                {
                    checkpoint.State = GoalExecutionState.NeedsVerification;
                    break;
                }
                if (receipt.State == GoalExecutionState.NeedsVerification)
                {
                    // Approval-gated delivery goals: the effect of this step is applied only after the local human
                    // and the receiving application authorize it, so the remaining distinct planned work continues.
                    // Nothing is replayed; the goal still ends as NeedsVerification until the effect is confirmed.
                    logger.LogInformation("Goal {GoalId} continues after step {StepKey}; its external effect is approval gated and stays unverified.",
                        checkpoint.GoalId, step.Key);
                }
                // A completed text response does NOT by itself complete a larger user goal.
                // Append a NEW objective (not a replay of prior effects) only with an explicit budget.
                if (keyaccountReview is { Complete: false })
                {
                    var lastPlannedStep = stepIndex == checkpoint.Steps.Count - 1;
                    if (lastPlannedStep && !keyaccountReview.NeedsHumanReview &&
                        !string.IsNullOrWhiteSpace(keyaccountReview.NextObjective) &&
                        checkpoint.KeyaccountContinuationsScheduled < checkpoint.KeyaccountContinuationLimit &&
                        !string.Equals(keyaccountReview.NextObjective.Trim(), step.Objective.Trim(), StringComparison.OrdinalIgnoreCase) &&
                        !checkpoint.Steps.Any(item => string.Equals(item.Objective.Trim(),
                            keyaccountReview.NextObjective.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        var deliveryContinuation = approvalGatedDelivery && string.Equals(keyaccountReview.NextObjective.Trim(),
                            BuildDeliveryObjective(checkpoint), StringComparison.Ordinal);
                        checkpoint.Steps.Add(new GoalExecutionPlanStep
                        {
                            Key = (deliveryContinuation ? DeliveryStepKeyPrefix : "keyaccount-") +
                                (checkpoint.KeyaccountContinuationsScheduled + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                            TeamKey = step.TeamKey,
                            Objective = keyaccountReview.NextObjective.Trim()
                        });
                        logger.LogInformation("Goal {GoalId} Keyaccount Manager scheduled {Kind} continuation {Number} of {Limit}.",
                            checkpoint.GoalId, deliveryContinuation ? "delivery" : "objective",
                            checkpoint.KeyaccountContinuationsScheduled + 1, checkpoint.KeyaccountContinuationLimit);
                        checkpoint.KeyaccountContinuationsScheduled++;
                        checkpoint.KeyaccountPendingObjective = string.Empty;
                        await ledger.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    if (lastPlannedStep)
                    {
                        checkpoint.State = GoalExecutionState.NeedsVerification;
                        outcome.MissingRequirements.Add(keyaccountReview.NeedsHumanReview
                            ? "KeyaccountNeedsHumanReview" : "KeyaccountUnfinishedGoal");
                        break;
                    }
                }
                if (keyaccountReview is { ReviewerResponded: false } && stepIndex == checkpoint.Steps.Count - 1)
                {
                    checkpoint.State = GoalExecutionState.NeedsVerification;
                    outcome.MissingRequirements.Add("KeyaccountReviewerUnavailable");
                    break;
                }
            }
            if (checkpoint.State == GoalExecutionState.Running)
                checkpoint.State = checkpoint.Steps.All(step => checkpoint.Receipts.Any(receipt =>
                    receipt.StepKey == step.Key && receipt.State == GoalExecutionState.Completed))
                    ? GoalExecutionState.Completed : GoalExecutionState.NeedsVerification;
            await ledger.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
            outcome.Checkpoint = checkpoint;
            outcome.State = checkpoint.State;
            outcome.Evidence = checkpoint.Receipts.SelectMany(receipt => receipt.Evidence).ToList();
            outcome.CouncilResult ??= ReconstructResult(checkpoint);
            if (outcome.CouncilResult is not null)
            {
                outcome.CouncilResult.FinalAnswer = string.Join("\n\n", checkpoint.Receipts
                    .Where(receipt => receipt.State is GoalExecutionState.Completed or GoalExecutionState.NeedsVerification or GoalExecutionState.Failed)
                    .Select(receipt => receipt.Answer).Where(answer => !string.IsNullOrWhiteSpace(answer)));
                outcome.CouncilResult.DeliverableText = BuildDeliverableText(checkpoint);
                if (checkpoint.KeyaccountReviewEnabled)
                {
                    var latestReview = checkpoint.KeyaccountReviews.LastOrDefault();
                    var stateLabel = checkpoint.State == GoalExecutionState.Completed ?
                        "Review passed (model assessment, not external proof)" : "Unfinished: " + checkpoint.State;
                    outcome.CouncilResult.FinalAnswer += "\n\n## Keyaccount Manager for User\n" +
                        stateLabel + "\n" + (latestReview?.Reason ?? "Completion has not been reviewed") +
                        (string.IsNullOrWhiteSpace(checkpoint.KeyaccountPendingObjective) ? "" :
                            "\nNext objective: " + checkpoint.KeyaccountPendingObjective) +
                        "\nTechnical receipts and unfinished work are in the durable goal journal.";
                }
                outcome.CouncilResult.GoalExecution = new GoalExecutionSummary
                {
                    GoalId = checkpoint.GoalId,
                    PlannerSource = checkpoint.PlannerSource,
                    ReplanCount = checkpoint.ReplanCount,
                    CompletedSubgoals = checkpoint.Receipts.Count(receipt => receipt.State == GoalExecutionState.Completed),
                    TotalSubgoals = checkpoint.Steps.Count,
                    State = checkpoint.State,
                    TeamKey = outcome.SelectedTeamKey,
                    MissingRequirements = outcome.MissingRequirements.ToList(),
                    Evidence = outcome.Evidence.ToList(),
                    KeyaccountReview = checkpoint.KeyaccountReviews.LastOrDefault(),
                    PendingObjective = checkpoint.KeyaccountPendingObjective
                };
            }
            return outcome;
        }
        finally
        {
            request.CouncilRequest.RunId = originalRunId;
            request.CouncilRequest.Prompt = originalPrompt;
            request.CouncilRequest.CouncilTeamKey = originalTeam;
            request.CouncilRequest.MaxOutputTokens = originalOutputBudget;
            request.CouncilRequest.IncludeMemory = originalIncludeMemory;
            request.CouncilRequest.GoalRecoveryCompactContext = originalCompactFlag;
            request.CouncilRequest.MaxContextTokens = originalContextBudget;
            request.CouncilRequest.ResourceLoadPercent = originalLoad;
        }
    }

    /// <summary>Renders a database-owned Council subgoal briefing, preserving the original goal and prior verified work.</summary>
    /// <param name="checkpoint">Current execution journal.</param>
    /// <param name="step">Current subgoal.</param>
    /// <param name="stepIndex">Zero-based position.</param>
    /// <returns>Bounded step briefing.</returns>
    private string RenderStepPrompt(GoalExecutionCheckpoint checkpoint, GoalExecutionPlanStep step, int stepIndex)
    {
        var context = string.Join("\n", checkpoint.Receipts.Where(receipt => receipt.State == GoalExecutionState.Completed)
            .Select(receipt => receipt.StepKey + ": " + receipt.Answer));
        if (context.Length > 16000) context = context[^16000..];
        return runtimePolicy.GetString("GoalCouncilSubstepPromptTemplate")
            .Replace("{{OriginalGoal}}", checkpoint.GoalText, StringComparison.Ordinal)
            .Replace("{{ConversationContext}}", checkpoint.Prompt, StringComparison.Ordinal)
            .Replace("{{StepNumber}}", (stepIndex + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{{StepCount}}", checkpoint.Steps.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{{StepObjective}}", step.Objective, StringComparison.Ordinal)
            .Replace("{{PreviousOutputs}}", context, StringComparison.Ordinal);
    }

    /// <summary>Accepts only registered team keys when replanning unfinished work after a side-effect-free failure.</summary>
    /// <param name="request">Current caller constraints.</param>
    /// <param name="checkpoint">Durable plan and receipts.</param>
    /// <param name="failedIndex">Failed plan step position.</param>
    /// <param name="liveTeams">Current persisted team key set.</param>
    /// <param name="cancellationToken">Cancels replanning.</param>
    /// <returns>Whether a valid replacement for the remaining work was saved.</returns>
    private async Task<bool> TryReplanRemainingAsync(GoalExecutionRequest request, GoalExecutionCheckpoint checkpoint,
        int failedIndex, HashSet<string> liveTeams, CancellationToken cancellationToken)
    {
        if (checkpoint.ReplanCount >= Math.Max(0, runtimePolicy.GetInt("GoalMaximumReplans")))
            return false;
        var nextReplanNumber = checkpoint.ReplanCount + 1;
        var snapshot = await directory.SynchronizeAsync(cancellationToken).ConfigureAwait(false);
        var models = checkpoint.ModelSelectionKeys;
        var configured = await teams.GetTeamsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var last = checkpoint.Receipts.LastOrDefault(receipt => receipt.StepKey == checkpoint.Steps[failedIndex].Key);
        var contextFailure = IsGoalContextBudgetError(last?.FailureSummary);
        var cause = System.Text.Json.JsonSerializer.Serialize(new
        {
            contextFailure,
            maximumContextTokens = checkpoint.MaxContextTokens,
            resourceLoadPercent = checkpoint.ResourceLoadPercent,
            remainingSteps = checkpoint.Steps.Skip(failedIndex).Select(step => new { step.Objective, step.TeamKey }),
            failedObjective = checkpoint.Steps[failedIndex].Objective,
            verifiedFailure = last?.FailureSummary ?? "TeamUnavailable",
            completedSteps = checkpoint.Receipts.Where(receipt => receipt.State == GoalExecutionState.Completed)
                .Select(receipt => receipt.StepKey)
        });
        var proposal = await TrySemanticPlanAsync(checkpoint.GoalText, request, configured,
            snapshot.Functions.Where(item => item.AvailableToAi && !item.IsCoordinationOnly).ToArray(),
            snapshot.Skills.Where(item => item.IsEnabled && item.IsOnline).ToArray(), models,
            cancellationToken, recoveryContext: cause).ConfigureAwait(false);
        var availableSlots = Math.Max(1, runtimePolicy.GetInt("GoalMaximumPlanSteps")) - failedIndex;
        if (contextFailure && (proposal is null || proposal.Steps.Count > availableSlots))
            return false;
        var replacements = proposal?.Steps.Where(step => !string.IsNullOrWhiteSpace(step.Objective)
            && liveTeams.Contains(step.TeamKey)
            && (!checkpoint.PreserveSelectedTeam || string.Equals(step.TeamKey,
                checkpoint.RequestedTeamKey, StringComparison.OrdinalIgnoreCase)))
            .Take(availableSlots)
            .Select((step, index) => new GoalExecutionPlanStep
            {
                Key = "replan-" + nextReplanNumber + "-" + (index + 1),
                Objective = step.Objective, TeamKey = step.TeamKey
            }).ToList();
        if (replacements is not { Count: > 0 }) return false;
        if (contextFailure && replacements.Count != proposal!.Steps.Count)
            return false;
        if (contextFailure && replacements.Count == 1 && string.Equals(
                replacements[0].Objective.Trim(), checkpoint.Steps[failedIndex].Objective.Trim(),
                StringComparison.OrdinalIgnoreCase))
            return false;
        checkpoint.ReplanCount = nextReplanNumber;
        // Prior committed steps are immutable; failed steps remain in receipt history for audit.
        checkpoint.Steps = checkpoint.Steps.Take(failedIndex).Concat(replacements).ToList();
        await ledger.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Goal {GoalId} replanned remaining steps after safe failure; count {Count}.",
            checkpoint.GoalId, checkpoint.ReplanCount);
        return true;
    }

    /// <summary>Builds factual step, workspace and artifact evidence without taking assertions from model prose.</summary>
    /// <param name="result">Observed Council result.</param>
    /// <returns>Technical evidence records.</returns>
    private List<GoalExecutionEvidence> ObserveCouncil(MultiModelCouncilResult result)
    {
        var evidence = result.Steps.Select(step => new GoalExecutionEvidence
        {
            Kind = "CouncilStep", Source = step.WorkflowStepKey + ":" + step.ModelName,
            Succeeded = string.IsNullOrWhiteSpace(step.Error), CouncilRunId = result.RunId
        }).ToList();
        evidence.AddRange(result.Steps.Where(step => !string.IsNullOrWhiteSpace(step.VerifiedWorkspaceReadMetadataJson))
            .Select(step => new GoalExecutionEvidence
            {
                Kind = "VerifiedWorkspaceRead", Source = step.WorkflowStepKey,
                Succeeded = true, CouncilRunId = result.RunId
            }));
        evidence.AddRange(result.Artifacts.Select(artifact => new GoalExecutionEvidence
        {
            Kind = "CouncilArtifact", Source = artifact.Name,
            Succeeded = !string.IsNullOrWhiteSpace(artifact.FilePath) && File.Exists(artifact.FilePath)
                && artifact.MissingRequirements.Count == 0,
            CouncilRunId = result.RunId
        }));
        return evidence;
    }

    /// <summary>Reconstructs the completed answer projection without replaying an already committed Council subgoal.</summary>
    /// <param name="checkpoint">Current durable journal.</param>
    /// <returns>Stable answer projection for rejoined chat and 1-Wire clients.</returns>
    private MultiModelCouncilResult ReconstructResult(GoalExecutionCheckpoint checkpoint) => new()
    {
        RunId = checkpoint.GoalId,
        Prompt = checkpoint.Prompt,
        CouncilTeamKey = checkpoint.Steps.LastOrDefault()?.TeamKey ?? checkpoint.RequestedTeamKey,
        FinalAnswer = string.Join("\n\n", checkpoint.Receipts
            .Where(receipt => receipt.State is GoalExecutionState.Completed or GoalExecutionState.NeedsVerification)
            .Select(receipt => receipt.Answer)),
        DeliverableText = BuildDeliverableText(checkpoint),
        ModelNames = checkpoint.ModelSelectionKeys.ToList(),
        GoalExecution = new GoalExecutionSummary
        {
            GoalId = checkpoint.GoalId,
            State = checkpoint.State,
            TeamKey = checkpoint.Steps.LastOrDefault()?.TeamKey ?? checkpoint.RequestedTeamKey,
            PlannerSource = checkpoint.PlannerSource,
            ReplanCount = checkpoint.ReplanCount,
            CompletedSubgoals = checkpoint.Receipts.Count(receipt => receipt.State == GoalExecutionState.Completed),
            TotalSubgoals = checkpoint.Steps.Count,
            Evidence = checkpoint.Receipts.SelectMany(receipt => receipt.Evidence).ToList(),
            KeyaccountReview = checkpoint.KeyaccountReviews.LastOrDefault(),
            PendingObjective = checkpoint.KeyaccountPendingObjective
        }
    };
}

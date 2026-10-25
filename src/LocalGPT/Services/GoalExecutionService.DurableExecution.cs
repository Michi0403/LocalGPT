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
                ? request.KeyaccountContinuationLimit : runtimePolicy.GetInt(LocalGptRuntimeValue.GoalKeyaccountDefaultContinuations), 0, 100),
            KeyaccountExpectedArtifacts = Math.Clamp(councilRequest.KeyaccountExpectedArtifacts, 0, 100000),
            ModelSelectionKeys = plan.SelectedModels.ToList(),
            RequiredFunctions = request.RequiredFunctions.ToList(),
            RequiredCapabilities = request.RequiredCapabilities.Concat(councilRequest.RequestedOrganicCapabilities)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            MaxRounds = councilRequest.MaxRounds,
            MaxOutputTokens = councilRequest.MaxOutputTokens,
            MaxContextTokens = councilRequest.MaxContextTokens,
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
            Prompt = checkpoint.Prompt,
            GoalText = checkpoint.GoalText,
            KeyaccountReviewEnabled = checkpoint.KeyaccountReviewEnabled,
            KeyaccountContinuationLimit = checkpoint.KeyaccountContinuationLimit,
            KeyaccountExpectedArtifacts = checkpoint.KeyaccountExpectedArtifacts,
            CouncilTeamKey = checkpoint.RequestedTeamKey,
            MaxRounds = checkpoint.MaxRounds,
            MaxOutputTokens = checkpoint.MaxOutputTokens,
            MaxContextTokens = checkpoint.MaxContextTokens,
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
        var liveTeams = (await teams.GetTeamsAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            .Select(team => team.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
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
        var originalPrompt = request.CouncilRequest.Prompt;
        var originalTeam = request.CouncilRequest.CouncilTeamKey;
        try
        {
            for (var stepIndex = 0; stepIndex < checkpoint.Steps.Count; stepIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var step = checkpoint.Steps[stepIndex];
                var saved = checkpoint.Receipts.LastOrDefault(receipt => receipt.StepKey == step.Key);
                if (saved?.State == GoalExecutionState.Completed) continue;
                // Human-approved extension may proceed to a distinct later objective without
                // replaying an earlier unverifiable effect or pretending it was verified.
                if (saved?.State == GoalExecutionState.NeedsVerification &&
                    checkpoint.KeyaccountContinuationApproved && stepIndex < checkpoint.Steps.Count - 1)
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
                request.CouncilRequest.Prompt = checkpoint.Steps.Count == 1 ? originalPrompt :
                    RenderStepPrompt(checkpoint, step, stepIndex);
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
                // Council's public result does not expose a complete invocation-by-invocation
                // write receipt. Without that proof, an enabled consequential function is a
                // possible side effect even when no artifact was returned.
                receipt.ExternalEffectsPossible = councilResult.Artifacts.Count > 0 ||
                    checkpoint.RequiredFunctions.Count > 0 ||
                    directorySnapshot.Functions.Any(function => function.AvailableToAi && !function.IsCoordinationOnly &&
                        !function.IsReadOnly && (function.SupportsAutomaticInvocation || function.SupportsDirectInvocation)) ||
                    receipt.Evidence.Any(evidence => evidence.Kind == "VerifiedWorkspaceRead");
                receipt.FailureSummary = councilResult.Steps.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Error))?.Error
                    ?? (string.IsNullOrWhiteSpace(councilResult.FinalAnswer) ? "EmptyCouncilResult" : string.Empty);
                // The Keyaccount Manager checks every Council subrun, regardless of its saved team workflow.
                // This is a read-only reviewer, not a replacement for tool or user approval receipts.
                GoalKeyaccountReview? keyaccountReview = null;
                if (checkpoint.KeyaccountReviewEnabled)
                {
                    keyaccountReview = await ReviewKeyaccountCompletionAsync(request, checkpoint, step,
                        councilResult, receipt, cancellationToken).ConfigureAwait(false);
                    checkpoint.KeyaccountReviews.Add(keyaccountReview);
                    checkpoint.KeyaccountPendingObjective = keyaccountReview.Complete
                        ? string.Empty : keyaccountReview.NextObjective;
                }
                receipt.State = string.IsNullOrWhiteSpace(councilResult.FinalAnswer) ||
                    councilResult.Steps.Any(item => !string.IsNullOrWhiteSpace(item.Error))
                    ? GoalExecutionState.Failed
                    : councilResult.Artifacts.Count > 0 || checkpoint.RequiredFunctions.Count > 0 ||
                      checkpoint.RequiredCapabilities.Count > 0
                        ? GoalExecutionState.NeedsVerification : GoalExecutionState.Completed;
                receipt.UpdatedAtUtc = DateTime.UtcNow;
                await ledger.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false); // completion commit before next step
                outcome.CouncilResult = councilResult;
                if (receipt.State == GoalExecutionState.Failed)
                {
                    if (!receipt.ExternalEffectsPossible && await TryReplanRemainingAsync(request, checkpoint, stepIndex,
                        liveTeams, cancellationToken).ConfigureAwait(false))
                    {
                        outcome.PlanSteps = checkpoint.Steps.ToList();
                        stepIndex--; // retry only the newly proposed remaining work; prior committed work is never replayed
                        continue;
                    }
                    checkpoint.State = receipt.ExternalEffectsPossible ? GoalExecutionState.NeedsApproval : GoalExecutionState.Failed;
                    break;
                }
                if (receipt.State == GoalExecutionState.NeedsVerification)
                {
                    checkpoint.State = GoalExecutionState.NeedsVerification;
                    break;
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
                        checkpoint.Steps.Add(new GoalExecutionPlanStep
                        {
                            Key = "keyaccount-" + (checkpoint.KeyaccountContinuationsScheduled + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                            TeamKey = step.TeamKey,
                            Objective = keyaccountReview.NextObjective.Trim()
                        });
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
                    .Where(receipt => receipt.State is GoalExecutionState.Completed or GoalExecutionState.NeedsVerification)
                    .Select(receipt => receipt.Answer).Where(answer => !string.IsNullOrWhiteSpace(answer)));
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
        return runtimePolicy.GetString(LocalGptRuntimeValue.GoalCouncilSubstepPromptTemplate)
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
        if (checkpoint.ReplanCount >= Math.Max(0, runtimePolicy.GetInt(LocalGptRuntimeValue.GoalMaximumReplans)))
            return false;
        checkpoint.ReplanCount++;
        var snapshot = await directory.SynchronizeAsync(cancellationToken).ConfigureAwait(false);
        var models = checkpoint.ModelSelectionKeys;
        var configured = await teams.GetTeamsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var last = checkpoint.Receipts.LastOrDefault(receipt => receipt.StepKey == checkpoint.Steps[failedIndex].Key);
        var cause = System.Text.Json.JsonSerializer.Serialize(new
        {
            failedObjective = checkpoint.Steps[failedIndex].Objective,
            verifiedFailure = last?.FailureSummary ?? "TeamUnavailable",
            completedSteps = checkpoint.Receipts.Where(receipt => receipt.State == GoalExecutionState.Completed)
                .Select(receipt => receipt.StepKey)
        });
        var proposal = await TrySemanticPlanAsync(checkpoint.GoalText, request, configured,
            snapshot.Functions.Where(item => item.AvailableToAi && !item.IsCoordinationOnly).ToArray(),
            snapshot.Skills.Where(item => item.IsEnabled && item.IsOnline).ToArray(), models,
            cancellationToken, recoveryContext: cause).ConfigureAwait(false);
        var replacements = proposal?.Steps.Where(step => !string.IsNullOrWhiteSpace(step.Objective)
            && liveTeams.Contains(step.TeamKey)
            && (!checkpoint.PreserveSelectedTeam || string.Equals(step.TeamKey,
                checkpoint.RequestedTeamKey, StringComparison.OrdinalIgnoreCase)))
            .Take(Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.GoalMaximumPlanSteps)) - failedIndex)
            .Select((step, index) => new GoalExecutionPlanStep
            {
                Key = "replan-" + checkpoint.ReplanCount + "-" + (index + 1),
                Objective = step.Objective, TeamKey = step.TeamKey
            }).ToList();
        if (replacements is not { Count: > 0 }) return false;
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

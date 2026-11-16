using LocalGPT.BusinessObjects;
using System.Globalization;

namespace LocalGPT.Services;

/// <summary>
/// Context-fit continuation implemented as methods on the existing DI-scoped
/// GoalExecutionService. There is no additional service layer: the current durable
/// loop invokes these methods, which call the existing injected Council, planner,
/// ledger, runtime-policy and AI-help collaborators through normal instance wiring.
/// </summary>
public sealed partial class GoalExecutionService
{
    private const string ContextFitStepPrefix = "context-fit-";
    private const string ContextFitDiagnostic = "OllamaContextBudgetExceeded;";

    /// <summary>Identify only known model-context preflight errors, not generic crashes.</summary>
    private bool IsGoalContextBudgetError(string? error)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(error) &&
                (error.Contains(ContextFitDiagnostic, StringComparison.OrdinalIgnoreCase) ||
                 (error.Contains("needs about", StringComparison.OrdinalIgnoreCase) &&
                  error.Contains("context tokens", StringComparison.OrdinalIgnoreCase)));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Goal context error classification failed.");
            return false;
        }
    }

    /// <summary>
    /// Allow automatic replanning only after a recognized pre-request failure
    /// without any action/artifact/approval evidence. Uncertain side effects
    /// remain in the existing GoalExecution approval path.
    /// </summary>
    private bool IsGoalContextPreflightFailureWithoutEffects(
        MultiModelCouncilResult result, GoalExecutionStepReceipt receipt)
    {
        try
        {
            if (result.Artifacts.Count != 0 || result.Steps.Count == 0 ||
                receipt.Evidence.Any(item => item.Kind is
                    "ApprovalGatedRequest" or "CouncilArtifact" or "VerifiedWorkspaceRead"))
                return false;
            var failures = result.Steps
                .Where(step => !string.IsNullOrWhiteSpace(step.Error)).ToList();
            if (failures.Count == 0 || failures.Any(step => !IsGoalContextBudgetError(step.Error)))
                return false;
            // Council can already have completed other actions before a later
            // member exceeded context. Never turn an uncertain write into a retry.
            foreach (var step in result.Steps)
            {
                var evidence = (step.VisibleContent ?? string.Empty) + "\n" + (step.Content ?? string.Empty);
                if (evidence.Contains("Function call ·", StringComparison.OrdinalIgnoreCase) ||
                    evidence.Contains("Function result ·", StringComparison.OrdinalIgnoreCase) ||
                    evidence.Contains("<localgpt-dx-call>", StringComparison.OrdinalIgnoreCase) ||
                    evidence.Contains("codegen.review.execute", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Goal context preflight evidence inspection failed for Council run {RunId}.", result.RunId);
            return false; // uncertain = never replay automatically
        }
    }

    /// <summary>
    /// The current GoalExecutionService calls its existing semantic replanner
    /// directly. That planner selects registered role-capable teams and the
    /// *already selected* model, or this method schedules one smaller work item
    /// in the original team. Existing goal checkpointing owns the retry bound.
    /// </summary>
    private async Task<bool> TryRecoverGoalContextFailureAsync(
        GoalExecutionRequest request, GoalExecutionCheckpoint checkpoint,
        int failedIndex, HashSet<string> liveTeams, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var limit = Math.Max(0, runtimePolicy.GetInt("GoalMaximumReplans"));
            if (checkpoint.ReplanCount >= limit)
                return false;

            // Directly invokes the existing DI-wired goal semantic planner.
            if (await TryReplanRemainingAsync(request, checkpoint, failedIndex,
                liveTeams, cancellationToken).ConfigureAwait(false))
            {
                logger.LogInformation(
                    "Goal {GoalId} planned corrective subgoals through its injected model runtime; replan {Replan}.",
                    checkpoint.GoalId, checkpoint.ReplanCount);
                return true;
            }

            // The selected model may fail to produce valid planner JSON. Keep
            // trying to deliver the goal through a single, *changed* compact
            // Council request; never duplicate the original oversized prompt.
            if (checkpoint.ReplanCount >= limit)
                return false;
            var previous = checkpoint.Steps[failedIndex];
            if (!liveTeams.Contains(previous.TeamKey))
                return false;
            var nextReplan = checkpoint.ReplanCount + 1;
            var replacement = new GoalExecutionPlanStep
            {
                Key = ContextFitStepPrefix + nextReplan.ToString(CultureInfo.InvariantCulture),
                TeamKey = previous.TeamKey,
                Objective = previous.Objective
            };
            checkpoint.ReplanCount = nextReplan;
            checkpoint.Steps = checkpoint.Steps.Take(failedIndex).Append(replacement).ToList();
            await ledger.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Goal {GoalId} requested a compact continuation in team {Team} (replan {Replan}/{Limit}); " +
                "the previous failed Council receipt remains immutable.",
                checkpoint.GoalId, previous.TeamKey, nextReplan, limit);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Goal {GoalId} could not safely replan its context-failed subgoal.",
                checkpoint.GoalId);
            throw;
        }
    }

    /// <summary>Run the reduced briefing for a newly planned recovery objective.</summary>
    private bool IsGoalContextRecoveryStep(GoalExecutionCheckpoint checkpoint, GoalExecutionPlanStep step)
    {
        try
        {
            if (step.Key.StartsWith(ContextFitStepPrefix, StringComparison.OrdinalIgnoreCase))
                return true;
            return step.Key.StartsWith("replan-", StringComparison.OrdinalIgnoreCase) &&
                checkpoint.Receipts.Any(receipt => IsGoalContextBudgetError(receipt.FailureSummary));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Goal {GoalId} could not classify its recovery step {StepKey}.",
                checkpoint.GoalId, step.Key);
            return false;
        }
    }

    /// <summary>
    /// Handoff briefing for Council. The instruction is user-editable and seeded
    /// in the runtime policy, not hard-coded in a wrapper or newly created service.
    /// Original goal, current objective and verified outputs remain available;
    /// full prior failure transcripts remain in durable storage, not the prompt.
    /// </summary>
    private string RenderGoalContextRecoveryPrompt(
        GoalExecutionCheckpoint checkpoint, GoalExecutionPlanStep step, int stepIndex)
    {
        try
        {
            var previous = string.Join("\n", checkpoint.Receipts
                .Where(receipt => receipt.State == GoalExecutionState.Completed)
                .Select(receipt => receipt.StepKey + ": " + receipt.Answer));
            var evidenceLimit = Math.Max(0,
                runtimePolicy.GetInt("CouncilFinalRecoveryTaskExcerptCharacters"));
            if (previous.Length > evidenceLimit)
                previous = evidenceLimit > 0 ? previous[^evidenceLimit..] : string.Empty;
            var prompt = runtimePolicy.GetString("GoalCouncilSubstepPromptTemplate")
                .Replace("{{OriginalGoal}}", checkpoint.GoalText, StringComparison.Ordinal)
                .Replace("{{ConversationContext}}", string.Empty, StringComparison.Ordinal)
                .Replace("{{StepNumber}}", (stepIndex + 1).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
                .Replace("{{StepCount}}", checkpoint.Steps.Count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
                .Replace("{{StepObjective}}", step.Objective, StringComparison.Ordinal)
                .Replace("{{PreviousOutputs}}", previous, StringComparison.Ordinal);
            return runtimePolicy.GetString("GoalContextRecoverySubstepInstruction") +
                Environment.NewLine + Environment.NewLine + prompt;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Goal {GoalId} could not assemble the recovery briefing for {StepKey}.",
                checkpoint.GoalId, step.Key);
            // No empty prompt and no silent success, keep original user task visible.
            return checkpoint.GoalText;
        }
    }

    /// <summary>
    /// Adapt the *attempt*, not the saved user preference, using the measured
    /// provider overflow and the existing database-owned Ollama budget policy.
    /// No regex static fallback or duplicate context-sizing service is introduced.
    /// </summary>
    private int FitGoalRecoveryOutputBudget(GoalExecutionCheckpoint checkpoint)
    {
        try
        {
            var original = Math.Max(1, checkpoint.MaxOutputTokens);
            var failure = checkpoint.Receipts.LastOrDefault(receipt =>
                IsGoalContextBudgetError(receipt.FailureSummary))?.FailureSummary;
            if (string.IsNullOrWhiteSpace(failure))
                return original;
            long required = 0;
            long allowed = 0;
            long output = 0;
            // The Ollama client reports numerical facts, it does not decide how
            // to replan. This observer can consume that diagnostic without a
            // model-dependent heuristic or new runtime regex pattern.
            foreach (var field in failure.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                if (field.StartsWith("required=", StringComparison.OrdinalIgnoreCase))
                    long.TryParse(field[9..], NumberStyles.Integer, CultureInfo.InvariantCulture, out required);
                else if (field.StartsWith("allowed=", StringComparison.OrdinalIgnoreCase))
                    long.TryParse(field[8..], NumberStyles.Integer, CultureInfo.InvariantCulture, out allowed);
                else if (field.StartsWith("output=", StringComparison.OrdinalIgnoreCase))
                    long.TryParse(field[7..], NumberStyles.Integer, CultureInfo.InvariantCulture, out output);
            }
            if (allowed <= 0 || required <= allowed || output <= 0)
                return original;
            var policy = runtimePolicy.GetJson<OllamaRequestBudgetPolicy>("OllamaRequestBudgetPolicyJson");
            var minimum = Math.Min(original, Math.Max(1, policy.MinimumFittedOutputTokens));
            var fitted = output - (required - allowed) -
                Math.Max(0, policy.ContextSafetyMarginTokens) -
                Math.Max(0, policy.ThinkingReserveTokens);
            var adapted = (int)Math.Clamp(fitted, minimum, original);
            logger.LogInformation(
                "Goal {GoalId} reduced its next output budget from {Original} to {Adapted} " +
                "using observed required={Required} and allowed={Allowed}; saved preference unchanged.",
                checkpoint.GoalId, original, adapted, required, allowed);
            return adapted;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Goal {GoalId} could not fit its output budget; original is retained.",
                checkpoint.GoalId);
            return Math.Max(1, checkpoint.MaxOutputTokens);
        }
    }
}

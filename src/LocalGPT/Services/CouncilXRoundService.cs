using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Concurrent;

namespace LocalGPT.Services;

/// <summary>Provides process-local, bounded X-Round control state for active configured Council workflows.</summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CouncilXRoundService(ILogger<CouncilXRoundService> logger) : ICouncilXRoundService
{
    /// <summary>Active X-Round policy keyed by the exact executing Council run/round/phase identity.</summary>
    private readonly ConcurrentDictionary<string, CouncilXRoundStepContext> activeContexts = new(StringComparer.Ordinal);

    /// <summary>Pending X-Round directives keyed by the exact executing Council run/round/phase identity.</summary>
    private readonly ConcurrentDictionary<string, ConcurrentQueue<CouncilXRoundDirective>> pending = new(StringComparer.Ordinal);

    /// <summary>Consumed cross-step transition counts keyed by Council run and source workflow step.</summary>
    private readonly ConcurrentDictionary<string, int> transitionCounts = new(StringComparer.Ordinal);

    /// <summary>
    /// Performs activate as part of the council x round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public void Activate(CouncilXRoundStepContext context)
    {
        try
        {
            activeContexts[BuildExecutionKey(context.RunId, context.Round, context.Phase)] = context;
            logger.LogDebug("Activated X-Round policy for Council run {RunId}, round {Round}, phase {Phase}.",
                context.RunId, context.Round, context.Phase);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Activating Council X-Round policy failed for run {RunId}.", context.RunId);
            throw;
        }
    }

    /// <summary>
    /// Performs deactivate as part of the council x round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public void Deactivate(Guid runId, int round, string phase)
    {
        try
        {
            activeContexts.TryRemove(BuildExecutionKey(runId, round, phase), out _);
            logger.LogDebug("Deactivated X-Round policy for Council run {RunId}, round {Round}, phase {Phase}.",
                runId, round, phase);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Deactivating Council X-Round policy failed for run {RunId}.", runId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves active as part of the council x round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public CouncilXRoundStepContext? GetActive(Guid runId, int round, string phase)
    {
        try
        {
            var found = activeContexts.TryGetValue(BuildExecutionKey(runId, round, phase), out var context);
            logger.LogTrace("Read X-Round policy for Council run {RunId}, round {Round}, phase {Phase}; found={Found}.",
                runId, round, phase, found);
            return found ? context : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reading Council X-Round policy failed for run {RunId}.", runId);
            throw;
        }
    }

    /// <summary>
    /// Performs request as part of the council x round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public CouncilXRoundDirective Request(
        AmbientLocalGptContextSnapshot ambient, CouncilXRoundAction action, string targetStepKey = "",
        string reason = "", string text = "", string prompt = "", string teamKey = "", string modelName = "")
    {
        try
        {
            if (ambient.CouncilRunId is not Guid runId)
                throw new InvalidOperationException("X-Round functions are available only inside an active Council workflow step.");

            var executionKey = BuildExecutionKey(runId, ambient.CouncilRound, ambient.Phase);
            if (!activeContexts.TryGetValue(executionKey, out var context))
                throw new InvalidOperationException("The current Council step has no active X-Round policy.");

            ValidateAction(context, action);
            var target = string.IsNullOrWhiteSpace(targetStepKey) ? context.DefaultTargetStepKey.Trim() : targetStepKey.Trim();
            if (action is CouncilXRoundAction.ReconsiderStep or CouncilXRoundAction.ReexecuteStep &&
                string.IsNullOrWhiteSpace(target))
                throw new InvalidOperationException("A revisit X-Function needs a target workflow step key or a configured default target.");

            var directive = new CouncilXRoundDirective(
                Guid.NewGuid(), runId, ambient.CouncilRound, ambient.Phase, context.StepKey, action, target,
                reason?.Trim() ?? string.Empty, text?.Trim() ?? string.Empty, prompt?.Trim() ?? string.Empty,
                string.IsNullOrWhiteSpace(teamKey) ? context.ChildCouncilTeamKey.Trim() : teamKey.Trim(),
                string.IsNullOrWhiteSpace(modelName) ? context.ChildModelName.Trim() : modelName.Trim(),
                string.IsNullOrWhiteSpace(ambient.ActorDisplayName) ? "Council participant" : ambient.ActorDisplayName,
                DateTime.UtcNow);

            pending.GetOrAdd(executionKey, _ => new ConcurrentQueue<CouncilXRoundDirective>()).Enqueue(directive);
            logger.LogInformation("Council run {RunId} queued X-Round action {Action} from step {StepKey}; payload text was omitted.",
                runId, action, context.StepKey);
            return directive;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Queuing an X-Round request failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs drain as part of the council x round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public IReadOnlyList<CouncilXRoundDirective> Drain(Guid runId, int round, string phase)
    {
        try
        {
            var key = BuildExecutionKey(runId, round, phase);
            if (!pending.TryRemove(key, out var queue))
            {
                logger.LogTrace("No X-Round directives were queued for Council run {RunId}, round {Round}, phase {Phase}.",
                    runId, round, phase);
                return [];
            }

            var directives = new List<CouncilXRoundDirective>();
            while (queue.TryDequeue(out var directive))
                directives.Add(directive);
            logger.LogInformation("Drained {Count} X-Round directive(s) for Council run {RunId}; payload text was omitted.",
                directives.Count, runId);
            return directives;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Draining Council X-Round directives failed for run {RunId}.", runId);
            throw;
        }
    }

    /// <summary>
    /// Attempts to consume transition budget as part of the council x round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public bool TryConsumeTransitionBudget(Guid runId, string sourceStepKey, int maximumTransitions, out int usedTransitions)
    {
        var maximum = Math.Max(1, maximumTransitions);
        var key = $"{runId:N}|{sourceStepKey.Trim().ToLowerInvariant()}";
        try
        {
            usedTransitions = transitionCounts.AddOrUpdate(key, 1, (_, current) => checked(current + 1));
            return usedTransitions <= maximum;
        }
        catch (OverflowException ex)
        {
            usedTransitions = int.MaxValue;
            logger.LogWarning(ex, "Council X-Round transition counter overflowed for run {RunId}, step {StepKey}.",
                runId, sourceStepKey);
            return false;
        }
        catch (Exception ex)
        {
            usedTransitions = 0;
            logger.LogError(ex, "Consuming Council X-Round transition budget failed for run {RunId}, step {StepKey}.",
                runId, sourceStepKey);
            throw;
        }
    }

    /// <summary>
    /// Performs end run as part of the council x round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public void EndRun(Guid runId)
    {
        try
        {
            var prefix = runId.ToString("N") + "|";
            foreach (var key in activeContexts.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)))
                activeContexts.TryRemove(key, out _);
            foreach (var key in pending.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)))
                pending.TryRemove(key, out _);
            foreach (var key in transitionCounts.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)))
                transitionCounts.TryRemove(key, out _);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Clearing X-Round state failed for Council run {RunId}.", runId);
        }
    }

    /// <summary>Validates that the active workflow step grants the requested X-Round action.</summary>
    /// <param name="context">Context value supplied to the council x round operation and used when producing its result.</param>
    /// <param name="action">Action value supplied to the council x round operation and used when producing its result.</param>
    private void ValidateAction(CouncilXRoundStepContext context, CouncilXRoundAction action)
    {
        try
        {
            var allowed = action switch
            {
                CouncilXRoundAction.ReconsiderStep or CouncilXRoundAction.ReexecuteStep => context.CanRevisit,
                CouncilXRoundAction.ReturnText => context.CanReturnText,
                CouncilXRoundAction.StartSingleModel => context.CanStartSingleModel,
                CouncilXRoundAction.StartCouncil => context.CanStartCouncil,
                _ => false
            };
            if (!allowed)
                throw new InvalidOperationException($"X-Round action '{action}' is not enabled for workflow step '{context.StepKey}'.");
            System.Diagnostics.Trace.TraceInformation($"Validated X-Round action {action} for workflow step {context.StepKey}.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Validating X-Round action failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>Builds the process-local identity used to scope one executing Council workflow step.</summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="round">Round value supplied to the council x round operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the council x round operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildExecutionKey(Guid runId, int round, string phase)
    {
        try
        {
            var key = $"{runId:N}|{round}|{phase?.Trim() ?? string.Empty}";
            System.Diagnostics.Trace.TraceInformation($"Built X-Round execution identity for Council run {runId:N}, round {round}.");
            return key;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Building X-Round execution identity failed: {ex.Message}");
            throw;
        }
    }
}

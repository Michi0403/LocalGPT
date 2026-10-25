using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Inspect and explicitly resume interrupted durable goal runs through the established approval system.</summary>
/// <param name="goals">Goal orchestration and recovery service.</param>
/// <param name="logger">Controller diagnostics without private goal content.</param>
[ApiController]
[Route("api/goals")]
public sealed class GoalExecutionController(IGoalExecutionService goals, ILogger<GoalExecutionController> logger) : ControllerBase
{
    /// <summary>Reads a persisted goal run, including already committed substeps, without executing any tools.</summary>
    /// <param name="goalId">Stable goal identifier.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Checkpoint projection or not found.</returns>
    [HttpGet("{goalId:guid}")]
    public async Task<IResult> InspectAsync(Guid goalId, CancellationToken cancellationToken)
    {
        try
        {
            var checkpoint = await goals.GetCheckpointAsync(goalId, cancellationToken).ConfigureAwait(false);
            return checkpoint is null ? Results.NotFound() : Results.Ok(checkpoint);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Goal checkpoint inspection failed for {GoalId}.", goalId);
            return Results.InternalServerError("Goal checkpoint unavailable.");
        }
    }

    /// <summary>Explicitly continue an existing goal with another distinct objective and new Council rounds.</summary>
    /// <param name="goalId">Original run ID.</param>
    /// <param name="request">User-selected objective and new round allowance.</param>
    /// <param name="cancellationToken">Cancels the continuation.</param>
    [HttpPost("{goalId:guid}/continue")]
    [HumanApprovalRequired("goal.execution.continue", "Continue Keyaccount goal", "Approve new distinct Council objectives and their execution; old effects cannot be silently replayed.", "High", "Goal continuation reviewer", requiredBeforeCompletion: true)]
    public async Task<IResult> ContinueAsync(Guid goalId, KeyaccountContinuationInput request, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await goals.ContinueWithNewObjectiveAsync(goalId, request.Objective,
                request.AdditionalRounds, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (ArgumentException error) { return Results.BadRequest(error.Message); }
        catch (InvalidOperationException error) { return Results.Conflict(error.Message); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Goal continuation failed for {GoalId}.", goalId);
            return Results.InternalServerError("Goal continuation failed.");
        }
    }

    /// <summary>Operator-approved retry of only the remaining work after inspecting uncertain interrupted effects.</summary>
    /// <param name="goalId">Stable goal identifier.</param>
    /// <param name="cancellationToken">Cancels execution.</param>
    /// <returns>Resumed result with updated ledger state.</returns>
    [HttpPost("{goalId:guid}/resume")]
    [HumanApprovalRequired("goal.execution.resume", "Resume interrupted goal", "Review existing effects and explicitly retry the interrupted Council step without replaying completed steps.", "High", "Goal execution reviewer", requiredBeforeCompletion: true)]
    public async Task<IResult> ResumeAsync(Guid goalId, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await goals.ResumeAsync(goalId, confirmInterruptedRetry: true, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Goal resume failed for {GoalId}.", goalId);
            return Results.InternalServerError("Goal could not be resumed.");
        }
    }
}

/// <summary>User-supplied continuation after reviewing the goal journal.</summary>
public sealed class KeyaccountContinuationInput
{
    /// <summary>Distinct new task; defaults to the previously proposed unfinished objective.</summary>
    public string? Objective { get; set; }
    /// <summary>How many additional Council objectives may be queued for this human-approved continuation.</summary>
    public int AdditionalRounds { get; set; } = 3;
}

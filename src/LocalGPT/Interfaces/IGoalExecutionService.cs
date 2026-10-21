using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Routes typed user goals through live capability discovery, configured teams and the existing Council engine.</summary>
public interface IGoalExecutionService
{
    /// <summary>Discovers live capabilities and configured team coverage without executing the user goal.</summary>
    /// <param name="request">The goal and Council execution preferences.</param>
    /// <param name="cancellationToken">Cancels runtime discovery.</param>
    /// <returns>A typed plan with selected team, participant identities and missing requirements.</returns>
    Task<GoalExecutionResult> PlanAsync(GoalExecutionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Executes a planned goal using the existing Council and records service-observed evidence.</summary>
    /// <param name="request">The original goal and Council execution preferences.</param>
    /// <param name="cancellationToken">Cancels the goal and delegated Council operation.</param>
    /// <returns>The routed Council result and verifiable execution observations.</returns>
    Task<GoalExecutionResult> ExecuteAsync(GoalExecutionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Inspect durable goal state after a restart without executing or replaying tools.</summary>
    /// <param name="goalId">Previously issued goal identity.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Checkpoint or null if unknown.</returns>
    Task<GoalExecutionCheckpoint?> GetCheckpointAsync(Guid goalId, CancellationToken cancellationToken = default);

    /// <summary>Resume at the first incomplete subgoal, requiring explicit confirmation before an interrupted call may be retried.</summary>
    /// <param name="goalId">Previously issued goal identity.</param>
    /// <param name="confirmInterruptedRetry">Explicit operator decision for possibly consequential interrupted work.</param>
    /// <param name="cancellationToken">Cancels execution.</param>
    /// <returns>Observed result and current durable checkpoint.</returns>
    Task<GoalExecutionResult> ResumeAsync(Guid goalId, bool confirmInterruptedRetry, CancellationToken cancellationToken = default);
}

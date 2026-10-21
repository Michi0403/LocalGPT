using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Durable, serialized goal-checkpoint storage and per-goal execution coordination.</summary>
public interface IGoalExecutionLedgerService
{
    /// <summary>Acquires an exclusive lock for one goal through the duration of an execution or resume.</summary>
    /// <param name="goalId">Stable goal identity.</param>
    /// <param name="cancellationToken">Cancels lock acquisition.</param>
    /// <returns>A lease releasing the lock when disposed.</returns>
    Task<IDisposable> AcquireAsync(Guid goalId, CancellationToken cancellationToken = default);

    /// <summary>Atomically checkpoints goal state before and after each consequential operation.</summary>
    /// <param name="checkpoint">Fully materialized state to save.</param>
    /// <param name="cancellationToken">Cancels the persistence operation.</param>
    /// <returns>A task completing only after the checkpoint is durable.</returns>
    Task SaveAsync(GoalExecutionCheckpoint checkpoint, CancellationToken cancellationToken = default);

    /// <summary>Reads a checkpoint; an in-flight step is reported as interrupted after restart.</summary>
    /// <param name="goalId">Goal identity to inspect.</param>
    /// <param name="cancellationToken">Cancels the read operation.</param>
    /// <returns>The saved checkpoint, or null if no goal exists.</returns>
    Task<GoalExecutionCheckpoint?> LoadAsync(Guid goalId, CancellationToken cancellationToken = default);
}

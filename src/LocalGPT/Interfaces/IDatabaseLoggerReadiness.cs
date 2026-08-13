namespace LocalGPT.Interfaces;

/// <summary>
/// Keeps database-backed diagnostics dormant until migration and deterministic seeding have completed.
/// </summary>
public interface IDatabaseLoggerReadiness
{
    /// <summary>
    /// Gets a value indicating whether ready applies to the database logger readiness state.
    /// </summary>
    /// <value>The is ready value exposed by <see cref="IDatabaseLoggerReadiness"/>.</value>
    bool IsReady { get; }

    /// <summary>
    /// Performs wait until ready for <see cref="IDatabaseLoggerReadiness"/>, keeping the operation consistent with the state and invariants of the surrounding database logger readiness workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs mark ready for <see cref="IDatabaseLoggerReadiness"/>, keeping the operation consistent with the state and invariants of the surrounding database logger readiness workflow.
    /// </summary>
    void MarkReady();
}

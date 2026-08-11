namespace LocalGPT.Interfaces;

/// <summary>
/// Keeps database-backed diagnostics dormant until migration and deterministic seeding have completed.
/// </summary>
public interface IDatabaseLoggerReadiness
{
    bool IsReady { get; }

    /// <summary>
    /// Runs the wait until ready async operation.
    /// </summary>
    Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the mark ready operation.
    /// </summary>
    void MarkReady();
}

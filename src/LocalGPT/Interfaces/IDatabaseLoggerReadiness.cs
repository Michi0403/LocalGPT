namespace LocalGPT.Interfaces;

/// <summary>
/// Keeps database-backed diagnostics dormant until migration and deterministic seeding have completed.
/// </summary>
public interface IDatabaseLoggerReadiness
{
    bool IsReady { get; }

    Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);

    void MarkReady();
}

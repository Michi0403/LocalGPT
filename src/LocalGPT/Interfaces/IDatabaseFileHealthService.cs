namespace LocalGPT.Interfaces;

/// <summary>
/// Owns SQLite file validation and conservative recovery before EF Core opens the store.
/// </summary>
public interface IDatabaseFileHealthService
{
    string DatabasePath { get; }
    /// <summary>
    /// Ensures healthy or recover async.
    /// </summary>
    Task EnsureHealthyOrRecoverAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the recover malformed database async operation.
    /// </summary>
    Task RecoverMalformedDatabaseAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Determines whether sqlite corruption.
    /// </summary>
    bool IsSqliteCorruption(Exception exception);
}

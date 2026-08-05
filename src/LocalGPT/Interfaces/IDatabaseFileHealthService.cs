namespace LocalGPT.Interfaces;

/// <summary>
/// Owns SQLite file validation and conservative recovery before EF Core opens the store.
/// </summary>
public interface IDatabaseFileHealthService
{
    string DatabasePath { get; }
    Task EnsureHealthyOrRecoverAsync(CancellationToken cancellationToken = default);
    Task RecoverMalformedDatabaseAsync(CancellationToken cancellationToken = default);
    bool IsSqliteCorruption(Exception exception);
}

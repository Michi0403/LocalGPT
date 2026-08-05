namespace LocalGPT.Interfaces;

/// <summary>
/// Reconciles verified legacy SQLite schema state with EF migration history before normal migration.
/// It preserves data, creates compatibility backups, and refuses ambiguous partial schemas.
/// </summary>
public interface IDatabaseMigrationCompatibilityService
{
    Task PrepareAsync(CancellationToken cancellationToken = default);
}

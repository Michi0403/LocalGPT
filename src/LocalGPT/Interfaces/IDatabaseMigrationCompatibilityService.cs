namespace LocalGPT.Interfaces;

/// <summary>
/// Reconciles verified legacy SQLite schema state with EF migration history before normal migration.
/// It preserves data, creates compatibility backups, and refuses ambiguous partial schemas.
/// </summary>
public interface IDatabaseMigrationCompatibilityService
{
    /// <summary>
    /// Performs prepare as part of the database migration compatibility service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task PrepareAsync(CancellationToken cancellationToken = default);
}

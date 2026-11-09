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

    /// <summary>
    /// Finishes schema changes that EF cannot express on SQLite, right after migration and before any seed stage runs.
    /// Obsolete columns are dropped only after checking the real database (column present, replacing migration recorded,
    /// replacement columns present, nothing in the schema still using the column), with a compatibility backup first and
    /// the backfill, drop and verification in one transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the reconciliation has finished.</returns>
    Task ReconcileAfterMigrationAsync(CancellationToken cancellationToken = default);
}

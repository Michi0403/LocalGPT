namespace LocalGPT.Interfaces;

/// <summary>
/// Owns SQLite file validation and conservative recovery before EF Core opens the store.
/// </summary>
public interface IDatabaseFileHealthService
{
    /// <summary>
    /// Gets the database path used by this database file health instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The database path value exposed by <see cref="IDatabaseFileHealthService"/>.</value>
    string DatabasePath { get; }
    /// <summary>
    /// Ensures healthy or recover as part of the database file health service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task EnsureHealthyOrRecoverAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs recover malformed database as part of the database file health service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task RecoverMalformedDatabaseAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Determines whether sqlite corruption as part of the database file health service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="exception">Exception value supplied to the database file health operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool IsSqliteCorruption(Exception exception);
}

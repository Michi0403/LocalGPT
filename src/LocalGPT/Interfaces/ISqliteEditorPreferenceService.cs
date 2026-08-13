using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for sqlite editor preference behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ISqliteEditorPreferenceService
{
    /// <summary>
    /// Retrieves overrides as part of the sqlite editor preference service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="tableName">Table name value supplied to the sqlite editor preference operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The i read only dictionary string sqlite editor field override produced by the operation.</returns>
    Task<IReadOnlyDictionary<string, SqliteEditorFieldOverride>> GetOverridesAsync(string tableName, CancellationToken cancellationToken = default);
    /// <summary>
    /// Persists override as part of the sqlite editor preference service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="preference">Preference value supplied to the sqlite editor preference operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The sqlite editor field override produced by the operation.</returns>
    Task<SqliteEditorFieldOverride> SaveOverrideAsync(SqliteEditorFieldOverride preference, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs infer editor kind as part of the sqlite editor preference service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="column">Column value supplied to the sqlite editor preference operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the sqlite editor preference operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string InferEditorKind(SqliteColumnSummary column, string? value);
}

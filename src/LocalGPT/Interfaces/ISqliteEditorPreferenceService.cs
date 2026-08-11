using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the sqlite editor preference service contract.
/// </summary>
public interface ISqliteEditorPreferenceService
{
    /// <summary>
    /// Gets overrides async.
    /// </summary>
    Task<IReadOnlyDictionary<string, SqliteEditorFieldOverride>> GetOverridesAsync(string tableName, CancellationToken cancellationToken = default);
    /// <summary>
    /// Saves override async.
    /// </summary>
    Task<SqliteEditorFieldOverride> SaveOverrideAsync(SqliteEditorFieldOverride preference, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the infer editor kind operation.
    /// </summary>
    string InferEditorKind(SqliteColumnSummary column, string? value);
}

using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ISqliteEditorPreferenceService
{
    Task<IReadOnlyDictionary<string, SqliteEditorFieldOverride>> GetOverridesAsync(string tableName, CancellationToken cancellationToken = default);
    Task<SqliteEditorFieldOverride> SaveOverrideAsync(SqliteEditorFieldOverride preference, bool userConfirmed, CancellationToken cancellationToken = default);
    string InferEditorKind(SqliteColumnSummary column, string? value);
}

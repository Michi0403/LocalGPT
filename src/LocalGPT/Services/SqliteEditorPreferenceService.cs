using System.Collections.Frozen;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates sqlite editor preference behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="dbContextFactory">Local gpt memory database context dependency used by the sqlite editor preference workflow to provide the corresponding application capability.</param>
/// <param name="databaseInitializer">Database initialization service dependency used by the sqlite editor preference workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class SqliteEditorPreferenceService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<SqliteEditorPreferenceService> logger) : ISqliteEditorPreferenceService
{
    /// <summary>
    /// Gets the allowed kinds value that forms part of the sqlite editor preference state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The allowed kinds value exposed by <see cref="SqliteEditorPreferenceService"/>.</value>
    private FrozenSet<string> AllowedKinds { get; } = new[]
    {
        "Automatic", "Text", "LongText", "Number", "Boolean", "DateTime", "Guid", "Json", "Secret"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Retrieves overrides as part of the sqlite editor preference service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="tableName">Table name value supplied to the sqlite editor preference operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The i read only dictionary string sqlite editor field override produced by the operation.</returns>
    public async Task<IReadOnlyDictionary<string, SqliteEditorFieldOverride>> GetOverridesAsync(string tableName, CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await db.SqliteEditorFieldOverrides.AsNoTracking()
                .Where(item => item.TableName == tableName)
                .ToDictionaryAsync(item => item.ColumnName, StringComparer.OrdinalIgnoreCase, cancellationToken)
                .ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(SqliteEditorPreferenceService)}.{nameof(GetOverridesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(SqliteEditorPreferenceService)}.{nameof(GetOverridesAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Persists override as part of the sqlite editor preference service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="preference">Preference value supplied to the sqlite editor preference operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The sqlite editor field override produced by the operation.</returns>
    public async Task<SqliteEditorFieldOverride> SaveOverrideAsync(SqliteEditorFieldOverride preference, bool userConfirmed, CancellationToken cancellationToken = default)
    {
    try
    {
            if (!userConfirmed)
                throw new InvalidOperationException("Fresh human confirmation is required before changing a persistent SQLite editor preference.");
            if (!AllowedKinds.Contains(preference.EditorKind))
                throw new ArgumentException("Unsupported editor kind.", nameof(preference));
            ArgumentException.ThrowIfNullOrWhiteSpace(preference.TableName);
            ArgumentException.ThrowIfNullOrWhiteSpace(preference.ColumnName);

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var entity = await db.SqliteEditorFieldOverrides.SingleOrDefaultAsync(
                item => item.TableName == preference.TableName && item.ColumnName == preference.ColumnName,
                cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                entity = new SqliteEditorFieldOverride
                {
                    TableName = preference.TableName.Trim(),
                    ColumnName = preference.ColumnName.Trim()
                };
                db.SqliteEditorFieldOverrides.Add(entity);
            }

            entity.EditorKind = preference.EditorKind;
            entity.InputMask = preference.InputMask?.Trim() ?? string.Empty;
            entity.FormatString = preference.FormatString?.Trim() ?? string.Empty;
            entity.NullText = string.IsNullOrWhiteSpace(preference.NullText) ? "[null]" : preference.NullText.Trim();
            entity.IsSensitive = preference.IsSensitive || preference.EditorKind.Equals("Secret", StringComparison.OrdinalIgnoreCase);
            entity.RequireHumanApproval = preference.RequireHumanApproval;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved SQLite editor preference for {TableName}.{ColumnName}; mask content omitted.", entity.TableName, entity.ColumnName);
            return entity;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(SqliteEditorPreferenceService)}.{nameof(SaveOverrideAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(SqliteEditorPreferenceService)}.{nameof(SaveOverrideAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs infer editor kind as part of the sqlite editor preference service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="column">Column value supplied to the sqlite editor preference operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the sqlite editor preference operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string InferEditorKind(SqliteColumnSummary column, string? value)
    {
    try
    {
            var name = column.Name;
            var type = column.Type;
            if (name.Contains("Secret", StringComparison.OrdinalIgnoreCase) || name.Contains("Password", StringComparison.OrdinalIgnoreCase) || name.Contains("Token", StringComparison.OrdinalIgnoreCase))
                return "Secret";
            if (name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(value, out _))
                return "Guid";
            if (type.Contains("BOOL", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Is", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Has", StringComparison.OrdinalIgnoreCase))
                return "Boolean";
            if (type.Contains("INT", StringComparison.OrdinalIgnoreCase) || type.Contains("REAL", StringComparison.OrdinalIgnoreCase) || type.Contains("NUM", StringComparison.OrdinalIgnoreCase) || type.Contains("DEC", StringComparison.OrdinalIgnoreCase))
                return "Number";
            if (name.Contains("Date", StringComparison.OrdinalIgnoreCase) || name.EndsWith("AtUtc", StringComparison.OrdinalIgnoreCase) || name.EndsWith("On", StringComparison.OrdinalIgnoreCase))
                return "DateTime";
            if (name.EndsWith("Json", StringComparison.OrdinalIgnoreCase))
                return "Json";
            if ((value?.Length ?? 0) > 120 || name.Contains("Content", StringComparison.OrdinalIgnoreCase) || name.Contains("Description", StringComparison.OrdinalIgnoreCase) || name.Contains("Message", StringComparison.OrdinalIgnoreCase))
                return "LongText";
            return "Text";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(SqliteEditorPreferenceService)}.{nameof(InferEditorKind)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(SqliteEditorPreferenceService)}.{nameof(InferEditorKind)} failed.");
        throw;
    }
}
}

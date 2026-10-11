using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Data.Sqlite;

namespace LocalGPT.Services;

/// <summary>Builds the user-facing entity hierarchy from the live database schema, not a stale table list.</summary>
/// <param name="tableEditor">Maintained SQLite table reader and database-path owner.</param>
/// <param name="logger">Service-local diagnostics.</param>
public sealed class DataManagementCatalogService(
    ISqliteTableEditorService tableEditor,
    ILogger<DataManagementCatalogService> logger) : IDataManagementCatalogService
{
    /// <summary>Reads all managed tables and SQLite's actual foreign-key relationships.</summary>
    /// <param name="cancellationToken">Cancellation token for the database read.</param>
    /// <returns>Grouped table metadata with parent/child navigation edges.</returns>
    public async Task<IReadOnlyList<DataManagementTableInfo>> GetTablesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tables = await tableEditor.GetTablesAsync(cancellationToken).ConfigureAwait(false);
            if (tables.Count == 0) return [];

            var names = tables.Select(table => table.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var relations = new List<DataManagementRelation>();
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = tableEditor.DatabasePath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString();
            var connection = new SqliteConnection(connectionString);
            await using var configuredConnectionAsyncDisposal = connection.ConfigureAwait(false);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            foreach (var table in tables)
            {
                var command = connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    // Identifiers originate in sqlite_master, but still quote them before using PRAGMA.
                    var quotedName = "\"" + table.Name.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
                    command.CommandText = $"PRAGMA foreign_key_list({quotedName});";
                    var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    await using var configuredReaderAsyncDisposal = reader.ConfigureAwait(false);
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var parent = reader.GetString(2);
                        if (!names.Contains(parent)) continue;
                        relations.Add(new DataManagementRelation(
                            table.Name,
                            parent,
                            reader.GetString(3),
                            reader.IsDBNull(4) ? string.Empty : reader.GetString(4)));
                    }
                }
            }

            return tables.Select(table => new DataManagementTableInfo(
                    table.Name,
                    ClassifyGroup(table.Name),
                    table.RowCount,
                    table.ColumnCount,
                    relations.Where(relation => relation.ChildTable.Equals(table.Name, StringComparison.OrdinalIgnoreCase)).ToList(),
                    relations.Where(relation => relation.ParentTable.Equals(table.Name, StringComparison.OrdinalIgnoreCase)).ToList()))
                .OrderBy(table => table.GroupKey, StringComparer.Ordinal)
                .ThenBy(table => table.Parents.Count)
                .ThenBy(table => table.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not build the live SQLite entity hierarchy.");
            throw;
        }
    }

    /// <summary>Groups database areas for navigation without altering entity, relation or policy state.</summary>
    /// <param name="name">Database table name.</param>
    /// <returns>Stable UI navigation group key.</returns>
    private string ClassifyGroup(string name)
    {
        try
        {
            if (name.StartsWith("Human", StringComparison.OrdinalIgnoreCase) || name.StartsWith("DeferredDx", StringComparison.OrdinalIgnoreCase)) return "approvals";
            if (name.StartsWith("LocalGptProject", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Project", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("EmbeddedFirmware", StringComparison.OrdinalIgnoreCase)) return "projects";
            if (name.StartsWith("ChatMemory", StringComparison.OrdinalIgnoreCase) || name.EndsWith("LogEntries", StringComparison.OrdinalIgnoreCase)) return "conversations";
            if (name.StartsWith("CouncilKnowledge", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Regex", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Prompt", StringComparison.OrdinalIgnoreCase) || name.StartsWith("OrganicSkill", StringComparison.OrdinalIgnoreCase)) return "knowledge";
            if (name.StartsWith("Council", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Hardware", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("ConfiguredAi", StringComparison.OrdinalIgnoreCase)) return "councils";
            return "system";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not classify table {TableName} in the Data Management navigation.", name);
            throw new InvalidOperationException("Could not classify a SQLite table for navigation.", ex);
        }
    }
}

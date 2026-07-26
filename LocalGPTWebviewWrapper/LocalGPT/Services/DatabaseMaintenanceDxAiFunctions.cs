using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services.Helpers;

namespace LocalGPT.Services;

public sealed class ListSqliteTablesFunction(
    ISqliteTableEditorService editor,
    ILogger<ListSqliteTablesFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.sqlite.tables.list",
        "POST",
        "/api/dxai/functions/localgpt.sqlite.tables.list/invoke",
        "List every user-maintainable LocalGPT SQLite table with row and schema counts before selecting a targeted read or write operation.",
        "No parameters.",
        "Read-only metadata. Internal sqlite_* tables are excluded.",
        IsReadOnly: true,
        AvailableToAi: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var tables = await editor.GetTablesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogDebug("DXAIFunction listed {TableCount} SQLite tables.", tables.Count);
        return DxAiFunctionJsonHelper.Success(tables);
    }
}

public sealed class PreviewSqliteTableFunction(
    ISqliteTableEditorService editor,
    ILogger<PreviewSqliteTableFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.sqlite.table.preview",
        "POST",
        "/api/dxai/functions/localgpt.sqlite.table.preview/invoke",
        "Read a bounded, masked preview of one LocalGPT SQLite table after a structured project/requirement plan identifies why that table is relevant.",
        "JSON parameters: tableName required; take optional 1-100.",
        "Read-only. Values in password, secret, token, key, connection-string, and raw payload columns are masked. Use a separately approved exact read only when the human needs those values.",
        IsReadOnly: true,
        AvailableToAi: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"tableName":{"type":"string"},"take":{"type":"integer","minimum":1,"maximum":100}},"required":["tableName"],"additionalProperties":false}
        """);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = DxAiFunctionJsonHelper.Deserialize<ReadTableParameters>(request.Parameters);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.TableName);
        var snapshot = await editor.GetTableAsync(parameters.TableName, Math.Clamp(parameters.Take, 1, 100), cancellationToken).ConfigureAwait(false);
        foreach (var row in snapshot.Rows)
        {
            foreach (var key in row.Values.Keys.ToList())
            {
                if (IsSensitiveColumn(key))
                    row.Values[key] = "<masked>";
            }
        }
        logger.LogDebug("DXAIFunction previewed SQLite table {TableName} with {RowCount} row(s).", parameters.TableName, snapshot.Rows.Count);
        return DxAiFunctionJsonHelper.Success(snapshot);
    }

    private static bool IsSensitiveColumn(string name) =>
        name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("apikey", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("connectionstring", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("parametersjson", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("payloadjson", StringComparison.OrdinalIgnoreCase);

    private sealed class ReadTableParameters
    {
        public string TableName { get; set; } = string.Empty;
        public int Take { get; set; } = 50;
    }
}

public sealed class ReadExactSqliteTableFunction(
    ISqliteTableEditorService editor,
    ILogger<ReadExactSqliteTableFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.sqlite.table.read_exact",
        "POST",
        "/api/dxai/functions/localgpt.sqlite.table.read_exact/invoke",
        "Read an exact bounded SQLite table preview when masked values are insufficient for the user's requested maintenance task.",
        "JSON parameters: tableName required; take optional 1-100.",
        "Potentially sensitive database content can be returned. Exact table name and row limit are shown in the Human Collaboration Inbox and require one-use approval.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: false,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"tableName":{"type":"string"},"take":{"type":"integer","minimum":1,"maximum":100}},"required":["tableName"],"additionalProperties":false}
        """);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = DxAiFunctionJsonHelper.Deserialize<ReadTableParameters>(request.Parameters);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.TableName);
        var snapshot = await editor.GetTableAsync(parameters.TableName, Math.Clamp(parameters.Take, 1, 100), cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Approved exact SQLite read completed for table {TableName} with {RowCount} row(s); values omitted from logs.", parameters.TableName, snapshot.Rows.Count);
        return DxAiFunctionJsonHelper.Success(snapshot);
    }

    private sealed class ReadTableParameters
    {
        public string TableName { get; set; } = string.Empty;
        public int Take { get; set; } = 50;
    }
}

public sealed class UpsertSqliteRowFunction(
    ISqliteTableEditorService editor,
    ILogger<UpsertSqliteRowFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.sqlite.row.upsert",
        "POST",
        "/api/dxai/functions/localgpt.sqlite.row.upsert/invoke",
        "Insert or update one exact LocalGPT SQLite row after the council maps the operation to an approved project requirement.",
        "JSON parameters: tableName required; rowId optional for update; values required object keyed by real column names.",
        "Writes one row only. The Human Collaboration Inbox shows the table, row ID, column names, and redacted sensitive values. Approval is exact and one-use.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"tableName":{"type":"string"},"rowId":{"type":["integer","null"]},"values":{"type":"object","additionalProperties":{"type":["string","null"]}}},"required":["tableName","values"],"additionalProperties":false}
        """);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = DxAiFunctionJsonHelper.Deserialize<UpsertParameters>(request.Parameters);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.TableName);
        if (parameters.Values.Count == 0)
            throw new JsonException("At least one column value is required.");
        if (parameters.RowId is long rowId)
        {
            await editor.UpdateRowAsync(
                parameters.TableName,
                rowId,
                parameters.Values.Select(pair => new SqliteCellUpdate { ColumnName = pair.Key, Value = pair.Value }).ToList(),
                cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved SQLite update completed for {TableName} row {RowId}; values omitted from logs.", parameters.TableName, rowId);
            return DxAiFunctionJsonHelper.Success(new { parameters.TableName, RowId = rowId, Operation = "Updated" });
        }

        await editor.InsertRowAsync(parameters.TableName, parameters.Values, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Approved SQLite insert completed for {TableName}; values omitted from logs.", parameters.TableName);
        return DxAiFunctionJsonHelper.Success(new { parameters.TableName, Operation = "Inserted" });
    }

    private sealed class UpsertParameters
    {
        public string TableName { get; set; } = string.Empty;
        public long? RowId { get; set; }
        public Dictionary<string, string?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

public sealed class DeleteSqliteRowFunction(
    ISqliteTableEditorService editor,
    ILogger<DeleteSqliteRowFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.sqlite.row.delete",
        "POST",
        "/api/dxai/functions/localgpt.sqlite.row.delete/invoke",
        "Delete one exact row from a LocalGPT SQLite table after structured requirement mapping and user review.",
        "JSON parameters: tableName and rowId required.",
        "Destructive and one-row bounded. Exact table and row ID require one-use approval. Existing foreign-key restrictions remain active.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"tableName":{"type":"string"},"rowId":{"type":"integer"}},"required":["tableName","rowId"],"additionalProperties":false}
        """);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = DxAiFunctionJsonHelper.Deserialize<DeleteParameters>(request.Parameters);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.TableName);
        await editor.DeleteRowAsync(parameters.TableName, parameters.RowId, cancellationToken).ConfigureAwait(false);
        logger.LogWarning("Approved SQLite delete completed for {TableName} row {RowId}.", parameters.TableName, parameters.RowId);
        return DxAiFunctionJsonHelper.Success(new { parameters.TableName, parameters.RowId, Operation = "Deleted" });
    }

    private sealed class DeleteParameters
    {
        public string TableName { get; set; } = string.Empty;
        public long RowId { get; set; }
    }
}

public sealed class ImportProjectTextDocumentFunction(
    ISafeTextDocumentService documents,
    ILogger<ImportProjectTextDocumentFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.document.import_text",
        "POST",
        "/api/dxai/functions/project.document.import_text/invoke",
        "Import one harmless allowlisted text document into a database-first project revision as untrusted reference data.",
        "JSON parameters: projectId required; revisionId optional; filePath required.",
        "Reads one local text file only after approval. Binary content, oversized files, unknown extensions, control-byte payloads, and invalid encodings are rejected. Text is never evaluated as regex, command, or instruction authority.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: false,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"projectId":{"type":"string","format":"uuid"},"revisionId":{"type":["string","null"],"format":"uuid"},"filePath":{"type":"string"}},"required":["projectId","filePath"],"additionalProperties":false}
        """);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = DxAiFunctionJsonHelper.Deserialize<ImportParameters>(request.Parameters);
        var imported = await documents.ImportAsync(parameters.ProjectId, parameters.RevisionId, parameters.FilePath, userConfirmed: true, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Approved project text import completed as {ImportId}; content omitted from logs.", imported.Id);
        return DxAiFunctionJsonHelper.Success(new
        {
            imported.Id,
            imported.ProjectId,
            imported.RevisionId,
            imported.SourceName,
            imported.ContentHash,
            imported.ContentType,
            imported.EncodingName,
            imported.Status,
            imported.SafetyNotes
        });
    }

    private sealed class ImportParameters
    {
        public Guid ProjectId { get; set; }
        public Guid? RevisionId { get; set; }
        public string FilePath { get; set; } = string.Empty;
    }
}

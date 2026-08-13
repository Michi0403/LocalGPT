using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services.Helpers;

namespace LocalGPT.Services;

/// <summary>
/// Represents a list sqlite tables function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the list sqlite tables function workflow to provide the corresponding application capability.</param>
/// <param name="editor">Sqlite table editor service dependency used by the list sqlite tables function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ListSqliteTablesFunction(IDxAiFunctionJsonService json,
    ISqliteTableEditorService editor,
    ILogger<ListSqliteTablesFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the list sqlite tables function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ListSqliteTablesFunction"/>.</value>
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

    /// <summary>
    /// Performs invoke for <see cref="ListSqliteTablesFunction"/>, keeping the operation consistent with the state and invariants of the surrounding list sqlite tables function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var tables = await editor.GetTablesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogDebug("DXAIFunction listed {TableCount} SQLite tables.", tables.Count);
            return json.Success(tables);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ListSqliteTablesFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ListSqliteTablesFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents a preview sqlite table function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the preview sqlite table function workflow to provide the corresponding application capability.</param>
/// <param name="editor">Sqlite table editor service dependency used by the preview sqlite table function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PreviewSqliteTableFunction(IDxAiFunctionJsonService json,
    ISqliteTableEditorService editor,
    ILogger<PreviewSqliteTableFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the preview sqlite table function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="PreviewSqliteTableFunction"/>.</value>
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

    /// <summary>
    /// Performs invoke for <see cref="PreviewSqliteTableFunction"/>, keeping the operation consistent with the state and invariants of the surrounding preview sqlite table function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<SqliteTableReadParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
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
            return json.Success(snapshot);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PreviewSqliteTableFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PreviewSqliteTableFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether sensitive column for <see cref="PreviewSqliteTableFunction"/>, keeping the operation consistent with the state and invariants of the surrounding preview sqlite table function workflow.
    /// </summary>
    /// <param name="name">Name value supplied to the preview sqlite table function operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsSensitiveColumn(string name) {
    try
    {
        return name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("apikey", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("connectionstring", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("parametersjson", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("payloadjson", StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PreviewSqliteTableFunction)}.{nameof(IsSensitiveColumn)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PreviewSqliteTableFunction)}.{nameof(IsSensitiveColumn)} failed.");
        throw;
    }
}

}

/// <summary>
/// Represents a read exact sqlite table function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the read exact sqlite table function workflow to provide the corresponding application capability.</param>
/// <param name="editor">Sqlite table editor service dependency used by the read exact sqlite table function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ReadExactSqliteTableFunction(IDxAiFunctionJsonService json,
    ISqliteTableEditorService editor,
    ILogger<ReadExactSqliteTableFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the read exact sqlite table function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ReadExactSqliteTableFunction"/>.</value>
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

    /// <summary>
    /// Performs invoke for <see cref="ReadExactSqliteTableFunction"/>, keeping the operation consistent with the state and invariants of the surrounding read exact sqlite table function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<SqliteTableReadParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.TableName);
            var snapshot = await editor.GetTableAsync(parameters.TableName, Math.Clamp(parameters.Take, 1, 100), cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved exact SQLite read completed for table {TableName} with {RowCount} row(s); values omitted from logs.", parameters.TableName, snapshot.Rows.Count);
            return json.Success(snapshot);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ReadExactSqliteTableFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ReadExactSqliteTableFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}

/// <summary>
/// Represents an upsert sqlite row function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the upsert sqlite row function workflow to provide the corresponding application capability.</param>
/// <param name="editor">Sqlite table editor service dependency used by the upsert sqlite row function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class UpsertSqliteRowFunction(IDxAiFunctionJsonService json,
    ISqliteTableEditorService editor,
    ILogger<UpsertSqliteRowFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the upsert sqlite row function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="UpsertSqliteRowFunction"/>.</value>
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

    /// <summary>
    /// Performs invoke for <see cref="UpsertSqliteRowFunction"/>, keeping the operation consistent with the state and invariants of the surrounding upsert sqlite row function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<SqliteRowUpsertParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
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
                return json.Success(new { parameters.TableName, RowId = rowId, Operation = "Updated" });
            }

            await editor.InsertRowAsync(parameters.TableName, parameters.Values, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved SQLite insert completed for {TableName}; values omitted from logs.", parameters.TableName);
            return json.Success(new { parameters.TableName, Operation = "Inserted" });
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UpsertSqliteRowFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UpsertSqliteRowFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}

/// <summary>
/// Represents a delete sqlite row function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the delete sqlite row function workflow to provide the corresponding application capability.</param>
/// <param name="editor">Sqlite table editor service dependency used by the delete sqlite row function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class DeleteSqliteRowFunction(IDxAiFunctionJsonService json,
    ISqliteTableEditorService editor,
    ILogger<DeleteSqliteRowFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the delete sqlite row function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="DeleteSqliteRowFunction"/>.</value>
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

    /// <summary>
    /// Performs invoke for <see cref="DeleteSqliteRowFunction"/>, keeping the operation consistent with the state and invariants of the surrounding delete sqlite row function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<SqliteRowDeleteParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.TableName);
            await editor.DeleteRowAsync(parameters.TableName, parameters.RowId, cancellationToken).ConfigureAwait(false);
            logger.LogWarning("Approved SQLite delete completed for {TableName} row {RowId}.", parameters.TableName, parameters.RowId);
            return json.Success(new { parameters.TableName, parameters.RowId, Operation = "Deleted" });
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DeleteSqliteRowFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DeleteSqliteRowFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}

/// <summary>
/// Represents an import project text document function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the import project text document function workflow to provide the corresponding application capability.</param>
/// <param name="documents">Safe text document service dependency used by the import project text document function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ImportProjectTextDocumentFunction(IDxAiFunctionJsonService json,
    ISafeTextDocumentService documents,
    ILogger<ImportProjectTextDocumentFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the import project text document function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ImportProjectTextDocumentFunction"/>.</value>
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

    /// <summary>
    /// Performs invoke for <see cref="ImportProjectTextDocumentFunction"/>, keeping the operation consistent with the state and invariants of the surrounding import project text document function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<ProjectTextDocumentImportParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            var imported = await documents.ImportAsync(parameters.ProjectId, parameters.RevisionId, parameters.FilePath, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved project text import completed as {ImportId}; content omitted from logs.", imported.Id);
            return json.Success(new
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ImportProjectTextDocumentFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ImportProjectTextDocumentFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}

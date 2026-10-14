using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Persists bounded function operation metadata in the existing SystemVariables store; never stores input or output payloads.</summary>
/// <param name="databaseInitialization">Database initialization service dependency used by the DevExpress AI function invocation audit workflow to provide the corresponding application capability.</param>
/// <param name="dbContextFactory">Local gpt memory database context dependency used by the DevExpress AI function invocation audit workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class DxAiFunctionInvocationAuditService(
    IDatabaseInitializationService databaseInitialization,
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    ILogger<DxAiFunctionInvocationAuditService> logger) : IDxAiFunctionInvocationAuditService
{
    /// <summary>
    /// Defines the audit data type constant used by <see cref="DxAiFunctionInvocationAuditService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string AuditDataType = "DxAiFunctionInvocationAudit";
    /// <summary>
    /// Defines the prefix constant used by <see cref="DxAiFunctionInvocationAuditService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string Prefix = "DxInvocation.";

    /// <summary>
    /// Performs record as part of the DevExpress AI function invocation audit service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="result">Result value supplied to the DevExpress AI function invocation audit operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task RecordAsync(DxAiFunctionInvocationResult result, CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitialization.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var dbDisposal = db.ConfigureAwait(false);
            var key = Prefix + result.OperationId.ToString("N");
            var entity = await db.SystemVariables.FirstOrDefaultAsync(item => item.Name == key, cancellationToken).ConfigureAwait(false);
            var audit = new DxAiFunctionInvocationAuditEntry
            {
                OperationId = result.OperationId,
                FunctionName = result.FunctionName,
                Status = result.Status,
                Succeeded = result.Succeeded,
                CreatedAtUtc = DateTime.UtcNow
            };
            if (entity is null)
            {
                entity = new SystemVariable { Name = key, DataType = AuditDataType, ValueString = JsonSerializer.Serialize(audit) };
                db.SystemVariables.Add(entity);
            }
            else
            {
                entity.DataType = AuditDataType;
                entity.ValueString = JsonSerializer.Serialize(audit);
                entity.LastUpdated = audit.CreatedAtUtc;
            }
            // Retention is bounded by a technical storage ceiling. No prompt, parameters or results are persisted.
            var old = await db.SystemVariables.Where(item => item.DataType == AuditDataType)
                .OrderByDescending(item => item.LastUpdated).Skip(250).ToListAsync(cancellationToken).ConfigureAwait(false);
            db.SystemVariables.RemoveRange(old);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            // Function execution must never be repeated simply because optional audit storage failed.
            logger.LogWarning(exception, "Persisting DX function operation metadata failed for operation {OperationId}.", result.OperationId);
        }
    }

    /// <summary>
    /// Retrieves recent as part of the DevExpress AI function invocation audit service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="take">Take value supplied to the DevExpress AI function invocation audit operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<DxAiFunctionInvocationAuditEntry>> GetRecentAsync(int take = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitialization.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var dbDisposal = db.ConfigureAwait(false);
            var records = await db.SystemVariables.AsNoTracking().Where(item => item.DataType == AuditDataType)
                .OrderByDescending(item => item.LastUpdated).Take(Math.Clamp(take, 1, 250))
                .Select(item => item.ValueString).ToListAsync(cancellationToken).ConfigureAwait(false);
            var entries = new List<DxAiFunctionInvocationAuditEntry>();
            foreach (var record in records)
            {
                try
                {
                    var entry = JsonSerializer.Deserialize<DxAiFunctionInvocationAuditEntry>(record);
                    if (entry is not null) entries.Add(entry);
                }
                catch (JsonException exception)
                {
                    logger.LogWarning(exception, "A stored DX function audit record was invalid; the record content was not logged.");
                }
            }
            return entries;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading sanitized DX function operation history failed.");
            throw;
        }
    }
}

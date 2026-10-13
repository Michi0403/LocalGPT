using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Persists bounded function operation metadata in the existing SystemVariables store; never stores input or output payloads.</summary>
public sealed class DxAiFunctionInvocationAuditService(
    IDatabaseInitializationService databaseInitialization,
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    ILogger<DxAiFunctionInvocationAuditService> logger) : IDxAiFunctionInvocationAuditService
{
    private const string AuditDataType = "DxAiFunctionInvocationAudit";
    private const string Prefix = "DxInvocation.";

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

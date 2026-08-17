using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>Persists bounded Remote Control execution audit rows independently from connector and pipeline orchestration.</summary>
/// <param name="dbContextFactory">Database context factory.</param>
/// <param name="databaseInitializer">Database initialization dependency.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
public sealed class RemoteControlExecutionStoreService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<RemoteControlExecutionStoreService> logger) : IRemoteControlExecutionStoreService
{
    /// <summary>
    /// Performs start as part of the remote control execution store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<RemoteControlExecutionRecord> StartAsync(
        string connectorKey,
        string pipelineKey,
        RemoteControlTriggerKind trigger,
        int payloadBytes,
        int? httpStatusCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var record = new RemoteControlExecutionRecord
            {
                ConnectorKey = connectorKey?.Trim() ?? string.Empty,
                PipelineKey = pipelineKey?.Trim() ?? string.Empty,
                Trigger = trigger,
                StartedAtUtc = DateTime.UtcNow,
                PayloadBytes = Math.Max(0, payloadBytes),
                HttpStatusCode = httpStatusCode,
                Summary = "Started"
            };
            db.RemoteControlExecutionRecords.Add(record);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return record;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Starting a Remote Control execution audit row was cancelled.");
            else
                logger.LogError(exception, "Starting a Remote Control execution audit row failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs complete as part of the remote control execution store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task CompleteAsync(Guid executionId, bool succeeded, int stepCount, string summary, string error, CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var record = await db.RemoteControlExecutionRecords.SingleOrDefaultAsync(item => item.Id == executionId, cancellationToken).ConfigureAwait(false);
            if (record is null) return;
            record.CompletedAtUtc = DateTime.UtcNow;
            record.Succeeded = succeeded;
            record.StepCount = Math.Max(0, stepCount);
            record.Summary = Bound(summary, 512);
            record.Error = Bound(error, 1024);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Completing Remote Control execution audit {ExecutionId} was cancelled.", executionId);
            else
                logger.LogError(exception, "Completing Remote Control execution audit {ExecutionId} failed.", executionId);
            throw;
        }
    }

    /// <summary>
    /// Performs list as part of the remote control execution store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<RemoteControlExecutionRecord>> ListAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var boundedTake = Math.Clamp(take, 1, 500);
            return await db.RemoteControlExecutionRecords.AsNoTracking()
                .OrderByDescending(item => item.StartedAtUtc)
                .Take(boundedTake)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Listing Remote Control execution audits was cancelled.");
            else
                logger.LogError(exception, "Listing Remote Control execution audits failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs bound as part of the remote control execution store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the remote control execution store operation and used when producing its result.</param>
    /// <param name="maximumLength">Maximum length value supplied to the remote control execution store operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Bound(string? value, int maximumLength)
    {
        try
        {
            var normalized = value?.Trim() ?? string.Empty;
            return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bounding Remote Control audit text failed; content was omitted.");
            throw;
        }
    }
}

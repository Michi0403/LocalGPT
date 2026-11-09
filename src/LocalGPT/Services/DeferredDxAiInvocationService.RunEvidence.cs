using LocalGPT.BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>
/// Read-only run evidence for goal review: which approval-gated function requests a Council run recorded and where
/// each one stands. No parameters or results leave the deferred store through this view.
/// </summary>
public sealed partial class DeferredDxAiInvocationService
{
    /// <summary>
    /// Lists the approval-gated function requests one Council run recorded, as technical evidence for goal review.
    /// </summary>
    /// <param name="councilRunId">Identifier of the Council run.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>Function name, status and timestamps of each recorded request, oldest first.</returns>
    public async Task<IReadOnlyList<DeferredDxAiInvocationSummary>> GetForRunAsync(
        Guid councilRunId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
                var rows = await db.DeferredDxAiInvocations.AsNoTracking()
                    .Where(item => item.CouncilRunId == councilRunId)
                    .OrderBy(item => item.CreatedAtUtc)
                    .Take(64)
                    .Select(item => new DeferredDxAiInvocationSummary(
                        item.Id, item.ApprovalRequestId, item.FunctionName, item.Status, item.ResultStatus, item.UpdatedAtUtc))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                logger.LogDebug("Council run {CouncilRunId} recorded {Count} approval-gated function request(s).", councilRunId, rows.Count);
                return rows;
            }
            finally
            {
                databaseGate.Release();
            }
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(DeferredDxAiInvocationService)}.{nameof(GetForRunAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(DeferredDxAiInvocationService)}.{nameof(GetForRunAsync)} failed.");
            throw;
        }
    }
}

using LocalGPT.BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>Exposes persisted human decisions to the source coordinator without consuming them again.</summary>
public sealed partial class HumanCollaborationService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CouncilSourceHumanDecision>> GetSourcePlanningDecisionsAsync(
        Guid councilRunId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var approvalKind = vocabulary.Get().HumanRequestApproval;
            var answered = vocabulary.Get().HumanStatusAnswered;
            var consumed = vocabulary.Get().HumanStatusConsumed;
            var requests = await db.HumanCollaborationRequests.AsNoTracking()
                .Where(item => item.CouncilRunId == councilRunId && item.RequestKind != approvalKind &&
                    (item.Status == answered || item.Status == consumed) && item.DecidedAtUtc != null &&
                    item.UserResponse != "")
                .OrderBy(item => item.DecidedAtUtc).ThenBy(item => item.Id)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            return requests.Select(item => new CouncilSourceHumanDecision(item.Id, item.Title,
                item.Description + " " + item.ResponsePrompt,
                item.UserResponse, item.DecidedAtUtc!.Value)).ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading source-planning human decisions for Council {CouncilRunId} failed.", councilRunId);
            throw;
        }
    }
}

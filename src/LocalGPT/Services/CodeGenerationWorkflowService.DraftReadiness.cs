using System.Text.Json;
using LocalGPT.BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>
/// Read-only, persisted progress checks for Council source-draft loops. This extends the
/// existing code-generation workflow service; it does not create another orchestrator.
/// </summary>
public sealed partial class CodeGenerationWorkflowService
{
    /// <inheritdoc />
    public async Task<(bool Exists, bool ReadyToSubmit, int CompletedFiles, int PlannedFiles)>
        GetCouncilDraftReadinessAsync(Guid councilRunId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (councilRunId == Guid.Empty)
                return (false, false, 0, 0);

            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var database = db.ConfigureAwait(false);
            var row = await db.CodeGenerationChangeReviews.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == councilRunId && item.CouncilRunId == councilRunId,
                    cancellationToken).ConfigureAwait(false);
            if (row is null)
                return (false, false, 0, 0);

            // A successfully submitted draft has already passed CreateReviewCoreAsync's
            // completeness and validation checks. Never re-open or duplicate its approval.
            if (string.Equals(row.Status, CodeGenerationReviewStatuses.AwaitingUserDecision,
                    StringComparison.Ordinal))
                return (true, true, 0, 0);

            if (!string.Equals(row.Status, DraftStatus, StringComparison.Ordinal))
                return (true, false, 0, 0);

            var plan = JsonSerializer.Deserialize<CreateCodeGenerationReviewRequest>(
                row.PayloadJson, JsonOptions);
            var manifest = plan?.DraftManifest;
            var files = plan?.Files;
            if (manifest is not { Count: > 0 } || files is null)
                return (true, false, files?.Count ?? 0, manifest?.Count ?? 0);

            var accepted = 0;
            foreach (var expected in manifest)
            {
                if (accepted >= files.Count)
                    break;
                var file = files[accepted];
                if (file is null || file.Content is null ||
                    !string.Equals(file.RelativePath, expected.Key, StringComparison.OrdinalIgnoreCase))
                    return (true, false, accepted, manifest.Count);
                accepted++;
            }

            // A model's SOURCE_DRAFT_READY prose is never proof of file completion.
            // Only the durable, ordered manifest and complete source payload may end the loop.
            var ready = accepted == manifest.Count && files.Count == manifest.Count;
            return (true, ready, accepted, manifest.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            // An unavailable database must never turn model prose into proof of completion.
            logger.LogWarning(error,
                "Could not verify persisted Council source draft for run {CouncilRunId}; loop completion remains unverified.",
                councilRunId);
            return (true, false, 0, 0);
        }
    }
}

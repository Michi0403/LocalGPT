using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

public sealed partial class CodeGenerationWorkflowService
{
    /// <summary>Internal review status for a draft that cannot be executed or downloaded.</summary>
    private const string DraftStatus = "SourceDraft";

    /// <inheritdoc />
    public async Task<(object Progress, CodeGenerationReviewSnapshot? Review)> ProcessDraftAsync(
        CreateCodeGenerationReviewRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var runId = request.CouncilRunId ?? throw new ArgumentException("An active Council run is required.");
            var operation = request.DraftOperation;
            if (operation is not ("begin" or "next" or "complete" or "submit"))
                throw new ArgumentException("Select begin, next, complete or submit.");

            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var database = db.ConfigureAwait(false);
            var draft = await db.CodeGenerationChangeReviews.AsNoTracking()
                .SingleOrDefaultAsync(row => row.Id == runId && row.CouncilRunId == runId, cancellationToken)
                .ConfigureAwait(false);
            if (draft is null)
            {
                if (operation != "begin")
                    throw new ArgumentException("Begin the Council source draft before taking a file.");
                draft = await BeginDraftAsync(db, runId, request, cancellationToken).ConfigureAwait(false);
            }

            if (draft.Status != DraftStatus)
            {
                var review = ToSnapshot(draft, DeserializePayload(draft.PayloadJson));
                return (new { ReviewId = review.Id, review.ReviewHash, review.Status }, operation == "submit" ? review : null);
            }

            var plan = JsonSerializer.Deserialize<CreateCodeGenerationReviewRequest>(draft.PayloadJson, JsonOptions)
                ?? throw new InvalidOperationException("The saved source draft could not be read.");
            var manifest = plan.DraftManifest ?? throw new InvalidOperationException("The saved source draft has no manifest.");
            var nextFile = manifest.Skip(plan.Files.Count).FirstOrDefault();

            if (operation == "submit")
            {
                if (plan.Files.Count != manifest.Count)
                    throw new ArgumentException($"Complete the remaining {manifest.Count - plan.Files.Count} workspace file(s) before submitting.");

                var review = await CreateReviewCoreAsync(plan, draft, cancellationToken).ConfigureAwait(false);
                return (new { ReviewId = review.Id, review.ReviewHash, review.Status, FileCount = manifest.Count }, review);
            }

            if (operation == "complete")
            {
                if (request.Files is not [{ Content: { } } submittedFile])
                    throw new ArgumentException("Complete accepts exactly one file with its full content.");
                var file = NormalizeFile(submittedFile);
                var previous = plan.Files.Find(item => string.Equals(item.RelativePath, file.RelativePath, StringComparison.OrdinalIgnoreCase));
                if (previous is not null && previous.Content != file.Content)
                    throw new ArgumentException("That workspace file was already completed with different content.");
                if (previous is null)
                {
                    if (!string.Equals(nextFile.Key, file.RelativePath, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("Complete the workspace file returned by next.");
                    var length = Encoding.UTF8.GetByteCount(file.Content);
                    var maximum = runtimePolicy.GetLong("MaxSingleFileBytes");
                    if (maximum > 0 && length > maximum)
                        throw new ArgumentException($"The file exceeds the configured maximum of {maximum} bytes.");
                    file.RelativePath = nextFile.Key;
                    file.Purpose = nextFile.Value;
                    plan.Files.Add(file);
                    var payload = JsonSerializer.Serialize(plan, JsonOptions);
                    var changed = await db.CodeGenerationChangeReviews
                        .Where(row => row.Id == runId && row.Status == DraftStatus && row.PayloadJson == draft.PayloadJson)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(row => row.PayloadJson, payload)
                            .SetProperty(row => row.UpdatedAtUtc, DateTime.UtcNow), cancellationToken).ConfigureAwait(false);
                    if (changed == 0)
                        throw new ArgumentException("The workspace changed during completion. Ask for the next file again.");
                }
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(file.Content)));
                logger.LogInformation("Council source draft {RunId} completed {CompletedFiles}/{FileCount} files.", runId, plan.Files.Count, manifest.Count);
                return (new { CompletedPath = previous?.RelativePath ?? file.RelativePath, Sha256 = hash,
                    CompletedFiles = plan.Files.Count, FileCount = manifest.Count, ReadyToSubmit = plan.Files.Count == manifest.Count }, null);
            }

            return (new { RelativePath = nextFile.Key, WorkOrder = nextFile.Value,
                CompletedFiles = plan.Files.Count, FileCount = manifest.Count, ReadyToSubmit = plan.Files.Count == manifest.Count }, null);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Council source draft operation canceled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Council source draft operation failed.");
            throw;
        }
    }

    /// <summary>Persists the ordered work manifest before authoring any file.</summary>
    /// <param name="db">Database used to persist the draft.</param>
    /// <param name="runId">Originating Council run identifier.</param>
    /// <param name="request">Manifest and output intent.</param>
    /// <param name="cancellationToken">Cancels the draft insertion.</param>
    /// <returns>The durable draft row.</returns>
    private async Task<CodeGenerationChangeReview> BeginDraftAsync(
        LocalGPT.BusinessObjects.EFCore.LocalGptMemoryDbContext db,
        Guid runId, CreateCodeGenerationReviewRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Goal) || request.DraftManifest is not { Count: > 0 } paths ||
                request.Files is { Count: > 0 } || request.CodeDomTypes is { Count: > 0 })
                throw new ArgumentException("Begin requires a goal and file manifest. Send each file's content separately.");

            var manifest = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (path, workOrder) in paths)
            {
                var relativePath = NormalizeRelativePath(path);
                if (relativePath == "." || !manifest.TryAdd(relativePath, workOrder))
                    throw new ArgumentException("The file manifest must contain distinct relative file paths.");
            }

            request.DraftManifest = manifest;
            request.Files = [];
            var draft = new CodeGenerationChangeReview
            {
                Id = runId,
                CouncilRunId = runId,
                Status = DraftStatus,
                PayloadJson = JsonSerializer.Serialize(request, JsonOptions)
            };
            db.CodeGenerationChangeReviews.Add(draft);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return draft;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Council source draft initialization canceled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Council source draft initialization failed.");
            throw;
        }
    }
}

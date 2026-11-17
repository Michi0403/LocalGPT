using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>Preserves approved review lineage during targeted source repair.</summary>
public sealed partial class CodeGenerationWorkflowService
{
    /// <summary>Normalizes declared dependency paths and rejects missing or cyclic file dependencies.</summary>
    /// <param name="manifest">Canonical source paths with work orders.</param>
    /// <param name="dependencies">Optional architect-declared direct dependencies.</param>
    /// <returns>Validated direct file-dependency graph.</returns>
    private Dictionary<string, List<string>> NormalizeDraftDependencies(Dictionary<string, string> manifest,
        Dictionary<string, List<string>>? dependencies)
    {
        try
        {
            var normalized = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (rawPath, rawDependencies) in dependencies ?? [])
            {
                var path = NormalizeRelativePath(rawPath);
                var direct = rawDependencies.Select(NormalizeRelativePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (!manifest.ContainsKey(path) || direct.Any(dependency => !manifest.ContainsKey(dependency)) ||
                    !normalized.TryAdd(path, direct))
                    throw new ArgumentException("Draft dependencies must name distinct paths in the source manifest.");
            }
            var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (resolved.Count < manifest.Count)
            {
                var ready = manifest.Keys.Where(path => !resolved.Contains(path) &&
                    (normalized.GetValueOrDefault(path) ?? []).All(resolved.Contains)).ToList();
                if (ready.Count == 0)
                    throw new ArgumentException("The draft dependency graph contains a cycle. Declare mutual interface contracts without cyclic file dependencies.");
                resolved.UnionWith(ready);
            }
            return normalized;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Validating source draft dependencies failed.");
            throw;
        }
    }

    /// <summary>Creates a new draft from an actual immutable review, preserving every unaffected source file.</summary>
    /// <param name="db">Owned database context.</param>
    /// <param name="runId">Originating Council execution identifier.</param>
    /// <param name="head">Current lineage head, which must match the requested base.</param>
    /// <param name="request">Exact base review identity/hash and paths with corrective work orders.</param>
    /// <param name="cancellationToken">Cancels creation of the successor draft.</param>
    /// <returns>Persisted successor draft with its own immutable-review identity.</returns>
    private async Task<CodeGenerationChangeReview> BeginRepairDraftAsync(LocalGptMemoryDbContext db,
        Guid runId, CodeGenerationChangeReview? head, CreateCodeGenerationReviewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (head is null || head.Id != request.DraftRepairReviewId ||
                !string.Equals(head.ReviewHash, request.DraftRepairReviewHash, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(head.ReviewHash) ||
                head.Status is not (CodeGenerationReviewStatuses.BuildFailed or CodeGenerationReviewStatuses.BuildPassed or CodeGenerationReviewStatuses.Generated) ||
                request.DraftManifest is not { Count: > 0 })
                throw new ArgumentException("Repair requires the current completed source review, its exact hash and specific faulty or missing file paths. Pending approvals cannot be replaced.");
            var payload = DeserializePayload(head.PayloadJson);
            if (payload.DraftManifest is null || payload.CodeDomTypes.Count != 0)
                throw new ArgumentException("Targeted draft repair requires an existing file-authored source manifest.");
            var manifest = new Dictionary<string, string>(payload.DraftManifest, StringComparer.OrdinalIgnoreCase);
            var repairedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var previousSource = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (rawPath, workOrder) in request.DraftManifest)
            {
                var path = NormalizeRelativePath(rawPath);
                if (path == "." || !repairedPaths.Add(path) || string.IsNullOrWhiteSpace(workOrder))
                    throw new ArgumentException("Each corrective work order must name a distinct source file and its concrete diagnostic.");
                var previous = payload.Files.SingleOrDefault(file => string.Equals(file.RelativePath, path, StringComparison.OrdinalIgnoreCase));
                if (previous is not null)
                    previousSource[path] = previous.Content;
                manifest[path] = workOrder;
            }
            var plan = new CreateCodeGenerationReviewRequest
            {
                CouncilRunId = runId,
                ProjectId = head.ProjectId,
                ProjectRevisionId = head.ProjectRevisionId,
                Title = head.Title,
                Goal = head.Goal,
                CurrentProjectState = head.CurrentProjectState,
                CouncilSummary = head.CouncilSummary,
                ChangeSummary = head.ChangeSummary,
                SafetySummary = head.SafetySummary,
                Outputs = payload.Outputs,
                Files = payload.Files.Where(file => !repairedPaths.Contains(file.RelativePath)).ToList(),
                DraftManifest = manifest,
                DraftWorkspaceMode = payload.DraftWorkspaceMode,
                DraftGuestLanguageKey = payload.DraftGuestLanguageKey,
                DraftAssignments = payload.DraftAssignments.Where(assignment => !repairedPaths.Contains(assignment.RelativePath)).ToList(),
                DraftInspections = payload.DraftInspections.Where(inspection => !repairedPaths.Contains(inspection.Path)).ToList(),
                DraftSharedContract = payload.DraftSharedContract,
                DraftDependencies = NormalizeDraftDependencies(manifest, payload.DraftDependencies),
                DraftRepairSources = previousSource,
                DraftRepairReviewId = head.Id,
                DraftRepairReviewHash = head.ReviewHash
            };
            var draft = new CodeGenerationChangeReview
            {
                CouncilRunId = runId,
                ProjectId = head.ProjectId,
                ProjectRevisionId = head.ProjectRevisionId,
                Title = head.Title,
                Goal = head.Goal,
                Status = DraftStatus,
                PayloadJson = JsonSerializer.Serialize(plan, JsonOptions)
            };
            // The previous review remains unchanged. Creation alone grants no file-system or build approval.
            db.CodeGenerationChangeReviews.Add(draft);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Created corrective source draft {DraftId} from review {ReviewId}: {Count} reopened paths.",
                draft.Id, head.Id, repairedPaths.Count);
            return draft;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Preparing targeted source repair for Council {RunId} failed.", runId);
            throw;
        }
    }
}

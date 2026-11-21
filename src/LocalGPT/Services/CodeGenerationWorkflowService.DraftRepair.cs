using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>Preserves approved review lineage during targeted source repair.</summary>
public sealed partial class CodeGenerationWorkflowService
{
    /// <summary>Stable content hash for the run-owned path/work-order manifest, not source bytes.</summary>
    private string ComputeDraftManifestHash(IReadOnlyDictionary<string, string> manifest)
    {
        var normalized = manifest.OrderBy(item => item.Key.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase)
            .Select(item => new { Path = item.Key.Replace('\\', '/'), WorkOrder = item.Value });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(normalized, JsonOptions))));
    }

    /// <summary>Adds reviewed work obligations to an active source draft, retaining every source hash and prior inspection.
    /// The exact manifest hash and draft ID fence stale coordinator proposals. No approval or filesystem write occurs.</summary>
    private async Task<(object Progress, CodeGenerationReviewSnapshot? Review)> AmendDraftManifestAsync(
        LocalGptMemoryDbContext db, Guid runId, CodeGenerationChangeReview row,
        CreateCodeGenerationReviewRequest request, CancellationToken cancellationToken)
    {
        if (row.Status != DraftStatus || row.Id != request.DraftAmendmentDraftId ||
            string.IsNullOrWhiteSpace(request.DraftAmendmentManifestHash) ||
            request.Files is { Count: > 0 } || request.CodeDomTypes is { Count: > 0 } ||
            (request.DraftManifest is not { Count: > 0 } && request.DraftAmendmentReopenHashes is not { Count: > 0 }))
            throw new ArgumentException("Amend requires an active exact draft ID, its manifest hash and new path/work orders or exact existing file hashes; files and codeDomTypes are forbidden.");
        var plan = ReadDraftPlan(row);
        var policy = runtimePolicy.GetJson<CouncilFileWorkRuntimeParameters>("CouncilFileWorkRuntimeParametersJson");
        if (plan.DraftAmendmentCount >= Math.Max(0, policy.MaximumDraftAmendments))
            throw new ArgumentException("The active draft exhausted the configured amendment limit. Preserve the draft and request supervised continuation.");
        var original = plan.DraftManifest!;
        if (!string.Equals(ComputeDraftManifestHash(original), request.DraftAmendmentManifestHash, StringComparison.Ordinal))
            throw new ArgumentException("The draft manifest changed since planning. Refresh exact paths and the manifest hash before amending.");
        var expanded = new Dictionary<string, string>(original, StringComparer.OrdinalIgnoreCase);
        var additions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (rawPath, workOrder) in request.DraftManifest ?? [])
        {
            if (rawPath.TrimEnd().EndsWith('/') || rawPath.TrimEnd().EndsWith('\\') || string.IsNullOrWhiteSpace(workOrder))
                throw new ArgumentException("An amendment must name concrete new source files and nonempty work orders.");
            var path = NormalizeRelativePath(rawPath);
            if (path == "." || string.Equals(path, "CHANGE_REVIEW.md", StringComparison.OrdinalIgnoreCase) ||
                !additions.TryAdd(path, workOrder) || !expanded.TryAdd(path, workOrder))
                throw new ArgumentException($"Amendment path '{path}' already exists or is repeated; existing source files must not be overwritten.");
        }
        var limit = runtimePolicy.GetJson<CouncilFileWorkRuntimeParameters>("CouncilFileWorkRuntimeParametersJson");
        if (expanded.Count > Math.Max(1, limit.MaximumManifestFiles))
            throw new ArgumentException("Amendment exceeds the configured manifest file limit.");
        foreach (var path in expanded.Keys)
        {
            var segments = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any(segment => string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Amendment must not author compiler output directories.");
            for (var count = 1; count < segments.Length; count++)
            {
                if (expanded.ContainsKey(string.Join("/", segments.Take(count))))
                    throw new ArgumentException($"Manifest path '{path}' conflicts with an existing file/directory.");
            }
        }
        var reopened = new Dictionary<string, CodeGenerationFileSpec>(StringComparer.OrdinalIgnoreCase);
        foreach (var (rawPath, expectedHash) in request.DraftAmendmentReopenHashes ?? [])
        {
            var path = NormalizeRelativePath(rawPath);
            var originalFile = plan.Files.SingleOrDefault(file => string.Equals(file.RelativePath, path, StringComparison.OrdinalIgnoreCase));
            if (originalFile is null || !original.ContainsKey(path) || additions.ContainsKey(path))
                throw new ArgumentException($"Only an existing committed source file may be reopened: '{path}'.");
            var currentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(originalFile.Content)));
            if (!string.Equals(currentHash, expectedHash, StringComparison.Ordinal))
                throw new ArgumentException($"Source hash for '{path}' changed; refresh the draft and propose a new repair.");
            reopened.Add(path, originalFile);
        }
        var edges = new Dictionary<string, List<string>>(plan.DraftDependencies ?? [], StringComparer.OrdinalIgnoreCase);
        foreach (var (rawPath, prerequisites) in request.DraftDependencies ?? [])
        {
            var path = NormalizeRelativePath(rawPath);
            if (prerequisites is null)
                throw new ArgumentException($"Amended file dependencies for '{path}' cannot be null.");
            var normalized = prerequisites.Select(NormalizeRelativePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (edges.TryGetValue(path, out var existing))
            {
                if (existing.Any(previous => !normalized.Contains(previous, StringComparer.OrdinalIgnoreCase)))
                    throw new ArgumentException($"Amendment cannot remove original dependencies for '{path}'.");
                if (normalized.Except(existing, StringComparer.OrdinalIgnoreCase).Any(dependency => !additions.ContainsKey(dependency)))
                    throw new ArgumentException($"New dependencies for '{path}' must be newly added manifest paths.");
            }
            edges[path] = normalized;
        }
        var validated = NormalizeDraftDependencies(expanded, edges);
        var humanDecisions = await humanCollaboration.GetSourcePlanningDecisionsAsync(runId, cancellationToken).ConfigureAwait(false);
        var coverage = ValidateSourceManifestCoverage(plan.Goal, expanded, humanDecisions);
        if (!string.IsNullOrWhiteSpace(coverage))
            throw new ArgumentException("Amended manifest still omits a protected requirement: " + coverage);
        var invalidatedPaths = validated.Where(edge => !additions.ContainsKey(edge.Key) &&
            !(plan.DraftDependencies?.GetValueOrDefault(edge.Key) ?? []).SequenceEqual(edge.Value, StringComparer.OrdinalIgnoreCase))
            .Select(edge => edge.Key).ToList();
        plan.DraftManifest = expanded;
        plan.DraftDependencies = validated;
        plan.DraftAmendmentCount++;
        foreach (var (path, originalFile) in reopened)
        {
            plan.DraftRepairSources[path] = originalFile.Content;
            plan.DraftReopenAttemptOffsets[path] = plan.DraftAssignments.Count(assignment =>
                string.Equals(assignment.RelativePath, path, StringComparison.OrdinalIgnoreCase));
            plan.Files.Remove(originalFile);
        }
        plan.DraftRequirements = BuildSourceRequirementGraph(plan.Goal, expanded, humanDecisions, plan.Files);
        // The draft still owns the exact existing files, hashes, author attempts and prior inspection receipts.
        // Changed dependency hashes invalidate only the affected inspections when read by GetDraftInspectionsAsync.
        if (!await SaveDraftPlanAsync(db, row, plan, cancellationToken).ConfigureAwait(false))
            throw new ArgumentException("Concurrent source work changed the draft. Refresh its manifest hash; no amendment was committed.");
        logger.LogInformation("Council {RunId} source draft {DraftId} amended with {NewCount} new paths and {ReopenedCount} explicitly reopened sources; retained {ExistingCount} original manifest paths.",
            runId, row.Id, additions.Count, reopened.Count, original.Count);
        return (new { ReviewId = row.Id, NewPaths = additions.Keys.ToList(), ReopenedPaths = reopened.Keys.ToList(),
            FileCount = expanded.Count, CompletedFiles = plan.Files.Count, AmendmentCount = plan.DraftAmendmentCount,
            ManifestSha256 = ComputeDraftManifestHash(expanded),
            RequiresReinspection = invalidatedPaths }, null);
    }

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
                if (rawDependencies is null || rawDependencies.Any(string.IsNullOrWhiteSpace))
                    throw new ArgumentException($"Draft dependency owner '{path}' requires a list of nonempty prerequisite paths.");
                var direct = rawDependencies.Select(NormalizeRelativePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (!manifest.ContainsKey(path))
                    throw new ArgumentException($"Draft dependency owner '{path}' is not a source manifest path.");
                var unknown = direct.FirstOrDefault(dependency => !manifest.ContainsKey(dependency));
                if (unknown is not null)
                    throw new ArgumentException($"Draft dependency '{unknown}' for '{path}' is not a source manifest path.");
                if (!normalized.TryAdd(path, direct))
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
                !head.ApprovalConsumed ||
                head.Status is not (CodeGenerationReviewStatuses.BuildFailed or CodeGenerationReviewStatuses.BuildPassed or CodeGenerationReviewStatuses.Generated or
                    CodeGenerationReviewStatuses.BuildDisabled or CodeGenerationReviewStatuses.BuildNotAttempted) ||
                (request.DraftManifest is not { Count: > 0 } && request.DraftRepairRemoveHashes is not { Count: > 0 }))
                throw new ArgumentException("Repair requires the current completed source review, its exact hash and corrected/missing or explicitly removed file paths. Pending approvals cannot be replaced.");
            var payload = DeserializePayload(head.PayloadJson);
            if (payload.DraftManifest is null || payload.CodeDomTypes.Count != 0)
                throw new ArgumentException("Targeted draft repair requires an existing file-authored source manifest.");
            var manifest = new Dictionary<string, string>(payload.DraftManifest, StringComparer.OrdinalIgnoreCase);
            var removedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (rawPath, expectedHash) in request.DraftRepairRemoveHashes ?? [])
            {
                var path = NormalizeRelativePath(rawPath);
                var oldFile = payload.Files.SingleOrDefault(file => string.Equals(file.RelativePath, path, StringComparison.OrdinalIgnoreCase));
                if (!manifest.ContainsKey(path) || oldFile is null || !removedPaths.Add(path) ||
                    !string.Equals(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(oldFile.Content))),
                        expectedHash, StringComparison.Ordinal))
                    throw new ArgumentException($"Removing '{path}' from a successor review requires its exact previously reviewed source hash.");
                manifest.Remove(path);
            }
            var repairedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var previousSource = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (rawPath, workOrder) in request.DraftManifest ?? [])
            {
                var path = NormalizeRelativePath(rawPath);
                if (path == "." || removedPaths.Contains(path) || !repairedPaths.Add(path) || string.IsNullOrWhiteSpace(workOrder))
                    throw new ArgumentException("Each corrective work order must name a distinct source file and its concrete diagnostic.");
                var previous = payload.Files.SingleOrDefault(file => string.Equals(file.RelativePath, path, StringComparison.OrdinalIgnoreCase));
                if (previous is not null)
                    previousSource[path] = previous.Content;
                manifest[path] = workOrder;
            }
            if (manifest.Count == 0 || manifest.Count > Math.Max(1,
                runtimePolicy.GetJson<CouncilFileWorkRuntimeParameters>("CouncilFileWorkRuntimeParametersJson").MaximumManifestFiles))
                throw new ArgumentException("The successor manifest must remain nonempty and within the configured file limit.");
            var latestDecisions = await humanCollaboration.GetSourcePlanningDecisionsAsync(runId, cancellationToken).ConfigureAwait(false);
            var coverage = ValidateSourceManifestCoverage(head.Goal, manifest, latestDecisions);
            if (!string.IsNullOrWhiteSpace(coverage))
                throw new ArgumentException("Successor repair omits a protected requirement: " + coverage);
            var retainedDependencies = new Dictionary<string, List<string>>(
                payload.DraftDependencies ?? [], StringComparer.OrdinalIgnoreCase);
            foreach (var path in removedPaths) retainedDependencies.Remove(path);
            foreach (var (rawPath, requirements) in request.DraftDependencies ?? [])
            {
                var owner = NormalizeRelativePath(rawPath);
                if (!repairedPaths.Contains(owner) || requirements is null || requirements.Any(string.IsNullOrWhiteSpace))
                    throw new ArgumentException($"Only an explicitly repaired or added file may revise its dependency edges: '{owner}'.");
                retainedDependencies[owner] = requirements.Select(NormalizeRelativePath)
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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
                Files = payload.Files.Where(file => !repairedPaths.Contains(file.RelativePath) &&
                    !removedPaths.Contains(file.RelativePath)).ToList(),
                DraftManifest = manifest,
                DraftWorkspaceMode = payload.DraftWorkspaceMode,
                DraftGuestLanguageKey = payload.DraftGuestLanguageKey,
                DraftAssignments = payload.DraftAssignments.Where(assignment => !repairedPaths.Contains(assignment.RelativePath) &&
                    !removedPaths.Contains(assignment.RelativePath)).ToList(),
                DraftInspections = payload.DraftInspections.Where(inspection => !repairedPaths.Contains(inspection.Path) &&
                    !removedPaths.Contains(inspection.Path)).ToList(),
                DraftRequirements = BuildSourceRequirementGraph(head.Goal, manifest, latestDecisions,
                    payload.Files.Where(file => !repairedPaths.Contains(file.RelativePath) && !removedPaths.Contains(file.RelativePath)).ToList()),
                DraftSharedContract = payload.DraftSharedContract,
                DraftDependencies = NormalizeDraftDependencies(manifest, retainedDependencies),
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
            logger.LogInformation("Created corrective source draft {DraftId} from review {ReviewId}: {ReopenedCount} reopened and {RemovedCount} explicitly removed paths; old review is immutable.",
                draft.Id, head.Id, repairedPaths.Count, removedPaths.Count);
            return draft;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Preparing targeted source repair for Council {RunId} failed.", runId);
            throw;
        }
    }
}

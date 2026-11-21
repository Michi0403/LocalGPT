using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>Owns exclusive file assignment, source receipt and draft concurrency behavior.</summary>
public sealed partial class CodeGenerationWorkflowService
{
    /// <inheritdoc />
    public async Task<CodeGenerationDraftProgress?> GetCouncilDraftProgressAsync(Guid runId, CancellationToken cancellationToken, bool includePathHistory = false)
    {
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var row = await FindCouncilDraftAsync(db, runId, cancellationToken).ConfigureAwait(false);
            if (row is null)
                return null;
            var isDraft = row.Status == DraftStatus;
            var plan = isDraft ? ReadDraftPlan(row) : null;
            var payload = isDraft ? null : DeserializePayload(row.PayloadJson);
            var manifest = plan?.DraftManifest ?? payload?.DraftManifest;
            if (manifest is null)
                return null;
            var files = plan?.Files ?? payload!.Files;
            var hashes = files.ToDictionary(file => file.RelativePath,
                file => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(file.Content))),
                StringComparer.OrdinalIgnoreCase);
            var assignments = plan?.DraftAssignments ?? payload!.DraftAssignments;
            var decisions = plan?.DraftInspections ?? payload!.DraftInspections;
            var dependencies = plan?.DraftDependencies ?? payload?.DraftDependencies ?? [];
            var paths = includePathHistory ? manifest.Keys.Select(path =>
            {
                var attempts = assignments.Where(item => string.Equals(item.RelativePath, path, StringComparison.OrdinalIgnoreCase)).ToList();
                var inspections = decisions.Where(item => string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase)).ToList();
                var current = hashes.GetValueOrDefault(path) ?? string.Empty;
                var direct = dependencies.FirstOrDefault(pair => string.Equals(pair.Key, path, StringComparison.OrdinalIgnoreCase)).Value ?? [];
                var accepted = current.Length > 0 && direct.All(hashes.ContainsKey) && inspections.Any(item =>
                    item.Accepted == true && item.Sha256 == current && item.DependencyHashes.Count == direct.Count &&
                    direct.All(dependency => item.DependencyHashes.TryGetValue(dependency, out var inspected) && inspected == hashes[dependency]));
                var last = attempts.LastOrDefault();
                return new CodeGenerationDraftPathProgress(path, current, last?.ModelName ?? string.Empty,
                    last?.Attempt ?? 0, accepted, !string.IsNullOrWhiteSpace(last?.Diagnostic) ? last.Diagnostic : inspections.LastOrDefault()?.Diagnostic ?? string.Empty,
                    direct, attempts, inspections) { DependencyDisplay = string.Join(", ", direct) };
            }).ToList() : new List<CodeGenerationDraftPathProgress>();
            return new CodeGenerationDraftProgress(row.Id, row.ProjectId, row.ProjectRevisionId, row.Status,
                row.ReviewHash, row.BuildStatus, row.WorkspaceName, row.ZipFileName,
                manifest.Keys.Where(path => !hashes.ContainsKey(path)).ToList(), hashes, isDraft,
                plan?.DraftWorkspaceMode ?? payload!.DraftWorkspaceMode,
                plan?.DraftGuestLanguageKey ?? payload!.DraftGuestLanguageKey) { AmendmentCount = plan?.DraftAmendmentCount ?? 0, ManifestSha256 = ComputeDraftManifestHash(manifest), ManifestWorkOrders = new Dictionary<string, string>(manifest, StringComparer.OrdinalIgnoreCase), Paths = paths, Tree = BuildDraftProgressTree(paths) };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading source draft progress for Council {RunId} failed.", runId);
            throw;
        }
    }

    /// <summary>Creates a stable folder hierarchy from canonical manifest paths for the read-only tree view.</summary>
    /// <param name="paths">Current committed and missing file projections.</param>
    /// <returns>Directory and file nodes ordered by canonical path.</returns>
    private IReadOnlyList<CodeGenerationDraftTreeNode> BuildDraftProgressTree(IReadOnlyList<CodeGenerationDraftPathProgress> paths)
    {
        try
        {
            var roots = new List<CodeGenerationDraftTreeNode>();
            foreach (var path in paths.OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase))
            {
                var parts = path.Path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                var children = roots;
                for (var index = 0; index < parts.Length - 1; index++)
                {
                    var folder = children.FirstOrDefault(node => node.File is null && string.Equals(node.Name, parts[index], StringComparison.OrdinalIgnoreCase));
                    if (folder is null)
                    {
                        folder = new CodeGenerationDraftTreeNode { Name = parts[index] };
                        children.Add(folder);
                    }
                    children = folder.Children;
                }
                if (parts.Length > 0)
                    children.Add(new CodeGenerationDraftTreeNode { Name = $"{parts[^1]} · {(!string.IsNullOrWhiteSpace(path.Sha256) ? path.InspectionAccepted ? "Inspected" : "Stored" : "Missing")}", Path = path.Path, File = path });
            }
            return roots;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Preparing Council source progress tree failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetCouncilDraftRunIdsAsync(IReadOnlyList<Guid> runIds, CancellationToken cancellationToken)
    {
        try
        {
            var ids = runIds.Distinct().Take(50).ToArray();
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await db.CodeGenerationChangeReviews.AsNoTracking()
                .Where(row => row.CouncilRunId.HasValue && ids.Contains(row.CouncilRunId.Value) &&
                    (row.Id == row.CouncilRunId.Value || row.Status == DraftStatus))
                .Select(row => row.CouncilRunId!.Value).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading Council draft run index failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CodeGenerationDraftWorkPacket>> ReserveDraftFilesAsync(Guid runId,
        IReadOnlyList<string> modelNames, int maximumAttempts, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var row = await FindCouncilDraftAsync(db, runId, cancellationToken).ConfigureAwait(false);
            if (row is null || row.Status != DraftStatus)
                return [];
            var plan = ReadDraftPlan(row);
            var manifest = plan.DraftManifest!;
            var completed = plan.Files.ToDictionary(file => file.RelativePath, StringComparer.OrdinalIgnoreCase);
            var available = modelNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var packets = new List<CodeGenerationDraftWorkPacket>();
            // The coordinator invokes this only after the preceding batch has drained. A new
            // persisted attempt fences any unfinished attempt of the same path after restart/Stop.
            foreach (var (path, workOrder) in manifest)
            {
                if (available.Count == 0)
                    break;
                if (completed.ContainsKey(path))
                    continue;
                var prior = plan.DraftAssignments.Where(item => string.Equals(item.RelativePath, path,
                    StringComparison.OrdinalIgnoreCase)).ToList();
                if (prior.Count - plan.DraftReopenAttemptOffsets.GetValueOrDefault(path) >= maximumAttempts)
                    continue;
                var dependencies = plan.DraftDependencies?.GetValueOrDefault(path) ?? [];
                if (dependencies.Any(dependency => !completed.ContainsKey(dependency)))
                    continue;
                var sourceDependencies = dependencies.Select(dependency => completed[dependency]).ToList();
                // Learn from real accepted source receipts, never model-family names or prose ratings.
                // Avoid the last failed route where another eligible worker exists.
                var last = prior.LastOrDefault();
                var model = available.OrderBy(candidate => last is not null &&
                        string.Equals(last.ModelName, candidate, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                    .ThenBy(candidate => prior.Count(attempt => string.Equals(attempt.ModelName, candidate, StringComparison.OrdinalIgnoreCase)))
                    .ThenByDescending(candidate => plan.DraftAssignments.Count(attempt =>
                        string.Equals(attempt.ModelName, candidate, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrEmpty(attempt.Sha256) && string.Equals(Path.GetExtension(attempt.RelativePath), Path.GetExtension(path), StringComparison.OrdinalIgnoreCase) &&
                        plan.DraftInspections.Any(inspection => inspection.Accepted == true && inspection.Sha256 == attempt.Sha256 &&
                            string.Equals(inspection.Path, attempt.RelativePath, StringComparison.OrdinalIgnoreCase))))
                    .First();
                available.Remove(model);
                var assignment = new CodeGenerationDraftAssignment
                {
                    DraftId = row.Id,
                    RelativePath = path,
                    ModelName = model,
                    Attempt = prior.Count + 1,
                    DependencyHashes = sourceDependencies.ToDictionary(file => file.RelativePath,
                        file => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(file.Content))),
                        StringComparer.OrdinalIgnoreCase)
                };
                plan.DraftAssignments.Add(assignment);
                var lastRejection = plan.DraftInspections.LastOrDefault(inspection => inspection.Accepted == false &&
                    string.Equals(inspection.Path, path, StringComparison.OrdinalIgnoreCase));
                var correctiveEvidence = string.Join(Environment.NewLine, new[] { workOrder, lastRejection?.Diagnostic, last?.Diagnostic }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));
                packets.Add(new CodeGenerationDraftWorkPacket(assignment, plan.Goal, plan.DraftSharedContract,
                    correctiveEvidence, sourceDependencies, plan.DraftRepairSources.GetValueOrDefault(path) ?? string.Empty));
            }
            if (packets.Count > 0 && !await SaveDraftPlanAsync(db, row, plan, cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("The source draft changed during assignment. No file workers were started.");
            logger.LogInformation("Council {RunId} reserved {Count} independent source-file assignments.", runId, packets.Count);
            return packets;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reserving source draft assignments failed for Council {RunId}.", runId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CodeGenerationDraftAssignment> CompleteDraftFileAsync(Guid runId,
        CodeGenerationDraftAssignment assignment, string modelName, string visibleContent, string? error,
        CancellationToken cancellationToken, SessionSandboxRunSnapshot? guestReceipt = null)
    {
        try
        {
            if (!string.Equals(assignment.ModelName, modelName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A source-file completion must come from its assigned provider route.");
            var source = string.IsNullOrWhiteSpace(error)
                ? guestReceipt is null ? ExtractAssignedFileSource(visibleContent) : ExtractGuestFileSource(assignment, guestReceipt)
                : null;
            var diagnostic = error ?? string.Empty;
            if (source is null && string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The author did not return exactly one complete fenced source file.";
            var maximum = runtimePolicy.GetLong("MaxSingleFileBytes");
            if (source is not null && maximum > 0 && Encoding.UTF8.GetByteCount(source) > maximum)
            {
                diagnostic = "The source file exceeds the configured draft file-size limit.";
                source = null;
            }
            // Multiple independent completions may race in the same JSON row; re-read and
            // merge only this path when another writer wins the compare-and-swap.
            var conflicts = Math.Max(1, runtimePolicy.GetJson<CouncilFileWorkRuntimeParameters>("CouncilFileWorkRuntimeParametersJson").MaximumCommitConflicts);
            for (var attempt = 0; attempt < conflicts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var row = await db.CodeGenerationChangeReviews.AsNoTracking().SingleOrDefaultAsync(item =>
                    item.Id == assignment.DraftId && item.CouncilRunId == runId && item.Status == DraftStatus,
                    cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("The assigned source draft is no longer writable.");
                var plan = ReadDraftPlan(row);
                if (source is not null && (plan.DraftWorkspaceMode == "HyperlightSourceDraft") != (guestReceipt is not null))
                    throw new InvalidOperationException("Candidate execution mode differs from the persisted workspace decision.");
                if (guestReceipt is not null && (guestReceipt.ProjectId != row.ProjectId || guestReceipt.LanguageKey != plan.DraftGuestLanguageKey))
                    throw new InvalidOperationException("Guest receipt does not belong to the source draft project or selected runtime.");
                var current = plan.DraftAssignments.LastOrDefault(item => string.Equals(item.RelativePath,
                    assignment.RelativePath, StringComparison.OrdinalIgnoreCase));
                if (current is null || current.Id != assignment.Id || !string.Equals(current.ModelName, modelName,
                    StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The file assignment was replaced. Its stale response cannot write source.");
                if (!string.IsNullOrWhiteSpace(current.Sha256) || !string.IsNullOrWhiteSpace(current.Diagnostic))
                    return current;
                if (plan.Files.Any(file => string.Equals(file.RelativePath, current.RelativePath, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("The reserved file already has committed source.");
                if (source is not null)
                {
                    var targetDiagnostic = ValidateDraftProjectTarget(plan.Goal, current.RelativePath, source);
                    if (string.IsNullOrWhiteSpace(targetDiagnostic))
                        targetDiagnostic = ValidateDraftSourceReferences(current.RelativePath, source,
                            plan.DraftManifest ?? new Dictionary<string, string>());
                    if (!string.IsNullOrEmpty(targetDiagnostic))
                    {
                        diagnostic = targetDiagnostic;
                        source = null;
                    }
                }
                if (source is not null)
                {
                    var candidateHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
                    if (plan.DraftRepairSources.ContainsKey(current.RelativePath) &&
                        plan.DraftInspections.Any(item => item.Accepted == false && item.Sha256 == candidateHash &&
                            string.Equals(item.Path, current.RelativePath, StringComparison.OrdinalIgnoreCase) &&
                            item.DependencyHashes.Count == current.DependencyHashes.Count &&
                            current.DependencyHashes.All(pair => item.DependencyHashes.TryGetValue(pair.Key, out var previousHash) && previousHash == pair.Value)))
                    {
                        diagnostic = runtimePolicy.GetString("CouncilFileWorkRepeatedCandidateNotice");
                        source = null;
                    }
                }
                if (source is null)
                    current.Diagnostic = diagnostic;
                else
                {
                    current.GuestRunId = guestReceipt?.RunId;
                    current.GuestSourceSha256 = guestReceipt?.SourceSha256 ?? string.Empty;
                    current.Sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
                    plan.Files.Add(new CodeGenerationFileSpec
                    {
                        RelativePath = current.RelativePath,
                        Content = source,
                        Purpose = plan.DraftManifest![current.RelativePath]
                    });
                }
                if (await SaveDraftPlanAsync(db, row, plan, cancellationToken).ConfigureAwait(false))
                {
                    logger.LogInformation("Council {RunId} source assignment {AssignmentId} committed; accepted={Accepted}.",
                        runId, current.Id, source is not null);
                    return current;
                }
            }
            throw new InvalidOperationException("The source draft remained busy while storing a file receipt.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Completing source assignment {AssignmentId} failed for Council {RunId}.", assignment.Id, runId);
            throw;
        }
    }

    /// <summary>Finds the active source draft or its latest submitted successor without guessing a model-provided identifier.</summary>
    /// <param name="db">Owned database context.</param>
    /// <param name="runId">Council execution identifier.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The source draft lineage head, if any.</returns>
    private async Task<CodeGenerationChangeReview?> FindCouncilDraftAsync(LocalGptMemoryDbContext db,
        Guid runId, CancellationToken cancellationToken)
    {
        try
        {
            var active = await db.CodeGenerationChangeReviews.AsNoTracking().SingleOrDefaultAsync(row =>
                row.CouncilRunId == runId && row.Status == DraftStatus, cancellationToken).ConfigureAwait(false);
            if (active is not null)
                return active;
            var candidates = await db.CodeGenerationChangeReviews.AsNoTracking()
                .Where(row => row.CouncilRunId == runId).OrderByDescending(row => row.CreatedAtUtc)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            return candidates.FirstOrDefault(row => row.Id == runId || DeserializePayload(row.PayloadJson).DraftManifest is not null);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Locating source draft lineage failed for Council {RunId}.", runId);
            throw;
        }
    }

    /// <summary>Deserializes and normalizes the draft's path-keyed state after each database read.</summary>
    /// <param name="row">Persisted writable source draft.</param>
    /// <returns>Draft with case-insensitive canonical path dictionaries.</returns>
    private CreateCodeGenerationReviewRequest ReadDraftPlan(CodeGenerationChangeReview row)
    {
        try
        {
            var plan = JsonSerializer.Deserialize<CreateCodeGenerationReviewRequest>(row.PayloadJson, JsonOptions)
                ?? throw new InvalidOperationException("The saved source draft could not be read.");
            plan.DraftManifest = new Dictionary<string, string>(plan.DraftManifest ?? throw new InvalidOperationException(
                "The source draft has no manifest."), StringComparer.OrdinalIgnoreCase);
            plan.DraftDependencies = new Dictionary<string, List<string>>(plan.DraftDependencies ?? [], StringComparer.OrdinalIgnoreCase);
            plan.DraftRepairSources = new Dictionary<string, string>(plan.DraftRepairSources ?? [], StringComparer.OrdinalIgnoreCase);
            plan.DraftAmendmentReopenHashes = new Dictionary<string, string>(plan.DraftAmendmentReopenHashes ?? [], StringComparer.OrdinalIgnoreCase);
            plan.DraftReopenAttemptOffsets = new Dictionary<string, int>(plan.DraftReopenAttemptOffsets ?? [], StringComparer.OrdinalIgnoreCase);
            plan.DraftAssignments ??= [];
            plan.DraftInspections ??= [];
            plan.DraftRequirements ??= [];
            plan.Files ??= [];
            return plan;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading source draft metadata failed for review {ReviewId}.", row.Id);
            throw;
        }
    }

    /// <summary>Atomically replaces one unchanged writable draft payload.</summary>
    /// <param name="db">Owned database context.</param>
    /// <param name="row">Original row and payload used as the concurrency token.</param>
    /// <param name="plan">Merged draft payload.</param>
    /// <param name="cancellationToken">Cancels the update.</param>
    /// <returns>True when this writer committed its exact revision.</returns>
    private async Task<bool> SaveDraftPlanAsync(LocalGptMemoryDbContext db, CodeGenerationChangeReview row,
        CreateCodeGenerationReviewRequest plan, CancellationToken cancellationToken)
    {
        try
        {
            var payload = JsonSerializer.Serialize(plan, JsonOptions);
            return await db.CodeGenerationChangeReviews.Where(item => item.Id == row.Id &&
                    item.Status == DraftStatus && item.PayloadJson == row.PayloadJson)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.PayloadJson, payload)
                    .SetProperty(item => item.UpdatedAtUtc, DateTime.UtcNow), cancellationToken).ConfigureAwait(false) == 1;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Persisting source draft {ReviewId} failed.", row.Id);
            throw;
        }
    }

    /// <summary>Extracts one complete fenced file from an author's response without interpreting it as a command.</summary>
    /// <param name="visibleContent">Provider-visible answer after thinking removal.</param>
    /// <returns>Exact candidate source between the two matching fence lines, or null for incomplete/ambiguous output.</returns>
    private string? ExtractAssignedFileSource(string visibleContent)
    {
        try
        {
            var answer = visibleContent.Trim();
            var lines = answer.Split('\n');
            var fences = Enumerable.Range(0, lines.Length).Where(index => lines[index].TrimStart().StartsWith("```", StringComparison.Ordinal)).ToArray();
            if (fences.Length != 2 || fences[1] <= fences[0] + 1 ||
                !string.Equals(lines[fences[1]].Trim(), "```", StringComparison.Ordinal))
                return null;
            var source = string.Join("\n", lines[(fences[0] + 1)..fences[1]]) + "\n";
            return string.IsNullOrWhiteSpace(source) || source.Contains('\0') ? null : source;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Extracting candidate file bytes failed; response content omitted.");
            throw;
        }
    }
}

using System.Security.Cryptography;
using System.Text;
using LocalGPT.BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>Independent inspection and targeted correction of stored source-draft candidates.</summary>
public sealed partial class CodeGenerationWorkflowService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CodeGenerationDraftInspection>> GetDraftInspectionsAsync(Guid runId, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var row = await FindCouncilDraftAsync(db, runId, cancellationToken).ConfigureAwait(false);
            if (row is null || row.Status != DraftStatus)
                return [];
            var plan = ReadDraftPlan(row);
            var files = plan.Files.ToDictionary(file => file.RelativePath, StringComparer.OrdinalIgnoreCase);
            var inspections = new List<CodeGenerationDraftInspection>();
            foreach (var file in plan.Files)
            {
                var directPaths = plan.DraftDependencies?.GetValueOrDefault(file.RelativePath) ?? [];
                if (directPaths.Any(path => !files.ContainsKey(path)))
                    continue;
                var dependencies = directPaths.Select(path => files[path]).ToList();
                var hashes = dependencies.ToDictionary(dep => dep.RelativePath,
                    dep => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dep.Content))), StringComparer.OrdinalIgnoreCase);
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(file.Content)));
                if (plan.DraftInspections.Any(decision => decision.Accepted == true && string.Equals(decision.Path, file.RelativePath, StringComparison.OrdinalIgnoreCase) &&
                    decision.Sha256 == hash && decision.DependencyHashes.Count == hashes.Count &&
                    hashes.All(pair => decision.DependencyHashes.TryGetValue(pair.Key, out var existing) && existing == pair.Value)))
                    continue;
                var author = plan.DraftAssignments.LastOrDefault(assignment => string.Equals(assignment.RelativePath, file.RelativePath,
                    StringComparison.OrdinalIgnoreCase) && assignment.Sha256 == hash)?.ModelName ?? string.Empty;
                var previousDecisions = plan.DraftInspections.Where(decision => string.Equals(decision.Path, file.RelativePath, StringComparison.OrdinalIgnoreCase) &&
                        decision.Sha256 == hash && decision.DependencyHashes.Count == hashes.Count &&
                        hashes.All(pair => decision.DependencyHashes.TryGetValue(pair.Key, out var existing) && existing == pair.Value)).ToList();
                inspections.Add(new CodeGenerationDraftInspection(row.Id, file.RelativePath, author, hash, file.Content,
                    plan.Goal, plan.DraftManifest![file.RelativePath], plan.DraftSharedContract, dependencies, hashes,
                    previousDecisions.Select(decision => decision.Reviewer).Distinct(StringComparer.OrdinalIgnoreCase).ToList())
                    { PreviousDecisions = previousDecisions });
            }
            return inspections;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading candidate inspection packets for Council {RunId} failed.", runId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RecordDraftInspectionAsync(Guid runId, CodeGenerationDraftInspection packet,
        CodeGenerationDraftInspectionDecision decision, int requiredNegativeInspections, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(decision.Reviewer) || string.Equals(packet.Author, decision.Reviewer, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Independent inspection requires a different author and reviewer.");
            if (decision.Sha256 != packet.Sha256)
                throw new ArgumentException("The requested inspection hash does not match its candidate.");
            if (decision.Accepted == false && string.IsNullOrWhiteSpace(decision.Diagnostic))
                throw new ArgumentException("A corrective inspection needs a specific defect.");
            var conflicts = Math.Max(1, runtimePolicy.GetJson<CouncilFileWorkRuntimeParameters>("CouncilFileWorkRuntimeParametersJson").MaximumCommitConflicts);
            for (var attempt = 0; attempt < conflicts; attempt++)
            {
                await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var row = await FindCouncilDraftAsync(db, runId, cancellationToken).ConfigureAwait(false);
                if (row is null || row.Id != packet.DraftId || row.Status != DraftStatus)
                    throw new InvalidOperationException("The inspected source draft is no longer writable.");
                var plan = ReadDraftPlan(row);
                var file = plan.Files.SingleOrDefault(file => string.Equals(file.RelativePath, packet.Path, StringComparison.OrdinalIgnoreCase));
                if (file is null || Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(file.Content))) != packet.Sha256)
                    throw new InvalidOperationException("The inspected candidate was replaced before the decision could be recorded.");
                foreach (var (path, hash) in packet.DependencyHashes)
                {
                    var dependency = plan.Files.SingleOrDefault(file => string.Equals(file.RelativePath, path, StringComparison.OrdinalIgnoreCase));
                    if (dependency is null || Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dependency.Content))) != hash)
                        throw new InvalidOperationException("A reviewed dependency changed; a fresh inspection is necessary.");
                }
                // A model vote cannot authorize changing a user-pinned framework. Retain the candidate
                // and require another valid independent inspection; never convert this into acceptance.
                if (decision.Accepted == false && new[] { ".csproj", ".fsproj", ".vbproj", ".props", ".targets" }.Contains(Path.GetExtension(packet.Path), StringComparer.OrdinalIgnoreCase) &&
                    string.IsNullOrEmpty(ValidateDraftProjectTarget(plan.Goal, file.RelativePath, file.Content)) &&
                    IsContradictoryFrameworkInspection(plan.Goal, decision.Diagnostic))
                    throw new ArgumentException(runtimePolicy.GetString("CouncilFileWorkConflictingFrameworkReviewNotice"));
                decision.Path = packet.Path;
                decision.DependencyHashes = new Dictionary<string, string>(packet.DependencyHashes, StringComparer.OrdinalIgnoreCase);
                var matching = plan.DraftInspections.Where(item =>
                    string.Equals(item.Path, packet.Path, StringComparison.OrdinalIgnoreCase) && item.Sha256 == packet.Sha256 &&
                    item.DependencyHashes.Count == packet.DependencyHashes.Count &&
                    packet.DependencyHashes.All(pair => item.DependencyHashes.TryGetValue(pair.Key, out var hash) && hash == pair.Value)).ToList();
                if (matching.Any(item => string.Equals(item.Reviewer, decision.Reviewer, StringComparison.OrdinalIgnoreCase)))
                    return; // Duplicate transport delivery is not another independent vote.
                plan.DraftInspections.Add(decision);
                var rejectedBy = matching.Where(item => item.Accepted == false).Select(item => item.Reviewer)
                    .Append(decision.Reviewer).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                if (decision.Accepted == false && rejectedBy >= Math.Max(1, requiredNegativeInspections))
                {
                    // Reopen only the defective file. Its previous source is evidence for the next attempt;
                    // dependency-hash mismatches cause independent reinspection, not destruction of good source.
                    plan.DraftRepairSources[packet.Path] = file.Content;
                    // Keep the architect work order immutable; the rejection remains in the inspection history.
                    plan.Files.Remove(file);
                }
                if (await SaveDraftPlanAsync(db, row, plan, cancellationToken).ConfigureAwait(false))
                {
                    logger.LogInformation("Recorded inspection of Council {RunId} candidate {CandidateHash}; accepted={Accepted}.", runId, packet.Sha256, decision.Accepted);
                    return;
                }
            }
            throw new InvalidOperationException("The source draft remained busy during candidate inspection.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Recording independent inspection for Council {RunId} failed.", runId);
            throw;
        }
    }
}

using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Owns quarantine inspection, reviewed promotion and automatic project synchronization for uploaded source material.</summary>
public sealed class ProjectIngestionService(
    IChatUploadWorkspaceService workspaces,
    IRegexCuratorService regexCurator,
    IRegexPatternService regexPatterns,
    ICouncilKnowledgeService knowledge,
    IProjectEvidenceClassifierService classifier,
    IAmbientLocalGptContext ambientContext,
    IServiceScopeFactory scopeFactory,
    CouncilTextService councilText,
    CouncilRuntimeService councilRuntime,
    LocalGptCatalogService catalog,
    ILogger<ProjectIngestionService> logger) : IProjectIngestionService
{
    private readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<ProjectIngestionGateRecord> InspectAsync(string workspaceName, CancellationToken cancellationToken = default)
    {
        try
        {
            var root = workspaces.ResolveWorkspacePath(workspaceName) ?? throw new KeyNotFoundException($"Upload workspace '{workspaceName}' was not found.");
            var originalRoot = Path.Combine(root, "original");
            if (!Directory.Exists(originalRoot)) throw new InvalidDataException("The upload workspace has no quarantine/original tree.");

            var record = new ProjectIngestionGateRecord { WorkspaceName = workspaceName, Status = "Inspecting" };
            var approvedPatterns = await regexCurator.ListApprovedPatternsAsync(cancellationToken).ConfigureAwait(false);
            var compiled = approvedPatterns.Select(pattern => (pattern.Name, Regex: regexPatterns.Compile(pattern.Pattern, pattern.Flags, TimeSpan.FromSeconds(2)))).ToList();
            long totalBytes = 0;
            var fileCount = 0;

            foreach (var path in Directory.EnumerateFiles(originalRoot, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                fileCount++;
                if (fileCount > catalog.MaxFiles)
                {
                    record.RejectionReasons.Add($"Quarantine exceeds MaxFiles ({catalog.MaxFiles:n0}).");
                    break;
                }
                var info = new FileInfo(path);
                totalBytes += info.Length;
                if (info.Length > catalog.MaxSingleFileBytes) record.RejectionReasons.Add($"{info.Name}: exceeds MaxSingleFileBytes.");
                if (totalBytes > catalog.MaxTotalFileBytes) record.RejectionReasons.Add("Quarantine exceeds MaxTotalFileBytes.");

                var relative = councilText.ToForwardSlash(Path.GetRelativePath(originalRoot, path), logger);
                var evidence = new ProjectIngestionFileEvidence
                {
                    RelativePath = relative,
                    Length = info.Length,
                    Sha256 = await HashFileAsync(path, cancellationToken).ConfigureAwait(false),
                    Kind = councilRuntime.DetermineFileKind(path, logger),
                    IsArchive = path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                };

                if (evidence.IsArchive)
                    InspectZip(path, evidence, record.RejectionReasons);

                var matchInput = relative;
                if (!evidence.IsArchive && info.Length <= Math.Min(catalog.MaxSingleFileBytes, 256_000) && councilRuntime.IsTextLike(path, logger))
                {
                    try
                    {
                        var text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                        matchInput += "\n" + text[..Math.Min(text.Length, 64_000)];
                    }
                    catch (Exception ex) { logger.LogDebug(ex, "Could not include bounded text from {RelativePath} in regex inspection.", relative); }
                }
                foreach (var pattern in compiled)
                {
                    if (pattern.Regex.IsMatch(matchInput)) evidence.ApprovedRegexMatches.Add(pattern.Name);
                }
                record.Files.Add(evidence);
            }

            var classified = await classifier.ClassifyAsync(originalRoot, cancellationToken).ConfigureAwait(false);
            record.ProjectKinds.AddRange(classified.ProjectKinds);
            record.Toolchains.AddRange(classified.Toolchains);
            record.Domains.AddRange(classified.Domains);
            record.MatchedEvidenceRules.AddRange(classified.MatchedRuleNames);
            var approvedKnowledge = await knowledge.GetEntriesAsync(false, 500, cancellationToken).ConfigureAwait(false);
            var evidenceTerms = record.Toolchains.Concat(record.ProjectKinds).Concat(record.Domains).ToList();
            record.ApprovedKnowledgeHints = approvedKnowledge
                .Where(entry => entry.IsUserApproved && !entry.IsArchived && evidenceTerms.Any(term =>
                    entry.Topic.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    entry.Tags.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    entry.Scope.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .Select(entry => $"{entry.Topic} [{entry.Id:N}]")
                .Take(40).ToList();

            record.DeterministicChecksPassed = record.RejectionReasons.Count == 0;
            record.Status = record.DeterministicChecksPassed ? "AwaitingIndependentReview" : "Rejected";
            record.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await SaveRecordAsync(root, record, cancellationToken).ConfigureAwait(false);
            return record;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Inspecting quarantined workspace {WorkspaceName} failed.", workspaceName);
            throw;
        }
    }

    public async Task<ProjectIngestionGateRecord?> GetAsync(string workspaceName, CancellationToken cancellationToken = default)
    {
        try
        {
            var root = workspaces.ResolveWorkspacePath(workspaceName);
            if (root is null) return null;
            var path = Path.Combine(root, "ingestion-gate.json");
            if (!File.Exists(path)) return null;
            var record = JsonSerializer.Deserialize<ProjectIngestionGateRecord>(await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false), catalog.JsonOptions);
            logger.LogDebug("Loaded ingestion gate state for workspace {WorkspaceName}.", workspaceName);
            return record;
        }
        catch (Exception ex) { logger.LogError(ex, "Loading ingestion gate for workspace {WorkspaceName} failed.", workspaceName); throw; }
    }

    public async Task<ProjectIngestionGateRecord> ReviewAsync(ProjectIngestionReviewRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var root = workspaces.ResolveWorkspacePath(request.WorkspaceName) ?? throw new KeyNotFoundException($"Upload workspace '{request.WorkspaceName}' was not found.");
            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var record = await GetAsync(request.WorkspaceName, cancellationToken).ConfigureAwait(false) ?? await InspectAsync(request.WorkspaceName, cancellationToken).ConfigureAwait(false);
                var current = ambientContext.Current;
                var identity = ReviewerIdentity(current);
                record.Reviews.RemoveAll(review => string.Equals(review.ReviewerIdentity, identity, StringComparison.OrdinalIgnoreCase));
                record.Reviews.Add(new ProjectIngestionReviewEvidence
                {
                    ReviewerIdentity = identity,
                    ReviewerKind = Bound(current.ActorKind, 80, "System"),
                    Approved = request.Approved,
                    Notes = Bound(request.Notes, 3000, string.Empty),
                    ReviewedAtUtc = DateTimeOffset.UtcNow
                });
                var approvals = record.Reviews.Where(review => review.Approved).Select(review => review.ReviewerIdentity).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                record.Status = !record.DeterministicChecksPassed || record.Reviews.Any(review => !review.Approved)
                    ? "Rejected"
                    : approvals >= Math.Max(1, record.RequiredIndependentApprovals) ? "AwaitingUserApproval" : "AwaitingIndependentReview";
                record.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await SaveRecordAsync(root, record, cancellationToken).ConfigureAwait(false);
                return record;
            }
            finally { Gate.Release(); }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reviewing quarantined workspace failed.");
            throw;
        }
    }

    public async Task<ProjectIngestionGateRecord> PromoteAsync(string workspaceName, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed) throw new InvalidOperationException("Project workspace promotion requires explicit user confirmation.");
            var root = workspaces.ResolveWorkspacePath(workspaceName) ?? throw new KeyNotFoundException($"Upload workspace '{workspaceName}' was not found.");
            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var record = await GetAsync(workspaceName, cancellationToken).ConfigureAwait(false) ?? await InspectAsync(workspaceName, cancellationToken).ConfigureAwait(false);
                var approvals = record.Reviews.Where(review => review.Approved).Select(review => review.ReviewerIdentity).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                if (!record.DeterministicChecksPassed) throw new InvalidOperationException("Quarantine deterministic checks have not passed.");
                if (record.Reviews.Any(review => !review.Approved)) throw new InvalidOperationException("At least one independent reviewer requested changes.");
                if (approvals < Math.Max(1, record.RequiredIndependentApprovals)) throw new InvalidOperationException($"At least {record.RequiredIndependentApprovals} independent approvals are required before promotion.");

                var originalRoot = Path.Combine(root, "original");
                var promotedRoot = Path.Combine(root, "extracted");
                if (Directory.Exists(promotedRoot)) Directory.Delete(promotedRoot, true);
                Directory.CreateDirectory(promotedRoot);
                long expandedBytes = 0;
                int expandedEntries = 0;
                foreach (var source in Directory.EnumerateFiles(originalRoot, "*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (source.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        var zipRoot = Path.Combine(promotedRoot, councilText.SanitizeFileName(Path.GetFileNameWithoutExtension(source), logger));
                        Directory.CreateDirectory(zipRoot);
                        (expandedEntries, expandedBytes) = await ExtractApprovedZipAsync(source, zipRoot, expandedEntries, expandedBytes, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var relative = Path.GetRelativePath(originalRoot, source);
                        var destination = Path.GetFullPath(Path.Combine(promotedRoot, relative));
                        if (!councilRuntime.IsInsideRoot(promotedRoot, destination, logger)) throw new InvalidDataException("Quarantined file attempted to escape promoted root.");
                        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                        await CopyFileAsync(source, destination, cancellationToken).ConfigureAwait(false);
                    }
                }

                var classification = await classifier.ClassifyAsync(promotedRoot, cancellationToken).ConfigureAwait(false);
                record.ProjectKinds = classification.ProjectKinds.ToList();
                record.Toolchains = classification.Toolchains.ToList();
                record.Domains = classification.Domains.ToList();
                record.MatchedEvidenceRules = classification.MatchedRuleNames.ToList();
                record.UserApprovedPromotion = true;
                record.PromotedRoot = promotedRoot;
                record.PromotedAtUtc = DateTimeOffset.UtcNow;
                record.UpdatedAtUtc = DateTimeOffset.UtcNow;
                record.Status = "Promoted";
                await SaveRecordAsync(root, record, cancellationToken).ConfigureAwait(false);

                using var scope = scopeFactory.CreateScope();
                var sync = scope.ServiceProvider.GetRequiredService<ILearningProjectWorkspaceSyncService>();
                _ = await sync.SynchronizeAsync(workspaceName, cancellationToken).ConfigureAwait(false);
                return record;
            }
            finally { Gate.Release(); }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Promoting quarantined workspace {WorkspaceName} failed.", workspaceName);
            throw;
        }
    }

    private void InspectZip(string path, ProjectIngestionFileEvidence evidence, List<string> rejectionReasons)
    {
        try
        {
            using var archive = ZipFile.OpenRead(path);
            long expanded = 0;
            var entries = 0;
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Name)) continue;
                entries++;
                if (entries > catalog.MaxZipEntries) { rejectionReasons.Add($"{evidence.RelativePath}: archive exceeds MaxZipEntries."); break; }
                if (entry.Length > catalog.MaxZipEntryBytes) rejectionReasons.Add($"{evidence.RelativePath}: archive entry exceeds MaxZipEntryBytes.");
                expanded += entry.Length;
                if (expanded > catalog.MaxExtractedBytes) { rejectionReasons.Add($"{evidence.RelativePath}: archive exceeds MaxExtractedBytes."); break; }
                if (councilText.BuildSafeZipRelativePath(entry.FullName, logger) is null) rejectionReasons.Add($"{evidence.RelativePath}: contains an unsafe archive path.");
            }
            evidence.ArchiveEntryCount = entries;
            evidence.ArchiveExpandedBytes = expanded;
            logger.LogDebug("Inspected ZIP quarantine metadata for {RelativePath}: {EntryCount} entries.", evidence.RelativePath, entries);
        }
        catch (InvalidDataException)
        {
            rejectionReasons.Add($"{evidence.RelativePath}: archive could not be parsed.");
        }
    }

    private async Task<(int TotalEntries, long TotalBytes)> ExtractApprovedZipAsync(string zipPath, string destinationRoot, int totalEntries, long totalBytes, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(entry.Name)) continue;
            totalEntries++;
            if (totalEntries > catalog.MaxZipEntries) throw new InvalidDataException("Archive promotion exceeded MaxZipEntries.");
            if (entry.Length > catalog.MaxZipEntryBytes) throw new InvalidDataException("Archive promotion encountered an oversized entry.");
            totalBytes += entry.Length;
            if (totalBytes > catalog.MaxExtractedBytes) throw new InvalidDataException("Archive promotion exceeded MaxExtractedBytes.");
            var relative = councilText.BuildSafeZipRelativePath(entry.FullName, logger) ?? throw new InvalidDataException("Archive promotion encountered an unsafe path.");
            var destination = Path.GetFullPath(Path.Combine(destinationRoot, relative));
            if (!councilRuntime.IsInsideRoot(destinationRoot, destination, logger)) throw new InvalidDataException("Archive promotion encountered a traversal path.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            var source = entry.Open();
            await using var configuredSource = source.ConfigureAwait(false);
            var target = File.Create(destination);
            await using var configuredTarget = target.ConfigureAwait(false);
            await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
        }
        return (totalEntries, totalBytes);
    }

    private async Task CopyFileAsync(string source, string destination, CancellationToken cancellationToken)
    {
        try
        {
            var input = File.OpenRead(source);
            await using var configuredInput = input.ConfigureAwait(false);
            var output = File.Create(destination);
            await using var configuredOutput = output.ConfigureAwait(false);
            await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("Copied one approved quarantine file into the promoted workspace.");
        }
        catch (Exception ex) { logger.LogError(ex, "Copying one approved quarantine file failed."); throw; }
    }

    private async Task<string> HashFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var stream = File.OpenRead(path);
            await using var configuredStream = stream.ConfigureAwait(false);
            var result = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
            logger.LogDebug("Hashed one quarantined project file.");
            return result;
        }
        catch (Exception ex) { logger.LogError(ex, "Hashing one quarantined project file failed."); throw; }
    }

    private string ReviewerIdentity(AmbientLocalGptContextSnapshot current)
    {
        try
        {
            var result = Bound(string.Join(":", new[] { current.ActorKind, current.ActorDisplayName, current.CouncilRunId?.ToString("N") ?? current.CorrelationId, current.Phase }.Where(value => !string.IsNullOrWhiteSpace(value))), 300, "LocalGPT:System");
            logger.LogDebug("Resolved ambient project-ingestion reviewer identity.");
            return result;
        }
        catch (Exception ex) { logger.LogError(ex, "Resolving ambient project-ingestion reviewer identity failed."); throw; }
    }

    private string Bound(string? value, int maxLength, string fallback)
    {
        try
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            var result = normalized[..Math.Min(normalized.Length, maxLength)];
            logger.LogDebug("Bounded one ingestion metadata value.");
            return result;
        }
        catch (Exception ex) { logger.LogError(ex, "Bounding ingestion metadata failed."); throw; }
    }

    private async Task SaveRecordAsync(string root, ProjectIngestionGateRecord record, CancellationToken cancellationToken)
    {
        try
        {
            var path = Path.Combine(root, "ingestion-gate.json");
            var temporary = path + ".tmp";
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(record, catalog.JsonOptions), cancellationToken).ConfigureAwait(false);
            File.Move(temporary, path, true);
            logger.LogDebug("Saved ingestion gate state for workspace {WorkspaceName}.", record.WorkspaceName);
        }
        catch (Exception ex) { logger.LogError(ex, "Saving ingestion gate state failed."); throw; }
    }

}

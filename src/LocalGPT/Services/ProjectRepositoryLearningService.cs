using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.BusinessObjects.Models;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalGPT.Services;

/// <summary>Builds review-required Knowledge and regex candidates from immutable source revision evidence and its delta from the previous revision.</summary>
public sealed class ProjectRepositoryLearningService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    ICouncilKnowledgeService knowledge,
    IRegexPatternService regexPatterns,
    IRegexCuratorService regexCurator,
    ILogger<ProjectRepositoryLearningService> logger) : IProjectRepositoryLearningService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ProjectRepositoryLearningResult>> StageCandidatesAsync(
        IReadOnlyList<LearningProjectSyncResult> synchronizedProjects,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(synchronizedProjects);
            var results = new List<ProjectRepositoryLearningResult>();
            foreach (var synchronized in synchronizedProjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(await StageOneAsync(synchronized, cancellationToken).ConfigureAwait(false));
            }
            return results;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Staging review-required repository learning candidates failed; source content was omitted from diagnostics.");
            throw;
        }
    }

    private async Task<ProjectRepositoryLearningResult> StageOneAsync(LearningProjectSyncResult synchronized, CancellationToken cancellationToken)
    {
        try
        {
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var revision = await db.LocalGptProjectRevisions
                .SingleAsync(item => item.Id == synchronized.RevisionId, cancellationToken)
                .ConfigureAwait(false);
            var currentFiles = await db.LocalGptProjectTrackedFiles.AsNoTracking()
                .Where(item => item.ProjectId == synchronized.ProjectId && item.RevisionId == synchronized.RevisionId && item.Exists)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var previousFiles = revision.ParentRevisionId is null
                ? []
                : await db.LocalGptProjectTrackedFiles.AsNoTracking()
                    .Where(item => item.ProjectId == synchronized.ProjectId && item.RevisionId == revision.ParentRevisionId && item.Exists)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);

            var currentByPath = currentFiles
                .GroupBy(item => item.ProjectRelativePath, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var previousByPath = previousFiles
                .GroupBy(item => item.ProjectRelativePath, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var added = currentByPath.Keys.Except(previousByPath.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
            var removed = previousByPath.Keys.Except(currentByPath.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
            var changed = currentByPath.Keys.Intersect(previousByPath.Keys, StringComparer.OrdinalIgnoreCase)
                .Where(path => !string.Equals(currentByPath[path].ContentHash, previousByPath[path].ContentHash, StringComparison.OrdinalIgnoreCase))
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();

            revision.Summary = BuildRevisionSummary(synchronized, added, changed, removed, revision.ParentRevisionId is not null);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var knowledgeIds = new List<Guid>();
            var snapshotId = DeterministicGuid($"repository-snapshot|{synchronized.ProjectId:N}|{synchronized.RevisionId:N}");
            await knowledge.SaveEntryAsync(new CouncilKnowledgeEntry
            {
                Id = snapshotId,
                Topic = $"{synchronized.ProjectName} source snapshot {synchronized.Version}",
                Scope = $"Project:{synchronized.ProjectName}",
                Content = BuildSnapshotKnowledge(synchronized, currentFiles),
                Source = "Source-backed repository ingestion",
                Tags = "repository;source-map;review-required;project-knowledge",
                Confidence = 95,
                VerificationStatus = "SourceMapped",
                ReviewStatus = "NeedsUserReview",
                SourceHash = synchronized.SourceSnapshotHash,
                SourceDateUtc = DateTime.UtcNow,
                IsUserApproved = false,
                IsPinned = false,
                IsArchived = false
            }, cancellationToken).ConfigureAwait(false);
            knowledgeIds.Add(snapshotId);

            if (revision.ParentRevisionId is not null)
            {
                var deltaId = DeterministicGuid($"repository-delta|{synchronized.ProjectId:N}|{synchronized.RevisionId:N}");
                await knowledge.SaveEntryAsync(new CouncilKnowledgeEntry
                {
                    Id = deltaId,
                    Topic = $"{synchronized.ProjectName} source changes for {synchronized.Version}",
                    Scope = $"Project:{synchronized.ProjectName}:RevisionDelta",
                    Content = BuildDeltaKnowledge(added, changed, removed),
                    Source = "Source-backed repository revision comparison",
                    Tags = "repository;revision-delta;source-map;review-required;project-knowledge",
                    Confidence = 100,
                    VerificationStatus = "SourceMapped",
                    ReviewStatus = "NeedsUserReview",
                    SourceHash = synchronized.SourceSnapshotHash,
                    SourceDateUtc = DateTime.UtcNow,
                    IsUserApproved = false,
                    IsPinned = false,
                    IsArchived = false
                }, cancellationToken).ConfigureAwait(false);
                knowledgeIds.Add(deltaId);
            }

            var regexNames = await StageRepositoryRegexAsync(synchronized, currentFiles, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Staged source-backed repository learning candidates for {ProjectName}: {AddedCount} added, {ChangedCount} changed, {RemovedCount} removed file(s), {KnowledgeCount} Knowledge candidate(s), {RegexCount} regex candidate(s).",
                synchronized.ProjectName,
                added.Count,
                changed.Count,
                removed.Count,
                knowledgeIds.Count,
                regexNames.Count);

            return new ProjectRepositoryLearningResult(
                synchronized.ProjectId,
                synchronized.RevisionId,
                synchronized.ProjectName,
                added.Count,
                changed.Count,
                removed.Count,
                knowledgeIds,
                regexNames);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Staging repository learning candidates failed for project {ProjectName}.", synchronized.ProjectName);
            throw;
        }
    }

    private async Task<IReadOnlyList<string>> StageRepositoryRegexAsync(
        LearningProjectSyncResult synchronized,
        IReadOnlyList<LocalGptProjectTrackedFile> files,
        CancellationToken cancellationToken)
    {
        try
        {
            var marker = files.Select(item => item.ProjectRelativePath).FirstOrDefault(path =>
                synchronized.ProjectName.Equals("LocalGPT Core", StringComparison.OrdinalIgnoreCase)
                    ? path.EndsWith("src/LocalGPT/LocalGPT.csproj", StringComparison.OrdinalIgnoreCase)
                    : synchronized.ProjectName.Equals("PublisherStudio", StringComparison.OrdinalIgnoreCase)
                        ? path.EndsWith("src/PublisherStudio.Web/PublisherStudio.Web.csproj", StringComparison.OrdinalIgnoreCase)
                        : path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                          || path.EndsWith("package.json", StringComparison.OrdinalIgnoreCase)
                          || path.EndsWith("pyproject.toml", StringComparison.OrdinalIgnoreCase));
            var projectKind = synchronized.ProjectName.Equals("LocalGPT Core", StringComparison.OrdinalIgnoreCase)
                ? "LocalGPTRepository"
                : synchronized.ProjectName.Equals("PublisherStudio", StringComparison.OrdinalIgnoreCase)
                    ? "PublisherStudioRepository"
                    : "SourceRepository";
            var toolchain = !string.IsNullOrWhiteSpace(marker) && marker.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                ? "DotNet"
                : string.IsNullOrWhiteSpace(marker)
                    ? "Git"
                    : "Source";
            var name = $"project.evidence::SourceCode::{projectKind}::{toolchain}";
            var pattern = string.IsNullOrWhiteSpace(marker)
                ? @"(?i)(?:^|!/|/)\.git/config$"
                : $@"(?i)(?:^|!/|/){Regex.Escape(marker.Replace('\\', '/').TrimStart('/'))}$";
            await regexCurator.MarkSuggestedAsync(
                name,
                $"Source-backed marker learned from reviewed {synchronized.ProjectName} revision {synchronized.RevisionId:N}.",
                "Repository identity evidence; remains inactive until curator/user approval.",
                cancellationToken).ConfigureAwait(false);
            await regexPatterns.AddOrUpdateAsync(new RegexPatternDto(name, pattern, "CultureInvariant")).ConfigureAwait(false);
            return [name];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Staging repository-identity regex candidate failed for project {ProjectName}.", synchronized.ProjectName);
            throw;
        }
    }

    private string BuildRevisionSummary(
        LearningProjectSyncResult synchronized,
        IReadOnlyList<string> added,
        IReadOnlyList<string> changed,
        IReadOnlyList<string> removed,
        bool hasParent)
    {
        try
        {
            if (!hasParent)
                return $"Initial source-backed revision for {synchronized.ProjectName} {synchronized.Version}; {synchronized.TrackedFileCount} tracked file(s).";
            return $"Source-backed delta for {synchronized.ProjectName} {synchronized.Version}: {added.Count} added, {changed.Count} changed, {removed.Count} removed tracked file(s).";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building source revision summary failed.");
            throw;
        }
    }

    private string BuildSnapshotKnowledge(LearningProjectSyncResult synchronized, IReadOnlyList<LocalGptProjectTrackedFile> files)
    {
        try
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Project: {synchronized.ProjectName}");
            builder.AppendLine($"Version: {synchronized.Version}");
            builder.AppendLine($"Source snapshot SHA-256: {synchronized.SourceSnapshotHash}");
            if (!string.IsNullOrWhiteSpace(synchronized.SdkVersion))
                builder.AppendLine($"SDK: {synchronized.SdkVersion}");
            if (synchronized.TargetFrameworks.Count > 0)
                builder.AppendLine($"Target frameworks: {string.Join(", ", synchronized.TargetFrameworks)}");
            builder.AppendLine($"Tracked files: {synchronized.TrackedFileCount}");
            builder.AppendLine("Bounded source map (first 240 tracked paths):");
            foreach (var path in files.Select(item => item.ProjectRelativePath).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).Take(240))
                builder.AppendLine($"- {path}");
            builder.AppendLine("This is source-backed evidence staged for human review; repository file contents were not automatically promoted to trusted Knowledge.");
            return Bound(builder.ToString(), 48_000);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building repository snapshot Knowledge candidate failed.");
            throw;
        }
    }

    private string BuildDeltaKnowledge(IReadOnlyList<string> added, IReadOnlyList<string> changed, IReadOnlyList<string> removed)
    {
        try
        {
            var builder = new StringBuilder();
            AppendDeltaGroup(builder, "Added", added);
            AppendDeltaGroup(builder, "Changed", changed);
            AppendDeltaGroup(builder, "Removed", removed);
            builder.AppendLine("Delta is derived from tracked-file content hashes between canonical project revisions and is staged for human review.");
            return Bound(builder.ToString(), 48_000);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building repository delta Knowledge candidate failed.");
            throw;
        }
    }

    private void AppendDeltaGroup(StringBuilder builder, string heading, IReadOnlyList<string> paths)
    {
        try
        {
            builder.AppendLine($"{heading} ({paths.Count}):");
            foreach (var path in paths.Take(300))
                builder.AppendLine($"- {path}");
            if (paths.Count > 300)
                builder.AppendLine($"- … {paths.Count - 300} additional path(s) omitted from this bounded candidate.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting a repository delta group failed.");
            throw;
        }
    }

    private Guid DeterministicGuid(string value)
    {
        try
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return new Guid(hash.AsSpan(0, 16));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating deterministic repository-learning identifier failed.");
            throw;
        }
    }

    private string Bound(string value, int maximum)
    {
        try
        {
            return value.Length <= maximum ? value : value[..maximum];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bounding repository-learning content failed.");
            throw;
        }
    }
}

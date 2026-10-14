using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Allows project/file/curated-regex navigation without direct DbContext access. The tracked file and its
/// verified hash are authoritative; the runtime plugin is only an explicitly approved publication snapshot.
/// </summary>
/// <param name="projects">Local gpt project service dependency used by the project source navigation workflow to provide the corresponding application capability.</param>
/// <param name="architecture">Project architecture service dependency used by the project source navigation workflow to provide the corresponding application capability.</param>
/// <param name="maintenance">Project maintenance service dependency used by the project source navigation workflow to provide the corresponding application capability.</param>
/// <param name="curators">Regex curator service dependency used by the project source navigation workflow to provide the corresponding application capability.</param>
/// <param name="compilation">Regex compilation service dependency used by the project source navigation workflow to provide the corresponding application capability.</param>
/// <param name="safeText">Safe text document service dependency used by the project source navigation workflow to provide the corresponding application capability.</param>
/// <param name="platform">Platform runtime service dependency used by the project source navigation workflow to provide the corresponding application capability.</param>
/// <param name="plugins">Runtime plugin service dependency used by the project source navigation workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ProjectSourceNavigationService(
    ILocalGptProjectService projects,
    IProjectArchitectureService architecture,
    IProjectMaintenanceService maintenance,
    IRegexCuratorService curators,
    IRegexCompilationService compilation,
    ISafeTextDocumentService safeText,
    IPlatformRuntimeService platform,
    IRuntimePluginService plugins,
    ILogger<ProjectSourceNavigationService> logger) : IProjectSourceNavigationService
{
    /// <summary>
    /// Performs navigate as part of the project source navigation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project source navigation result produced by the operation.</returns>
    public async Task<ProjectSourceNavigationResult> NavigateAsync(ProjectSourceNavigationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.ProjectId == Guid.Empty) throw new ArgumentException("A project ID is required.", nameof(request));
            var project = await projects.GetProjectAsync(request.ProjectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException("The selected project does not exist.");
            if (request.RevisionId.HasValue)
            {
                var revisions = await architecture.GetRevisionsAsync(request.ProjectId, cancellationToken).ConfigureAwait(false);
                if (!revisions.Any(item => item.Id == request.RevisionId))
                    throw new InvalidOperationException("The selected revision does not belong to this project.");
            }
            var allFiles = await maintenance.GetTrackedFilesAsync(request.ProjectId, request.RevisionId, cancellationToken).ConfigureAwait(false);
            var source = request.FocusTrackedFileId is Guid focus
                ? allFiles.SingleOrDefault(file => file.Id == focus) ?? throw new KeyNotFoundException("The selected tracked file is outside this revision.")
                : null;
            var currentRevision = request.RevisionId is Guid revisionId
                ? (await architecture.GetRevisionsAsync(request.ProjectId, cancellationToken).ConfigureAwait(false)).Single(item => item.Id == revisionId)
                : null;
            var sourceRoot = platform.NormalizeAbsolutePath(!string.IsNullOrWhiteSpace(currentRevision?.SourceRootPath)
                ? currentRevision.SourceRootPath : project.Project.RootPath);
            var contentSamples = new Dictionary<Guid, string>();
            if (!platform.IsProtectedWorkspaceRoot(sourceRoot))
            {
                foreach (var file in allFiles.Where(item => item.Exists && item.IsUserApproved && item.LearningReadComplete
                             && item.ContentHash.Equals(item.LearningReviewedHash, StringComparison.OrdinalIgnoreCase)
                             && item.LearningStatus == "Completed" && item.SizeBytes <= 131_072).Take(80))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!platform.IsSameOrDescendantPath(sourceRoot, file.AbsolutePath)) continue;
                    // Reparse checks are included in the same per-file failure boundary as safe text reads:
                    // a stale or inaccessible file may be skipped without aborting discovery for the project.
                    try
                    {
                        var relative = Path.GetRelativePath(sourceRoot, file.AbsolutePath);
                        var currentSegment = sourceRoot;
                        var symlink = File.GetAttributes(sourceRoot).HasFlag(FileAttributes.ReparsePoint);
                        foreach (var part in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                        {
                            currentSegment = Path.Combine(currentSegment, part);
                            if ((File.Exists(currentSegment) || Directory.Exists(currentSegment)) && File.GetAttributes(currentSegment).HasFlag(FileAttributes.ReparsePoint))
                                symlink = true;
                        }
                        if (symlink) continue;
                        var sample = await safeText.ReadAsync(file.AbsolutePath, 16_384, cancellationToken).ConfigureAwait(false);
                        if (sample.ContentHash.Equals(file.ContentHash, StringComparison.OrdinalIgnoreCase))
                            contentSamples[file.Id] = sample.Text;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception exception)
                    {
                        logger.LogDebug(exception, "Reviewed project file could not be sampled; metadata-only regex matching remains available.");
                    }
                }
            }
            var approvedPatterns = await curators.ListApprovedPatternsAsync(cancellationToken).ConfigureAwait(false);
            var curatorStates = await curators.ListAsync(cancellationToken).ConfigureAwait(false);
            var states = curatorStates.ToDictionary(state => state.Name, StringComparer.OrdinalIgnoreCase);
            var matches = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
            var matchedForFile = new Dictionary<Guid, List<string>>();
            // Only existing, approved catalog patterns may influence ranking. Pattern text remains owned by the curator.
            foreach (var pattern in approvedPatterns.Take(256))
            {
                cancellationToken.ThrowIfCancellationRequested();
                Regex regex;
                try { regex = compilation.Compile(pattern.Pattern, pattern.Flags, TimeSpan.FromMilliseconds(150), pattern.Name); }
                catch (ArgumentException) { continue; }
                catch (RegexMatchTimeoutException) { continue; }
                foreach (var file in allFiles.Where(item => item.Exists).Take(2000))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var metadata = string.Join(' ', file.ProjectRelativePath, file.FileRole, file.Extension, file.ContentType);
                        if (!regex.IsMatch(metadata) && !(contentSamples.TryGetValue(file.Id, out var sample) && regex.IsMatch(sample))) continue;
                        if (!matches.TryGetValue(pattern.Name, out var ids)) matches[pattern.Name] = ids = [];
                        ids.Add(file.Id);
                        if (!matchedForFile.TryGetValue(file.Id, out var names)) matchedForFile[file.Id] = names = [];
                        names.Add(pattern.Name);
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        logger.LogWarning("Curated pattern {PatternName} timed out on project file metadata; no content was executed.", pattern.Name);
                        break;
                    }
                }
            }
            var candidates = allFiles.Where(item => item.Exists && item.IsUserApproved);
            if (source is not null)
                candidates = candidates.Where(file => file.Id == source.Id ||
                    (string.Equals(file.Extension, source.Extension, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(file.FileRole, source.FileRole, StringComparison.OrdinalIgnoreCase)
                    || matchedForFile.GetValueOrDefault(file.Id, []).Intersect(matchedForFile.GetValueOrDefault(source.Id, []), StringComparer.OrdinalIgnoreCase).Any()));
            if (!string.IsNullOrWhiteSpace(request.Query))
                candidates = candidates.Where(file => file.ProjectRelativePath.Contains(request.Query.Trim(), StringComparison.OrdinalIgnoreCase)
                    || file.FileRole.Contains(request.Query.Trim(), StringComparison.OrdinalIgnoreCase)
                    || matchedForFile.GetValueOrDefault(file.Id, []).Any(name => name.Contains(request.Query.Trim(), StringComparison.OrdinalIgnoreCase)));
            var selected = candidates.OrderByDescending(file => source is not null && file.Id == source.Id)
                .ThenByDescending(file => file.LearningReadComplete && string.Equals(file.LearningReviewedHash, file.ContentHash, StringComparison.OrdinalIgnoreCase))
                .ThenBy(file => file.ProjectRelativePath, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Clamp(request.Take, 1, 100)).ToArray();
            var selectedIds = selected.Select(file => file.Id).ToHashSet();
            var compilers = await maintenance.GetCompilerInstallationsAsync(cancellationToken).ConfigureAwait(false);
            var projectRoots = await maintenance.GetWorkspaceRootsAsync(request.ProjectId, cancellationToken).ConfigureAwait(false);
            // Never infer a project's compiler from a language name or from an unrelated installed SDK.
            // Only an explicit project-owned workspace relation authorizes preferred toolchain evidence.
            var preferredCompilerIds = projectRoots.Where(root => root.ProjectId == request.ProjectId && root.PreferredCompilerInstallationId.HasValue)
                .Select(root => root.PreferredCompilerInstallationId!.Value).ToHashSet();
            return new ProjectSourceNavigationResult
            {
                ProjectId = request.ProjectId, RevisionId = request.RevisionId,
                ProjectName = project.Project.Name, ProjectType = project.Project.ProjectType,
                Files = selected.Select(file => new ProjectSourceFileMatch
                {
                    TrackedFileId = file.Id, RelativePath = file.ProjectRelativePath, ContentHash = file.ContentHash,
                    FileRole = file.FileRole, Extension = file.Extension, LearningStatus = file.LearningStatus,
                    LearningReadComplete = file.LearningReadComplete,
                    ReviewedForCurrentHash = file.LearningStatus == "Completed" && file.LearningReadComplete && string.Equals(file.LearningReviewedHash, file.ContentHash, StringComparison.OrdinalIgnoreCase),
                    UserApproved = file.IsUserApproved, ProjectFilePath = Path.GetFileName(file.ProjectFilePath),
                    ApprovedPatternNames = matchedForFile.GetValueOrDefault(file.Id, []).ToArray()
                }).ToArray(),
                RelatedPatterns = matches.Where(entry => entry.Value.Any(selectedIds.Contains))
                    .Select(entry => new ProjectSourceCuratedPattern
                    {
                        Name = entry.Key, UsageScope = states.GetValueOrDefault(entry.Key)?.UsageScope ?? string.Empty,
                        Provenance = states.GetValueOrDefault(entry.Key)?.Provenance ?? string.Empty,
                        ReviewStatus = states.GetValueOrDefault(entry.Key)?.ReviewStatus ?? string.Empty,
                        RequiredIndependentApprovals = states.GetValueOrDefault(entry.Key)?.RequiredIndependentApprovals ?? 0,
                        IndependentApprovals = states.GetValueOrDefault(entry.Key)?.Reviews.Where(item => item.Approved)
                            .Select(item => item.ReviewerIdentity).Distinct(StringComparer.OrdinalIgnoreCase).Count() ?? 0,
                        MatchedTrackedFileIds = entry.Value.Where(selectedIds.Contains).ToArray()
                    }).OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).Take(100).ToArray(),
                Toolchains = compilers.Where(item => preferredCompilerIds.Contains(item.Id) && item.IsEnabled && item.LastValidationSucceeded)
                    .Select(item => new ProjectSourceToolchainEvidence
                    {
                        CompilerInstallationId = item.Id, Language = item.Language, Version = item.Version,
                        KnowledgeProfileKey = item.KnowledgeProfileKey, VersionKnowledgeEntryId = item.VersionKnowledgeEntryId,
                        LastValidationSucceeded = item.LastValidationSucceeded,
                        ProjectPreferred = true, TargetCompatibilityVerified = false
                    }).Take(40).ToArray()
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Navigating approved project source/regex relations failed; source contents omitted.");
            throw;
        }
    }

    /// <summary>
    /// Publishes approved script as part of the project source navigation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The publish tracked project script result produced by the operation.</returns>
    public async Task<PublishTrackedProjectScriptResult> PublishApprovedScriptAsync(PublishTrackedProjectScriptRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.UserConfirmed) throw new InvalidOperationException("Publishing executable project code requires explicit user approval.");
            ArgumentNullException.ThrowIfNull(request.Plugin);
            if (request.Plugin.Kind is not (RuntimePluginKind.CSharpScript or RuntimePluginKind.JavaScript or RuntimePluginKind.Python))
                throw new InvalidOperationException("Only text script plugins can originate from tracked source files.");
            var inspection = await NavigateAsync(new ProjectSourceNavigationRequest
            {
                ProjectId = request.ProjectId, RevisionId = request.RevisionId,
                FocusTrackedFileId = request.TrackedFileId, Take = 100
            }, cancellationToken).ConfigureAwait(false);
            var selected = inspection.Files.SingleOrDefault(file => file.TrackedFileId == request.TrackedFileId)
                ?? throw new InvalidOperationException("The requested file is not approved or no longer exists in the selected revision.");
            if (!selected.ReviewedForCurrentHash || !selected.UserApproved)
                throw new InvalidOperationException("Source must be completely read and reviewed for its current hash before publication.");
            var extension = request.Plugin.Kind switch
            {
                RuntimePluginKind.Python => ".py", RuntimePluginKind.JavaScript => ".js", _ => ".cs"
            };
            if (!selected.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The tracked source extension does not match the requested runtime plugin kind.");
            var tracked = (await maintenance.GetTrackedFilesAsync(request.ProjectId, request.RevisionId, cancellationToken).ConfigureAwait(false))
                .Single(file => file.Id == request.TrackedFileId);
            var project = await projects.GetProjectAsync(request.ProjectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException("The selected project was removed.");
            var selectedRevision = request.RevisionId.HasValue
                ? (await architecture.GetRevisionsAsync(request.ProjectId, cancellationToken).ConfigureAwait(false)).Single(r => r.Id == request.RevisionId)
                : null;
            var root = platform.NormalizeAbsolutePath(!string.IsNullOrWhiteSpace(selectedRevision?.SourceRootPath)
                ? selectedRevision.SourceRootPath : project.Project.RootPath);
            if (platform.IsProtectedWorkspaceRoot(root) || !platform.IsSameOrDescendantPath(root, tracked.AbsolutePath))
                throw new InvalidOperationException("Tracked source path is outside an approved project root.");
            // Resolve symlinks by rejecting them, including parent directory links; never follow unreviewed workspace indirections.
            var relative = Path.GetRelativePath(root, tracked.AbsolutePath);
            var segment = root;
            foreach (var part in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            {
                segment = Path.Combine(segment, part);
                if ((File.Exists(segment) || Directory.Exists(segment)) && File.GetAttributes(segment).HasFlag(FileAttributes.ReparsePoint))
                    throw new InvalidOperationException("Symbolic links are not accepted as executable project sources.");
            }
            if (File.GetAttributes(root).HasFlag(FileAttributes.ReparsePoint))
                throw new InvalidOperationException("The project root must not be a symbolic link.");
            if (tracked.SizeBytes > 1_000_000 || tracked.SizeBytes < 0 || new FileInfo(tracked.AbsolutePath).Length > 1_000_000)
                throw new InvalidOperationException("Source exceeds the bounded script publication size.");
            var bytes = await File.ReadAllBytesAsync(tracked.AbsolutePath, cancellationToken).ConfigureAwait(false);
            var contentHash = Convert.ToHexString(SHA256.HashData(bytes));
            if (!contentHash.Equals(selected.ContentHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Source changed after curation. Rescan and review the exact new hash.");
            var source = new UTF8Encoding(false, true).GetString(bytes);
            var definition = request.Plugin;
            definition.SourceCode = source;
            // Publication is a snapshot. The tracked source remains the canonical editable project file.
            var saved = await plugins.SaveAsync(new SaveRuntimePluginRequest { Plugin = definition, UserConfirmed = true }, cancellationToken).ConfigureAwait(false);
            var artifact = await architecture.SaveArtifactAsync(request.ProjectId, new SaveProjectArtifactRequest
            {
                RevisionId = request.RevisionId, ArtifactKind = "RuntimePluginPublication", Name = saved.FunctionName,
                DataType = "application/json", UserConfirmed = true,
                Description = "Approved publication snapshot of a versioned tracked source file; original project file is authoritative.",
                Value = JsonSerializer.Serialize(new { PluginId = saved.Id, request.TrackedFileId, request.RevisionId,
                    SourceHash = contentHash, CompilerInstallationId = saved.CompilerInstallationId })
            }, cancellationToken).ConfigureAwait(false);
            return new PublishTrackedProjectScriptResult
            {
                ProjectId = request.ProjectId, TrackedFileId = request.TrackedFileId, PluginId = saved.Id,
                ProjectArtifactId = artifact.Id, ApprovedSourceHash = contentHash, Status = "SavedNotBuilt"
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Publishing curated source to runtime-plugin registry failed; source and arguments omitted.");
            throw;
        }
    }
}

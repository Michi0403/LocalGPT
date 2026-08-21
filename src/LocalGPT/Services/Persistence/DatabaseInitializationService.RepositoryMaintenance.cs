using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LocalGPT.Services.Persistence;

/// <summary>Extends deterministic initialization with source-backed repository/project maintenance records.</summary>
public sealed partial class DatabaseInitializationService
{
    /// <summary>Matches the semantic release version encoded in a maintained top-level LocalGPT changelog filename.</summary>
    private readonly Regex RepositoryReleaseChangelogPattern = new(
        @"^CHANGELOG-v(?<version>\d+\.\d+\.\d+)(?:-|\.md$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(2));

    /// <summary>Backfills maintained LocalGPT release history from repository changelogs and selects the newest source-backed version as current.</summary>
    /// <param name="project">LocalGPT Core project seed model being reconciled.</param>
    /// <param name="repositoryRoot">Current LocalGPT repository root.</param>
    /// <returns>The semantic version that should be current after reconciliation.</returns>
    private string PrepareLocalGptReleaseHistory(LocalGptProject project, string repositoryRoot)
    {
        try
        {
            var sourceVersion = ResolveLocalGptSourceVersion(repositoryRoot);
            foreach (var path in Directory.EnumerateFiles(repositoryRoot, "CHANGELOG-v*.md", SearchOption.TopDirectoryOnly)
                         .OrderBy(item => item, StringComparer.OrdinalIgnoreCase))
            {
                var match = RepositoryReleaseChangelogPattern.Match(Path.GetFileName(path));
                if (!match.Success)
                    continue;

                var version = match.Groups["version"].Value;
                var summary = ReadReleaseSummary(path, version);
                EnsureVersion(project, version, repositoryRoot, summary);
                EnsureRevision(project, "main", $"seed-v{version}", repositoryRoot, summary);
            }

            EnsureVersion(
                project,
                sourceVersion,
                repositoryRoot,
                $"Current LocalGPT source release {sourceVersion} detected from src/LocalGPT/LocalGPT.csproj; repository-derived runtime requirements remain authoritative.");
            EnsureRevision(
                project,
                "main",
                $"seed-v{sourceVersion}",
                repositoryRoot,
                $"Current LocalGPT source release {sourceVersion} reconciled from the maintained repository rather than a manually copied seed tail.");

            var currentVersion = SelectNewestProjectVersion(project, sourceVersion);
            project.CurrentVersion = currentVersion;
            project.RootPath = repositoryRoot;
            project.Status = "Active";
            project.IsArchived = false;
            project.UpdatedAtUtc = DateTime.UtcNow;
            SetInMemoryProjectCurrentMarkers(project, currentVersion);
            return currentVersion;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Preparing source-backed LocalGPT release history failed.");
            throw;
        }
    }

    /// <summary>Reads the package version declared by the current LocalGPT source tree.</summary>
    /// <param name="repositoryRoot">Current repository root.</param>
    /// <returns>The declared LocalGPT package version.</returns>
    private string ResolveLocalGptSourceVersion(string repositoryRoot)
    {
        try
        {
            var projectPath = Path.Combine(repositoryRoot, "src", "LocalGPT", "LocalGPT.csproj");
            if (!File.Exists(projectPath))
                throw new FileNotFoundException("The LocalGPT project file used for source-backed version reconciliation was not found.", projectPath);

            var document = XDocument.Load(projectPath, LoadOptions.None);
            var version = document.Descendants("Version").Select(item => item.Value.Trim()).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
            if (string.IsNullOrWhiteSpace(version))
                throw new InvalidDataException("The LocalGPT project file does not declare a Version element.");
            return version;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the LocalGPT source version from the maintained project file failed.");
            throw;
        }
    }

    /// <summary>Reads a bounded human-facing summary from a maintained release changelog.</summary>
    /// <param name="path">Changelog path inside the current repository.</param>
    /// <param name="version">Semantic version represented by the changelog.</param>
    /// <returns>A bounded release summary.</returns>
    private string ReadReleaseSummary(string path, string version)
    {
        try
        {
            foreach (var line in File.ReadLines(path).Take(120))
            {
                var candidate = line.Trim();
                if (string.IsNullOrWhiteSpace(candidate) || candidate.StartsWith('#'))
                    continue;
                candidate = candidate.TrimStart('-', '*', ' ');
                if (candidate.Length == 0)
                    continue;
                return candidate.Length <= 900 ? candidate : candidate[..900];
            }
            return $"Maintained LocalGPT release {version}; details are retained in {Path.GetFileName(path)}.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a maintained LocalGPT release changelog summary failed for version {Version}.", version);
            throw;
        }
    }

    /// <summary>Selects the newest semantic project version without allowing an older running package to overwrite newer learned source evidence.</summary>
    /// <param name="project">Project whose versions are being compared.</param>
    /// <param name="sourceVersion">Version declared by the running source tree.</param>
    /// <returns>The newest parseable semantic version.</returns>
    private string SelectNewestProjectVersion(LocalGptProject project, string sourceVersion)
    {
        try
        {
            var candidates = project.Versions.Select(item => item.Version).Append(sourceVersion).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            string selected = sourceVersion;
            Version? selectedVersion = Version.TryParse(sourceVersion, out var parsedSource) ? parsedSource : null;
            foreach (var candidate in candidates)
            {
                if (!Version.TryParse(candidate, out var parsed))
                    continue;
                if (selectedVersion is null || parsed > selectedVersion)
                {
                    selected = candidate;
                    selectedVersion = parsed;
                }
            }
            return selected;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Selecting the current source-backed project version failed.");
            throw;
        }
    }

    /// <summary>Sets exactly one in-memory version and one matching revision current before missing seed records are attached to the context.</summary>
    /// <param name="project">Project seed model being reconciled.</param>
    /// <param name="currentVersion">Version selected as authoritative current state.</param>
    private void SetInMemoryProjectCurrentMarkers(LocalGptProject project, string currentVersion)
    {
        try
        {
            foreach (var version in project.Versions)
                version.IsCurrent = string.Equals(version.Version, currentVersion, StringComparison.OrdinalIgnoreCase);

            var revision = project.Revisions
                .Where(item => RevisionDeclaresVersion(item, currentVersion))
                .OrderByDescending(item => !string.IsNullOrWhiteSpace(item.SourceSnapshotHash))
                .ThenByDescending(item => item.UpdatedAtUtc)
                .FirstOrDefault()
                ?? project.Revisions.FirstOrDefault(item => string.Equals(item.RevisionName, $"seed-v{currentVersion}", StringComparison.OrdinalIgnoreCase));
            foreach (var item in project.Revisions)
                item.IsCurrent = revision is not null && item.Id == revision.Id;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Setting source-backed current project markers failed.");
            throw;
        }
    }

    /// <summary>Determines whether a stored revision represents a specified semantic source version.</summary>
    /// <param name="revision">Project revision to inspect.</param>
    /// <param name="version">Semantic version to match.</param>
    /// <returns><see langword="true"/> when the revision declares the version.</returns>
    private bool RevisionDeclaresVersion(LocalGptProjectRevision revision, string version)
    {
        try
        {
            if (string.Equals(revision.RevisionName, $"seed-v{version}", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.IsNullOrWhiteSpace(revision.ProjectStructureJson))
                return false;

            using var document = JsonDocument.Parse(revision.ProjectStructureJson);
            if (!document.RootElement.TryGetProperty("Version", out var versionElement) || versionElement.ValueKind != JsonValueKind.String)
                return false;
            var stored = versionElement.GetString() ?? string.Empty;
            return string.Equals(stored, version, StringComparison.OrdinalIgnoreCase) || string.Equals(stored, $"seed-v{version}", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Matching a project revision to semantic version {Version} failed.", version);
            throw;
        }
    }

    /// <summary>Updates already persisted LocalGPT Core scalar/current markers after additive seed records have been staged.</summary>
    /// <param name="db">Database context used for seed reconciliation.</param>
    /// <param name="project">Detached project snapshot containing old and newly added source history.</param>
    /// <param name="isNewProject">Whether the project itself will be inserted by this initialization pass.</param>
    /// <param name="currentVersion">Version selected as current.</param>
    /// <param name="repositoryRoot">Current repository root.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes after persisted current markers have been reconciled.</returns>
    private async Task ReconcilePersistedCoreProjectAsync(
        LocalGptMemoryDbContext db,
        LocalGptProject project,
        bool isNewProject,
        string currentVersion,
        string repositoryRoot,
        CancellationToken token)
    {
        try
        {
            if (isNewProject)
                return;

            var persistedProject = await db.LocalGptProjects.SingleAsync(item => item.Id == project.Id, token).ConfigureAwait(false);
            persistedProject.CurrentVersion = currentVersion;
            if (string.Equals(currentVersion, ResolveLocalGptSourceVersion(repositoryRoot), StringComparison.OrdinalIgnoreCase))
                persistedProject.RootPath = repositoryRoot;
            persistedProject.Status = "Active";
            persistedProject.IsArchived = false;
            persistedProject.UpdatedAtUtc = DateTime.UtcNow;

            var selectedRevision = project.Revisions
                .Where(item => RevisionDeclaresVersion(item, currentVersion))
                .OrderByDescending(item => !string.IsNullOrWhiteSpace(item.SourceSnapshotHash))
                .ThenByDescending(item => item.UpdatedAtUtc)
                .FirstOrDefault();

            var persistedVersions = await db.LocalGptProjectVersions.Where(item => item.ProjectId == project.Id).ToListAsync(token).ConfigureAwait(false);
            foreach (var version in persistedVersions)
                version.IsCurrent = string.Equals(version.Version, currentVersion, StringComparison.OrdinalIgnoreCase);

            var persistedRevisions = await db.LocalGptProjectRevisions.Where(item => item.ProjectId == project.Id).ToListAsync(token).ConfigureAwait(false);
            foreach (var revision in persistedRevisions)
                revision.IsCurrent = selectedRevision is not null && revision.Id == selectedRevision.Id;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Reconciling persisted LocalGPT Core current markers was cancelled.");
            else
                logger.LogError(exception, "Reconciling persisted LocalGPT Core current markers failed.");
            throw;
        }
    }

    /// <summary>Ensures LocalGPT has a canonical PublisherStudio project identity and public repository reference before any source snapshot is learned.</summary>
    /// <param name="db">Database context used for deterministic project seeding.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the PublisherStudio project seed has been staged.</returns>
    private async Task SeedPublisherStudioProjectAsync(LocalGptMemoryDbContext db, CancellationToken token)
    {
        try
        {
            var project = await db.LocalGptProjects
                .AsNoTracking()
                .Include(item => item.Topics)
                .Include(item => item.Versions)
                .Include(item => item.Revisions)
                .Include(item => item.Requirements)
                .Include(item => item.Artifacts)
                .AsSplitQuery()
                .FirstOrDefaultAsync(item => item.Name.Contains("PublisherStudio") || item.Name.Contains("BlazorPublisher"), token)
                .ConfigureAwait(false);
            var isNew = project is null;
            project ??= new LocalGptProject
            {
                Id = Guid.Parse("24adcb42-f32d-4520-a159-cc1e4d26852d"),
                Name = "PublisherStudio",
                Purpose = "Source-backed companion publishing application maintained by LocalGPT Learning Councils and explicit repository refreshes when the user supplies or requests current source evidence.",
                RootPath = string.Empty,
                ProjectType = "DotNetSolution",
                CurrentVersion = "unknown",
                Status = "AwaitingSource",
                RecommendGit = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            var topicIds = project.Topics.Select(item => item.Id).ToHashSet();
            var versionIds = project.Versions.Select(item => item.Id).ToHashSet();
            var revisionIds = project.Revisions.Select(item => item.Id).ToHashSet();
            var requirementIds = project.Requirements.Select(item => item.Id).ToHashSet();
            var artifactIds = project.Artifacts.Select(item => item.Id).ToHashSet();
            EnsureTopic(project, "Repository architecture and self-development", "Maintains PublisherStudio/BlazorPublisher source structure, exact versions, revisions, framework requirements and user-requested source-knowledge refresh evidence independently from LocalGPT.");
            EnsureRequirement(project, "Source-backed repository maintenance", "Recognize PublisherStudio or BlazorPublisher repositories from their actual source metadata and update this canonical project rather than creating a generic Learning Round project.", "localgpt.repository.knowledge.refresh", "Critical");
            EnsureArtifact(project, "Canonical repository source", "SourceRepository", "https://github.com/Michi0403/BlazorPublisher", "uri", "Canonical public PublisherStudio/BlazorPublisher repository supplied by the user. Councils may inspect it read-only and explicit refresh pipelines may update local project knowledge.");
            TrackMissingProjectSeedRecords(db, project, isNew, topicIds, versionIds, revisionIds, requirementIds, artifactIds);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Seeding canonical PublisherStudio project knowledge was cancelled.");
            else
                logger.LogError(exception, "Seeding canonical PublisherStudio project knowledge failed.");
            throw;
        }
    }

    /// <summary>Seeds two user-invokable manual pipelines for refreshing LocalGPT and PublisherStudio public repository knowledge.</summary>
    /// <param name="db">Database context used for pipeline seeding.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when missing example pipelines have been staged.</returns>
    private async Task SeedRepositoryRefreshPipelinesAsync(LocalGptMemoryDbContext db, CancellationToken token)
    {
        try
        {
            var keys = new[] { "repository-refresh.localgpt", "repository-refresh.publisherstudio" };
            var existingKeys = await db.RemoteControlPipelineDefinitions
                .AsNoTracking()
                .Where(item => keys.Contains(item.Key))
                .Select(item => item.Key)
                .ToListAsync(token)
                .ConfigureAwait(false);
            var existing = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!existing.Contains(keys[0]))
                db.RemoteControlPipelineDefinitions.Add(CreateRepositoryRefreshPipeline(keys[0], "Refresh LocalGPT GitHub knowledge", "https://github.com/Michi0403/LocalGPT"));
            if (!existing.Contains(keys[1]))
                db.RemoteControlPipelineDefinitions.Add(CreateRepositoryRefreshPipeline(keys[1], "Refresh PublisherStudio GitHub knowledge", "https://github.com/Michi0403/BlazorPublisher"));
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Seeding repository refresh pipelines was cancelled.");
            else
                logger.LogError(exception, "Seeding repository refresh pipelines failed.");
            throw;
        }
    }

    /// <summary>Creates one manual repository-refresh pipeline that delegates retrieval and project maintenance to existing DI-backed services.</summary>
    /// <param name="key">Stable pipeline key.</param>
    /// <param name="displayName">Human-facing pipeline name.</param>
    /// <param name="sourceUrl">Canonical public GitHub repository URL.</param>
    /// <returns>The seeded pipeline definition.</returns>
    private RemoteControlPipelineDefinition CreateRepositoryRefreshPipeline(string key, string displayName, string sourceUrl)
    {
        try
        {
            var steps = new[]
            {
                new RemoteControlPipelineStepDefinition
                {
                    Key = "refresh-repository-knowledge",
                    DisplayName = displayName,
                    FunctionName = "localgpt.repository.knowledge.refresh",
                    ArgumentsTemplateJson = JsonSerializer.Serialize(new { sourceUrl, branch = "main", maxFiles = 0 }),
                    ContinueOnFailure = false
                }
            };
            return new RemoteControlPipelineDefinition
            {
                Id = Guid.NewGuid(),
                Key = key,
                DisplayName = displayName,
                Description = "User-invoked example pipeline that reads the canonical public GitHub repository and refreshes LocalGPT's local source-backed project/version/revision/workspace/file knowledge. It never writes to GitHub.",
                ConnectorKey = string.Empty,
                Triggers = RemoteControlTriggerKind.Manual,
                StepsJson = JsonSerializer.Serialize(steps),
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating a deterministic repository refresh pipeline definition failed for {PipelineKey}.", key);
            throw;
        }
    }
}

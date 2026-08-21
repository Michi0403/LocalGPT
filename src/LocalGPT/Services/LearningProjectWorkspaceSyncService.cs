using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LocalGPT.Services;

/// <summary>Materializes repository-shaped chat upload evidence into LocalGPT's database-first project structure without copying or modifying the source project.</summary>
/// <param name="dbContextFactory">Database context factory used for project persistence.</param>
/// <param name="databaseInitializer">Database initializer used before project persistence.</param>
/// <param name="workspaces">Chat upload workspace service that owns the extracted source roots.</param>
/// <param name="logger">Logger used for source-ingestion diagnostics.</param>
public sealed class LearningProjectWorkspaceSyncService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    IChatUploadWorkspaceService workspaces,
    ILogger<LearningProjectWorkspaceSyncService> logger) : ILearningProjectWorkspaceSyncService
{
    /// <summary>
    /// Stores the shared read-only excluded directory names value used by <see cref="LearningProjectWorkspaceSyncService"/> across instances of the containing type.
    /// </summary>
    private readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", "bin", "obj", "node_modules"
    };

    /// <summary>
    /// Stores the shared read-only version element pattern value used by <see cref="LearningProjectWorkspaceSyncService"/> across instances of the containing type.
    /// </summary>
    private readonly Regex VersionElementPattern = new("<Version>\\s*([^<]+?)\\s*</Version>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));

    /// <summary>
    /// Performs synchronize as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<LearningProjectSyncResult>> SynchronizeAsync(string? workspaceName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var workspace = ResolveWorkspace(workspaceName);
            if (workspace is null)
            {
                logger.LogInformation("Learning project synchronization skipped because no chat upload workspace is available.");
                return [];
            }

            var extractedRoot = Path.Combine(workspace.RootPath, "extracted");
            if (!Directory.Exists(extractedRoot))
            {
                logger.LogInformation("Learning project synchronization found no extracted source root in workspace {WorkspaceName}.", workspace.WorkspaceName);
                return [];
            }

            var repositoryRoots = DiscoverRepositoryRoots(extractedRoot);
            if (repositoryRoots.Count == 0)
            {
                logger.LogInformation("Learning project synchronization found no repository-shaped source tree in workspace {WorkspaceName}.", workspace.WorkspaceName);
                return [];
            }

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var results = new List<LearningProjectSyncResult>(repositoryRoots.Count);
            foreach (var repositoryRoot in repositoryRoots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await SynchronizeRepositoryAsync(workspace, repositoryRoot, cancellationToken).ConfigureAwait(false);
                if (result is not null)
                    results.Add(result);
            }

            logger.LogInformation(
                "Learning project synchronization persisted {ProjectCount} source project(s) from chat workspace {WorkspaceName}.",
                results.Count,
                workspace.WorkspaceName);
            return results;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Learning project synchronization was cancelled.");
            else
                logger.LogError(exception, "Learning project synchronization failed; repository file content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Resolves workspace as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="workspaceName">Workspace name value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>The chat upload workspace summary produced by the operation.</returns>
    private ChatUploadWorkspaceSummary? ResolveWorkspace(string? workspaceName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(workspaceName))
                return workspaces.GetLatestWorkspace();

            return workspaces.ListWorkspaces(200)
                .FirstOrDefault(item => string.Equals(item.WorkspaceName, workspaceName.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the learning chat upload workspace failed.");
            throw;
        }
    }

    /// <summary>
    /// Discovers repository roots as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="extractedRoot">Extracted root value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> DiscoverRepositoryRoots(string extractedRoot)
    {
        try
        {
            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var projectFile in EnumerateRepositoryFiles(extractedRoot, "*.csproj"))
            {
                var directory = Path.GetDirectoryName(projectFile);
                if (string.IsNullOrWhiteSpace(directory))
                    continue;

                var root = FindRepositoryRoot(directory, extractedRoot);
                if (!string.IsNullOrWhiteSpace(root))
                    candidates.Add(root);
            }

            return candidates
                .Where(path => Directory.Exists(path))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Discovering repository roots in the chat upload workspace failed.");
            throw;
        }
    }

    /// <summary>
    /// Finds repository root as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="startDirectory">Start directory value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="extractedRoot">Extracted root value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string FindRepositoryRoot(string startDirectory, string extractedRoot)
    {
        try
        {
            var current = new DirectoryInfo(startDirectory);
            var extracted = Path.GetFullPath(extractedRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            DirectoryInfo? best = null;
            while (current is not null && current.FullName.StartsWith(extracted, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(Path.Combine(current.FullName, "global.json")) ||
                    current.EnumerateFiles("*.sln", SearchOption.TopDirectoryOnly).Any() ||
                    current.EnumerateFiles("*.slnx", SearchOption.TopDirectoryOnly).Any() ||
                    (Directory.Exists(Path.Combine(current.FullName, "src")) && current.EnumerateFiles("*.md", SearchOption.TopDirectoryOnly).Any()))
                {
                    best = current;
                }

                if (string.Equals(current.FullName.TrimEnd(Path.DirectorySeparatorChar), extracted, StringComparison.OrdinalIgnoreCase))
                    break;
                current = current.Parent;
            }

            return best?.FullName ?? startDirectory;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving a repository root failed; path content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Performs synchronize repository as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="workspace">Workspace value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="repositoryRoot">Repository root value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The learning project sync result produced by the operation.</returns>
    private async Task<LearningProjectSyncResult?> SynchronizeRepositoryAsync(
        ChatUploadWorkspaceSummary workspace,
        string repositoryRoot,
        CancellationToken cancellationToken)
    {
        try
        {
            var source = InspectRepository(repositoryRoot);
            if (source.ProjectFiles.Count == 0)
                return null;

            var files = EnumerateRepositoryFiles(repositoryRoot).ToList();
            if (files.Count == 0)
                return null;

            var now = DateTime.UtcNow;
            var sourceSnapshotHash = await ComputeRepositorySnapshotHashAsync(repositoryRoot, files, cancellationToken).ConfigureAwait(false);
            var structureEntries = files.Select(path => BuildStructureEntry(repositoryRoot, path)).ToList();
            var structureJson = JsonSerializer.Serialize(new
            {
                source.ProjectName,
                source.Version,
                source.SdkVersion,
                TargetFrameworks = source.TargetFrameworks,
                workspace.WorkspaceName,
                WorkspaceRoot = workspace.RootPath,
                RepositoryRoot = repositoryRoot,
                SourceSnapshotHash = sourceSnapshotHash,
                FileCount = structureEntries.Count,
                Files = structureEntries
            });

            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);

            var projects = await db.LocalGptProjects.ToListAsync(cancellationToken).ConfigureAwait(false);
            var project = projects.FirstOrDefault(item => IsSameProjectIdentity(item.Name, source.ProjectName));
            if (project is null)
            {
                project = new LocalGptProject
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = now,
                    Name = source.ProjectName,
                    Purpose = $"Source-backed project learned from chat upload workspace {workspace.WorkspaceName}.",
                    RecommendGit = true
                };
                db.LocalGptProjects.Add(project);
            }

            project.Name = source.ProjectName;
            project.RootPath = repositoryRoot;
            project.ProjectType = "DotNetSolution";
            project.SolutionPath = source.SolutionPath;
            project.CurrentVersion = source.Version;
            project.Status = "Active";
            project.IsArchived = false;
            project.UpdatedAtUtc = now;

            var existingVersions = await db.LocalGptProjectVersions
                .Where(item => item.ProjectId == project.Id)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var item in existingVersions)
                item.IsCurrent = string.Equals(item.Version, source.Version, StringComparison.OrdinalIgnoreCase);
            var version = existingVersions.FirstOrDefault(item => string.Equals(item.Version, source.Version, StringComparison.OrdinalIgnoreCase));
            if (version is null)
            {
                version = new LocalGptProjectVersion
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    Version = source.Version,
                    Notes = "Source-backed version detected from the uploaded repository. Runtime/framework values are read from repository metadata and are not model guesses.",
                    CreatedAtUtc = now
                };
                db.LocalGptProjectVersions.Add(version);
            }
            version.PathSnapshot = repositoryRoot;
            version.IsCurrent = true;

            var existingRevisions = await db.LocalGptProjectRevisions
                .Where(item => item.ProjectId == project.Id)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var item in existingRevisions)
                item.IsCurrent = false;
            var revisionName = $"chat-upload-{source.Version}-{sourceSnapshotHash[..12].ToLowerInvariant()}";
            var revision = existingRevisions.FirstOrDefault(item => string.Equals(item.SourceSnapshotHash, sourceSnapshotHash, StringComparison.OrdinalIgnoreCase));
            if (revision is null)
            {
                revision = new LocalGptProjectRevision
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    CreatedAtUtc = now,
                    BranchName = "chat-upload",
                    RevisionName = revisionName,
                    CreatedBy = "Chat upload workspace",
                    SourceSnapshotHash = sourceSnapshotHash
                };
                db.LocalGptProjectRevisions.Add(revision);
            }
            revision.UpdatedAtUtc = now;
            revision.IsCurrent = true;
            revision.SourceRootPath = repositoryRoot;
            revision.SolutionPath = source.SolutionPath;
            revision.ProjectStructureJson = structureJson;
            revision.SnapshotArchivePath = Path.Combine(workspace.RootPath, "original");

            var workspaceRoot = await db.ProjectWorkspaceRoots
                .SingleOrDefaultAsync(item => item.ProjectId == project.Id && item.ScopeKind == "Project" && item.Name == "Chat upload source", cancellationToken)
                .ConfigureAwait(false);
            if (workspaceRoot is null)
            {
                workspaceRoot = new ProjectWorkspaceRoot
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    ScopeKind = "Project",
                    Name = "Chat upload source",
                    CreatedAtUtc = now
                };
                db.ProjectWorkspaceRoots.Add(workspaceRoot);
            }
            workspaceRoot.RootPath = repositoryRoot;
            workspaceRoot.EnvironmentRootPath = repositoryRoot;
            workspaceRoot.EnvironmentKind = "LocalHost";
            workspaceRoot.SolutionPattern = @"(?i)\.(sln|slnx)$";
            workspaceRoot.ProjectTypePattern = @"(?i)DotNet.*";
            workspaceRoot.DefaultSubdirectoriesJson = JsonSerializer.Serialize(Directory.EnumerateDirectories(repositoryRoot, "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).Where(name => !string.IsNullOrWhiteSpace(name)).OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
            workspaceRoot.ExpectedStructureRegex = BuildExpectedStructureRegex(source);
            workspaceRoot.AccessPolicyJson = "[\"read\"]";
            workspaceRoot.LastPermissionStatus = "SourceBackedReadOnly";
            workspaceRoot.LastPermissionReadAccess = true;
            workspaceRoot.LastPermissionWriteAccess = false;
            workspaceRoot.LastPermissionSummary = $"Prefilled from chat upload workspace {workspace.WorkspaceName}; source is evidence and is not modified by learning synchronization.";
            workspaceRoot.LastPermissionCheckedAtUtc = now;
            workspaceRoot.Priority = 1;
            workspaceRoot.IsDefault = true;
            workspaceRoot.IsEnabled = true;
            workspaceRoot.UpdatedAtUtc = now;

            await SynchronizeSourceRequirementsAsync(db, project.Id, revision.Id, source, now, cancellationToken).ConfigureAwait(false);
            await SynchronizeTrackedFilesAsync(db, project.Id, revision.Id, repositoryRoot, source.SolutionPath, files, now, cancellationToken).ConfigureAwait(false);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new LearningProjectSyncResult(
                project.Id,
                revision.Id,
                source.ProjectName,
                source.Version,
                source.SdkVersion,
                source.TargetFrameworks,
                workspace.WorkspaceName,
                repositoryRoot,
                structureEntries.Count,
                sourceSnapshotHash);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Synchronizing one learned source repository was cancelled.");
            else
                logger.LogError(exception, "Synchronizing one learned source repository failed; repository content and paths were omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Performs inspect repository as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="repositoryRoot">Repository root value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>The repository inspection produced by the operation.</returns>
    private RepositoryInspection InspectRepository(string repositoryRoot)
    {
        try
        {
            var projectFiles = EnumerateRepositoryFiles(repositoryRoot, "*.csproj").ToList();
            var preferred = projectFiles.FirstOrDefault(path => string.Equals(Path.GetFileNameWithoutExtension(path), "LocalGPT", StringComparison.OrdinalIgnoreCase))
                ?? projectFiles.FirstOrDefault(path => string.Equals(Path.GetFileNameWithoutExtension(path), "PublisherStudio.Web", StringComparison.OrdinalIgnoreCase))
                ?? projectFiles.FirstOrDefault(path => string.Equals(Path.GetFileNameWithoutExtension(path), "PublisherStudio", StringComparison.OrdinalIgnoreCase))
                ?? projectFiles.FirstOrDefault(path => string.Equals(Path.GetFileNameWithoutExtension(path), "BlazorPublisher", StringComparison.OrdinalIgnoreCase))
                ?? projectFiles.OrderBy(path => path.Count(character => character is '/' or '\\')).ThenBy(path => path, StringComparer.OrdinalIgnoreCase).First();

            var projectName = CanonicalProjectName(Path.GetFileNameWithoutExtension(preferred), repositoryRoot, projectFiles);
            var version = ReadProjectVersion(preferred);
            var sdkVersion = ReadSdkVersion(Path.Combine(repositoryRoot, "global.json"));
            var frameworks = ReadTargetFrameworks(projectFiles);
            var solutionPath = EnumerateRepositoryFiles(repositoryRoot)
                .Where(path => path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? string.Empty;

            return new RepositoryInspection(projectName, version, sdkVersion, frameworks, solutionPath, projectFiles);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Inspecting repository metadata failed; source content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Determines the canonical project name as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="preferredProjectName">Preferred project name value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="repositoryRoot">Repository root value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="projectFiles">String dependency used by the learning project workspace sync workflow to provide the corresponding application capability.</param>
    /// <returns>The string produced by the operation.</returns>
    private string CanonicalProjectName(string preferredProjectName, string repositoryRoot, IReadOnlyList<string> projectFiles)
    {
        try
        {
            if (projectFiles.Any(path => string.Equals(Path.GetFileName(path), "LocalGPT.csproj", StringComparison.OrdinalIgnoreCase)))
                return "LocalGPT";
            if (projectFiles.Any(path => Path.GetFileNameWithoutExtension(path).StartsWith("PublisherStudio", StringComparison.OrdinalIgnoreCase)) ||
                projectFiles.Any(path => string.Equals(Path.GetFileName(path), "BlazorPublisher.csproj", StringComparison.OrdinalIgnoreCase)) ||
                Path.GetFileName(repositoryRoot).Contains("PublisherStudio", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(repositoryRoot).Contains("BlazorPublisher", StringComparison.OrdinalIgnoreCase))
                return "PublisherStudio";

            var name = preferredProjectName;
            if (string.IsNullOrWhiteSpace(name))
                name = Path.GetFileName(repositoryRoot);
            return Regex.Replace(name, @"-v?\d+(?:\.\d+){1,3}.*$", string.Empty, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2)).Trim(' ', '-', '_');
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving source-backed project identity failed.");
            throw;
        }
    }

    /// <summary>
    /// Reads project version as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectFile">Project file value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ReadProjectVersion(string projectFile)
    {
        try
        {
            var text = File.ReadAllText(projectFile);
            var match = VersionElementPattern.Match(text);
            return match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value)
                ? match.Groups[1].Value.Trim()
                : "0.0.0-source";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading project version metadata failed; file path omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Reads sdk version as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="globalJsonPath">Global json path value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ReadSdkVersion(string globalJsonPath)
    {
        try
        {
            if (!File.Exists(globalJsonPath))
                return string.Empty;
            using var document = JsonDocument.Parse(File.ReadAllText(globalJsonPath));
            return document.RootElement.TryGetProperty("sdk", out var sdk) && sdk.TryGetProperty("version", out var version)
                ? version.GetString()?.Trim() ?? string.Empty
                : string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Reading global.json SDK metadata failed; the source-backed SDK requirement will remain unspecified.");
            return string.Empty;
        }
    }

    /// <summary>
    /// Reads target frameworks as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectFiles">String dependency used by the learning project workspace sync workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<string> ReadTargetFrameworks(IReadOnlyList<string> projectFiles)
    {
        try
        {
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var projectFile in projectFiles)
            {
                try
                {
                    var document = XDocument.Load(projectFile, LoadOptions.None);
                    foreach (var element in document.Descendants().Where(item => item.Name.LocalName is "TargetFramework" or "TargetFrameworks"))
                    {
                        foreach (var value in element.Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            values.Add(value);
                    }
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Reading target-framework metadata from one project file failed; that file will not contribute a framework claim.");
                }
            }
            return values.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Collecting source-backed target framework metadata failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs synchronize source requirements as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="db">Database value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="source">Source value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="now">Now value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SynchronizeSourceRequirementsAsync(
        LocalGptMemoryDbContext db,
        Guid projectId,
        Guid revisionId,
        RepositoryInspection source,
        DateTime now,
        CancellationToken cancellationToken)
    {
        try
        {
            var existing = await db.LocalGptProjectRequirements
                .Where(item => item.ProjectId == projectId && item.SourceKind == "RepositoryMetadata")
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var required = new List<(string Name, string Description, string Capability)>();
            if (!string.IsNullOrWhiteSpace(source.SdkVersion))
                required.Add((".NET SDK", $"Repository global.json requires SDK {source.SdkVersion}.", $"dotnet-sdk:{source.SdkVersion}"));
            foreach (var framework in source.TargetFrameworks)
                required.Add(("Target framework", $"Repository project metadata declares target framework {framework}.", $"target-framework:{framework}"));

            var requiredCapabilities = required
                .Select(item => item.Capability)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var requirement in required)
            {
                var row = existing.FirstOrDefault(item => string.Equals(item.RequiredCapability, requirement.Capability, StringComparison.OrdinalIgnoreCase));
                if (row is null)
                {
                    row = new LocalGptProjectRequirement
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        CreatedAtUtc = now,
                        SourceKind = "RepositoryMetadata"
                    };
                    db.LocalGptProjectRequirements.Add(row);
                }
                row.RevisionId = revisionId;
                row.Name = requirement.Name;
                row.Description = requirement.Description;
                row.RequirementType = "Runtime";
                row.Status = "Required";
                row.Priority = "Required";
                row.RequiredCapability = requirement.Capability;
                row.IsUserApproved = true;
                row.UpdatedAtUtc = now;
            }

            foreach (var stale in existing.Where(item => !requiredCapabilities.Contains(item.RequiredCapability)))
            {
                stale.RevisionId = revisionId;
                stale.Status = "Superseded";
                stale.Priority = "Historical";
                stale.IsUserApproved = false;
                stale.UpdatedAtUtc = now;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Synchronizing source-backed project requirements failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs synchronize tracked files as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="db">Database value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="repositoryRoot">Repository root value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="solutionPath">Solution path value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="files">String dependency used by the learning project workspace sync workflow to provide the corresponding application capability.</param>
    /// <param name="now">Now value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SynchronizeTrackedFilesAsync(
        LocalGptMemoryDbContext db,
        Guid projectId,
        Guid revisionId,
        string repositoryRoot,
        string solutionPath,
        IReadOnlyList<string> files,
        DateTime now,
        CancellationToken cancellationToken)
    {
        try
        {
            var existingRows = await db.LocalGptProjectTrackedFiles
                .Where(item => item.ProjectId == projectId && item.RevisionId == revisionId)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var existing = existingRows.ToDictionary(item => item.ProjectRelativePath, StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relative = NormalizeRelativePath(Path.GetRelativePath(repositoryRoot, path));
                seen.Add(relative);
                var info = new FileInfo(path);
                var hash = await ComputeFileHashAsync(path, cancellationToken).ConfigureAwait(false);
                if (!existing.TryGetValue(relative, out var row))
                {
                    row = new LocalGptProjectTrackedFile
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        RevisionId = revisionId,
                        ProjectRelativePath = relative
                    };
                    db.LocalGptProjectTrackedFiles.Add(row);
                }

                row.StableFileKey = CreateStableFileKey(projectId, relative);
                row.AbsolutePath = path;
                row.WorkspaceRelativePath = relative;
                row.SolutionPath = solutionPath;
                row.ProjectFilePath = ResolveOwningProjectFile(repositoryRoot, path);
                row.FileName = Path.GetFileName(path);
                row.Extension = Path.GetExtension(path);
                row.ContentType = GuessContentType(path);
                row.EncodingName = IsLikelyTextFile(path) ? "utf-8" : "binary";
                row.FileRole = GuessFileRole(relative);
                row.ContentHash = hash;
                row.SizeBytes = info.Length;
                row.LastWriteTimeUtc = info.LastWriteTimeUtc;
                row.LastSeenAtUtc = now;
                row.Exists = true;
                row.IsGenerated = relative.Contains("/generated/", StringComparison.OrdinalIgnoreCase) || relative.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
            }

            foreach (var row in existing.Values.Where(item => !seen.Contains(item.ProjectRelativePath)))
            {
                row.Exists = false;
                row.LastSeenAtUtc = now;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Synchronizing tracked project-file structure failed; file content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Performs enumerate repository files as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="searchPattern">Search pattern value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IEnumerable<string> EnumerateRepositoryFiles(string root, string searchPattern = "*")
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            IEnumerable<string> childDirectories;
            IEnumerable<string> files;
            try
            {
                childDirectories = Directory.EnumerateDirectories(directory).Where(path => !ExcludedDirectoryNames.Contains(Path.GetFileName(path))).ToArray();
                files = Directory.EnumerateFiles(directory, searchPattern, SearchOption.TopDirectoryOnly).ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
                yield return file;
            foreach (var child in childDirectories)
                pending.Push(child);
        }
    }

    /// <summary>
    /// Builds structure entry as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="path">Path value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
    private object BuildStructureEntry(string root, string path)
    {
        try
        {
            var info = new FileInfo(path);
            return new
            {
                Path = NormalizeRelativePath(Path.GetRelativePath(root, path)),
                SizeBytes = info.Length,
                LastWriteTimeUtc = info.LastWriteTimeUtc
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building one source project structure entry failed; source paths were omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Builds expected structure regex as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildExpectedStructureRegex(RepositoryInspection source)
    {
        try
        {
            var expressions = new List<string> { @"(^|/)src(/|$)" };
            if (!string.IsNullOrWhiteSpace(source.SolutionPath))
                expressions.Add(@"(^|/)[^/]+\.slnx?$" );
            return $"(?is)({string.Join('|', expressions)})";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the source-backed project structure regex failed.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether same project identity as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="databaseName">Database name value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="sourceName">Source name value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsSameProjectIdentity(string databaseName, string sourceName)
    {
        try
        {
            if (string.Equals(databaseName?.Trim(), sourceName, StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(sourceName, "LocalGPT", StringComparison.OrdinalIgnoreCase))
                return databaseName?.StartsWith("LocalGPT ", StringComparison.OrdinalIgnoreCase) == true || string.Equals(databaseName, "LocalGPT Core", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(sourceName, "PublisherStudio", StringComparison.OrdinalIgnoreCase))
                return databaseName?.Contains("PublisherStudio", StringComparison.OrdinalIgnoreCase) == true || databaseName?.Contains("BlazorPublisher", StringComparison.OrdinalIgnoreCase) == true;
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Comparing source-backed project identities failed.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes relative path as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeRelativePath(string value)
    {
        try
        {
            return value.Replace('\\', '/').TrimStart('/');
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing one project-relative path failed; path content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Creates stable file key as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="relativePath">Relative path value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string CreateStableFileKey(Guid projectId, string relativePath)
    {
        try
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{projectId:N}|{relativePath.ToLowerInvariant()}"));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating one stable project-file key failed; path content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Computes file hash as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> ComputeFileHashAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 64, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Computing one learned source-file hash was cancelled.");
            else
                logger.LogError(exception, "Computing one learned source-file hash failed; path content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Computes repository snapshot hash as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="files">String dependency used by the learning project workspace sync workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> ComputeRepositorySnapshotHashAsync(string root, IReadOnlyList<string> files, CancellationToken cancellationToken)
    {
        try
        {
            using var incremental = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var path in files.OrderBy(path => NormalizeRelativePath(Path.GetRelativePath(root, path)), StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relative = NormalizeRelativePath(Path.GetRelativePath(root, path));
                var pathBytes = Encoding.UTF8.GetBytes(relative + "\n");
                incremental.AppendData(pathBytes);
                var fileHash = await ComputeFileHashAsync(path, cancellationToken).ConfigureAwait(false);
                incremental.AppendData(Encoding.ASCII.GetBytes(fileHash));
            }
            return Convert.ToHexString(incremental.GetHashAndReset()).ToLowerInvariant();
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Computing the learned repository snapshot hash was cancelled.");
            else
                logger.LogError(exception, "Computing the learned repository snapshot hash failed; source paths were omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Resolves owning project file as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="repositoryRoot">Repository root value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="path">Path value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveOwningProjectFile(string repositoryRoot, string path)
    {
        try
        {
            var current = new DirectoryInfo(Path.GetDirectoryName(path) ?? repositoryRoot);
            var root = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            while (current is not null && current.FullName.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                var project = current.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly).OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
                if (project is not null)
                    return project.FullName;
                current = current.Parent;
            }
            return string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the owning project file failed; source paths were omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Performs guess content type as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string GuessContentType(string path)
    {
        try
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".json" => "application/json",
                ".xml" or ".csproj" or ".props" or ".targets" => "application/xml",
                ".html" or ".htm" => "text/html",
                ".css" => "text/css",
                ".js" or ".mjs" => "text/javascript",
                ".md" or ".txt" or ".cs" or ".razor" or ".ps1" or ".sh" or ".yml" or ".yaml" => "text/plain",
                _ => "application/octet-stream"
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Determining tracked-file content type failed; path content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether likely text file as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsLikelyTextFile(string path)
    {
        try
        {
            return GuessContentType(path) != "application/octet-stream";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Determining whether one tracked file is text failed; path content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Performs guess file role as part of the learning project workspace sync service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="relativePath">Relative path value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string GuessFileRole(string relativePath)
    {
        try
        {
            if (relativePath.StartsWith("docs/", StringComparison.OrdinalIgnoreCase) || relativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) return "Documentation";
            if (relativePath.Contains("/tests/", StringComparison.OrdinalIgnoreCase) || relativePath.StartsWith("tests/", StringComparison.OrdinalIgnoreCase)) return "Test";
            if (relativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || relativePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || relativePath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase) || relativePath.EndsWith("global.json", StringComparison.OrdinalIgnoreCase)) return "ProjectMetadata";
            if (relativePath.StartsWith("build/", StringComparison.OrdinalIgnoreCase) || relativePath.StartsWith(".github/", StringComparison.OrdinalIgnoreCase)) return "BuildAutomation";
            return "Source";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Determining one tracked-file role failed; path content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Represents a repository inspection helper type nested within <see cref="LearningProjectWorkspaceSyncService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="ProjectName">Project name value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="Version">Version value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="SdkVersion">Sdk version value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="TargetFrameworks">String dependency used by the learning project workspace sync workflow to provide the corresponding application capability.</param>
    /// <param name="SolutionPath">Solution path value supplied to the learning project workspace sync operation and used when producing its result.</param>
    /// <param name="ProjectFiles">String dependency used by the learning project workspace sync workflow to provide the corresponding application capability.</param>
    private sealed record RepositoryInspection(
        string ProjectName,
        string Version,
        string SdkVersion,
        IReadOnlyList<string> TargetFrameworks,
        string SolutionPath,
        IReadOnlyList<string> ProjectFiles);
}

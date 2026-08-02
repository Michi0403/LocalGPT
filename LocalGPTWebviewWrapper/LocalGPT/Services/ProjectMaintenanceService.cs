using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

public sealed class ProjectMaintenanceService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<ProjectMaintenanceService> logger) : IProjectMaintenanceService
{
    private const int MaxCompilerCandidates = 200;
    private const int MaxCapturedCharacters = 2_000_000;

    public async Task<IReadOnlyList<ProjectWorkspaceRoot>> GetWorkspaceRootsAsync(Guid? projectId = null, CancellationToken cancellationToken = default)
    {
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = db.ProjectWorkspaceRoots.AsNoTracking();
        if (projectId is Guid id)
            query = query.Where(item => item.ProjectId == null || item.ProjectId == id);
        return await query.OrderBy(item => item.Priority).ThenBy(item => item.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProjectWorkspaceRoot> SaveWorkspaceRootAsync(SaveProjectWorkspaceRootRequest request, CancellationToken cancellationToken = default)
    {
        RequireConfirmation(request.UserConfirmed, "saving a workspace root");
        var name = RequireText(request.Name, nameof(request.Name), 200);
        var rootPath = NormalizeAbsolutePath(request.RootPath, nameof(request.RootPath));
        var scope = NormalizeScope(request.ScopeKind);
        ValidateRegex(request.ProjectTypePattern, nameof(request.ProjectTypePattern), allowEmpty: true);
        ValidateRegex(request.SolutionPattern, nameof(request.SolutionPattern), allowEmpty: false);
        ValidateRegex(request.ExpectedStructureRegex, nameof(request.ExpectedStructureRegex), allowEmpty: true);
        ValidateJsonObject(request.EnvironmentVariablesJson, nameof(request.EnvironmentVariablesJson));
        ValidateJsonArray(request.DefaultSubdirectoriesJson, nameof(request.DefaultSubdirectoriesJson));
        ValidateWorkspaceAccessPolicyJson(request.AccessPolicyJson);
        if (scope == "Project" && request.ProjectId is null)
            throw new ArgumentException("A project-scoped workspace requires a project id.", nameof(request.ProjectId));

        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (request.ProjectId is Guid projectId && !await db.LocalGptProjects.AnyAsync(item => item.Id == projectId, cancellationToken).ConfigureAwait(false))
            throw new KeyNotFoundException($"Project {projectId} was not found.");

        ProjectWorkspaceRoot item;
        if (request.Id is Guid id)
            item = await db.ProjectWorkspaceRoots.SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Workspace root {id} was not found.");
        else
        {
            item = new ProjectWorkspaceRoot();
            db.ProjectWorkspaceRoots.Add(item);
        }

        if (request.IsDefault)
        {
            var competing = await db.ProjectWorkspaceRoots
                .Where(entry => entry.Id != item.Id && entry.ScopeKind == scope && entry.ProjectId == request.ProjectId && entry.IsDefault)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var existing in competing) existing.IsDefault = false;
        }

        item.Name = name;
        item.RootPath = rootPath;
        item.ScopeKind = scope;
        item.ProjectId = request.ProjectId;
        item.ProjectTypePattern = Trim(request.ProjectTypePattern, 240);
        item.SolutionPattern = TrimOrFallback(request.SolutionPattern, 1000, @"(?i)\.(sln|slnx)$");
        item.EnvironmentKind = TrimOrFallback(request.EnvironmentKind, 80, "LocalHost");
        item.EnvironmentRootPath = NormalizeOptionalPath(request.EnvironmentRootPath);
        item.PreferredCompilerInstallationId = request.PreferredCompilerInstallationId;
        item.BuildArguments = Trim(request.BuildArguments, 16000);
        item.EnvironmentVariablesJson = TrimOrFallback(request.EnvironmentVariablesJson, 32000, "{}");
        item.DefaultSubdirectoriesJson = TrimOrFallback(request.DefaultSubdirectoriesJson, 16000, "[]");
        item.AccessPolicyJson = TrimOrFallback(request.AccessPolicyJson, 64000, "[]");
        item.ExpectedStructureRegex = Trim(request.ExpectedStructureRegex, 16000);
        item.LastPermissionStatus = "NotChecked";
        item.LastPermissionSummary = string.Empty;
        item.LastPermissionReadAccess = false;
        item.LastPermissionWriteAccess = false;
        item.LastPermissionCheckedAtUtc = null;
        item.Priority = Math.Clamp(request.Priority, 0, 10000);
        item.IsDefault = request.IsDefault;
        item.IsEnabled = request.IsEnabled;
        item.UpdatedAtUtc = DateTime.UtcNow;
        item.LastResolvedAtUtc = DateTime.UtcNow;
        item.LastResolutionStatus = Directory.Exists(rootPath) ? "Available" : "Missing";
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Saved {ScopeKind} workspace root {WorkspaceRootId}; path content omitted from logs.", item.ScopeKind, item.Id);
        return item;
    }

    public async Task<ProjectWorkspaceResolution> ResolveWorkspaceAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var project = await db.LocalGptProjects.AsNoTracking().SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Project {projectId} was not found.");
        var roots = await db.ProjectWorkspaceRoots.Where(item => item.IsEnabled).OrderBy(item => item.Priority).ToListAsync(cancellationToken).ConfigureAwait(false);

        ProjectWorkspaceRoot? selected = roots.FirstOrDefault(item => item.ScopeKind == "Project" && item.ProjectId == projectId);
        var reason = selected is null ? string.Empty : "Project-specific workspace";
        if (selected is null)
        {
            selected = roots.FirstOrDefault(item => item.ScopeKind == "ProjectType" && RegexMatches(item.ProjectTypePattern, project.ProjectType));
            if (selected is not null) reason = $"Project type matched {selected.ProjectTypePattern}";
        }
        if (selected is null)
        {
            selected = roots.FirstOrDefault(item => item.ScopeKind == "Global" && item.IsDefault)
                ?? roots.FirstOrDefault(item => item.ScopeKind == "Global");
            if (selected is not null) reason = "Global workspace";
        }

        var path = selected?.RootPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocalGPT", "Workspaces");
            reason = "Per-user LocalGPT fallback";
        }

        var exists = Directory.Exists(path);
        if (selected is not null)
        {
            selected.LastResolvedAtUtc = DateTime.UtcNow;
            selected.LastResolutionStatus = exists ? "Available" : "Missing";
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        return new ProjectWorkspaceResolution(selected?.Id, path, selected?.ScopeKind ?? "Fallback", reason, exists);
    }

    public async Task<WorkspacePermissionAssessment> AssessWorkspacePermissionsAsync(Guid workspaceRootId, bool userConfirmedWriteProbe, CancellationToken cancellationToken = default)
    {
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var workspace = await db.ProjectWorkspaceRoots.SingleOrDefaultAsync(item => item.Id == workspaceRootId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Workspace root {workspaceRootId} was not found.");
        var findings = new List<WorkspacePermissionFinding>();
        var root = workspace.RootPath;
        var rootExists = Directory.Exists(root);
        var readAccess = false;
        var writeAccess = false;
        var expectedSubdirectories = ParseStringArray(workspace.DefaultSubdirectoriesJson);
        var checkedAt = DateTime.UtcNow;

        if (!rootExists)
        {
            findings.Add(new("Danger", "ROOT_MISSING", "The workspace root does not exist."));
        }
        else
        {
            readAccess = CanEnumerateDirectory(root);
            if (!readAccess)
                findings.Add(new("Danger", "ROOT_READ_DENIED", "The current LocalGPT process cannot enumerate the workspace root."));
            if (IsBroadOrSystemRoot(root))
                findings.Add(new("Danger", "ROOT_TOO_BROAD", "The workspace points at a drive, user-profile, operating-system, or program-files root. LocalGPT would have substantially broader rights than a bounded project workspace needs."));

            if (userConfirmedWriteProbe)
                writeAccess = await ProbeDirectoryWriteAsync(root, cancellationToken).ConfigureAwait(false);
            else
                findings.Add(new("Warning", "WRITE_NOT_PROBED", "Write access was not probed because fresh user confirmation was not supplied."));
            if (userConfirmedWriteProbe && !writeAccess)
                findings.Add(new("Danger", "ROOT_WRITE_DENIED", "The current LocalGPT process could not create and remove a bounded probe file in the workspace root."));

            foreach (var relative in expectedSubdirectories)
            {
                var safeRelative = NormalizeRelativePolicyPath(relative);
                if (string.IsNullOrWhiteSpace(safeRelative))
                {
                    findings.Add(new("Danger", "SUBDIRECTORY_INVALID", "An expected subdirectory is absolute, empty, or contains a parent-path escape.", relative));
                    continue;
                }
                var fullPath = Path.GetFullPath(Path.Combine(root, safeRelative));
                if (!IsPathInside(root, fullPath))
                {
                    findings.Add(new("Danger", "SUBDIRECTORY_ESCAPE", "An expected subdirectory escapes the workspace root.", relative));
                    continue;
                }
                if (!Directory.Exists(fullPath))
                    findings.Add(new("Warning", "SUBDIRECTORY_MISSING", "An expected workspace subdirectory is missing.", safeRelative));
            }

            var relativeEntries = EnumerateRelativeEntries(root, 5000, findings);
            if (!string.IsNullOrWhiteSpace(workspace.ExpectedStructureRegex))
            {
                var structure = string.Join("\n", relativeEntries);
                if (!CompileRegex(workspace.ExpectedStructureRegex, nameof(workspace.ExpectedStructureRegex), @"(?s).*").IsMatch(structure))
                    findings.Add(new("Warning", "STRUCTURE_REGEX_MISMATCH", "The current directory/file map does not satisfy the configured expected-structure regular expression."));
            }
            foreach (var rule in ParseAccessPolicy(workspace.AccessPolicyJson))
                EvaluateAccessPolicyRule(rule, relativeEntries, root, writeAccess, findings);
        }

        var environmentRoot = string.IsNullOrWhiteSpace(workspace.EnvironmentRootPath) ? root : workspace.EnvironmentRootPath;
        if (!string.IsNullOrWhiteSpace(environmentRoot))
        {
            if (!Directory.Exists(environmentRoot))
                findings.Add(new("Warning", "ENVIRONMENT_ROOT_MISSING", "The configured local environment root does not exist."));
            else if (rootExists && !IsPathInside(root, environmentRoot))
                findings.Add(new("Warning", "ENVIRONMENT_OUTSIDE_WORKSPACE", "The local environment root is outside the workspace. Review this intentionally before Council build execution."));
        }

        if (workspace.PreferredCompilerInstallationId is Guid compilerId)
        {
            var compiler = await db.ProjectCompilerInstallations.AsNoTracking().SingleOrDefaultAsync(item => item.Id == compilerId, cancellationToken).ConfigureAwait(false);
            if (compiler is null || !compiler.IsEnabled)
                findings.Add(new("Danger", "COMPILER_UNAVAILABLE", "The assigned compiler installation is missing or disabled."));
            else if (!File.Exists(compiler.ExecutablePath))
                findings.Add(new("Danger", "COMPILER_PATH_MISSING", "The assigned compiler executable does not exist."));
            else if (!compiler.LastValidationSucceeded)
                findings.Add(new("Warning", "COMPILER_UNVALIDATED", "The assigned compiler has not completed a successful version probe."));
        }
        else
        {
            findings.Add(new("Warning", "COMPILER_NOT_ASSIGNED", "No preferred compiler installation is assigned to this workspace."));
        }

        var status = findings.Any(item => item.Severity == "Danger") ? "Danger" : findings.Any(item => item.Severity == "Warning") ? "Warning" : "Approved";
        workspace.LastPermissionStatus = status;
        workspace.LastPermissionReadAccess = readAccess;
        workspace.LastPermissionWriteAccess = writeAccess;
        workspace.LastPermissionCheckedAtUtc = checkedAt;
        workspace.LastPermissionSummary = Trim(string.Join(" | ", findings.Take(20).Select(item => $"{item.Severity}:{item.Code}:{item.Message}")), 4000);
        workspace.LastResolvedAtUtc = checkedAt;
        workspace.LastResolutionStatus = rootExists ? "Available" : "Missing";
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Workspace {WorkspaceRootId} permission assessment completed with status {Status}, read={ReadAccess}, write={WriteAccess}; paths were omitted from logs.", workspace.Id, status, readAccess, writeAccess);
        return new WorkspacePermissionAssessment(workspace.Id, status, checkedAt, rootExists, readAccess, writeAccess, environmentRoot, workspace.PreferredCompilerInstallationId, expectedSubdirectories, findings);
    }

    public async Task<IReadOnlyList<ProjectCompilerInstallation>> GetCompilerInstallationsAsync(CancellationToken cancellationToken = default)
    {
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.ProjectCompilerInstallations.AsNoTracking()
            .OrderBy(item => item.Language).ThenByDescending(item => item.IsDefaultForLanguage).ThenBy(item => item.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProjectCompilerInstallation> SaveCompilerInstallationAsync(SaveProjectCompilerInstallationRequest request, CancellationToken cancellationToken = default)
    {
        RequireConfirmation(request.UserConfirmed, "saving a compiler installation");
        var executable = NormalizeAbsolutePath(request.ExecutablePath, nameof(request.ExecutablePath));
        ValidateJsonObject(request.EnvironmentVariablesJson, nameof(request.EnvironmentVariablesJson));
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        ProjectCompilerInstallation item;
        if (request.Id is Guid id)
            item = await db.ProjectCompilerInstallations.SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Compiler installation {id} was not found.");
        else
        {
            item = new ProjectCompilerInstallation();
            db.ProjectCompilerInstallations.Add(item);
        }

        var language = RequireText(request.Language, nameof(request.Language), 80);
        if (request.IsDefaultForLanguage)
        {
            var defaults = await db.ProjectCompilerInstallations
                .Where(entry => entry.Id != item.Id && entry.Language == language && entry.IsDefaultForLanguage)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var existing in defaults) existing.IsDefaultForLanguage = false;
        }

        item.Name = RequireText(request.Name, nameof(request.Name), 200);
        item.Language = language;
        item.ExecutablePath = executable;
        item.CompilerHomePath = NormalizeOptionalPath(request.CompilerHomePath);
        item.Version = Trim(request.Version, 160);
        item.Architecture = Trim(request.Architecture, 80);
        item.DiscoverySource = TrimOrFallback(request.DiscoverySource, 80, "Custom");
        item.ValidationArguments = TrimOrFallback(request.ValidationArguments, 500, DefaultValidationArguments(language, executable));
        item.EnvironmentVariablesJson = string.IsNullOrWhiteSpace(request.EnvironmentVariablesJson) ? "{}" : request.EnvironmentVariablesJson.Trim();
        item.IsEnabled = request.IsEnabled;
        item.IsDefaultForLanguage = request.IsDefaultForLanguage;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Saved compiler installation {CompilerId} for language {Language}; executable path omitted from logs.", item.Id, item.Language);
        return item;
    }

    /// <summary>Discovers compiler and runtime executables from approved local search locations.</summary>
    /// <param name="request">Discovery roots, persistence preference and explicit approval.</param>
    /// <param name="cancellationToken">Cancels local discovery and persistence.</param>
    /// <returns>A task that returns detected or persisted compiler profiles.</returns>
    public async Task<IReadOnlyList<ProjectCompilerInstallation>> DiscoverCompilerInstallationsAsync(DiscoverProjectCompilersRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireConfirmation(request.UserConfirmed, "discovering and saving compiler installations");
        try
        {
            var candidates = await Task.Run(
                () => DiscoverCompilerCandidates(request.CustomSearchRoots, cancellationToken).Take(MaxCompilerCandidates).ToList(),
                cancellationToken).ConfigureAwait(false);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var defaultLanguages = (await db.ProjectCompilerInstallations.AsNoTracking()
                .Where(item => item.IsDefaultForLanguage)
                .Select(item => item.Language)
                .ToListAsync(cancellationToken).ConfigureAwait(false))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var saved = new List<ProjectCompilerInstallation>();
            foreach (var candidate in candidates)
            {
                var existing = await db.ProjectCompilerInstallations.SingleOrDefaultAsync(item => item.ExecutablePath == candidate.Path, cancellationToken).ConfigureAwait(false);
                if (existing is null)
                {
                    existing = new ProjectCompilerInstallation
                    {
                        Name = candidate.Name,
                        Language = candidate.Language,
                        ExecutablePath = candidate.Path,
                        CompilerHomePath = Path.GetDirectoryName(candidate.Path) ?? string.Empty,
                        DiscoverySource = candidate.Source,
                        ValidationArguments = DefaultValidationArguments(candidate.Language, candidate.Path),
                        IsEnabled = true,
                        IsDefaultForLanguage = defaultLanguages.Add(candidate.Language)
                    };
                    db.ProjectCompilerInstallations.Add(existing);
                }
                saved.Add(existing);
            }
            if (request.SaveDiscovered)
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Discovered {CompilerCount} compiler executable candidate(s).", saved.Count);
            return saved;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogInformation(exception, "Compiler discovery was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Compiler discovery failed; search paths were omitted from logs.");
            throw;
        }
    }

    /// <summary>Executes the bounded validation command for one stored compiler profile.</summary>
    /// <param name="compilerId">Stored compiler identifier.</param>
    /// <param name="userConfirmed">Whether the user approved native process execution.</param>
    /// <param name="cancellationToken">Cancels process execution and persistence.</param>
    /// <returns>A task that returns the updated validation profile.</returns>
    public async Task<ProjectCompilerInstallation> ValidateCompilerInstallationAsync(Guid compilerId, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        RequireConfirmation(userConfirmed, "executing a compiler version probe");
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var compiler = await db.ProjectCompilerInstallations.SingleOrDefaultAsync(item => item.Id == compilerId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Compiler installation {compilerId} was not found.");
            var result = await RunProcessAsync(compiler.ExecutablePath, compiler.ValidationArguments, compiler.CompilerHomePath, compiler.EnvironmentVariablesJson, 30, cancellationToken).ConfigureAwait(false);
            compiler.LastValidatedAtUtc = DateTime.UtcNow;
            compiler.LastValidationSucceeded = result.ExitCode == 0;
            compiler.LastValidationMessage = Trim(result.Output, 4000);
            if (compiler.LastValidationSucceeded && string.IsNullOrWhiteSpace(compiler.Version))
                compiler.Version = FirstNonEmptyLine(result.Output, 160);
            compiler.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Compiler installation {CompilerId} validation completed with success={Succeeded} and exit code {ExitCode}.", compiler.Id, compiler.LastValidationSucceeded, result.ExitCode);
            return compiler;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogInformation(exception, "Compiler installation {CompilerId} validation was cancelled.", compilerId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Compiler installation {CompilerId} validation failed; executable path and output were omitted from logs.", compilerId);
            throw;
        }
    }

    /// <summary>Deletes one unreferenced compiler installation after explicit user confirmation.</summary>
    /// <param name="compilerId">Stored compiler installation identifier.</param>
    /// <param name="userConfirmed">Whether the user approved the destructive database change.</param>
    /// <param name="cancellationToken">Cancellation token for database work.</param>
    /// <returns>A task whose result is true when the compiler record was removed.</returns>
    public async Task<bool> DeleteCompilerInstallationAsync(Guid compilerId, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        RequireConfirmation(userConfirmed, "deleting a compiler installation");
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var compiler = await db.ProjectCompilerInstallations.SingleOrDefaultAsync(item => item.Id == compilerId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Compiler installation {compilerId} was not found.");
            var workspaceReference = await db.ProjectWorkspaceRoots.AnyAsync(item => item.PreferredCompilerInstallationId == compilerId, cancellationToken).ConfigureAwait(false);
            var verificationReference = await db.ProjectBuildVerifications.AnyAsync(item => item.CompilerInstallationId == compilerId, cancellationToken).ConfigureAwait(false);
            if (workspaceReference || verificationReference)
                throw new InvalidOperationException("The compiler installation is still referenced by a workspace or build verification and cannot be deleted.");
            db.ProjectCompilerInstallations.Remove(compiler);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Deleted compiler installation {CompilerId}; executable path was omitted from logs.", compilerId);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Deleting compiler installation {CompilerId} failed; executable paths were omitted from logs.", compilerId);
            throw;
        }
    }

    public async Task<ProjectScanResult> ScanProjectFilesAsync(Guid projectId, ScanProjectFilesRequest request, CancellationToken cancellationToken = default)
    {
        RequireConfirmation(request.UserConfirmed, "scanning project files and storing path metadata");
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var project = await db.LocalGptProjects.SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Project {projectId} was not found.");
        LocalGptProjectRevision? revision = null;
        if (request.RevisionId is Guid revisionId)
            revision = await db.LocalGptProjectRevisions.SingleOrDefaultAsync(item => item.Id == revisionId && item.ProjectId == projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The selected revision does not belong to the project.");

        var root = NormalizeAbsolutePath(!string.IsNullOrWhiteSpace(revision?.SourceRootPath) ? revision.SourceRootPath : project.RootPath, nameof(project.RootPath));
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException("The stored project or revision root does not exist.");
        var workspace = await ResolveWorkspaceAsync(projectId, cancellationToken).ConfigureAwait(false);
        var include = CompileRegex(project.FileIncludePattern, nameof(project.FileIncludePattern), @"(?s).*");
        var exclude = CompileRegex(project.FileExcludePattern, nameof(project.FileExcludePattern), @"(?!)");
        var solutionRegex = CompileRegex(project.SolutionSearchPattern, nameof(project.SolutionSearchPattern), @"(?i)\.(sln|slnx)$");
        var maximum = Math.Clamp(request.MaximumFiles, 1, 100000);
        var maxBytes = Math.Clamp(request.MaximumFileBytes, 1024, 4L * 1024 * 1024 * 1024);
        var now = DateTime.UtcNow;
        var warnings = new List<string>();
        var existing = await db.LocalGptProjectTrackedFiles
            .Where(item => item.ProjectId == projectId && item.RevisionId == request.RevisionId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var item in existing) item.Exists = false;
        var byPath = existing.ToDictionary(item => item.ProjectRelativePath, StringComparer.OrdinalIgnoreCase);

        var configuredSolution = !string.IsNullOrWhiteSpace(revision?.SolutionPath) ? revision.SolutionPath : project.SolutionPath;
        var explicitSolution = NormalizeOptionalPath(configuredSolution);
        var solutionPath = File.Exists(explicitSolution) && IsPathInside(root, explicitSolution) ? explicitSolution : string.Empty;
        var filesSeen = 0;
        var filesStored = 0;
        var skipped = 0;
        foreach (var path in EnumerateFilesSafe(root, warnings))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (++filesSeen > maximum) { warnings.Add($"Stopped after {maximum} files."); break; }
            var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            if (exclude.IsMatch(relative) || !include.IsMatch(relative)) { skipped++; continue; }
            var info = new FileInfo(path);
            if (info.Length > maxBytes) { warnings.Add($"Skipped {relative}: file exceeds the approved {maxBytes:n0}-byte scan limit."); skipped++; continue; }
            if (string.IsNullOrWhiteSpace(solutionPath) && solutionRegex.IsMatch(relative)) solutionPath = path;
            var hash = await HashFileAsync(path, cancellationToken).ConfigureAwait(false);
            var revisionIdentity = request.RevisionId?.ToString("N") ?? "base";
            var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(projectId.ToString("N") + "|" + revisionIdentity + "|" + relative.ToLowerInvariant())));
            var patterns = DefaultPatternsFor(info.Extension);
            if (!byPath.TryGetValue(relative, out var tracked))
            {
                tracked = new LocalGptProjectTrackedFile { ProjectId = projectId, ProjectRelativePath = relative };
                db.LocalGptProjectTrackedFiles.Add(tracked);
                byPath[relative] = tracked;
            }
            tracked.RevisionId = request.RevisionId;
            tracked.StableFileKey = key;
            tracked.AbsolutePath = Path.GetFullPath(path);
            tracked.WorkspaceRelativePath = workspace.Exists && IsPathInside(workspace.RootPath, path)
                ? Path.GetRelativePath(workspace.RootPath, path).Replace('\\', '/')
                : relative;
            tracked.SolutionPath = solutionPath;
            tracked.ProjectFilePath = FindNearestProjectFile(root, path);
            tracked.FileName = info.Name;
            tracked.Extension = info.Extension;
            tracked.ContentType = ContentTypeFor(info.Extension);
            tracked.EncodingName = IsTextExtension(info.Extension) ? "utf-8-or-detected-at-read" : "binary";
            tracked.FileRole = patterns.Role;
            if (string.IsNullOrWhiteSpace(tracked.StructureRegex)) tracked.StructureRegex = patterns.Structure;
            if (string.IsNullOrWhiteSpace(tracked.ContentFormatRegex)) tracked.ContentFormatRegex = patterns.Content;
            tracked.ContentHash = hash;
            tracked.SizeBytes = info.Length;
            tracked.LastWriteTimeUtc = info.LastWriteTimeUtc;
            tracked.LastSeenAtUtc = now;
            tracked.Exists = true;
            tracked.IsGenerated = IsGeneratedPath(relative);
            tracked.IsUserApproved = true;
            filesStored++;
        }

        if (revision is not null)
        {
            revision.SourceRootPath = root;
            revision.SolutionPath = solutionPath;
            revision.ReadyForTesting = false;
            revision.UpdatedAtUtc = now;
        }
        else
        {
            project.SolutionPath = solutionPath;
            project.UpdatedAtUtc = now;
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Scanned project {ProjectId} revision {RevisionId}: {FilesSeen} seen, {FilesStored} stored, {FilesSkipped} skipped; paths omitted from logs.", projectId, request.RevisionId, filesSeen, filesStored, skipped);
        return new ProjectScanResult(projectId, request.RevisionId, root, solutionPath, filesSeen, filesStored, skipped, warnings);
    }

    public async Task<IReadOnlyList<LocalGptProjectTrackedFile>> GetTrackedFilesAsync(Guid projectId, Guid? revisionId = null, CancellationToken cancellationToken = default)
    {
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = db.LocalGptProjectTrackedFiles.AsNoTracking()
            .Where(item => item.ProjectId == projectId && item.RevisionId == revisionId);
        return await query.OrderBy(item => item.ProjectRelativePath).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<LocalGptProjectTrackedFile> SaveTrackedFilePatternAsync(Guid trackedFileId, SaveTrackedFilePatternRequest request, CancellationToken cancellationToken = default)
    {
        RequireConfirmation(request.UserConfirmed, "saving file content and structure regex fields");
        ValidateRegex(request.StructureRegex, nameof(request.StructureRegex), allowEmpty: true);
        ValidateRegex(request.ContentFormatRegex, nameof(request.ContentFormatRegex), allowEmpty: true);
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var item = await db.LocalGptProjectTrackedFiles.SingleOrDefaultAsync(entry => entry.Id == trackedFileId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Tracked file {trackedFileId} was not found.");
        item.StructureRegex = Trim(request.StructureRegex, 16000);
        item.ContentFormatRegex = Trim(request.ContentFormatRegex, 16000);
        item.FileRole = TrimOrFallback(request.FileRole, 120, "Source");
        item.IsUserApproved = true;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Saved regex metadata for tracked file {TrackedFileId}; path and regex content omitted from logs.", trackedFileId);
        return item;
    }

    public async Task<LocalGptProjectRevision> RegisterRevisionWorkspaceAsync(Guid projectId, Guid revisionId, string sourceRootPath, string solutionPath, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        RequireConfirmation(userConfirmed, "registering the generated revision workspace");
        var root = NormalizeAbsolutePath(sourceRootPath, nameof(sourceRootPath));
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException("The generated revision workspace does not exist.");
        var normalizedSolution = NormalizeOptionalPath(solutionPath);
        if (!string.IsNullOrWhiteSpace(normalizedSolution) && (!File.Exists(normalizedSolution) || !IsPathInside(root, normalizedSolution)))
            throw new ArgumentException("The revision solution path must exist inside the revision workspace.", nameof(solutionPath));
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var revision = await db.LocalGptProjectRevisions.SingleOrDefaultAsync(item => item.Id == revisionId && item.ProjectId == projectId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("The project revision was not found.");
        revision.SourceRootPath = root;
        revision.SolutionPath = normalizedSolution;
        revision.CompileVerified = false;
        revision.CouncilVerified = false;
        revision.ReadyForTesting = false;
        revision.SourceSnapshotHash = string.Empty;
        revision.SnapshotArchivePath = string.Empty;
        revision.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Registered generated workspace for project {ProjectId} revision {RevisionId}; paths omitted from logs.", projectId, revisionId);
        return revision;
    }

    public async Task<ProjectBuildVerification> RunBuildVerificationAsync(Guid projectId, RunProjectBuildVerificationRequest request, CancellationToken cancellationToken = default)
    {
        RequireConfirmation(request.UserConfirmed, "executing the selected compiler against the project revision");
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var project = await db.LocalGptProjects.SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Project {projectId} was not found.");
        var revision = await db.LocalGptProjectRevisions.SingleOrDefaultAsync(item => item.Id == request.RevisionId && item.ProjectId == projectId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("The project revision was not found.");
        var workspaceCandidates = await db.ProjectWorkspaceRoots.Where(item => item.IsEnabled && (item.ProjectId == null || item.ProjectId == projectId)).OrderBy(item => item.Priority).ToListAsync(cancellationToken).ConfigureAwait(false);
        var workspace = workspaceCandidates.FirstOrDefault(item => item.ScopeKind == "Project" && item.ProjectId == projectId)
            ?? workspaceCandidates.FirstOrDefault(item => item.ScopeKind == "ProjectType" && RegexMatches(item.ProjectTypePattern, project.ProjectType))
            ?? workspaceCandidates.FirstOrDefault(item => item.ScopeKind == "Global" && item.IsDefault)
            ?? workspaceCandidates.FirstOrDefault(item => item.ScopeKind == "Global");
        if (workspace is not null)
        {
            if (workspace.LastPermissionCheckedAtUtc is null)
                throw new InvalidOperationException("Assess the selected workspace permissions before running a compiler in it.");
            if (string.Equals(workspace.LastPermissionStatus, "Danger", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The selected workspace permission assessment contains danger findings. Correct them before build execution.");
            if (!workspace.LastPermissionReadAccess || !workspace.LastPermissionWriteAccess)
                throw new InvalidOperationException("The selected workspace has not proven the read and write access required for compiler execution. Run the rights assessment with the bounded write probe first.");
        }

        var compilerId = request.CompilerInstallationId != Guid.Empty ? request.CompilerInstallationId : workspace?.PreferredCompilerInstallationId ?? Guid.Empty;
        var compiler = await db.ProjectCompilerInstallations.SingleOrDefaultAsync(item => item.Id == compilerId && item.IsEnabled, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("The selected or workspace-assigned compiler installation was not found or is disabled.");
        if (!compiler.LastValidationSucceeded)
            throw new InvalidOperationException("Validate the selected compiler installation successfully before using it for a revision build.");

        var root = NormalizeAbsolutePath(!string.IsNullOrWhiteSpace(revision.SourceRootPath) ? revision.SourceRootPath : project.RootPath, nameof(project.RootPath));
        var configuredSolution = !string.IsNullOrWhiteSpace(revision.SolutionPath) ? revision.SolutionPath : project.SolutionPath;
        var target = File.Exists(configuredSolution) && IsPathInside(root, configuredSolution) ? configuredSolution : root;
        var trackedFiles = await db.LocalGptProjectTrackedFiles.AsNoTracking()
            .Where(item => item.ProjectId == projectId && item.RevisionId == revision.Id && item.Exists && item.IsUserApproved && !item.IsGenerated)
            .OrderBy(item => item.ProjectRelativePath)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (trackedFiles.Count == 0)
            throw new InvalidOperationException("Scan the selected revision before running its build verification.");
        var beforeState = await CaptureTrackedSourceStateAsync(trackedFiles, requireStoredHashMatch: true, cancellationToken).ConfigureAwait(false);

        var arguments = !string.IsNullOrWhiteSpace(request.Arguments)
            ? request.Arguments.Trim()
            : !string.IsNullOrWhiteSpace(workspace?.BuildArguments)
                ? workspace.BuildArguments.Trim()
                : DefaultBuildArguments(compiler.Language, target, request.Configuration);
        var executionEnvironmentJson = MergeEnvironmentJson(compiler.EnvironmentVariablesJson, workspace?.EnvironmentVariablesJson);
        var timeout = Math.Clamp(request.TimeoutSeconds, 10, 7200);
        var outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocalGPT", "BuildVerifications", projectId.ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        var verification = new ProjectBuildVerification
        {
            ProjectId = projectId,
            RevisionId = revision.Id,
            CompilerInstallationId = compiler.Id,
            Configuration = TrimOrFallback(request.Configuration, 80, "Debug"),
            ExecutablePath = compiler.ExecutablePath,
            Arguments = arguments,
            WorkingDirectory = root,
            StartedAtUtc = DateTime.UtcNow,
            SourceSnapshotHash = beforeState.Hash
        };
        verification.OutputLogPath = Path.Combine(outputDirectory, verification.Id.ToString("N") + ".log");
        verification.EvidenceManifestPath = Path.Combine(outputDirectory, verification.Id.ToString("N") + ".manifest.json");
        db.ProjectBuildVerifications.Add(verification);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var build = await RunProcessAsync(compiler.ExecutablePath, arguments, root, executionEnvironmentJson, timeout, cancellationToken).ConfigureAwait(false);
        var testsExecuted = build.ExitCode == 0 && !string.IsNullOrWhiteSpace(request.TestArguments);
        var testsExitCode = 0;
        var combined = new StringBuilder().AppendLine("BUILD").AppendLine(build.Output);
        if (testsExecuted)
        {
            var tests = await RunProcessAsync(compiler.ExecutablePath, request.TestArguments.Trim(), root, executionEnvironmentJson, timeout, cancellationToken).ConfigureAwait(false);
            testsExitCode = tests.ExitCode;
            combined.AppendLine().AppendLine("TESTS").AppendLine(tests.Output);
        }
        var afterState = await CaptureTrackedSourceStateAsync(trackedFiles, requireStoredHashMatch: false, cancellationToken).ConfigureAwait(false);
        var sourceChanged = !string.Equals(beforeState.Hash, afterState.Hash, StringComparison.Ordinal);
        var output = Limit(combined.ToString(), MaxCapturedCharacters);
        await File.WriteAllTextAsync(verification.OutputLogPath, output, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        var evidence = JsonSerializer.Serialize(new
        {
            verification.Id,
            ProjectId = projectId,
            RevisionId = revision.Id,
            CompilerId = compiler.Id,
            Compiler = new { compiler.Name, compiler.Language, compiler.Version, compiler.Architecture, compiler.ExecutablePath, compiler.CompilerHomePath },
            WorkingDirectory = root,
            Workspace = workspace is null ? null : new { workspace.Id, workspace.Name, workspace.EnvironmentKind, workspace.EnvironmentRootPath, workspace.LastPermissionStatus },
            Target = target,
            BuildArguments = arguments,
            TestArguments = testsExecuted ? request.TestArguments.Trim() : string.Empty,
            BuildExitCode = build.ExitCode,
            TestsExecuted = testsExecuted,
            TestsExitCode = testsExecuted ? testsExitCode : (int?)null,
            SourceHashBefore = beforeState.Hash,
            SourceHashAfter = afterState.Hash,
            SourceChangedDuringVerification = sourceChanged,
            Files = beforeState.Entries
        }, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(verification.EvidenceManifestPath, evidence, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);

        verification.CompletedAtUtc = DateTime.UtcNow;
        verification.ExitCode = build.ExitCode;
        verification.SourceChangedDuringVerification = sourceChanged;
        verification.BuildSucceeded = build.ExitCode == 0 && !sourceChanged;
        verification.TestsExecuted = testsExecuted;
        verification.TestsSucceeded = testsExecuted && testsExitCode == 0 && !sourceChanged;
        verification.OutputHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output)));
        verification.Summary = sourceChanged
            ? "Source files changed during build or test execution; rescan and repeat verification."
            : verification.BuildSucceeded && (!testsExecuted || verification.TestsSucceeded)
                ? (testsExecuted ? "Build and requested tests succeeded for the unchanged source state." : "Build succeeded for the unchanged source state; no tests were requested.")
                : "Build or requested tests failed; review the local evidence and log.";
        revision.CompileVerified = verification.BuildSucceeded && (!testsExecuted || verification.TestsSucceeded);
        revision.CouncilVerified = false;
        revision.ReadyForTesting = false;
        revision.SourceSnapshotHash = beforeState.Hash;
        revision.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Build verification {VerificationId} for project {ProjectId} completed: build={BuildSucceeded}, testsExecuted={TestsExecuted}, tests={TestsSucceeded}, sourceChanged={SourceChanged}, exit={ExitCode}.", verification.Id, projectId, verification.BuildSucceeded, verification.TestsExecuted, verification.TestsSucceeded, verification.SourceChangedDuringVerification, verification.ExitCode);
        return verification;
    }

    public async Task<ProjectBuildVerification> RecordCouncilBuildReviewAsync(Guid verificationId, RecordCouncilBuildReviewRequest request, CancellationToken cancellationToken = default)
    {
        RequireConfirmation(request.UserConfirmed, "recording the council build review");
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var verification = await db.ProjectBuildVerifications.Include(item => item.Revision).SingleOrDefaultAsync(item => item.Id == verificationId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Build verification {verificationId} was not found.");
        verification.CouncilReviewSucceeded = request.CompileErrorsAbsent && verification.BuildSucceeded && !verification.SourceChangedDuringVerification && (!verification.TestsExecuted || verification.TestsSucceeded);
        verification.CouncilReviewSummary = Trim(request.Summary, 16000);
        if (verification.Revision is not null)
        {
            verification.Revision.CouncilVerified = verification.CouncilReviewSucceeded;
            verification.Revision.UpdatedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Council review recorded for build verification {VerificationId}: success={Succeeded}.", verificationId, verification.CouncilReviewSucceeded);
        return verification;
    }

    public async Task<ProjectBuildVerification> ApproveRevisionReadyForTestAsync(Guid projectId, Guid revisionId, ApproveRevisionReadyForTestRequest request, CancellationToken cancellationToken = default)
    {
        RequireConfirmation(request.UserConfirmed, "approving a revision as ready for testing");
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var project = await db.LocalGptProjects.SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Project {projectId} was not found.");
        var revision = await db.LocalGptProjectRevisions.SingleOrDefaultAsync(item => item.Id == revisionId && item.ProjectId == projectId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("The project revision was not found.");
        var verification = await db.ProjectBuildVerifications.SingleOrDefaultAsync(item => item.Id == request.VerificationId && item.RevisionId == revisionId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("The selected verification does not belong to the revision.");
        if (!verification.BuildSucceeded || verification.SourceChangedDuringVerification) throw new InvalidOperationException("The revision cannot be approved before a successful build of an unchanged source state.");
        if (request.RequireTests && (!verification.TestsExecuted || !verification.TestsSucceeded)) throw new InvalidOperationException("The revision cannot be approved before the requested tests were executed successfully.");
        if (!verification.CouncilReviewSucceeded) throw new InvalidOperationException("The revision cannot be approved before the council records a compile-error-free review.");

        var files = await db.LocalGptProjectTrackedFiles.AsNoTracking()
            .Where(item => item.ProjectId == projectId && item.RevisionId == revisionId && item.Exists && item.IsUserApproved && !item.IsGenerated)
            .OrderBy(item => item.ProjectRelativePath)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (files.Count == 0) throw new InvalidOperationException("Scan and approve the project files before approving a ready-for-test revision.");
        var currentState = await CaptureTrackedSourceStateAsync(files, requireStoredHashMatch: true, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(currentState.Hash, verification.SourceSnapshotHash, StringComparison.Ordinal))
            throw new InvalidOperationException("The project files changed after the successful build verification. Rescan and repeat build, tests, and council review.");

        if (request.CreateLosslessSnapshot)
        {
            var workspace = await ResolveWorkspaceAsync(projectId, cancellationToken).ConfigureAwait(false);
            Directory.CreateDirectory(workspace.RootPath);
            var directory = Path.Combine(workspace.RootPath, "LocalGPT-Revisions", SafeFileName(project.Name), revision.Id.ToString("N"));
            Directory.CreateDirectory(directory);
            var archivePath = Path.Combine(directory, "source-snapshot.zip");
            if (File.Exists(archivePath)) File.Delete(archivePath);
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!File.Exists(file.AbsolutePath)) throw new FileNotFoundException("A tracked source file disappeared before snapshot creation.", file.AbsolutePath);
                    archive.CreateEntryFromFile(file.AbsolutePath, file.ProjectRelativePath.Replace('\\', '/'), CompressionLevel.Optimal);
                }
                var entry = archive.CreateEntry(".localgpt-manifest.json", CompressionLevel.Optimal);
                await using var stream = entry.Open();
                await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                await writer.WriteAsync(currentState.ManifestJson.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
            verification.SnapshotArchivePath = archivePath;
            revision.SnapshotArchivePath = archivePath;
        }
        verification.SourceSnapshotHash = currentState.Hash;
        verification.UserApprovedReadyForTest = true;
        revision.SourceSnapshotHash = currentState.Hash;
        revision.CompileVerified = true;
        revision.CouncilVerified = true;
        revision.ReadyForTesting = true;
        revision.ApprovedForTestingAtUtc = DateTime.UtcNow;
        revision.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Revision {RevisionId} for project {ProjectId} was approved as ready for testing using verification {VerificationId} and source hash prefix {SourceHashPrefix}.", revisionId, projectId, verification.Id, currentState.Hash[..Math.Min(12, currentState.Hash.Length)]);
        return verification;
    }

    private IEnumerable<(string Name, string Language, string Path, string Source)> DiscoverCompilerCandidates(IEnumerable<string>? customRoots, CancellationToken cancellationToken)
    {
        var approvedCustomRoots = (customRoots ?? [])
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .ToArray();
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var specs = new[]
        {
            (".NET SDK", "DotNet", OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"),
            ("MSBuild", "DotNet", OperatingSystem.IsWindows() ? "MSBuild.exe" : "msbuild"),
            ("Java compiler", "Java", OperatingSystem.IsWindows() ? "javac.exe" : "javac"),
            ("Java runtime", "Java", OperatingSystem.IsWindows() ? "java.exe" : "java"),
            ("Python", "Python", OperatingSystem.IsWindows() ? "python.exe" : "python3"),
            ("Python launcher", "Python", OperatingSystem.IsWindows() ? "py.exe" : "python"),
            ("PowerShell", "PowerShell", OperatingSystem.IsWindows() ? "pwsh.exe" : "pwsh"),
            ("Windows PowerShell", "PowerShell", "powershell.exe"),
            ("MSVC C++", "Cpp", "cl.exe"),
            ("GNU C++", "Cpp", OperatingSystem.IsWindows() ? "g++.exe" : "g++"),
            ("Clang C++", "Cpp", OperatingSystem.IsWindows() ? "clang++.exe" : "clang++"),
            ("PlatformIO Core", "Embedded", OperatingSystem.IsWindows() ? "platformio.exe" : "platformio"),
            ("Arduino CLI", "Embedded", OperatingSystem.IsWindows() ? "arduino-cli.exe" : "arduino-cli")
        };
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        foreach (var spec in specs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidate = Path.Combine(dir, spec.Item3);
            if (File.Exists(candidate) && found.Add(Path.GetFullPath(candidate))) yield return (spec.Item1, spec.Item2, Path.GetFullPath(candidate), "PATH");
        }
        var knownRoots = new List<string>();
        if (OperatingSystem.IsWindows())
        {
            knownRoots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet"));
            knownRoots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "dotnet"));
            knownRoots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Java"));
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            knownRoots.Add(Path.Combine(localAppData, "Programs", "Python"));
            knownRoots.Add(Path.Combine(userProfile, ".platformio", "penv", "Scripts"));
            knownRoots.Add(Path.Combine(localAppData, "Programs", "Arduino IDE", "resources", "app", "lib", "backend", "resources"));
            knownRoots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Arduino IDE", "resources", "app", "lib", "backend", "resources"));
            knownRoots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell"));
            knownRoots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio"));
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            knownRoots.AddRange(new[]
            {
                "/usr/share/dotnet", "/usr/local/share/dotnet", "/opt/dotnet", Path.Combine(home, ".dotnet"),
                "/usr/lib/jvm", "/usr/java", "/opt/java", "/opt/jdk",
                "/usr/bin", "/usr/local/bin", "/opt/homebrew/bin", "/opt/homebrew/opt",
                "/usr/local/microsoft/powershell", "/opt/microsoft/powershell"
            });
        }
        foreach (var customRoot in approvedCustomRoots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { knownRoots.Add(Path.GetFullPath(Environment.ExpandEnvironmentVariables(customRoot.Trim()))); }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                logger.LogDebug(ex, "Ignored invalid custom compiler search root; path content was omitted.");
            }
        }
        foreach (var root in knownRoots.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        foreach (var file in EnumerateCompilerFiles(root, specs.Select(spec => spec.Item3).ToHashSet(StringComparer.OrdinalIgnoreCase), 7, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!found.Add(file)) continue;
            var spec = specs.First(item => string.Equals(item.Item3, Path.GetFileName(file), StringComparison.OrdinalIgnoreCase));
            var isCustomRoot = approvedCustomRoots
                .Select(item =>
                {
                    try { return Path.GetFullPath(item); }
                    catch { return string.Empty; }
                })
                .Any(item => string.Equals(item, root, StringComparison.OrdinalIgnoreCase));
            yield return (spec.Item1, spec.Item2, file, isCustomRoot ? "CustomRoot" : "CommonPath");
        }
    }

    private IEnumerable<string> EnumerateCompilerFiles(string root, HashSet<string> names, int maxDepth, CancellationToken cancellationToken)
    {
        var pending = new Queue<(string Path, int Depth)>();
        pending.Enqueue((root, 0));
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = pending.Dequeue();
            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(current.Path); } catch { continue; }
            foreach (var file in files) if (names.Contains(Path.GetFileName(file))) yield return Path.GetFullPath(file);
            if (current.Depth >= maxDepth) continue;
            IEnumerable<string> dirs;
            try { dirs = Directory.EnumerateDirectories(current.Path); } catch { continue; }
            foreach (var dir in dirs) pending.Enqueue((dir, current.Depth + 1));
        }
    }

    private IEnumerable<string> EnumerateFilesSafe(string root, ICollection<string> warnings)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(current); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { warnings.Add($"Could not read {current}: {ex.Message}"); continue; }
            foreach (var file in files) yield return file;
            IEnumerable<string> dirs;
            try { dirs = Directory.EnumerateDirectories(current); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { warnings.Add($"Could not enumerate {current}: {ex.Message}"); continue; }
            foreach (var dir in dirs) pending.Push(dir);
        }
    }

    private async Task<(int ExitCode, string Output)> RunProcessAsync(string executable, string arguments, string? workingDirectory, string? environmentVariablesJson, int timeoutSeconds, CancellationToken cancellationToken)
    {
        if (!File.Exists(executable)) throw new FileNotFoundException("The configured compiler executable does not exist.", executable);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory! : Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        if (!string.IsNullOrWhiteSpace(environmentVariablesJson))
        {
            try
            {
                var environment = JsonSerializer.Deserialize<Dictionary<string, string>>(environmentVariablesJson) ?? [];
                foreach (var pair in environment)
                    process.StartInfo.Environment[pair.Key] = pair.Value;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("The compiler environment JSON is invalid.", ex);
            }
        }
        if (!process.Start()) throw new InvalidOperationException("The compiler process could not be started.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);
        try { await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false); }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
            throw;
        }
        var output = Limit((await stdoutTask.ConfigureAwait(false)) + Environment.NewLine + (await stderrTask.ConfigureAwait(false)), MaxCapturedCharacters);
        return (process.ExitCode, output);
    }

    private string DefaultBuildArguments(string language, string target, string configuration) => language.ToLowerInvariant() switch
    {
        "dotnet" => $"build \"{target}\" --configuration \"{configuration}\" --nologo",
        "java" => $"\"{target}\"",
        "python" => $"-m compileall \"{target}\"",
        "powershell" => $"-NoProfile -NonInteractive -Command \"Get-ChildItem -LiteralPath '{target.Replace("'", "''")}' -Filter *.ps1 -Recurse | ForEach-Object {{ [void][scriptblock]::Create((Get-Content -Raw -LiteralPath $_.FullName)) }}\"",
        _ => throw new InvalidOperationException("No safe default build arguments exist for this compiler. Enter explicit reviewed arguments.")
    };

    private string DefaultValidationArguments(string language, string executable) => language.ToLowerInvariant() switch
    {
        "powershell" when Path.GetFileName(executable).StartsWith("powershell", StringComparison.OrdinalIgnoreCase) => "-NoProfile -NonInteractive -Command \"$PSVersionTable.PSVersion.ToString()\"",
        "powershell" => "-NoProfile -NonInteractive -Command \"$PSVersionTable.PSVersion.ToString()\"",
        "java" => "-version",
        "embedded" when Path.GetFileName(executable).StartsWith("arduino-cli", StringComparison.OrdinalIgnoreCase) => "version",
        "embedded" => "--version",
        "cpp" when Path.GetFileName(executable).StartsWith("cl", StringComparison.OrdinalIgnoreCase) => "",
        _ => "--version"
    };

    private (string Role, string Structure, string Content) DefaultPatternsFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".cs" => ("CSharpSource", @"(?m)^\s*(?:public|internal|private|protected)?\s*(?:sealed\s+|abstract\s+|static\s+|partial\s+)*(?:class|record|interface|enum|struct)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", @"(?s).*"),
        ".razor" => ("RazorComponent", @"(?m)^\s*@(?:page|code|functions|inject|using)\b|<(?<component>[A-Z][A-Za-z0-9.]*)\b", @"(?s).*"),
        ".csproj" or ".props" or ".targets" => ("MSBuild", @"<(?<element>Project|PropertyGroup|ItemGroup|Target|PackageReference|ProjectReference)\b", @"(?s)^\s*<Project\b.*</Project>\s*$"),
        ".sln" or ".slnx" => ("Solution", @"(?m)^(?:Project\(|\s*<Project\b)", @"(?s).*"),
        ".json" => ("Json", "\"(?<property>[^\"]+)\"\\s*:", @"(?s)^\s*[\[{].*[\]}]\s*$"),
        ".ps1" => ("PowerShell", @"(?mi)^\s*(?:function|class|enum)\s+(?<name>[A-Za-z_][A-Za-z0-9_-]*)", @"(?s).*"),
        ".java" => ("JavaSource", @"(?m)^\s*(?:public|protected|private)?\s*(?:abstract\s+|final\s+)?(?:class|interface|enum|record)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", @"(?s).*"),
        ".py" => ("PythonSource", @"(?m)^\s*(?:async\s+)?(?:def|class)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", @"(?s).*"),
        ".ino" or ".pde" => ("ArduinoSketch", @"(?m)^\s*(?:void\s+(?<entry>setup|loop)\s*\(|#define\s+(?<define>[A-Za-z_][A-Za-z0-9_]*)|(?:class|struct|enum)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*))", @"(?s).*"),
        ".cpp" or ".cc" or ".cxx" or ".c" or ".h" or ".hpp" => ("CppSource", @"(?m)^\s*(?:class|struct|enum|namespace)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", @"(?s).*"),
        ".ini" or ".toml" or ".cfg" or ".conf" => ("ToolchainConfiguration", @"(?m)^\s*(?:\[(?<section>[^]]+)\]|(?<key>[A-Za-z_][A-Za-z0-9_.-]*)\s*=)", @"(?s).*"),
        ".cmake" or ".kconfig" or ".sdkconfig" => ("EmbeddedBuildConfiguration", @"(?mi)^\s*(?<directive>project|set|option|config|menuconfig|source|include)\b", @"(?s).*"),
        _ => ("Document", string.Empty, @"(?s).*")
    };

    private string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".json" => "application/json", ".xml" or ".csproj" or ".props" or ".targets" or ".slnx" => "application/xml",
        ".md" => "text/markdown", ".yml" or ".yaml" => "application/yaml", _ => IsTextExtension(extension) ? "text/plain" : "application/octet-stream"
    };
    private bool IsTextExtension(string extension) => new[] { ".cs", ".razor", ".csproj", ".sln", ".slnx", ".json", ".xml", ".props", ".targets", ".ps1", ".cmd", ".md", ".yml", ".yaml", ".java", ".py", ".ino", ".pde", ".cpp", ".cc", ".cxx", ".c", ".h", ".hpp", ".ini", ".toml", ".cfg", ".conf", ".cmake", ".kconfig", ".sdkconfig", ".txt", ".css", ".js", ".ts", ".html" }.Contains(extension.ToLowerInvariant());
    private bool IsGeneratedPath(string relative) => Regex.IsMatch(relative, @"(?i)(^|/)(bin|obj|node_modules|artifacts|\.vs)(/|$)", RegexOptions.CultureInvariant, runtimePolicy.RegexTimeout);
    private string FindNearestProjectFile(string root, string file)
    {
        var directory = Path.GetDirectoryName(file);
        while (!string.IsNullOrWhiteSpace(directory) && directory.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var project = Directory.EnumerateFiles(directory, "*.*proj", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (project is not null) return project;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogDebug(ex, "Could not inspect project files in directory {DirectoryPath}.", directory);
                return string.Empty;
            }
            directory = Path.GetDirectoryName(directory);
        }
        return string.Empty;
    }
    private async Task<string> HashFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }
    private async Task<ProjectTrackedSourceState> CaptureTrackedSourceStateAsync(IReadOnlyList<LocalGptProjectTrackedFile> files, bool requireStoredHashMatch, CancellationToken cancellationToken)
    {
        var entries = new List<ProjectSourceManifestEntry>(files.Count);
        foreach (var file in files.OrderBy(item => item.ProjectRelativePath, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(file.AbsolutePath)) throw new FileNotFoundException("A tracked project file is missing. Rescan the project before continuing.", file.AbsolutePath);
            var hash = await HashFileAsync(file.AbsolutePath, cancellationToken).ConfigureAwait(false);
            if (requireStoredHashMatch && !string.Equals(hash, file.ContentHash, StringComparison.Ordinal))
                throw new InvalidOperationException($"Tracked file '{file.ProjectRelativePath}' changed after the last approved scan. Rescan before building or approving the revision.");
            var size = new FileInfo(file.AbsolutePath).Length;
            entries.Add(new ProjectSourceManifestEntry(file.ProjectRelativePath.Replace('\\', '/'), hash, size));
        }
        var canonical = string.Join("\n", entries.Select(item => item.RelativePath + "|" + item.ContentHash + "|" + item.SizeBytes));
        var hashValue = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        var manifestJson = JsonSerializer.Serialize(new { SourceHash = hashValue, Files = entries }, new JsonSerializerOptions { WriteIndented = true });
        return new ProjectTrackedSourceState(hashValue, manifestJson, entries);
    }

    private bool IsPathInside(string root, string path)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
        var normalizedPath = Path.GetFullPath(path);
        return normalizedPath.StartsWith(normalizedRoot, comparison) || string.Equals(Path.TrimEndingDirectorySeparator(normalizedPath), Path.TrimEndingDirectorySeparator(root), comparison);
    }

    private bool RegexMatches(string pattern, string input) => !string.IsNullOrWhiteSpace(pattern) && CompileRegex(pattern, nameof(pattern), @"(?!)").IsMatch(input ?? string.Empty);
    private Regex CompileRegex(string? pattern, string parameter, string fallback)
    {
        try { return new Regex(string.IsNullOrWhiteSpace(pattern) ? fallback : pattern, RegexOptions.CultureInvariant, runtimePolicy.RegexTimeout); }
        catch (ArgumentException ex) { throw new ArgumentException("The regular expression is invalid.", parameter, ex); }
    }
    private void ValidateRegex(string? pattern, string parameter, bool allowEmpty)
    {
        if (string.IsNullOrWhiteSpace(pattern)) { if (allowEmpty) return; throw new ArgumentException("A regular expression is required.", parameter); }
        _ = CompileRegex(pattern, parameter, pattern);
    }
    private void ValidateJsonArray(string? json, string parameter)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try { using var doc = JsonDocument.Parse(json); if (doc.RootElement.ValueKind != JsonValueKind.Array) throw new ArgumentException("A JSON array is required.", parameter); }
        catch (JsonException ex) { throw new ArgumentException("The JSON array is invalid.", parameter, ex); }
    }
    private void ValidateWorkspaceAccessPolicyJson(string? json)
    {
        ValidateJsonArray(json, nameof(json));
        foreach (var rule in ParseAccessPolicy(json))
        {
            ValidateRegex(rule.RelativePathRegex, nameof(rule.RelativePathRegex), allowEmpty: false);
            if (rule.ExpectedEntryKind != "File" && rule.ExpectedEntryKind != "Directory" && rule.ExpectedEntryKind != "Either")
                throw new ArgumentException("ExpectedEntryKind must be File, Directory, or Either.", nameof(json));
            if (rule.RequiredAccess != "Read" && rule.RequiredAccess != "ReadWrite" && rule.RequiredAccess != "Execute" && rule.RequiredAccess != "ReadWriteExecute")
                throw new ArgumentException("RequiredAccess is invalid.", nameof(json));
            if (rule.Severity != "Warning" && rule.Severity != "Danger")
                throw new ArgumentException("Severity must be Warning or Danger.", nameof(json));
        }
    }
    private List<string> ParseStringArray(string? json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(string.IsNullOrWhiteSpace(json) ? "[]" : json)?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(100).ToList() ?? []; }
        catch (JsonException) { return []; }
    }
    private List<WorkspaceAccessPolicyRule> ParseAccessPolicy(string? json)
    {
        try { return JsonSerializer.Deserialize<List<WorkspaceAccessPolicyRule>>(string.IsNullOrWhiteSpace(json) ? "[]" : json, new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true })?.Take(200).ToList() ?? []; }
        catch (JsonException) { return []; }
    }
    private List<string> EnumerateRelativeEntries(string root, int maximum, List<WorkspacePermissionFinding> findings)
    {
        var result = new List<string>();
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0 && result.Count < maximum)
        {
            var current = pending.Pop();
            try
            {
                foreach (var directory in Directory.EnumerateDirectories(current))
                {
                    result.Add(Path.GetRelativePath(root, directory).Replace('\\', '/') + "/");
                    if (result.Count >= maximum) break;
                    pending.Push(directory);
                }
                if (result.Count >= maximum) break;
                foreach (var file in Directory.EnumerateFiles(current))
                {
                    result.Add(Path.GetRelativePath(root, file).Replace('\\', '/'));
                    if (result.Count >= maximum) break;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                findings.Add(new("Warning", "ENUMERATION_PARTIAL", "Part of the workspace could not be inspected.", Path.GetRelativePath(root, current).Replace('\\', '/')));
            }
        }
        if (result.Count >= maximum)
            findings.Add(new("Warning", "ENTRY_LIMIT", $"Workspace assessment stopped after {maximum} entries."));
        return result;
    }
    private void EvaluateAccessPolicyRule(WorkspaceAccessPolicyRule rule, IReadOnlyList<string> entries, string root, bool rootWriteAccess, List<WorkspacePermissionFinding> findings)
    {
        var regex = CompileRegex(rule.RelativePathRegex, nameof(rule.RelativePathRegex), @"(?!)");
        var matches = entries.Where(entry => regex.IsMatch(entry)).Take(100).ToArray();
        if (rule.Required && matches.Length == 0)
        {
            findings.Add(new(rule.Severity, "POLICY_NO_MATCH", $"Required workspace policy '{Trim(rule.Name, 160)}' matched no file or directory."));
            return;
        }
        foreach (var relative in matches)
        {
            var isDirectory = relative.EndsWith("/", StringComparison.Ordinal);
            if ((rule.ExpectedEntryKind == "File" && isDirectory) || (rule.ExpectedEntryKind == "Directory" && !isDirectory))
                findings.Add(new(rule.Severity, "POLICY_KIND", $"Workspace policy '{Trim(rule.Name, 160)}' matched the wrong entry kind.", relative));
            var fullPath = Path.GetFullPath(Path.Combine(root, relative.TrimEnd('/').Replace('/', Path.DirectorySeparatorChar)));
            if (!IsPathInside(root, fullPath))
            {
                findings.Add(new("Danger", "POLICY_ESCAPE", "A workspace policy match escaped the configured root.", relative));
                continue;
            }
            if (rule.RequiredAccess.Contains("Read", StringComparison.OrdinalIgnoreCase) && !(isDirectory ? CanEnumerateDirectory(fullPath) : CanOpenRead(fullPath)))
                findings.Add(new(rule.Severity, "POLICY_READ_DENIED", $"Workspace policy '{Trim(rule.Name, 160)}' requires read access that is unavailable.", relative));
            if (rule.RequiredAccess.Contains("Write", StringComparison.OrdinalIgnoreCase) && !rootWriteAccess)
                findings.Add(new(rule.Severity, "POLICY_WRITE_UNPROVEN", $"Workspace policy '{Trim(rule.Name, 160)}' requires write access, but the bounded workspace write probe did not succeed.", relative));
            if (rule.RequiredAccess.Contains("Execute", StringComparison.OrdinalIgnoreCase) && isDirectory)
                findings.Add(new("Warning", "POLICY_EXECUTE_DIRECTORY", $"Execute access for directory policy '{Trim(rule.Name, 160)}' is not inferred; validate the assigned compiler/tool explicitly.", relative));
        }
    }
    private bool CanEnumerateDirectory(string path)
    {
        try { _ = Directory.EnumerateFileSystemEntries(path).Take(1).ToArray(); return true; } catch { return false; }
    }
    private bool CanOpenRead(string path)
    {
        try { using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete); return true; } catch { return false; }
    }
    private async Task<bool> ProbeDirectoryWriteAsync(string root, CancellationToken cancellationToken)
    {
        var probe = Path.Combine(root, $".localgpt-rights-probe-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(probe, "LocalGPT bounded workspace rights probe.", Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            File.Delete(probe);
            return true;
        }
        catch
        {
            try { if (File.Exists(probe)) File.Delete(probe); } catch { }
            return false;
        }
    }
    private bool IsBroadOrSystemRoot(string path)
    {
        var normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        var root = Path.TrimEndingDirectorySeparator(Path.GetPathRoot(normalized) ?? string.Empty);
        if (string.Equals(normalized, root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) return true;
        var protectedRoots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        }.Where(item => !string.IsNullOrWhiteSpace(item));
        return protectedRoots.Any(item => string.Equals(normalized, Path.TrimEndingDirectorySeparator(Path.GetFullPath(item)), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
    }
    private string NormalizeRelativePolicyPath(string value)
    {
        var normalized = (value ?? string.Empty).Trim().Replace('\\', '/').Trim('/');
        return normalized.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(normalized) ? string.Empty : normalized;
    }

    private void ValidateJsonObject(string? json, string parameter)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try { using var doc = JsonDocument.Parse(json); if (doc.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("A JSON object is required.", parameter); }
        catch (JsonException ex) { throw new ArgumentException("The JSON object is invalid.", parameter, ex); }
    }
    private string MergeEnvironmentJson(string? compilerJson, string? workspaceJson)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var json in new[] { compilerJson, workspaceJson })
        {
            if (string.IsNullOrWhiteSpace(json)) continue;
            try
            {
                foreach (var pair in JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [])
                    merged[pair.Key] = pair.Value;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("A compiler or workspace environment JSON object is invalid.", ex);
            }
        }
        return JsonSerializer.Serialize(merged);
    }

    private string NormalizeScope(string? scope) => (scope ?? string.Empty).Trim() switch { "Project" => "Project", "ProjectType" => "ProjectType", "Global" => "Global", _ => throw new ArgumentException("ScopeKind must be Project, ProjectType, or Global.", nameof(scope)) };
    private string NormalizeAbsolutePath(string? value, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A path is required.", parameter);
        try { return Path.GetFullPath(Environment.ExpandEnvironmentVariables(value.Trim())); }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { throw new ArgumentException("The path is invalid.", parameter, ex); }
    }
    private string NormalizeOptionalPath(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : NormalizeAbsolutePath(value, nameof(value));
    private void RequireConfirmation(bool confirmed, string operation) { if (!confirmed) throw new InvalidOperationException($"Fresh human confirmation is required before {operation}."); }
    private string RequireText(string? value, string parameter, int max) { var result = Trim(value, max); return string.IsNullOrWhiteSpace(result) ? throw new ArgumentException("A value is required.", parameter) : result; }
    private string TrimOrFallback(string? value, int max, string fallback) { var result = Trim(value, max); return string.IsNullOrWhiteSpace(result) ? fallback : result; }
    private string Trim(string? value, int max) { var result = value?.Trim() ?? string.Empty; return result.Length <= max ? result : result[..max]; }
    private string Limit(string value, int max) => value.Length <= max ? value : value[..max];
    private string FirstNonEmptyLine(string value, int max) => Trim(value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), max);
    private string SafeFileName(string value) => string.Concat(value.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
}

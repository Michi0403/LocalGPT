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

/// <summary>
/// Coordinates project maintenance behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed partial class ProjectMaintenanceService : IProjectMaintenanceService
    {
        /// <summary>
        /// Stores the database context factory dependency used by <see cref="ProjectMaintenanceService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory;
        /// <summary>
        /// Stores the database initialization service dependency used by <see cref="ProjectMaintenanceService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IDatabaseInitializationService databaseInitializer;
        /// <summary>
        /// Stores the local GPT runtime policy data service dependency used by <see cref="ProjectMaintenanceService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ILocalGptRuntimePolicyDataService runtimePolicy;
        /// <summary>
        /// Stores the regex compilation service dependency used by <see cref="ProjectMaintenanceService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IRegexCompilationService regexCompilation;
        /// <summary>
        /// Stores the logger used by <see cref="ProjectMaintenanceService"/> to record operational diagnostics without coupling callers to logging details.
        /// </summary>
        private readonly ILogger<ProjectMaintenanceService> logger;

        /// <summary>Initializes the type with its dependency-injected collaborators.</summary>
        /// <param name="dbContextFactory">Injected dependency used by the ProjectMaintenanceService.</param>
        /// <param name="databaseInitializer">Injected dependency used by the ProjectMaintenanceService.</param>
        /// <param name="runtimePolicy">Injected dependency used by the ProjectMaintenanceService.</param>
        /// <param name="regexCompilation">Injected bounded regular-expression compiler used by project policy evaluation.</param>
        /// <param name="logger">Injected dependency used by the ProjectMaintenanceService.</param>
        public ProjectMaintenanceService(
            IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
            IDatabaseInitializationService databaseInitializer,
            ILocalGptRuntimePolicyDataService runtimePolicy,
            IRegexCompilationService regexCompilation,
            ILogger<ProjectMaintenanceService> logger)
        {
            this.dbContextFactory = dbContextFactory;
            this.databaseInitializer = databaseInitializer;
            this.runtimePolicy = runtimePolicy;
            this.regexCompilation = regexCompilation;
            this.logger = logger;
        }

    /// <summary>
    /// Retrieves workspace roots as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<ProjectWorkspaceRoot>> GetWorkspaceRootsAsync(Guid? projectId = null, CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var query = db.ProjectWorkspaceRoots.AsNoTracking();
            if (projectId is Guid id)
                query = query.Where(item => item.ProjectId == null || item.ProjectId == id);
            return await query.OrderBy(item => item.Priority).ThenBy(item => item.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(GetWorkspaceRootsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(GetWorkspaceRootsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Persists workspace root as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project workspace root produced by the operation.</returns>
    public async Task<ProjectWorkspaceRoot> SaveWorkspaceRootAsync(SaveProjectWorkspaceRootRequest request, CancellationToken cancellationToken = default)
    {
    try
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
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(SaveWorkspaceRootAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(SaveWorkspaceRootAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves workspace as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project workspace resolution produced by the operation.</returns>
    public async Task<ProjectWorkspaceResolution> ResolveWorkspaceAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ResolveWorkspaceAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ResolveWorkspaceAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs assess workspace permissions as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="workspaceRootId">Identifier of the workspace root to use for this operation.</param>
    /// <param name="userConfirmedWriteProbe">Value indicating whether user confirmed write probe should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The workspace permission assessment produced by the operation.</returns>
    public async Task<WorkspacePermissionAssessment> AssessWorkspacePermissionsAsync(Guid workspaceRootId, bool userConfirmedWriteProbe, CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
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

                var maximumAssessmentEntries = Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.MaxFiles));
                var relativeEntries = EnumerateRelativeEntries(root, maximumAssessmentEntries, findings);
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(AssessWorkspacePermissionsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(AssessWorkspacePermissionsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves compiler installations as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<ProjectCompilerInstallation>> GetCompilerInstallationsAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            return await db.ProjectCompilerInstallations.AsNoTracking()
                .OrderBy(item => item.Language).ThenByDescending(item => item.IsDefaultForLanguage).ThenBy(item => item.Name)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(GetCompilerInstallationsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(GetCompilerInstallationsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Persists compiler installation as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project compiler installation produced by the operation.</returns>
    public async Task<ProjectCompilerInstallation> SaveCompilerInstallationAsync(SaveProjectCompilerInstallationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            RequireConfirmation(request.UserConfirmed, "saving a compiler installation");
            var executable = NormalizeAbsolutePath(request.ExecutablePath, nameof(request.ExecutablePath));
            ValidateJsonObject(request.EnvironmentVariablesJson, nameof(request.EnvironmentVariablesJson));
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);

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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(SaveCompilerInstallationAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(SaveCompilerInstallationAsync)} failed.");
        throw;
    }
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
            var searchRoots = NormalizeCompilerSearchRoots(request);
            var maximumCompilerCandidates = Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ProjectMaintenanceMaximumCompilerCandidates));
            var candidates = await Task.Run(
                () => DiscoverCompilerCandidates(searchRoots, cancellationToken).Take(maximumCompilerCandidates).ToList(),
                cancellationToken).ConfigureAwait(false);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
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
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
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

    /// <summary>
    /// Deletes compiler installation as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
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

    /// <summary>
    /// Performs scan project files as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project scan result produced by the operation.</returns>
    public async Task<ProjectScanResult> ScanProjectFilesAsync(Guid projectId, ScanProjectFilesRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            RequireConfirmation(request.UserConfirmed, "scanning project files and storing path metadata");
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
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
            var configuredMaximumFiles = Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.MaxFiles));
            var maximum = request.MaximumFiles > 0
                ? Math.Min(request.MaximumFiles, configuredMaximumFiles)
                : configuredMaximumFiles;
            var configuredMaximumFileBytes = Math.Max(1L, runtimePolicy.GetLong(LocalGptRuntimeValue.MaxSingleFileBytes));
            var maxBytes = request.MaximumFileBytes > 0
                ? Math.Min(request.MaximumFileBytes, configuredMaximumFileBytes)
                : configuredMaximumFileBytes;
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ScanProjectFilesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ScanProjectFilesAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves tracked files as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<LocalGptProjectTrackedFile>> GetTrackedFilesAsync(Guid projectId, Guid? revisionId = null, CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var query = db.LocalGptProjectTrackedFiles.AsNoTracking()
                .Where(item => item.ProjectId == projectId && item.RevisionId == revisionId);
            return await query.OrderBy(item => item.ProjectRelativePath).ToListAsync(cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(GetTrackedFilesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(GetTrackedFilesAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Persists tracked file pattern as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="trackedFileId">Identifier of the tracked file to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The LocalGPT project tracked file produced by the operation.</returns>
    public async Task<LocalGptProjectTrackedFile> SaveTrackedFilePatternAsync(Guid trackedFileId, SaveTrackedFilePatternRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            RequireConfirmation(request.UserConfirmed, "saving file content and structure regex fields");
            ValidateRegex(request.StructureRegex, nameof(request.StructureRegex), allowEmpty: true);
            ValidateRegex(request.ContentFormatRegex, nameof(request.ContentFormatRegex), allowEmpty: true);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(SaveTrackedFilePatternAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(SaveTrackedFilePatternAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Registers revision workspace as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="sourceRootPath">Source root path value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="solutionPath">Solution path value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The LocalGPT project revision produced by the operation.</returns>
    public async Task<LocalGptProjectRevision> RegisterRevisionWorkspaceAsync(Guid projectId, Guid revisionId, string sourceRootPath, string solutionPath, bool userConfirmed, CancellationToken cancellationToken = default)
    {
    try
    {
            RequireConfirmation(userConfirmed, "registering the generated revision workspace");
            var root = NormalizeAbsolutePath(sourceRootPath, nameof(sourceRootPath));
            if (!Directory.Exists(root)) throw new DirectoryNotFoundException("The generated revision workspace does not exist.");
            var normalizedSolution = NormalizeOptionalPath(solutionPath);
            if (!string.IsNullOrWhiteSpace(normalizedSolution) && (!File.Exists(normalizedSolution) || !IsPathInside(root, normalizedSolution)))
                throw new ArgumentException("The revision solution path must exist inside the revision workspace.", nameof(solutionPath));
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(RegisterRevisionWorkspaceAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(RegisterRevisionWorkspaceAsync)} failed.");
        throw;
    }
}
}

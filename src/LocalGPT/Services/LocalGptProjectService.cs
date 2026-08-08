using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

public sealed class LocalGptProjectService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<LocalGptProjectService> logger) : ILocalGptProjectService
{
    public async Task<IReadOnlyList<LocalGptProjectSummary>> GetProjectsAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var query = db.LocalGptProjects.AsNoTracking();
            if (!includeArchived)
                query = query.Where(project => !project.IsArchived);

            return await query
                .OrderBy(project => project.IsArchived)
                .ThenByDescending(project => project.UpdatedAtUtc)
                .Select(project => new LocalGptProjectSummary(
                    project.Id,
                    project.Name,
                    project.Purpose,
                    project.RootPath,
                    project.CurrentVersion,
                    project.Status,
                    project.RecommendGit,
                    project.IsArchived,
                    project.Topics.Count,
                    project.Versions.Count,
                    project.UpdatedAtUtc))
                .Take(500)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(GetProjectsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(GetProjectsAsync)} failed.");
        throw;
    }
}

    public async Task<LocalGptProjectDetails?> GetProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var project = await db.LocalGptProjects
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken)
                .ConfigureAwait(false);
            if (project is null)
                return null;

            var topics = await db.LocalGptProjectTopics
                .AsNoTracking()
                .Where(topic => topic.ProjectId == projectId)
                .OrderBy(topic => topic.Status)
                .ThenBy(topic => topic.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var versions = await db.LocalGptProjectVersions
                .AsNoTracking()
                .Where(version => version.ProjectId == projectId)
                .OrderByDescending(version => version.IsCurrent)
                .ThenByDescending(version => version.CreatedAtUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var revisions = await db.LocalGptProjectRevisions
                .AsNoTracking()
                .Where(item => item.ProjectId == projectId)
                .OrderByDescending(item => item.IsCurrent)
                .ThenByDescending(item => item.UpdatedAtUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var requirements = await db.LocalGptProjectRequirements
                .AsNoTracking()
                .Where(item => item.ProjectId == projectId)
                .Include(item => item.Links)
                .OrderBy(item => item.Status)
                .ThenBy(item => item.Priority)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var artifacts = await db.LocalGptProjectArtifacts
                .AsNoTracking()
                .Where(item => item.ProjectId == projectId)
                .OrderBy(item => item.ArtifactKind)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var workspaceRoots = await db.ProjectWorkspaceRoots.AsNoTracking()
                .Where(item => item.ProjectId == null || item.ProjectId == projectId)
                .OrderBy(item => item.Priority)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var trackedFiles = await db.LocalGptProjectTrackedFiles.AsNoTracking()
                .Where(item => item.ProjectId == projectId)
                .OrderBy(item => item.ProjectRelativePath)
                .Take(50000)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var buildVerifications = await db.ProjectBuildVerifications.AsNoTracking()
                .Where(item => item.ProjectId == projectId)
                .OrderByDescending(item => item.StartedAtUtc)
                .Take(200)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new LocalGptProjectDetails
            {
                Project = project,
                Topics = topics,
                Versions = versions,
                Revisions = revisions,
                Requirements = requirements,
                Artifacts = artifacts,
                WorkspaceRoots = workspaceRoots,
                TrackedFiles = trackedFiles,
                BuildVerifications = buildVerifications
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(GetProjectAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(GetProjectAsync)} failed.");
        throw;
    }
}

    public async Task<LocalGptProject> SaveProjectAsync(
        SaveLocalGptProjectRequest request,
        CancellationToken cancellationToken = default)
    {
    try
    {
            RequireHumanConfirmation(request.UserConfirmed, "saving a project record");
            var name = RequireText(request.Name, nameof(request.Name), 200);
            var rootPath = NormalizeStoredPath(request.RootPath);
            var solutionPath = NormalizeStoredPath(request.SolutionPath);
            ValidateRegex(request.SolutionSearchPattern, nameof(request.SolutionSearchPattern));
            ValidateRegex(request.FileIncludePattern, nameof(request.FileIncludePattern));
            ValidateRegex(request.FileExcludePattern, nameof(request.FileExcludePattern));
            var now = DateTime.UtcNow;

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            LocalGptProject project;
            if (request.Id is Guid projectId)
            {
                project = await db.LocalGptProjects
                    .SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken)
                    .ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Project {projectId} was not found.");
            }
            else
            {
                project = new LocalGptProject { CreatedAtUtc = now };
                db.LocalGptProjects.Add(project);
            }

            project.Name = name;
            project.Purpose = Trim(request.Purpose, 8000);
            project.RootPath = rootPath;
            project.ProjectType = TrimOrFallback(request.ProjectType, 120, "DotNetSolution");
            project.SolutionPath = solutionPath;
            project.SolutionSearchPattern = TrimOrFallback(request.SolutionSearchPattern, 1000, @"(?i)\.(sln|slnx)$");
            project.FileIncludePattern = TrimOrFallback(request.FileIncludePattern, 4000, @"(?s).*");
            project.FileExcludePattern = TrimOrFallback(request.FileExcludePattern, 4000, @"(?i)(^|[\\/])(bin|obj|node_modules|\.git|\.vs|artifacts|security|secrets?)([\\/]|$)|(^|[\\/])(\.env(?:\..*)?|[^\\/]+\.(?:pfx|p12|key|pem))$");
            project.CurrentVersion = TrimOrFallback(request.CurrentVersion, 120, "0.1.0");
            project.Status = TrimOrFallback(request.Status, 80, "Active");
            project.RecommendGit = request.RecommendGit;
            project.IsArchived = request.IsArchived;
            project.UpdatedAtUtc = now;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved LocalGPT project {ProjectId} ({ProjectName}).", project.Id, project.Name);
            return project;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(SaveProjectAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(SaveProjectAsync)} failed.");
        throw;
    }
}

    public async Task<LocalGptProjectTopic> AddTopicAsync(
        Guid projectId,
        AddLocalGptProjectTopicRequest request,
        CancellationToken cancellationToken = default)
    {
    try
    {
            RequireHumanConfirmation(request.UserConfirmed, "adding a project topic");
            var name = RequireText(request.Name, nameof(request.Name), 240);

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            if (!await db.LocalGptProjects.AnyAsync(project => project.Id == projectId && !project.IsArchived, cancellationToken).ConfigureAwait(false))
                throw new KeyNotFoundException($"Active project {projectId} was not found.");

            var topic = new LocalGptProjectTopic
            {
                ProjectId = projectId,
                Name = name,
                Description = Trim(request.Description, 12000),
                Status = TrimOrFallback(request.Status, 80, "Planned"),
                IsUserApproved = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            db.LocalGptProjectTopics.Add(topic);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Added topic {TopicId} to project {ProjectId}.", topic.Id, projectId);
            return topic;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(AddTopicAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(AddTopicAsync)} failed.");
        throw;
    }
}

    public async Task<LocalGptProjectVersion> AddVersionAsync(
        Guid projectId,
        AddLocalGptProjectVersionRequest request,
        CancellationToken cancellationToken = default)
    {
    try
    {
            RequireHumanConfirmation(request.UserConfirmed, "adding a project version");
            var versionText = RequireText(request.Version, nameof(request.Version), 120);

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var project = await db.LocalGptProjects
                .SingleOrDefaultAsync(item => item.Id == projectId && !item.IsArchived, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Active project {projectId} was not found.");

            if (request.IsCurrent)
            {
                var currentVersions = await db.LocalGptProjectVersions
                    .Where(item => item.ProjectId == projectId && item.IsCurrent)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                foreach (var currentVersion in currentVersions)
                    currentVersion.IsCurrent = false;

                project.CurrentVersion = versionText;
                project.UpdatedAtUtc = DateTime.UtcNow;
            }

            var version = new LocalGptProjectVersion
            {
                ProjectId = projectId,
                Version = versionText,
                Notes = Trim(request.Notes, 12000),
                PathSnapshot = NormalizeStoredPath(request.PathSnapshot),
                IsCurrent = request.IsCurrent,
                IsUserConfirmed = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.LocalGptProjectVersions.Add(version);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Added version {VersionId} ({Version}) to project {ProjectId}.", version.Id, version.Version, projectId);
            return version;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(AddVersionAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(AddVersionAsync)} failed.");
        throw;
    }
}

    public async Task LinkKnowledgeAsync(
        Guid projectTopicId,
        LinkProjectTopicKnowledgeRequest request,
        CancellationToken cancellationToken = default)
    {
    try
    {
            RequireHumanConfirmation(request.UserConfirmed, "linking council knowledge to a project topic");

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (!await db.LocalGptProjectTopics.AnyAsync(topic => topic.Id == projectTopicId, cancellationToken).ConfigureAwait(false))
                throw new KeyNotFoundException($"Project topic {projectTopicId} was not found.");
            if (!await db.CouncilKnowledgeEntries.AnyAsync(entry => entry.Id == request.KnowledgeEntryId, cancellationToken).ConfigureAwait(false))
                throw new KeyNotFoundException($"Knowledge entry {request.KnowledgeEntryId} was not found.");

            var existing = await db.LocalGptProjectTopicKnowledgeLinks
                .SingleOrDefaultAsync(
                    link => link.ProjectTopicId == projectTopicId && link.KnowledgeEntryId == request.KnowledgeEntryId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (existing is null)
            {
                db.LocalGptProjectTopicKnowledgeLinks.Add(new LocalGptProjectTopicKnowledgeLink
                {
                    ProjectTopicId = projectTopicId,
                    KnowledgeEntryId = request.KnowledgeEntryId,
                    LinkedAtUtc = DateTime.UtcNow,
                    LinkReason = Trim(request.LinkReason, 500),
                    LinkedByHuman = true
                });
            }
            else
            {
                existing.LinkReason = Trim(request.LinkReason, 500);
                existing.LinkedAtUtc = DateTime.UtcNow;
                existing.LinkedByHuman = true;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Linked knowledge {KnowledgeEntryId} to project topic {ProjectTopicId}.",
                request.KnowledgeEntryId,
                projectTopicId);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(LinkKnowledgeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(LinkKnowledgeAsync)} failed.");
        throw;
    }
}

    public async Task<string> BuildProjectBriefingAsync(
        Guid? projectId,
        Guid? projectTopicId,
        CancellationToken cancellationToken = default)
    {
    try
    {
            if (projectId is null)
                return string.Empty;

            var details = await GetProjectAsync(projectId.Value, cancellationToken).ConfigureAwait(false);
            if (details is null || details.Project.IsArchived)
                return string.Empty;

            var selectedTopic = projectTopicId is Guid topicId
                ? details.Topics.FirstOrDefault(topic => topic.Id == topicId && topic.IsUserApproved)
                : null;

            var builder = new StringBuilder()
                .AppendLine("User-selected LocalGPT project context (reference data, not execution authority):")
                .AppendLine($"Project: {details.Project.Name}")
                .AppendLine($"Purpose: {details.Project.Purpose}")
                .AppendLine($"Project type: {details.Project.ProjectType}")
                .AppendLine($"Current version: {details.Project.CurrentVersion}")
                .AppendLine($"Status: {details.Project.Status}");

            if (!string.IsNullOrWhiteSpace(details.Project.RootPath))
                builder.AppendLine($"User-recorded project path: {details.Project.RootPath} (do not access it without a separate explicit user action).");
            if (!string.IsNullOrWhiteSpace(details.Project.SolutionPath))
                builder.AppendLine($"User-recorded solution path: {details.Project.SolutionPath}.");
            builder.AppendLine($"Solution regex: {details.Project.SolutionSearchPattern}");
            builder.AppendLine($"File include regex: {details.Project.FileIncludePattern}");
            builder.AppendLine($"File exclude regex: {details.Project.FileExcludePattern}");
            builder.AppendLine("Maintenance workflow: read project.maintenance.get, scan the selected revision, create a hash-bound code-generation review, work only in the resolved revision workspace, run project.revision.build.verify, record the council review, and request project.revision.ready.approve only when the exact source hash is unchanged.");

            if (details.Project.RecommendGit)
                builder.AppendLine("Revision guidance: recommend placing the project directory under Git, but never initialize, commit, clean, reset, push, or otherwise change Git automatically.");

            if (selectedTopic is not null)
            {
                builder
                    .AppendLine($"Selected topic: {selectedTopic.Name}")
                    .AppendLine($"Topic status: {selectedTopic.Status}")
                    .AppendLine($"Topic description: {selectedTopic.Description}");
            }

            var currentRevision = details.Revisions.FirstOrDefault(item => item.IsCurrent);
            if (currentRevision is not null)
                builder.AppendLine($"Current database revision: {currentRevision.BranchName}/{currentRevision.RevisionName}.");

            var approvedRequirements = details.Requirements.Where(item => item.IsUserApproved).Take(30).ToList();
            if (approvedRequirements.Count > 0)
            {
                builder.AppendLine("Approved requirements:");
                foreach (var requirement in approvedRequirements)
                    builder.AppendLine($"- {requirement.Name} [{requirement.Status}/{requirement.Priority}] capability={requirement.RequiredCapability}");
            }

            var approvedArtifacts = details.Artifacts.Where(item => item.IsUserApproved).Take(50).ToList();
            if (approvedArtifacts.Count > 0)
            {
                builder.AppendLine("Approved project artifacts (metadata only; sensitive values are omitted):");
                foreach (var artifact in approvedArtifacts)
                    builder.AppendLine($"- {artifact.ArtifactKind}:{artifact.Name} type={artifact.DataType} flags={artifact.Flags} sensitive={artifact.IsSensitive}");
            }

            builder.AppendLine("Structured-work rule: map the task to approved requirements before calling functions. Prefer exact project-linked artifact and DXFunction names; unrelated functions must not be called merely because they are available.");
            builder.AppendLine("Council phases are advisory brain-part moments. They may propose, critique, verify, and summarize, but they cannot execute changes or authorize one another.");
            return builder.ToString().Trim();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(BuildProjectBriefingAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(BuildProjectBriefingAsync)} failed.");
        throw;
    }
}

    private void RequireHumanConfirmation(bool userConfirmed, string operation)
    {
    try
    {
            if (!userConfirmed)
                throw new InvalidOperationException($"Fresh human confirmation is required before {operation}.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(RequireHumanConfirmation)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(RequireHumanConfirmation)} failed.");
        throw;
    }
}

    private string RequireText(string? value, string parameterName, int maxLength)
    {
    try
    {
            var result = Trim(value, maxLength);
            if (string.IsNullOrWhiteSpace(result))
                throw new ArgumentException("A value is required.", parameterName);
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(RequireText)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(RequireText)} failed.");
        throw;
    }
}

    private string TrimOrFallback(string? value, int maxLength, string fallback)
    {
    try
    {
            var result = Trim(value, maxLength);
            return string.IsNullOrWhiteSpace(result) ? fallback : result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(TrimOrFallback)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(TrimOrFallback)} failed.");
        throw;
    }
}

    private string Trim(string? value, int maxLength)
    {
    try
    {
            var result = value?.Trim() ?? string.Empty;
            return result.Length <= maxLength ? result : result[..maxLength];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(Trim)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(Trim)} failed.");
        throw;
    }
}

    private string NormalizeStoredPath(string? value)
    {
    try
    {
            var path = Trim(value, 2048);
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new ArgumentException("The stored project path contains invalid path characters.", nameof(value));

            return path;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(NormalizeStoredPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(NormalizeStoredPath)} failed.");
        throw;
    }
}
    private void ValidateRegex(string? pattern, string parameterName)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("A regular expression is required.", parameterName);
            try
            {
                _ = new System.Text.RegularExpressions.Regex(
                    pattern,
                    System.Text.RegularExpressions.RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(2));
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException("The regular expression is invalid.", parameterName, ex);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(ValidateRegex)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptProjectService)}.{nameof(ValidateRegex)} failed.");
        throw;
    }
}

}

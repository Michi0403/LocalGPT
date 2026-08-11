using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>
/// Provides project architecture service operations.
/// </summary>
public sealed class ProjectArchitectureService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<ProjectArchitectureService> logger) : IProjectArchitectureService
{
    /// <summary>
    /// Ensures council run project async.
    /// </summary>
    public async Task<(LocalGptProject Project, LocalGptProjectRevision Revision)> EnsureCouncilRunProjectAsync(
        Guid councilRunId,
        string? title,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var revisionName = $"council-{councilRunId:N}";
        var existingRevision = await db.LocalGptProjectRevisions
            .Include(item => item.Project)
            .SingleOrDefaultAsync(item => item.RevisionName == revisionName, cancellationToken)
            .ConfigureAwait(false);
        if (existingRevision?.Project is not null)
            return (existingRevision.Project, existingRevision);

        var now = DateTime.UtcNow;
        var projectName = BuildProjectName(title, prompt, councilRunId);
        var project = new LocalGptProject
        {
            Name = projectName,
            Purpose = Trim(prompt, 8000),
            CurrentVersion = "council-run",
            Status = "CouncilActive",
            RecommendGit = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var revision = new LocalGptProjectRevision
        {
            Project = project,
            ProjectId = project.Id,
            BranchName = "council/main",
            RevisionName = revisionName,
            Summary = "Database-first project created for an explicit DXChat AI Council run.",
            ProjectStructureJson = JsonSerializer.Serialize(new
            {
                kind = "council-run",
                councilRunId,
                files = Array.Empty<string>(),
                note = "Project structure is stored in the database and may later become CodeDOM input after user approval."
            }),
            CreatedBy = "Human-triggered AI Council",
            IsCurrent = true,
            IsUserApproved = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.LocalGptProjects.Add(project);
        db.LocalGptProjectRevisions.Add(revision);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Created database-first project {ProjectId} and revision {RevisionId} for council run {CouncilRunId}.", project.Id, revision.Id, councilRunId);
        return (project, revision);
    }

    /// <summary>
    /// Gets revisions async.
    /// </summary>
    public async Task<IReadOnlyList<LocalGptProjectRevision>> GetRevisionsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await db.LocalGptProjectRevisions.AsNoTracking()
                .Where(item => item.ProjectId == projectId)
                .OrderByDescending(item => item.IsCurrent)
                .ThenByDescending(item => item.UpdatedAtUtc)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(GetRevisionsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(GetRevisionsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Gets requirements async.
    /// </summary>
    public async Task<IReadOnlyList<LocalGptProjectRequirement>> GetRequirementsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await db.LocalGptProjectRequirements.AsNoTracking()
                .Where(item => item.ProjectId == projectId)
                .Include(item => item.Links)
                .OrderBy(item => item.Status)
                .ThenBy(item => item.Priority)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(GetRequirementsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(GetRequirementsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Gets artifacts async.
    /// </summary>
    public async Task<IReadOnlyList<LocalGptProjectArtifact>> GetArtifactsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await db.LocalGptProjectArtifacts.AsNoTracking()
                .Where(item => item.ProjectId == projectId)
                .OrderBy(item => item.ArtifactKind)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(GetArtifactsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(GetArtifactsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Saves revision async.
    /// </summary>
    public async Task<LocalGptProjectRevision> SaveRevisionAsync(Guid projectId, SaveProjectRevisionRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            RequireConfirmation(request.UserConfirmed, "saving a project revision or branch");
            var branch = RequireText(request.BranchName, nameof(request.BranchName), 160);
            var revisionName = RequireText(request.RevisionName, nameof(request.RevisionName), 160);
            ValidateStructureJson(request.ProjectStructureJson);

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var project = await db.LocalGptProjects.SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Project {projectId} was not found.");

            if (request.ParentRevisionId is Guid parentId &&
                !await db.LocalGptProjectRevisions.AnyAsync(item => item.Id == parentId && item.ProjectId == projectId, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The selected parent revision does not belong to this project.");
            }

            if (request.IsCurrent)
            {
                var current = await db.LocalGptProjectRevisions.Where(item => item.ProjectId == projectId && item.IsCurrent).ToListAsync(cancellationToken).ConfigureAwait(false);
                foreach (var item in current)
                    item.IsCurrent = false;
            }

            var revision = new LocalGptProjectRevision
            {
                ProjectId = projectId,
                ParentRevisionId = request.ParentRevisionId,
                BranchName = branch,
                RevisionName = revisionName,
                Summary = Trim(request.Summary, 12000),
                ProjectStructureJson = string.IsNullOrWhiteSpace(request.ProjectStructureJson) ? "{}" : request.ProjectStructureJson.Trim(),
                CreatedBy = "Human User",
                IsCurrent = request.IsCurrent,
                IsUserApproved = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            db.LocalGptProjectRevisions.Add(revision);
            project.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return revision;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(SaveRevisionAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(SaveRevisionAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Saves requirement async.
    /// </summary>
    public async Task<LocalGptProjectRequirement> SaveRequirementAsync(Guid projectId, SaveProjectRequirementRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            RequireConfirmation(request.UserConfirmed, "saving a project requirement");
            var name = RequireText(request.Name, nameof(request.Name), 240);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            if (!await db.LocalGptProjects.AnyAsync(item => item.Id == projectId, cancellationToken).ConfigureAwait(false))
                throw new KeyNotFoundException($"Project {projectId} was not found.");

            LocalGptProjectRequirement entity;
            if (request.Id is Guid id)
            {
                entity = await db.LocalGptProjectRequirements.SingleOrDefaultAsync(item => item.Id == id && item.ProjectId == projectId, cancellationToken).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Requirement {id} was not found.");
            }
            else
            {
                entity = new LocalGptProjectRequirement { ProjectId = projectId, CreatedAtUtc = DateTime.UtcNow };
                db.LocalGptProjectRequirements.Add(entity);
            }

            entity.RevisionId = request.RevisionId;
            entity.Name = name;
            entity.Description = Trim(request.Description, 16000);
            entity.RequirementType = Fallback(request.RequirementType, 80, "Functional");
            entity.Status = Fallback(request.Status, 80, "Planned");
            entity.Priority = Fallback(request.Priority, 40, "Normal");
            entity.RequiredCapability = Trim(request.RequiredCapability, 240);
            entity.SourceKind = "Human";
            entity.CouncilRating = Math.Clamp(request.CouncilRating, 0, 100);
            entity.IsUserApproved = true;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return entity;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(SaveRequirementAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(SaveRequirementAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Saves requirement link async.
    /// </summary>
    public async Task<LocalGptProjectRequirementLink> SaveRequirementLinkAsync(Guid projectId, SaveProjectRequirementLinkRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            RequireConfirmation(request.UserConfirmed, "linking a project requirement to a named LocalGPT capability");
            var targetKind = RequireText(request.TargetKind, nameof(request.TargetKind), 80);
            var targetName = RequireText(request.TargetName, nameof(request.TargetName), 240);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var requirement = await db.LocalGptProjectRequirements.SingleOrDefaultAsync(
                item => item.Id == request.RequirementId && item.ProjectId == projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException("The selected requirement does not belong to the active project.");

            var existing = await db.LocalGptProjectRequirementLinks.SingleOrDefaultAsync(
                item => item.RequirementId == requirement.Id && item.TargetKind == targetKind && item.TargetName == targetName,
                cancellationToken).ConfigureAwait(false);
            var entity = existing ?? new LocalGptProjectRequirementLink
            {
                RequirementId = requirement.Id,
                LinkedAtUtc = DateTime.UtcNow
            };
            if (existing is null)
                db.LocalGptProjectRequirementLinks.Add(entity);
            entity.TargetKind = targetKind;
            entity.TargetName = targetName;
            entity.TargetId = Trim(request.TargetId, 160);
            entity.TargetTable = Trim(request.TargetTable, 160);
            entity.LinkPurpose = Trim(request.LinkPurpose, 1000);
            entity.CouncilReviewStatus = "HumanApproved";
            entity.IsUserApproved = true;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Linked requirement {RequirementId} to {TargetKind}:{TargetName}; target content omitted.", requirement.Id, targetKind, targetName);
            return entity;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(SaveRequirementLinkAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(SaveRequirementLinkAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Saves artifact async.
    /// </summary>
    public async Task<LocalGptProjectArtifact> SaveArtifactAsync(Guid projectId, SaveProjectArtifactRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            RequireConfirmation(request.UserConfirmed, "saving a project-linked configuration artifact");
            var kind = Fallback(request.ArtifactKind, 80, "Configuration");
            var name = RequireText(request.Name, nameof(request.Name), 240);
            if (kind.Equals("Regex", StringComparison.OrdinalIgnoreCase))
                ValidateRegex(request.Value, request.Flags);

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            if (!await db.LocalGptProjects.AnyAsync(item => item.Id == projectId, cancellationToken).ConfigureAwait(false))
                throw new KeyNotFoundException($"Project {projectId} was not found.");

            LocalGptProjectArtifact entity;
            if (request.Id is Guid id)
            {
                entity = await db.LocalGptProjectArtifacts.SingleOrDefaultAsync(item => item.Id == id && item.ProjectId == projectId, cancellationToken).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Project artifact {id} was not found.");
            }
            else
            {
                entity = await db.LocalGptProjectArtifacts.SingleOrDefaultAsync(
                    item => item.ProjectId == projectId && item.ArtifactKind == kind && item.Name == name,
                    cancellationToken).ConfigureAwait(false)
                    ?? new LocalGptProjectArtifact { ProjectId = projectId, CreatedAtUtc = DateTime.UtcNow };
                if (db.Entry(entity).State == EntityState.Detached)
                    db.LocalGptProjectArtifacts.Add(entity);
            }

            entity.RevisionId = request.RevisionId;
            entity.RequirementId = request.RequirementId;
            entity.ArtifactKind = kind;
            entity.Name = name;
            entity.Value = Trim(request.Value, 64000);
            entity.DataType = Fallback(request.DataType, 120, "string");
            entity.Flags = Trim(request.Flags, 160);
            entity.Description = Trim(request.Description, 2000);
            entity.IsSensitive = request.IsSensitive;
            entity.IsUserApproved = true;
            entity.CouncilReviewStatus = "HumanApproved";
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved project artifact {ArtifactId} of kind {ArtifactKind} for project {ProjectId}; value content omitted.", entity.Id, entity.ArtifactKind, projectId);
            return entity;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(SaveArtifactAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(SaveArtifactAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds architecture briefing async.
    /// </summary>
    public async Task<string> BuildArchitectureBriefingAsync(Guid projectId, Guid? revisionId, CancellationToken cancellationToken = default)
    {
    try
    {
            var revisions = await GetRevisionsAsync(projectId, cancellationToken).ConfigureAwait(false);
            var requirements = await GetRequirementsAsync(projectId, cancellationToken).ConfigureAwait(false);
            var artifacts = await GetArtifactsAsync(projectId, cancellationToken).ConfigureAwait(false);
            var selectedRevision = revisionId is Guid id ? revisions.FirstOrDefault(item => item.Id == id) : revisions.FirstOrDefault(item => item.IsCurrent);

            var builder = new StringBuilder()
                .AppendLine("Database-first project architecture context:")
                .AppendLine($"Active revision: {(selectedRevision is null ? "unspecified" : $"{selectedRevision.BranchName}/{selectedRevision.RevisionName}")}")
                .AppendLine("Before any function call, map the task to one or more approved requirements and only select functions or artifacts whose names and purposes match that map.")
                .AppendLine("Approved requirements:");

            foreach (var requirement in requirements.Where(item => item.IsUserApproved).Take(40))
                builder.AppendLine($"- {requirement.Name} [{requirement.Status}/{requirement.Priority}] capability={requirement.RequiredCapability}; links={requirement.Links.Count}");

            builder.AppendLine("Approved project artifacts (names and metadata only; sensitive values are never briefed):");
            foreach (var artifact in artifacts.Where(item => item.IsUserApproved).Take(80))
                builder.AppendLine($"- {artifact.ArtifactKind}:{artifact.Name} type={artifact.DataType} flags={artifact.Flags} sensitive={artifact.IsSensitive} review={artifact.CouncilReviewStatus}");

            return builder.ToString().Trim();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(BuildArchitectureBriefingAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(BuildArchitectureBriefingAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds project name.
    /// </summary>
    private string BuildProjectName(string? title, string prompt, Guid runId)
    {
    try
    {
            var source = string.IsNullOrWhiteSpace(title) ? prompt : title;
            var normalized = new string(source.Where(ch => !char.IsControl(ch)).ToArray()).Trim();
            if (normalized.Length > 100)
                normalized = normalized[..100].TrimEnd();
            return string.IsNullOrWhiteSpace(normalized) ? $"Council project {runId:N}" : normalized;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(BuildProjectName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(BuildProjectName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Validates structure JSON.
    /// </summary>
    private void ValidateStructureJson(string? json)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(json))
                return;
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
                throw new ArgumentException("Project structure JSON must be an object or array.", nameof(json));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(ValidateStructureJson)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(ValidateStructureJson)} failed.");
        throw;
    }
}

    /// <summary>
    /// Validates regex.
    /// </summary>
    private void ValidateRegex(string pattern, string? flags)
    {
    try
    {
            if (pattern.Length > 16_000)
                throw new ArgumentException("Regex patterns are limited to 16,000 characters.", nameof(pattern));
            var options = System.Text.RegularExpressions.RegexOptions.CultureInvariant;
            if (!string.IsNullOrWhiteSpace(flags) && flags.Contains('i', StringComparison.OrdinalIgnoreCase))
                options |= System.Text.RegularExpressions.RegexOptions.IgnoreCase;
            _ = new System.Text.RegularExpressions.Regex(pattern, options, TimeSpan.FromSeconds(2));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(ValidateRegex)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(ValidateRegex)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the require confirmation operation.
    /// </summary>
    private void RequireConfirmation(bool confirmed, string operation)
    {
    try
    {
            if (!confirmed)
                throw new InvalidOperationException($"Fresh human confirmation is required before {operation}.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(RequireConfirmation)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(RequireConfirmation)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the require text operation.
    /// </summary>
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
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(RequireText)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(RequireText)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the fallback operation.
    /// </summary>
    private string Fallback(string? value, int maxLength, string fallback)
    {
    try
    {
            var result = Trim(value, maxLength);
            return string.IsNullOrWhiteSpace(result) ? fallback : result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(Fallback)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(Fallback)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the trim operation.
    /// </summary>
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
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(Trim)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectArchitectureService)}.{nameof(Trim)} failed.");
        throw;
    }
}
}

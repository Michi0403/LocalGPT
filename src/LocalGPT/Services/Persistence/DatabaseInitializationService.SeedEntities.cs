using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Coordinates database initialization behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed partial class DatabaseInitializationService
{
    /// <summary>
    /// Ensures topic as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="project">Project value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="description">Description value supplied to the database initialization operation and used when producing its result.</param>
    private void EnsureTopic(LocalGptProject project, string name, string description)
    {
    try
    {
            if (project.Topics.Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))) return;
            project.Topics.Add(new LocalGptProjectTopic
            {
                ProjectId = project.Id,
                Name = name,
                Description = description,
                Status = "Active",
                IsUserApproved = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(EnsureTopic)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(EnsureTopic)} failed.");
        throw;
    }
}

    /// <summary>
    /// Ensures version as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="project">Project value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="version">Version value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="path">Path value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="notes">Notes value supplied to the database initialization operation and used when producing its result.</param>
    private void EnsureVersion(LocalGptProject project, string version, string path, string notes)
    {
    try
    {
            if (project.Versions.Any(item => string.Equals(item.Version, version, StringComparison.OrdinalIgnoreCase))) return;
            var hasCurrentVersion = project.Versions.Any(existing => existing.IsCurrent);
            project.Versions.Add(new LocalGptProjectVersion
            {
                ProjectId = project.Id,
                Version = version,
                Notes = notes,
                PathSnapshot = path,
                IsCurrent = !hasCurrentVersion,
                IsUserConfirmed = true,
                CreatedAtUtc = DateTime.UtcNow
            });
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(EnsureVersion)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(EnsureVersion)} failed.");
        throw;
    }
}

    /// <summary>
    /// Ensures revision as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="project">Project value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="branch">Branch value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="revision">Revision value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="root">Root value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="summary">Summary value supplied to the database initialization operation and used when producing its result.</param>
    private void EnsureRevision(LocalGptProject project, string branch, string revision, string root, string summary)
    {
    try
    {
            if (project.Revisions.Any(item => string.Equals(item.BranchName, branch, StringComparison.OrdinalIgnoreCase) && string.Equals(item.RevisionName, revision, StringComparison.OrdinalIgnoreCase))) return;
            var hasCurrentRevision = project.Revisions.Any(existing => existing.IsCurrent);
            project.Revisions.Add(new LocalGptProjectRevision
            {
                ProjectId = project.Id,
                BranchName = branch,
                RevisionName = revision,
                Summary = summary,
                ProjectStructureJson = JsonSerializer.Serialize(new { RootPath = root, Seeded = true, Version = revision }),
                CreatedBy = "LocalGPT deterministic seed",
                IsCurrent = !hasCurrentRevision,
                IsUserApproved = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(EnsureRevision)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(EnsureRevision)} failed.");
        throw;
    }
}

    /// <summary>
    /// Ensures requirement as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="project">Project value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="description">Description value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="capability">Capability value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="priority">Priority value supplied to the database initialization operation and used when producing its result.</param>
    private void EnsureRequirement(LocalGptProject project, string name, string description, string capability, string priority)
    {
    try
    {
            if (project.Requirements.Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))) return;
            project.Requirements.Add(new LocalGptProjectRequirement
            {
                ProjectId = project.Id,
                Name = name,
                Description = description,
                RequirementType = "Architecture",
                Status = "Active",
                Priority = priority,
                RequiredCapability = capability,
                SourceKind = "DeterministicSeed",
                CouncilRating = 100,
                IsUserApproved = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(EnsureRequirement)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(EnsureRequirement)} failed.");
        throw;
    }
}

    /// <summary>
    /// Ensures artifact as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="project">Project value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="kind">Kind value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="dataType">Data type value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="description">Description value supplied to the database initialization operation and used when producing its result.</param>
    private void EnsureArtifact(LocalGptProject project, string name, string kind, string value, string dataType, string description)
    {
    try
    {
            if (project.Artifacts.Any(item => string.Equals(item.ArtifactKind, kind, StringComparison.OrdinalIgnoreCase) && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))) return;
            project.Artifacts.Add(new LocalGptProjectArtifact
            {
                ProjectId = project.Id,
                ArtifactKind = kind,
                Name = name,
                Value = value,
                DataType = dataType,
                Description = description,
                CouncilReviewStatus = "Current",
                IsUserApproved = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(EnsureArtifact)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(EnsureArtifact)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves repository root as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="contentRoot">Content root value supplied to the database initialization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveRepositoryRoot(string contentRoot)
    {
    try
    {
            var current = new DirectoryInfo(Path.GetFullPath(contentRoot));
            DirectoryInfo? projectFallback = null;
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "global.json")) &&
                    (Directory.Exists(Path.Combine(current.FullName, "src")) ||
                     current.EnumerateFiles("*.sln*", SearchOption.TopDirectoryOnly).Any()))
                {
                    return current.FullName;
                }

                if (File.Exists(Path.Combine(current.FullName, "src", "LocalGPT", "LocalGPT.csproj")))
                    return current.FullName;

                if (projectFallback is null && current.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly).Any())
                    projectFallback = current;
                current = current.Parent;
            }
            return projectFallback?.FullName ?? Path.GetFullPath(contentRoot);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(ResolveRepositoryRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(ResolveRepositoryRoot)} failed.");
        throw;
    }
}


}

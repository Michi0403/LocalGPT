using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates project organic context behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="projectArchitecture">Project architecture service dependency used by the project organic context workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ProjectOrganicContextService(
    IProjectArchitectureService projectArchitecture,
    ILogger<ProjectOrganicContextService> logger) : IProjectOrganicContextService
{
    /// <summary>
    /// Defines the artifact kind constant used by <see cref="ProjectOrganicContextService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string ArtifactKind = "OrganicProjectContext";
    /// <summary>
    /// Defines the artifact name constant used by <see cref="ProjectOrganicContextService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string ArtifactName = "LocalGPT organic project wiring";
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="ProjectOrganicContextService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Performs get as part of the project organic context service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project organic context produced by the operation.</returns>
    public async Task<ProjectOrganicContext> GetAsync(Guid projectId, Guid? revisionId, CancellationToken cancellationToken = default)
    {
        var artifacts = await projectArchitecture.GetArtifactsAsync(projectId, cancellationToken).ConfigureAwait(false);
        var artifact = artifacts
            .Where(item => item.IsUserApproved && string.Equals(item.ArtifactKind, ArtifactKind, StringComparison.OrdinalIgnoreCase))
            .Where(item => revisionId is null || item.RevisionId == revisionId || item.RevisionId is null)
            .OrderByDescending(item => item.RevisionId == revisionId)
            .ThenByDescending(item => item.UpdatedAtUtc)
            .FirstOrDefault();
        if (artifact is null || string.IsNullOrWhiteSpace(artifact.Value))
            return new ProjectOrganicContext { ProjectId = projectId, RevisionId = revisionId };
        try
        {
            var context = JsonSerializer.Deserialize<ProjectOrganicContext>(artifact.Value, JsonOptions)
                ?? new ProjectOrganicContext();
            context.ProjectId = projectId;
            context.RevisionId = revisionId ?? context.RevisionId;
            return context;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "The approved organic project context for {ProjectId} is invalid JSON and will be ignored.", projectId);
            return new ProjectOrganicContext { ProjectId = projectId, RevisionId = revisionId };
        }
    }

    /// <summary>
    /// Performs save as part of the project organic context service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project organic context produced by the operation.</returns>
    public async Task<ProjectOrganicContext> SaveAsync(Guid projectId, SaveProjectOrganicContextRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Saving project installer/compiler/regex/organ wiring requires current user confirmation.");
            request.ProjectId = projectId;
            request.LastCouncilActivityUtc ??= DateTimeOffset.UtcNow;
            var existing = await projectArchitecture.GetArtifactsAsync(projectId, cancellationToken).ConfigureAwait(false);
            var existingArtifact = existing
                .Where(item => string.Equals(item.ArtifactKind, ArtifactKind, StringComparison.OrdinalIgnoreCase))
                .Where(item => item.RevisionId == request.RevisionId)
                .OrderByDescending(item => item.UpdatedAtUtc)
                .FirstOrDefault();
            await projectArchitecture.SaveArtifactAsync(projectId, new SaveProjectArtifactRequest
            {
                Id = existingArtifact?.Id,
                RevisionId = request.RevisionId,
                ArtifactKind = ArtifactKind,
                Name = ArtifactName,
                Value = JsonSerializer.Serialize<ProjectOrganicContext>(request, JsonOptions),
                DataType = "application/json",
                Flags = "installer;compiler;commands;knowledge;regex;debug;build;organs;one-wire",
                Description = "Revision-aware project wiring used by council preparation and organic plugins. This record does not authorize command execution.",
                IsSensitive = false,
                UserConfirmed = true
            }, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved approved organic project context for project {ProjectId} and revision {RevisionId}.", projectId, request.RevisionId);
            return await GetAsync(projectId, request.RevisionId, cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectOrganicContextService)}.{nameof(SaveAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectOrganicContextService)}.{nameof(SaveAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds briefing as part of the project organic context service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public async Task<string> BuildBriefingAsync(Guid projectId, Guid? revisionId, CancellationToken cancellationToken = default)
    {
    try
    {
            var context = await GetAsync(projectId, revisionId, cancellationToken).ConfigureAwait(false);
            var builder = new StringBuilder()
                .AppendLine("Organic project wiring (approved database artifact):")
                .AppendLine($"- Installer: {(context.HasInstaller is null ? "unknown" : context.HasInstaller.Value ? "yes" : "no")}; path: {ValueOrUnknown(context.InstallerPath)}")
                .AppendLine($"- Compilers: {Join(context.Compilers)}")
                .AppendLine($"- System command references: {Join(context.SystemCommands)}")
                .AppendLine($"- Knowledge references: {Join(context.KnowledgeReferences)}")
                .AppendLine($"- Project regex patterns: {Join(context.ProjectRegexPatterns)}")
                .AppendLine($"- File regex patterns: {Join(context.FileRegexPatterns)}")
                .AppendLine($"- Debug paths: {Join(context.DebugPaths)}")
                .AppendLine($"- Last build successful: {(context.BuildSuccessful?.ToString() ?? "unknown")}")
                .AppendLine($"- Required organic capabilities: {Join(context.RequiredOrganicCapabilities)}")
                .AppendLine($"- External organ plugins: {Join(context.ExternalOrganPlugins)}")
                .AppendLine("Treat installer/bootstrap paths, fixed ports and launcher arguments as compatibility contracts. Propose changes explicitly and preserve them unless the user authorizes a migration.");
            return builder.ToString().Trim();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectOrganicContextService)}.{nameof(BuildBriefingAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectOrganicContextService)}.{nameof(BuildBriefingAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs join as part of the project organic context service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="values">String dependency used by the project organic context workflow to provide the corresponding application capability.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Join(IEnumerable<string> values)
    {
    try
    {
            var normalized = values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).Take(40).ToList();
            return normalized.Count == 0 ? "none recorded" : string.Join(", ", normalized);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectOrganicContextService)}.{nameof(Join)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectOrganicContextService)}.{nameof(Join)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs value or unknown as part of the project organic context service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the project organic context operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ValueOrUnknown(string value) {
    try
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectOrganicContextService)}.{nameof(ValueOrUnknown)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectOrganicContextService)}.{nameof(ValueOrUnknown)} failed.");
        throw;
    }
}
}

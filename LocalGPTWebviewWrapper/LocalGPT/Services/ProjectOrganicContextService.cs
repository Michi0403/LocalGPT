using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services;

public sealed class ProjectOrganicContextService(
    IProjectArchitectureService projectArchitecture,
    ILogger<ProjectOrganicContextService> logger) : IProjectOrganicContextService
{
    private const string ArtifactKind = "OrganicProjectContext";
    private const string ArtifactName = "LocalGPT organic project wiring";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

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

    public async Task<ProjectOrganicContext> SaveAsync(Guid projectId, SaveProjectOrganicContextRequest request, CancellationToken cancellationToken = default)
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

    public async Task<string> BuildBriefingAsync(Guid projectId, Guid? revisionId, CancellationToken cancellationToken = default)
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

    private static string Join(IEnumerable<string> values)
    {
        var normalized = values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).Take(40).ToList();
        return normalized.Count == 0 ? "none recorded" : string.Join(", ", normalized);
    }

    private static string ValueOrUnknown(string value) => string.IsNullOrWhiteSpace(value) ? "unknown" : value;
}

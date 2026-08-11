using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the project architecture service contract.
/// </summary>
public interface IProjectArchitectureService
{
    /// <summary>
    /// Ensures council run project async.
    /// </summary>
    Task<(LocalGptProject Project, LocalGptProjectRevision Revision)> EnsureCouncilRunProjectAsync(
        Guid councilRunId,
        string? title,
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets revisions async.
    /// </summary>
    Task<IReadOnlyList<LocalGptProjectRevision>> GetRevisionsAsync(Guid projectId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets requirements async.
    /// </summary>
    Task<IReadOnlyList<LocalGptProjectRequirement>> GetRequirementsAsync(Guid projectId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets artifacts async.
    /// </summary>
    Task<IReadOnlyList<LocalGptProjectArtifact>> GetArtifactsAsync(Guid projectId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Saves revision async.
    /// </summary>
    Task<LocalGptProjectRevision> SaveRevisionAsync(Guid projectId, SaveProjectRevisionRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Saves requirement async.
    /// </summary>
    Task<LocalGptProjectRequirement> SaveRequirementAsync(Guid projectId, SaveProjectRequirementRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Saves requirement link async.
    /// </summary>
    Task<LocalGptProjectRequirementLink> SaveRequirementLinkAsync(Guid projectId, SaveProjectRequirementLinkRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Saves artifact async.
    /// </summary>
    Task<LocalGptProjectArtifact> SaveArtifactAsync(Guid projectId, SaveProjectArtifactRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Builds architecture briefing async.
    /// </summary>
    Task<string> BuildArchitectureBriefingAsync(Guid projectId, Guid? revisionId, CancellationToken cancellationToken = default);
}

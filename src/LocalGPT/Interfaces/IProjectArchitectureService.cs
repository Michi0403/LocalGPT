using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for project architecture behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IProjectArchitectureService
{
    /// <summary>
    /// Ensures council run project as part of the project architecture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="title">Title value supplied to the project architecture operation and used when producing its result.</param>
    /// <param name="prompt">Prompt value supplied to the project architecture operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The LocalGPT project project LocalGPT project revision revision produced by the operation.</returns>
    Task<(LocalGptProject Project, LocalGptProjectRevision Revision)> EnsureCouncilRunProjectAsync(
        Guid councilRunId,
        string? title,
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves revisions as part of the project architecture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<LocalGptProjectRevision>> GetRevisionsAsync(Guid projectId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves requirements as part of the project architecture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<LocalGptProjectRequirement>> GetRequirementsAsync(Guid projectId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves artifacts as part of the project architecture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<LocalGptProjectArtifact>> GetArtifactsAsync(Guid projectId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Persists revision as part of the project architecture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The LocalGPT project revision produced by the operation.</returns>
    Task<LocalGptProjectRevision> SaveRevisionAsync(Guid projectId, SaveProjectRevisionRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Persists requirement as part of the project architecture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The LocalGPT project requirement produced by the operation.</returns>
    Task<LocalGptProjectRequirement> SaveRequirementAsync(Guid projectId, SaveProjectRequirementRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Persists requirement link as part of the project architecture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The LocalGPT project requirement link produced by the operation.</returns>
    Task<LocalGptProjectRequirementLink> SaveRequirementLinkAsync(Guid projectId, SaveProjectRequirementLinkRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Persists artifact as part of the project architecture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The LocalGPT project artifact produced by the operation.</returns>
    Task<LocalGptProjectArtifact> SaveArtifactAsync(Guid projectId, SaveProjectArtifactRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Builds architecture briefing as part of the project architecture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> BuildArchitectureBriefingAsync(Guid projectId, Guid? revisionId, CancellationToken cancellationToken = default);
}

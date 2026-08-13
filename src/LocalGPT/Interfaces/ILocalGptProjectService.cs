using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for LocalGPT project behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ILocalGptProjectService
{
    /// <summary>
    /// Retrieves projects as part of the LocalGPT project service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="includeArchived">Value indicating whether include archived should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<LocalGptProjectSummary>> GetProjectsAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves project as part of the LocalGPT project service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The LocalGPT project details produced by the operation.</returns>
    Task<LocalGptProjectDetails?> GetProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists project as part of the LocalGPT project service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The LocalGPT project produced by the operation.</returns>
    Task<LocalGptProject> SaveProjectAsync(
        SaveLocalGptProjectRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds topic as part of the LocalGPT project service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The LocalGPT project topic produced by the operation.</returns>
    Task<LocalGptProjectTopic> AddTopicAsync(
        Guid projectId,
        AddLocalGptProjectTopicRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds version as part of the LocalGPT project service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The LocalGPT project version produced by the operation.</returns>
    Task<LocalGptProjectVersion> AddVersionAsync(
        Guid projectId,
        AddLocalGptProjectVersionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Links knowledge as part of the LocalGPT project service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectTopicId">Identifier of the project topic to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task LinkKnowledgeAsync(
        Guid projectTopicId,
        LinkProjectTopicKnowledgeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds project briefing as part of the LocalGPT project service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="projectTopicId">Identifier of the project topic to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> BuildProjectBriefingAsync(
        Guid? projectId,
        Guid? projectTopicId,
        CancellationToken cancellationToken = default);
}

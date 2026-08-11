using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the local gpt project service contract.
/// </summary>
public interface ILocalGptProjectService
{
    /// <summary>
    /// Gets projects async.
    /// </summary>
    Task<IReadOnlyList<LocalGptProjectSummary>> GetProjectsAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets project async.
    /// </summary>
    Task<LocalGptProjectDetails?> GetProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves project async.
    /// </summary>
    Task<LocalGptProject> SaveProjectAsync(
        SaveLocalGptProjectRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds topic async.
    /// </summary>
    Task<LocalGptProjectTopic> AddTopicAsync(
        Guid projectId,
        AddLocalGptProjectTopicRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds version async.
    /// </summary>
    Task<LocalGptProjectVersion> AddVersionAsync(
        Guid projectId,
        AddLocalGptProjectVersionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the link knowledge async operation.
    /// </summary>
    Task LinkKnowledgeAsync(
        Guid projectTopicId,
        LinkProjectTopicKnowledgeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds project briefing async.
    /// </summary>
    Task<string> BuildProjectBriefingAsync(
        Guid? projectId,
        Guid? projectTopicId,
        CancellationToken cancellationToken = default);
}

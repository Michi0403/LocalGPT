using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ILocalGptProjectService
{
    Task<IReadOnlyList<LocalGptProjectSummary>> GetProjectsAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    Task<LocalGptProjectDetails?> GetProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<LocalGptProject> SaveProjectAsync(
        SaveLocalGptProjectRequest request,
        CancellationToken cancellationToken = default);

    Task<LocalGptProjectTopic> AddTopicAsync(
        Guid projectId,
        AddLocalGptProjectTopicRequest request,
        CancellationToken cancellationToken = default);

    Task<LocalGptProjectVersion> AddVersionAsync(
        Guid projectId,
        AddLocalGptProjectVersionRequest request,
        CancellationToken cancellationToken = default);

    Task LinkKnowledgeAsync(
        Guid projectTopicId,
        LinkProjectTopicKnowledgeRequest request,
        CancellationToken cancellationToken = default);

    Task<string> BuildProjectBriefingAsync(
        Guid? projectId,
        Guid? projectTopicId,
        CancellationToken cancellationToken = default);
}

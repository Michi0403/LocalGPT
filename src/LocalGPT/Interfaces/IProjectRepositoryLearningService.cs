using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Stages source-backed, review-required Knowledge and regex candidates from synchronized repository revisions.</summary>
public interface IProjectRepositoryLearningService
{
    /// <summary>Stages bounded repository snapshot and revision-delta candidates without automatically approving them.</summary>
    Task<IReadOnlyList<ProjectRepositoryLearningResult>> StageCandidatesAsync(
        IReadOnlyList<LearningProjectSyncResult> synchronizedProjects,
        CancellationToken cancellationToken = default);
}

using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Stages source-backed, review-required Knowledge and regex candidates from synchronized repository revisions.</summary>
public interface IProjectRepositoryLearningService
{
    /// <summary>Stages bounded repository snapshot and revision-delta candidates without automatically approving them.</summary>
    /// <param name="synchronizedProjects">Learning project sync result dependency used by the project repository learning workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<ProjectRepositoryLearningResult>> StageCandidatesAsync(
        IReadOnlyList<LearningProjectSyncResult> synchronizedProjects,
        CancellationToken cancellationToken = default);
}

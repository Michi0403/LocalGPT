using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Synchronizes source repositories extracted in a chat upload workspace into LocalGPT's existing project, version, revision, workspace-root, requirement, and tracked-file database model.</summary>
public interface ILearningProjectWorkspaceSyncService
{
    /// <summary>Synchronizes repository-shaped source trees from the requested or latest chat upload workspace.</summary>
    /// <param name="workspaceName">Optional exact chat upload workspace name. When empty, the latest workspace is used.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The source-backed project synchronization results.</returns>
    Task<IReadOnlyList<LearningProjectSyncResult>> SynchronizeAsync(string? workspaceName = null, CancellationToken cancellationToken = default);

    /// <summary>Synchronizes a repository downloaded by the bounded remote-knowledge importer into the same canonical project/version/revision/tracked-file model used for chat uploads.</summary>
    /// <param name="remoteSource">Remote repository cache result produced by the approved read-only importer.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The source-backed project synchronization results.</returns>
    Task<IReadOnlyList<LearningProjectSyncResult>> SynchronizeRemoteRepositoryAsync(RemoteKnowledgeImportResult remoteSource, CancellationToken cancellationToken = default);
}

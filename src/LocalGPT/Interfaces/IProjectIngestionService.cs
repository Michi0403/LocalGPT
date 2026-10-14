using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Coordinates quarantine inspection, independent review and explicit promotion of uploaded projects.</summary>
public interface IProjectIngestionService
{
    /// <summary>
    /// Performs inspect as part of the project ingestion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="workspaceName">Workspace name value supplied to the project ingestion operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project ingestion gate record produced by the operation.</returns>
    Task<ProjectIngestionGateRecord> InspectAsync(string workspaceName, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs get as part of the project ingestion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="workspaceName">Workspace name value supplied to the project ingestion operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project ingestion gate record produced by the operation.</returns>
    Task<ProjectIngestionGateRecord?> GetAsync(string workspaceName, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs review as part of the project ingestion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project ingestion gate record produced by the operation.</returns>
    Task<ProjectIngestionGateRecord> ReviewAsync(ProjectIngestionReviewRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs promote as part of the project ingestion service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="workspaceName">Workspace name value supplied to the project ingestion operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project ingestion gate record produced by the operation.</returns>
    Task<ProjectIngestionGateRecord> PromoteAsync(string workspaceName, bool userConfirmed, CancellationToken cancellationToken = default);
}

/// <summary>Reconstructs bounded file blobs and sends them through the same quarantine gate as normal uploads.</summary>
public interface IProjectBlobReconstructionService
{
    /// <summary>
    /// Performs start as part of the project blob reconstruction service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="manifest">Manifest value supplied to the project blob reconstruction operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project blob session snapshot produced by the operation.</returns>
    Task<ProjectBlobSessionSnapshot> StartAsync(ProjectBlobManifest manifest, CancellationToken cancellationToken = default);
    /// <summary>
    /// Adds chunk as part of the project blob reconstruction service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="relativePath">Relative path value supplied to the project blob reconstruction operation and used when producing its result.</param>
    /// <param name="chunkIndex">Chunk index value supplied to the project blob reconstruction operation and used when producing its result.</param>
    /// <param name="base64Data">Base64 data value supplied to the project blob reconstruction operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project blob session snapshot produced by the operation.</returns>
    Task<ProjectBlobSessionSnapshot> AddChunkAsync(Guid sessionId, string relativePath, int chunkIndex, string base64Data, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs finalize as part of the project blob reconstruction service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project blob session snapshot produced by the operation.</returns>
    Task<ProjectBlobSessionSnapshot> FinalizeAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

/// <summary>Provides reviewed project-type and toolchain evidence without executing source content.</summary>
public interface IProjectEvidenceClassifierService
{
    /// <summary>
    /// Performs classify as part of the project evidence classifier service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="rootPath">Root path value supplied to the project evidence classifier operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project evidence classification produced by the operation.</returns>
    Task<ProjectEvidenceClassification> ClassifyAsync(string rootPath, CancellationToken cancellationToken = default);
}

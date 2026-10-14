using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for learning round behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ILearningRoundService
{
    /// <summary>
    /// Builds snapshot as part of the learning round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="takePerSource">Take per source value supplied to the learning round operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The learning round snapshot produced by the operation.</returns>
    Task<LearningRoundSnapshot> BuildSnapshotAsync(int takePerSource = 200, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs maintain as part of the learning round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The learning maintenance result produced by the operation.</returns>
    Task<LearningMaintenanceResult> MaintainAsync(LearningMaintenanceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists every tracked file's persisted learning state with bounded page output.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="offset">Offset value supplied to the learning round operation and used when producing its result.</param>
    /// <param name="take">Take value supplied to the learning round operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The learning file checklist snapshot produced by the operation.</returns>
    Task<LearningFileChecklistSnapshot> GetFileChecklistAsync(Guid projectId, Guid? revisionId = null, int offset = 0, int take = 100, CancellationToken cancellationToken = default);

    /// <summary>Returns the next source-backed content page and records only the bytes actually read.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="fileId">Identifier of the file to use for this operation.</param>
    /// <param name="maximumCharacters">Maximum characters value supplied to the learning round operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The learning file read result produced by the operation.</returns>
    Task<LearningFileReadResult> ReadNextFileChunkAsync(Guid projectId, Guid? revisionId = null, Guid? fileId = null, int maximumCharacters = 6000, CancellationToken cancellationToken = default);

    /// <summary>Records a review only after the full content hash has been read, or a reasoned block.</summary>
    /// <param name="fileId">Identifier of the file to use for this operation.</param>
    /// <param name="evidence">Evidence value supplied to the learning round operation and used when producing its result.</param>
    /// <param name="blocked">Value indicating whether blocked should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The learning file review result produced by the operation.</returns>
    Task<LearningFileReviewResult> ReviewFileAsync(Guid fileId, string evidence, bool blocked = false, CancellationToken cancellationToken = default);
}

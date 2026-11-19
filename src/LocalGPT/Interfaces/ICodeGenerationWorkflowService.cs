using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for code generation workflow behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ICodeGenerationWorkflowService
{
    /// <summary>Reads compact, actual draft/review state for the originating Council.</summary>
    /// <param name="runId">Council execution identifier.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <param name="includePathHistory">Includes per-path assignment and inspection history for an explicitly requested detail view.</param>
    /// <returns>The active draft or latest submitted draft review, if present.</returns>
    Task<CodeGenerationDraftProgress?> GetCouncilDraftProgressAsync(Guid runId, CancellationToken cancellationToken, bool includePathHistory = false);

    /// <summary>Reserves distinct dependency-ready files for an authorized batch of model routes.</summary>
    /// <param name="runId">Council execution identifier.</param>
    /// <param name="modelNames">Eligible provider-qualified routes in scheduling order.</param>
    /// <param name="maximumAttempts">Configured maximum attempts per file in this workflow revision.</param>
    /// <param name="cancellationToken">Cancels reservation before inference.</param>
    /// <returns>Persisted distinct file work packets.</returns>
    Task<IReadOnlyList<CodeGenerationDraftWorkPacket>> ReserveDraftFilesAsync(Guid runId,
        IReadOnlyList<string> modelNames, int maximumAttempts, CancellationToken cancellationToken);

    /// <summary>Commits the candidate from one actual file-author response, or records a failed attempt.</summary>
    /// <param name="runId">Council execution identifier.</param>
    /// <param name="assignment">Exact server-issued assignment; obsolete assignments cannot write.</param>
    /// <param name="modelName">Actual executing provider-qualified model.</param>
    /// <param name="visibleContent">The completed model's visible file response, excluding provider reasoning.</param>
    /// <param name="error">Actual provider/worker error, if any.</param>
    /// <param name="cancellationToken">Cancels the draft compare-and-swap write.</param>
    /// <returns>Persisted assignment receipt or diagnostic.</returns>
    /// <param name="guestReceipt">Actual completed isolated script receipt when the draft selected guest authoring.</param>
    Task<CodeGenerationDraftAssignment> CompleteDraftFileAsync(Guid runId, CodeGenerationDraftAssignment assignment,
        string modelName, string visibleContent, string? error, CancellationToken cancellationToken,
        SessionSandboxRunSnapshot? guestReceipt = null);

    /// <summary>Gets current candidates and dependency evidence that still require independent inspection.</summary>
    /// <param name="runId">Owning Council execution.</param>
    /// <param name="cancellationToken">Cancels inspection reads.</param>
    /// <returns>Exact candidate packets with source and dependency hashes.</returns>
    Task<IReadOnlyList<CodeGenerationDraftInspection>> GetDraftInspectionsAsync(Guid runId, CancellationToken cancellationToken);

    /// <summary>Records an independent inspection and reopens only the specifically rejected source file.</summary>
    /// <param name="runId">Owning Council execution.</param>
    /// <param name="packet">Previously issued candidate inspection evidence.</param>
    /// <param name="decision">Validated independent opinion tied to the exact hash and reviewer.</param>
    /// <param name="requiredNegativeInspections">Configured corroboration threshold for the available independent reviewer pool.</param>
    /// <param name="cancellationToken">Cancels the persisted decision.</param>
    /// <returns>A task that completes after the decision is committed.</returns>
    Task RecordDraftInspectionAsync(Guid runId, CodeGenerationDraftInspection packet,
        CodeGenerationDraftInspectionDecision decision, int requiredNegativeInspections, CancellationToken cancellationToken);

    /// <summary>Processes one operation on the active Council's persisted source draft.</summary>
    /// <param name="request">Draft operation and its input.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>Compact progress or a finalized review.</returns>
    Task<(object Progress, CodeGenerationReviewSnapshot? Review)> ProcessDraftAsync(
        CreateCodeGenerationReviewRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Creates review as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The code generation review snapshot produced by the operation.</returns>
    Task<CodeGenerationReviewSnapshot> CreateReviewAsync(
        CreateCodeGenerationReviewRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves review as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="reviewId">Identifier of the review to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The code generation review snapshot produced by the operation.</returns>
    Task<CodeGenerationReviewSnapshot?> GetReviewAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists reviews as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="take">Take value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<CodeGenerationReviewSnapshot>> ListReviewsAsync(
        Guid? projectId = null,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes review as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="reviewId">Identifier of the review to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The code generation execution result produced by the operation.</returns>
    Task<CodeGenerationExecutionResult> ExecuteReviewAsync(
        Guid reviewId,
        ExecuteCodeGenerationReviewRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects review as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="reviewId">Identifier of the review to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The code generation review snapshot produced by the operation.</returns>
    Task<CodeGenerationReviewSnapshot> RejectReviewAsync(
        Guid reviewId,
        RejectCodeGenerationReviewRequest request,
        CancellationToken cancellationToken = default);
}

using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for code generation workflow behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ICodeGenerationWorkflowService
{
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

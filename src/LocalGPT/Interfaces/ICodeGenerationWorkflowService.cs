using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the code generation workflow service contract.
/// </summary>
public interface ICodeGenerationWorkflowService
{
    /// <summary>
    /// Creates review async.
    /// </summary>
    Task<CodeGenerationReviewSnapshot> CreateReviewAsync(
        CreateCodeGenerationReviewRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets review async.
    /// </summary>
    Task<CodeGenerationReviewSnapshot?> GetReviewAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the list reviews async operation.
    /// </summary>
    Task<IReadOnlyList<CodeGenerationReviewSnapshot>> ListReviewsAsync(
        Guid? projectId = null,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the execute review async operation.
    /// </summary>
    Task<CodeGenerationExecutionResult> ExecuteReviewAsync(
        Guid reviewId,
        ExecuteCodeGenerationReviewRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the reject review async operation.
    /// </summary>
    Task<CodeGenerationReviewSnapshot> RejectReviewAsync(
        Guid reviewId,
        RejectCodeGenerationReviewRequest request,
        CancellationToken cancellationToken = default);
}

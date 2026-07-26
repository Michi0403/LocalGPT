using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ICodeGenerationWorkflowService
{
    Task<CodeGenerationReviewSnapshot> CreateReviewAsync(
        CreateCodeGenerationReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<CodeGenerationReviewSnapshot?> GetReviewAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CodeGenerationReviewSnapshot>> ListReviewsAsync(
        Guid? projectId = null,
        int take = 20,
        CancellationToken cancellationToken = default);

    Task<CodeGenerationExecutionResult> ExecuteReviewAsync(
        Guid reviewId,
        ExecuteCodeGenerationReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<CodeGenerationReviewSnapshot> RejectReviewAsync(
        Guid reviewId,
        RejectCodeGenerationReviewRequest request,
        CancellationToken cancellationToken = default);
}

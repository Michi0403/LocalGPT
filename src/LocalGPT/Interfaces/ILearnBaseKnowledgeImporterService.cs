using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for learn base knowledge importer behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface ILearnBaseKnowledgeImporterService
    {
        /// <summary>
        /// Performs import as part of the learn base knowledge importer service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The learn base import result produced by the operation.</returns>
        Task<LearnBaseImportResult> ImportAsync(
            LearnBaseImportRequest request,
            CancellationToken cancellationToken = default);
    }
}

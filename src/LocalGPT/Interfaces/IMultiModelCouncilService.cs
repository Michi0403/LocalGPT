using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for multi model council behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface IMultiModelCouncilService
    {
        /// <summary>
        /// Retrieves candidates as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        Task<IReadOnlyList<MultiModelCouncilModelCandidate>> GetCandidatesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs run as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The multi model council result produced by the operation.</returns>
        Task<MultiModelCouncilResult> RunAsync(MultiModelCouncilRequest request, CancellationToken cancellationToken = default);
    }
}

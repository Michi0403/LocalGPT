using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for engineering benchmark behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface IEngineeringBenchmarkService
    {
        /// <summary>
        /// Performs run as part of the engineering benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The engineering benchmark result produced by the operation.</returns>
        Task<EngineeringBenchmarkResult> RunAsync(
            EngineeringBenchmarkRequest request,
            CancellationToken cancellationToken = default);
    }
}

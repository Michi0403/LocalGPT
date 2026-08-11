using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the multi model council service contract.
    /// </summary>
    public interface IMultiModelCouncilService
    {
        /// <summary>
        /// Gets candidates async.
        /// </summary>
        Task<IReadOnlyList<MultiModelCouncilModelCandidate>> GetCandidatesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs the run async operation.
        /// </summary>
        Task<MultiModelCouncilResult> RunAsync(MultiModelCouncilRequest request, CancellationToken cancellationToken = default);
    }
}

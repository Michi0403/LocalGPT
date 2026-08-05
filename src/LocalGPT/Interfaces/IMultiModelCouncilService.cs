using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    public interface IMultiModelCouncilService
    {
        Task<IReadOnlyList<MultiModelCouncilModelCandidate>> GetCandidatesAsync(CancellationToken cancellationToken = default);

        Task<MultiModelCouncilResult> RunAsync(MultiModelCouncilRequest request, CancellationToken cancellationToken = default);
    }
}

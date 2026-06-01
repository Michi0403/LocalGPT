using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    public interface ICouncilArtifactService
    {
        string ArtifactRoot { get; }
        Task<IReadOnlyList<CouncilArtifact>> CreateImplementationArtifactsAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            CancellationToken cancellationToken = default);
    }
}

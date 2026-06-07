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
        Task<IReadOnlyList<CouncilArtifact>> CreateMinecraftDatapackArtifactsAsync(
             MultiModelCouncilRequest request,
             MultiModelCouncilResult result,
             string timestamp,
             CancellationToken cancellationToken);
        Task<IReadOnlyList<CouncilArtifact>> CreateMinecraftSkeletonMatrixArtifactsAsync(
           MultiModelCouncilRequest request,
           MultiModelCouncilResult result,
           string timestamp,
           CancellationToken cancellationToken);
        Task<CouncilArtifact> CreateSolutionZipArtifactAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string timestamp,
            CancellationToken cancellationToken);
        Task<CouncilArtifact?> TryCreateDllArtifactAsync(
           string sourceFileName,
           string source,
           string targetArea,
           CancellationToken cancellationToken);
    }
}

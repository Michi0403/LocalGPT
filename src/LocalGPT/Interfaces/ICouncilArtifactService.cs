using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the council artifact service contract.
    /// </summary>
    public interface ICouncilArtifactService
    {
        string ArtifactRoot { get; }
        /// <summary>
        /// Creates implementation artifacts async.
        /// </summary>
        Task<IReadOnlyList<CouncilArtifact>> CreateImplementationArtifactsAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            CancellationToken cancellationToken = default);
        /// <summary>
        /// Creates minecraft datapack artifacts async.
        /// </summary>
        Task<IReadOnlyList<CouncilArtifact>> CreateMinecraftDatapackArtifactsAsync(
             MultiModelCouncilRequest request,
             MultiModelCouncilResult result,
             string timestamp,
             CancellationToken cancellationToken);
        /// <summary>
        /// Creates minecraft skeleton matrix artifacts async.
        /// </summary>
        Task<IReadOnlyList<CouncilArtifact>> CreateMinecraftSkeletonMatrixArtifactsAsync(
           MultiModelCouncilRequest request,
           MultiModelCouncilResult result,
           string timestamp,
           CancellationToken cancellationToken);
        /// <summary>
        /// Creates solution zip artifact async.
        /// </summary>
        Task<CouncilArtifact> CreateSolutionZipArtifactAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string timestamp,
            CancellationToken cancellationToken);
        /// <summary>
        /// Attempts to create dll artifact async.
        /// </summary>
        Task<CouncilArtifact?> TryCreateDllArtifactAsync(
           string sourceFileName,
           string source,
           string targetArea,
           bool userConfirmedArtifactBuild,
           CancellationToken cancellationToken);
    }
}

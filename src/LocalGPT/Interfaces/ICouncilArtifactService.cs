using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for council artifact behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface ICouncilArtifactService
    {
        /// <summary>
        /// Gets the artifact root value that forms part of the council artifact state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The artifact root value exposed by <see cref="ICouncilArtifactService"/>.</value>
        string ArtifactRoot { get; }
        /// <summary>
        /// Creates implementation artifacts as part of the council artifact service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        Task<IReadOnlyList<CouncilArtifact>> CreateImplementationArtifactsAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            CancellationToken cancellationToken = default);
        /// <summary>
        /// Creates minecraft datapack artifacts as part of the council artifact service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="timestamp">Timestamp value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        Task<IReadOnlyList<CouncilArtifact>> CreateMinecraftDatapackArtifactsAsync(
             MultiModelCouncilRequest request,
             MultiModelCouncilResult result,
             string timestamp,
             CancellationToken cancellationToken);
        /// <summary>
        /// Creates minecraft skeleton matrix artifacts as part of the council artifact service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="timestamp">Timestamp value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        Task<IReadOnlyList<CouncilArtifact>> CreateMinecraftSkeletonMatrixArtifactsAsync(
           MultiModelCouncilRequest request,
           MultiModelCouncilResult result,
           string timestamp,
           CancellationToken cancellationToken);
        /// <summary>
        /// Creates solution ZIP artifact as part of the council artifact service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="timestamp">Timestamp value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The council artifact produced by the operation.</returns>
        Task<CouncilArtifact> CreateSolutionZipArtifactAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string timestamp,
            CancellationToken cancellationToken);
        /// <summary>
        /// Attempts to create dll artifact as part of the council artifact service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="sourceFileName">Source file name value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="source">Source value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="targetArea">Target area value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="userConfirmedArtifactBuild">Value indicating whether user confirmed artifact build should apply to this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The council artifact produced by the operation.</returns>
        Task<CouncilArtifact?> TryCreateDllArtifactAsync(
           string sourceFileName,
           string source,
           string targetArea,
           bool userConfirmedArtifactBuild,
           CancellationToken cancellationToken);
    }
}

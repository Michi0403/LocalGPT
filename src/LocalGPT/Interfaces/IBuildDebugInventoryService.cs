using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for build debug inventory behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface IBuildDebugInventoryService
    {
        /// <summary>
        /// Gets the artifact root value that forms part of the build debug inventory state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The artifact root value exposed by <see cref="IBuildDebugInventoryService"/>.</value>
        string ArtifactRoot { get; }

        /// <summary>
        /// Performs capture as part of the build debug inventory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="copyFiles">Value indicating whether copy files should apply to this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The build debug inventory produced by the operation.</returns>
        Task<BuildDebugInventory> CaptureAsync(bool copyFiles = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Builds briefing as part of the build debug inventory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The string produced by the operation.</returns>
        Task<string> BuildBriefingAsync(CancellationToken cancellationToken = default);
    }
}

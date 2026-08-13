using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for minecraft mod workspace behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface IMinecraftModWorkspaceService
    {
        /// <summary>
        /// Gets the workspace root value that forms part of the minecraft mod workspace state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The workspace root value exposed by <see cref="IMinecraftModWorkspaceService"/>.</value>
        string WorkspaceRoot { get; }
        /// <summary>
        /// Creates workspace as part of the minecraft mod workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The minecraft mod workspace produced by the operation.</returns>
        Task<MinecraftModWorkspace> CreateWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default);
        /// <summary>
        /// Creates fabric workspace as part of the minecraft mod workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The minecraft mod workspace produced by the operation.</returns>
        Task<MinecraftModWorkspace> CreateFabricWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default);
        /// <summary>
        /// Determines whether path inside workspace root as part of the minecraft mod workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="path">Path value supplied to the minecraft mod workspace operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        bool IsPathInsideWorkspaceRoot(string path);
    }
}

using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the minecraft mod workspace service contract.
    /// </summary>
    public interface IMinecraftModWorkspaceService
    {
        string WorkspaceRoot { get; }
        /// <summary>
        /// Creates workspace async.
        /// </summary>
        Task<MinecraftModWorkspace> CreateWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default);
        /// <summary>
        /// Creates fabric workspace async.
        /// </summary>
        Task<MinecraftModWorkspace> CreateFabricWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default);
        /// <summary>
        /// Determines whether path inside workspace root.
        /// </summary>
        bool IsPathInsideWorkspaceRoot(string path);
    }
}

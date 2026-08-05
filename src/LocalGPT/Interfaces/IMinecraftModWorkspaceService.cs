using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    public interface IMinecraftModWorkspaceService
    {
        string WorkspaceRoot { get; }
        Task<MinecraftModWorkspace> CreateWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default);
        Task<MinecraftModWorkspace> CreateFabricWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default);
        bool IsPathInsideWorkspaceRoot(string path);
    }
}

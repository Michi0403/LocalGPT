using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    public interface IBuildDebugInventoryService
    {
        string ArtifactRoot { get; }

        Task<BuildDebugInventory> CaptureAsync(bool copyFiles = false, CancellationToken cancellationToken = default);

        Task<string> BuildBriefingAsync(CancellationToken cancellationToken = default);
    }
}

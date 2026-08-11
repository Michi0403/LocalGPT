using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the build debug inventory service contract.
    /// </summary>
    public interface IBuildDebugInventoryService
    {
        string ArtifactRoot { get; }

        /// <summary>
        /// Runs the capture async operation.
        /// </summary>
        Task<BuildDebugInventory> CaptureAsync(bool copyFiles = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Builds briefing async.
        /// </summary>
        Task<string> BuildBriefingAsync(CancellationToken cancellationToken = default);
    }
}

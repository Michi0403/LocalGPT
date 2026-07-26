using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IRuntimeCapabilityDirectoryService
{
    Task<RuntimeCapabilityDirectorySnapshot> SynchronizeAsync(CancellationToken cancellationToken = default);
}

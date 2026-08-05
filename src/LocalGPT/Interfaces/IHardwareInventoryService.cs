using LocalGPT.WireProtocol;

namespace LocalGPT.Interfaces;

public interface IHardwareInventoryService
{
    Task<IReadOnlyList<OneWireHardwareDescriptor>> GetHardwareAsync(CancellationToken cancellationToken = default);
}

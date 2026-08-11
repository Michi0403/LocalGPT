using LocalGPT.WireProtocol;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the hardware inventory service contract.
/// </summary>
public interface IHardwareInventoryService
{
    /// <summary>
    /// Gets hardware async.
    /// </summary>
    Task<IReadOnlyList<OneWireHardwareDescriptor>> GetHardwareAsync(CancellationToken cancellationToken = default);
}

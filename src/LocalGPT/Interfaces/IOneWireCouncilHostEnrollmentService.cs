using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Maintains Council work-host enrollment for peers that have already passed normal 1-Wire pairing and trust.</summary>
public interface IOneWireCouncilHostEnrollmentService
{
    Task<IReadOnlyList<OneWireCouncilHostEnrollment>> ListAsync(CancellationToken cancellationToken = default);
    Task<OneWireCouncilHostEnrollment> EnrollAsync(string peerId, bool userConfirmed, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string peerId, bool userConfirmed, CancellationToken cancellationToken = default);
}

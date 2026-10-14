using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Maintains Council work-host enrollment for peers that have already passed normal 1-Wire pairing and trust.</summary>
public interface IOneWireCouncilHostEnrollmentService
{
    /// <summary>
    /// Performs list as part of the one wire council host enrollment service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireCouncilHostEnrollment>> ListAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs enroll as part of the one wire council host enrollment service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire council host enrollment produced by the operation.</returns>
    Task<OneWireCouncilHostEnrollment> EnrollAsync(string peerId, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs remove as part of the one wire council host enrollment service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    Task<bool> RemoveAsync(string peerId, bool userConfirmed, CancellationToken cancellationToken = default);
}

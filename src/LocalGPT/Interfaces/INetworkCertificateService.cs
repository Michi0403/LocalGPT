using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for network certificate behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface INetworkCertificateService
{
    /// <summary>
    /// Creates default request as part of the network certificate service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The network certificate create request produced by the operation.</returns>
    NetworkCertificateCreateRequest CreateDefaultRequest();
    /// <summary>Checks that the selected certificate path is an existing PFX file on the LocalGPT host.</summary>
    /// <param name="path">User-selected local certificate path.</param>
    /// <returns>True when the path identifies an existing PFX file.</returns>
    bool IsExistingPfxFile(string? path);
    /// <summary>
    /// Performs create as part of the network certificate service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The network certificate create result produced by the operation.</returns>
    Task<NetworkCertificateCreateResult> CreateAsync(NetworkCertificateCreateRequest request, CancellationToken cancellationToken = default);
}

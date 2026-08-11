using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the network certificate service contract.
/// </summary>
public interface INetworkCertificateService
{
    /// <summary>
    /// Creates default request.
    /// </summary>
    NetworkCertificateCreateRequest CreateDefaultRequest();
    /// <summary>
    /// Creates async.
    /// </summary>
    Task<NetworkCertificateCreateResult> CreateAsync(NetworkCertificateCreateRequest request, CancellationToken cancellationToken = default);
}

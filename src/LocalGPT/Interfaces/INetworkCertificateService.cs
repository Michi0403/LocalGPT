using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface INetworkCertificateService
{
    NetworkCertificateCreateRequest CreateDefaultRequest();
    Task<NetworkCertificateCreateResult> CreateAsync(NetworkCertificateCreateRequest request, CancellationToken cancellationToken = default);
}

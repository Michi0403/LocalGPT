using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IRemoteKnowledgeImportService
{
    Task<RemoteKnowledgeImportResult> ImportAsync(
        RemoteKnowledgeImportRequest request,
        CancellationToken cancellationToken = default);
}

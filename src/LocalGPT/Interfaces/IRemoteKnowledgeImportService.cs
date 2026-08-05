using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IRemoteKnowledgeImportService
{
    List<string> ParseLabels(params string?[] values);

    Task<RemoteKnowledgeImportResult> ImportAsync(
        RemoteKnowledgeImportRequest request,
        CancellationToken cancellationToken = default);
}

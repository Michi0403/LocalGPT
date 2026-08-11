using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the remote knowledge import service contract.
/// </summary>
public interface IRemoteKnowledgeImportService
{
    /// <summary>
    /// Parses labels.
    /// </summary>
    List<string> ParseLabels(params string?[] values);

    /// <summary>
    /// Imports async.
    /// </summary>
    Task<RemoteKnowledgeImportResult> ImportAsync(
        RemoteKnowledgeImportRequest request,
        CancellationToken cancellationToken = default);
}

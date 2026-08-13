using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for remote knowledge import behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IRemoteKnowledgeImportService
{
    /// <summary>
    /// Parses labels as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="values">Values value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    List<string> ParseLabels(params string?[] values);

    /// <summary>
    /// Performs import as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The remote knowledge import result produced by the operation.</returns>
    Task<RemoteKnowledgeImportResult> ImportAsync(
        RemoteKnowledgeImportRequest request,
        CancellationToken cancellationToken = default);
}

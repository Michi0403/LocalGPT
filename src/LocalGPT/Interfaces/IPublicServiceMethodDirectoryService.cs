using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Shared business-purpose navigation of the persisted public-service method catalog for HTTP, UI and DXAiFunctions.</summary>
public interface IPublicServiceMethodDirectoryService
{
    /// <summary>
    /// Performs search as part of the public service method directory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="query">Query value supplied to the public service method directory operation and used when producing its result.</param>
    /// <param name="service">Service value supplied to the public service method directory operation and used when producing its result.</param>
    /// <param name="skip">Skip value supplied to the public service method directory operation and used when producing its result.</param>
    /// <param name="take">Take value supplied to the public service method directory operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The public service controller page produced by the operation.</returns>
    Task<PublicServiceControllerPage> SearchAsync(string? query, string? service, int skip, int take, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs groups as part of the public service method directory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<PublicServiceGroupDescriptor>> GroupsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs find as part of the public service method directory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="catalogKey">Catalog key value supplied to the public service method directory operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function catalog entry produced by the operation.</returns>
    Task<DxAiFunctionCatalogEntry?> FindAsync(string catalogKey, CancellationToken cancellationToken = default);
    /// <summary>
    /// Sets exposure as part of the public service method directory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The public service exposure result produced by the operation.</returns>
    Task<PublicServiceExposureResult> SetExposureAsync(PublicServiceExposureRequest request, CancellationToken cancellationToken = default);
}

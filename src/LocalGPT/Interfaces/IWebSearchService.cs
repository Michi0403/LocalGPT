using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Searches approved public web providers through a bounded service boundary.</summary>
public interface IWebSearchService
{
    /// <summary>Searches the selected provider after the caller has passed the normal LocalGPT approval boundary.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The web search response produced by the operation.</returns>
    Task<WebSearchResponse> SearchAsync(WebSearchRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Implements one named public web-search provider.</summary>
public interface IWebSearchProvider
{
    /// <summary>
    /// Gets the stable key used to identify or correlate this web search instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="IWebSearchProvider"/>.</value>
    string Key { get; }
    /// <summary>
    /// Performs search for <see cref="IWebSearchProvider"/>, keeping the operation consistent with the state and invariants of the surrounding web search workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The web search response produced by the operation.</returns>
    Task<WebSearchResponse> SearchAsync(WebSearchRequest request, CancellationToken cancellationToken = default);
}

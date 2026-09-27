using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Searches approved public web providers through a bounded service boundary.</summary>
public interface IWebSearchService
{
    /// <summary>Searches the selected provider after the caller has passed the normal LocalGPT approval boundary.</summary>
    Task<WebSearchResponse> SearchAsync(WebSearchRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Implements one named public web-search provider.</summary>
public interface IWebSearchProvider
{
    /// <summary>Gets the stable provider key.</summary>
    string Key { get; }
    /// <summary>Searches this provider and returns bounded attributed evidence.</summary>
    Task<WebSearchResponse> SearchAsync(WebSearchRequest request, CancellationToken cancellationToken = default);
}

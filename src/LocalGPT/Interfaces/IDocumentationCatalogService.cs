using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Locates versioned DocFX artifacts and exposes searchable compiler-generated XML comments for the running LocalGPT build.
/// </summary>
[DocumentationUpdated("2.2.8")]
public interface IDocumentationCatalogService
{
    /// <summary>Returns availability, version and application-relative links for generated documentation.</summary>
    /// <returns>The LocalGPT documentation status produced by the operation.</returns>
    LocalGptDocumentationStatus GetStatus();

    /// <summary>Returns the absolute PDF path when a compatible generated documentation artifact exists.</summary>
    /// <returns>The string produced by the operation.</returns>
    string? GetPdfPath();

    /// <summary>Returns one generated HTML or supporting asset path from the selected documentation root.</summary>
    /// <param name="relativePath">Relative path value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string? GetHtmlFilePath(string? relativePath);

    /// <summary>Returns one localized XML documentation member by its stable compiler member identifier.</summary>
    /// <param name="memberId">Identifier of the member to use for this operation.</param>
    /// <param name="culture">Culture value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <returns>The LocalGPT documentation comment produced by the operation.</returns>
    LocalGptDocumentationComment? GetComment(string memberId, string? culture = null);

    /// <summary>Searches member identifiers, summaries and remarks without exposing arbitrary files.</summary>
    /// <param name="query">Query value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <param name="limit">Limit value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <param name="culture">Culture value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<LocalGptDocumentationComment> SearchComments(string? query, int limit = 100, string? culture = null);
}

/// <summary>
/// Adapts maintained XML comment text to the active localization catalog while preserving the original text as fallback.
/// </summary>
[DocumentationUpdated("2.1.20")]
public interface IDocumentationTranslationAdapter
{
    /// <summary>Returns a localized copy of one documentation comment for the requested culture.</summary>
    /// <param name="comment">Comment value supplied to the documentation translation adapter operation and used when producing its result.</param>
    /// <param name="culture">Culture value supplied to the documentation translation adapter operation and used when producing its result.</param>
    /// <returns>The LocalGPT documentation comment produced by the operation.</returns>
    LocalGptDocumentationComment Adapt(LocalGptDocumentationComment comment, string? culture = null);
}


/// <summary>Coordinates accessible in-application documentation viewing for one LocalGPT circuit.</summary>
[DocumentationUpdated("2.3.6")]
public interface IDocumentationViewerService
{
    /// <summary>Raised when the viewer state changes.</summary>
    event Action? StateChanged;

    /// <summary>Gets the current scoped viewer state.</summary>
    /// <value>The state value exposed by <see cref="IDocumentationViewerService"/>.</value>
    LocalGptDocumentationViewerState State { get; }

    /// <summary>
    /// Performs open as part of the documentation viewer service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    void Open(LocalGptDocumentationViewerRequest request);

    /// <summary>
    /// Performs close as part of the documentation viewer service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    void Close();
}

using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Locates versioned DocFX artifacts and exposes searchable compiler-generated XML comments for the running LocalGPT build.
/// </summary>
[DocumentationUpdated("2.2.6")]
public interface IDocumentationCatalogService
{
    /// <summary>Returns availability, version and application-relative links for generated documentation.</summary>
    LocalGptDocumentationStatus GetStatus();

    /// <summary>Returns the absolute PDF path when a compatible generated documentation artifact exists.</summary>
    string? GetPdfPath();

    /// <summary>Returns one generated HTML or supporting asset path from the selected documentation root.</summary>
    string? GetHtmlFilePath(string? relativePath);

    /// <summary>Returns one localized XML documentation member by its stable compiler member identifier.</summary>
    LocalGptDocumentationComment? GetComment(string memberId, string? culture = null);

    /// <summary>Searches member identifiers, summaries and remarks without exposing arbitrary files.</summary>
    IReadOnlyList<LocalGptDocumentationComment> SearchComments(string? query, int limit = 100, string? culture = null);
}

/// <summary>
/// Adapts maintained XML comment text to the active localization catalog while preserving the original text as fallback.
/// </summary>
[DocumentationUpdated("2.1.20")]
public interface IDocumentationTranslationAdapter
{
    /// <summary>Returns a localized copy of one documentation comment for the requested culture.</summary>
    LocalGptDocumentationComment Adapt(LocalGptDocumentationComment comment, string? culture = null);
}

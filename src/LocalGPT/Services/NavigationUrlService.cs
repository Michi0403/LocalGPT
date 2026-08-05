using LocalGPT.BusinessObjects;
using System.Web;

namespace LocalGPT.Services;

/// <summary>
/// Preserves the shell sidebar state while creating application-relative navigation URLs.
/// </summary>
/// <param name="logger">Writes bounded navigation diagnostics.</param>
[DocumentationUpdated("2.1.20")]
public sealed class NavigationUrlService(ILogger<NavigationUrlService> logger)
{
    /// <summary>Gets the query-string key used for the sidebar state.</summary>
    public const string ToggleSidebarName = "toggledSidebar";

    /// <summary>
    /// Adds the current sidebar state to an application-relative URL while preserving an existing query string.
    /// </summary>
    /// <param name="baseUrl">Application-relative route, optionally containing a query string.</param>
    /// <param name="toggledSidebar">Current sidebar expansion state.</param>
    /// <returns>The route with a URL-encoded sidebar query value.</returns>
    public string GetUrl(string baseUrl, bool toggledSidebar)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
            var separator = baseUrl.Contains('?') ? '&' : '?';
            var result = $"{baseUrl}{separator}{ToggleSidebarName}={Uri.EscapeDataString(toggledSidebar.ToString())}";
            logger.LogTrace("Created a navigation URL with sidebar state {ToggledSidebar}.", toggledSidebar);
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create a navigation URL from a base URL.");
            throw;
        }
    }

    /// <summary>
    /// Adds the current sidebar state to an absolute or return URL and preserves its existing query values.
    /// </summary>
    /// <param name="toggledSidebar">Current sidebar expansion state.</param>
    /// <param name="returnUrl">Absolute or application return URL.</param>
    /// <returns>The normalized return URL with sidebar state.</returns>
    public string GetUrl(bool toggledSidebar, string returnUrl)
    {
        try
        {
            var baseUriBuilder = new UriBuilder(returnUrl);
            var query = HttpUtility.ParseQueryString(baseUriBuilder.Query);
            query[ToggleSidebarName] = toggledSidebar.ToString();
            baseUriBuilder.Query = query.ToString();
            var result = baseUriBuilder.Uri.IsAbsoluteUri
                ? baseUriBuilder.Uri.PathAndQuery + baseUriBuilder.Uri.Fragment
                : baseUriBuilder.Uri.ToString();
            logger.LogTrace("Created a return navigation URL with sidebar state {ToggledSidebar}.", toggledSidebar);
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create a navigation URL from the supplied return URL.");
            throw;
        }
    }
}

using System.Web;

namespace LocalGPT.Services;

public sealed class NavigationUrlService(ILogger<NavigationUrlService> logger)
{
    public const string ToggleSidebarName = "toggledSidebar";

    public string GetUrl(string baseUrl, bool toggledSidebar)
    {
        try
        {
            var result = $"{baseUrl}?{ToggleSidebarName}={toggledSidebar}";
            logger.LogTrace("Created a navigation URL with sidebar state {ToggledSidebar}.", toggledSidebar);
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create a navigation URL from a base URL.");
            throw;
        }
    }

    public string GetUrl(bool toggledSidebar, string returnUrl)
    {
        try
        {
            var baseUriBuilder = new UriBuilder(returnUrl);
            var query = HttpUtility.ParseQueryString(baseUriBuilder.Query);
            var baseUrl = baseUriBuilder.Fragment + baseUriBuilder.Host + baseUriBuilder.Path;
            var result = $"{baseUrl}?{ToggleSidebarName}={toggledSidebar}&{query}";
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

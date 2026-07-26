using System.Web;

namespace LocalGPT.Services
{
    public sealed class NavigationUrlService
    {
        public const string ToggleSidebarName = "toggledSidebar";

        public string GetUrl(string baseUrl, bool toggledSidebar)
        {
            return $"{baseUrl}?{ToggleSidebarName}={toggledSidebar}";
        }
        public string GetUrl(bool toggledSidebar, string returnUrl)
        {
            var baseUriBuilder = new UriBuilder(returnUrl);
            var query = HttpUtility.ParseQueryString(baseUriBuilder.Query);
            var baseUrl = baseUriBuilder.Fragment + baseUriBuilder.Host + baseUriBuilder.Path;

            return $"{baseUrl}?{ToggleSidebarName}={toggledSidebar}&{query}";
        }
    }
}
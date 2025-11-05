using System.Text;
namespace LocalGPT.Helper
{
    public static class UriBuilderHelper
    {
        /// <summary>
        /// Builds a full absolute URI based on the application's Kestrel configuration and a relative path.
        /// </summary>
        /// <param name="relativePath">A path like "chathub" or "api/messages"</param>
        /// <returns>A combined Uri</returns>
        public static Uri BuildAbsoluteUriFromConfig( string relativePath)
        {
            var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
            var baseUrl = $"http://localhost:{port}";

            //ArgumentNullException.ThrowIfNull(config.Kestrel);
            //ArgumentNullException.ThrowIfNull(config.Kestrel.Endpoints);
            //ArgumentNullException.ThrowIfNull(config.Kestrel.Endpoints.Http);

            //var baseUrl = config.Kestrel.Endpoints.Http.Url;

            //if (string.IsNullOrWhiteSpace(baseUrl))
            //    throw new InvalidOperationException("Kestrel base URL is not configured.");

            var normalizedBase = baseUrl.Replace("0.0.0.0", "localhost").TrimEnd('/');
            var combined = string.IsNullOrWhiteSpace(relativePath)
                ? normalizedBase
                : $"{normalizedBase}/{relativePath.TrimStart('/')}";

            return new Uri(combined, UriKind.Absolute);
        }
        public static string BuildODataQuery(params (string Key, string? Value)[] parts)
        {
            var sb = new StringBuilder();
            var first = true;
            foreach (var (key, value) in parts)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (!first) sb.Append('&'); else first = false;

                // IMPORTANT: don't encode the key ($filter/$select/$expand)
                sb.Append(key);
                sb.Append('=');

                // IMPORTANT: encode only the value
                sb.Append(Uri.EscapeDataString(value));
            }
            return sb.ToString();
        }
    }
}

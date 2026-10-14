using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Routes public web searches to the configured provider without granting the model unrestricted network access.</summary>
/// <param name="providers">Web search provider dependency used by the web search workflow to provide the corresponding application capability.</param>
/// <param name="runtimePolicy">Local gpt runtime policy data service dependency used by the web search workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class WebSearchService(IEnumerable<IWebSearchProvider> providers, ILocalGptRuntimePolicyDataService runtimePolicy, ILogger<WebSearchService> logger) : IWebSearchService
{
    /// <summary>
    /// Performs search as part of the web search service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<WebSearchResponse> SearchAsync(WebSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var query = (request.Query ?? string.Empty).Trim();
            if (query.Length is < 1 or > 500)
                throw new ArgumentException("Web search queries must contain between 1 and 500 characters.", nameof(request));

            var providerKey = string.IsNullOrWhiteSpace(request.Provider) ? "duckduckgo" : request.Provider.Trim();
            var provider = providers.FirstOrDefault(candidate => string.Equals(candidate.Key, providerKey, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Web search provider '{providerKey}' is not configured.");
            request.Query = query;
            request.Provider = provider.Key;
            request.MaximumResults = Math.Clamp(request.MaximumResults, 1, runtimePolicy.GetJson<ServiceQueryRuntimeParameters>(LocalGptRuntimeValue.ServiceQueryRuntimeParametersJson).WebSearchMaximumResults);
            return await provider.SearchAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Web search routing was cancelled.");
            else
                logger.LogError(exception, "Web search routing failed; query text and provider response content were omitted.");
            throw;
        }
    }
}

/// <summary>Uses DuckDuckGo's public Instant Answer API as the default attributed web-search provider.</summary>
/// <param name="httpClientFactory">Http client factory dependency used by the duck duck go web search workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class DuckDuckGoWebSearchProvider(IHttpClientFactory httpClientFactory, ILogger<DuckDuckGoWebSearchProvider> logger) : IWebSearchProvider
{
    /// <summary>
    /// Gets the stable key used to identify or correlate this duck duck go web search instance with related application state.
    /// </summary>
    /// <inheritdoc />
    public string Key => "duckduckgo";

    /// <summary>
    /// Performs search for <see cref="DuckDuckGoWebSearchProvider"/>, keeping the operation consistent with the state and invariants of the surrounding duck duck go web search workflow.
    /// </summary>
    /// <inheritdoc />
    public async Task<WebSearchResponse> SearchAsync(WebSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var route = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(request.Query)}&format=json&no_html=1&no_redirect=1&skip_disambig=0";
            using var message = new HttpRequestMessage(HttpMethod.Get, route);
            message.Headers.UserAgent.ParseAdd("LocalGPT/4.9.4");
            var client = httpClientFactory.CreateClient("LocalGPTWebSearch");
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var root = document.RootElement;
            var result = new WebSearchResponse
            {
                Provider = Key,
                Query = request.Query,
                Answer = ReadString(root, "Answer"),
                Abstract = ReadString(root, "AbstractText"),
                AbstractUrl = ReadString(root, "AbstractURL"),
                ProviderNote = "DuckDuckGo Instant Answer API evidence. It can return direct/related answers and attributed URLs, but it is not a complete organic-results API. Use localai.web.extract on an attributed URL when rendered page evidence is needed."
            };

            if (!string.IsNullOrWhiteSpace(result.Abstract))
            {
                result.Results.Add(new WebSearchItem
                {
                    Title = ReadString(root, "Heading"),
                    Url = result.AbstractUrl,
                    Summary = result.Abstract,
                    Category = "abstract"
                });
            }

            if (root.TryGetProperty("RelatedTopics", out var related) && related.ValueKind == JsonValueKind.Array)
                AddRelatedTopics(related, result.Results, request.MaximumResults);

            if (result.Results.Count > request.MaximumResults)
                result.Results = result.Results.Take(request.MaximumResults).ToList();

            logger.LogInformation("DuckDuckGo web search returned {Count} bounded attributed item(s); query and response content were omitted.", result.Results.Count);
            return result;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "DuckDuckGo web search was cancelled.");
            else
                logger.LogError(exception, "DuckDuckGo web search failed; query and provider response content were omitted.");
            throw;
        }
    }

    /// <summary>
    /// Adds related topics for <see cref="DuckDuckGoWebSearchProvider"/>, keeping the operation consistent with the state and invariants of the surrounding duck duck go web search workflow.
    /// </summary>
    /// <param name="related">Related value supplied to the duck duck go web search operation and used when producing its result.</param>
    /// <param name="results">Results value supplied to the duck duck go web search operation and used when producing its result.</param>
    /// <param name="maximumResults">Maximum results value supplied to the duck duck go web search operation and used when producing its result.</param>
    private void AddRelatedTopics(JsonElement related, List<WebSearchItem> results, int maximumResults)
    {
        try
        {
            foreach (var item in related.EnumerateArray())
            {
                if (results.Count >= maximumResults)
                    return;
                if (item.TryGetProperty("Topics", out var nested) && nested.ValueKind == JsonValueKind.Array)
                {
                    AddRelatedTopics(nested, results, maximumResults);
                    continue;
                }

                var text = ReadString(item, "Text");
                var url = ReadString(item, "FirstURL");
                if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(url))
                    continue;
                results.Add(new WebSearchItem
                {
                    Title = text,
                    Url = url,
                    Summary = text,
                    Category = "related"
                });
            }
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "DuckDuckGo related-topic parsing was cancelled.");
            else
                logger.LogError(exception, "DuckDuckGo related-topic parsing failed; provider content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Reads string for <see cref="DuckDuckGoWebSearchProvider"/>, keeping the operation consistent with the state and invariants of the surrounding duck duck go web search workflow.
    /// </summary>
    /// <param name="element">Element value supplied to the duck duck go web search operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the duck duck go web search operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ReadString(JsonElement element, string name)
    {
        try
        {
            return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "DuckDuckGo string parsing was cancelled.");
            else
                logger.LogError(exception, "DuckDuckGo string parsing failed; provider content was omitted.");
            throw;
        }
    }
}

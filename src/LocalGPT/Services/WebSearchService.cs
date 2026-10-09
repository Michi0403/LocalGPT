using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Routes public web searches to the configured provider without granting the model unrestricted network access.</summary>
public sealed class WebSearchService(IEnumerable<IWebSearchProvider> providers, ILocalGptRuntimePolicyDataService runtimePolicy, ILogger<WebSearchService> logger) : IWebSearchService
{
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
public sealed class DuckDuckGoWebSearchProvider(IHttpClientFactory httpClientFactory, ILogger<DuckDuckGoWebSearchProvider> logger) : IWebSearchProvider
{
    /// <inheritdoc />
    public string Key => "duckduckgo";

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

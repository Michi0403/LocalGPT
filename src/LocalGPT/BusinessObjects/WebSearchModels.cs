namespace LocalGPT.BusinessObjects;

/// <summary>Describes one user-approved public web search request.</summary>
public sealed class WebSearchRequest
{
    /// <summary>Gets or sets the query sent to the configured search provider.</summary>
    /// <value>The query value exposed by <see cref="WebSearchRequest"/>.</value>
    public string Query { get; set; } = string.Empty;
    /// <summary>Gets or sets the provider key. DuckDuckGo is the shipped default.</summary>
    /// <value>The provider value exposed by <see cref="WebSearchRequest"/>.</value>
    public string Provider { get; set; } = "duckduckgo";
    /// <summary>Gets or sets the maximum number of bounded result items returned to the caller.</summary>
    /// <value>The maximum results value exposed by <see cref="WebSearchRequest"/>.</value>
    public int MaximumResults { get; set; } = 8;
}

/// <summary>Represents one attributed public web search result item.</summary>
public sealed class WebSearchItem
{
    /// <summary>
    /// Gets or sets the title value that forms part of the web search item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="WebSearchItem"/>.</value>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the attributed public source URL when supplied by the provider.</summary>
    /// <value>The URL value exposed by <see cref="WebSearchItem"/>.</value>
    public string Url { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded provider summary associated with the item.</summary>
    /// <value>The summary value exposed by <see cref="WebSearchItem"/>.</value>
    public string Summary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the category value that forms part of the web search item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The category value exposed by <see cref="WebSearchItem"/>.</value>
    public string Category { get; set; } = string.Empty;
}

/// <summary>Represents one attributed response from a public search provider.</summary>
public sealed class WebSearchResponse
{
    /// <summary>
    /// Gets or sets the provider value that forms part of the web search state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The provider value exposed by <see cref="WebSearchResponse"/>.</value>
    public string Provider { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the query value that forms part of the web search state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The query value exposed by <see cref="WebSearchResponse"/>.</value>
    public string Query { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the answer value that forms part of the web search state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The answer value exposed by <see cref="WebSearchResponse"/>.</value>
    public string Answer { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the abstract value that forms part of the web search state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The abstract value exposed by <see cref="WebSearchResponse"/>.</value>
    public string Abstract { get; set; } = string.Empty;
    /// <summary>Gets or sets the source URL for the provider abstract when available.</summary>
    /// <value>The abstract URL value exposed by <see cref="WebSearchResponse"/>.</value>
    public string AbstractUrl { get; set; } = string.Empty;
    /// <summary>Gets or sets bounded attributed related results returned by the provider.</summary>
    /// <value>The results value exposed by <see cref="WebSearchResponse"/>.</value>
    public List<WebSearchItem> Results { get; set; } = [];
    /// <summary>Gets or sets a provider capability note that callers must preserve when interpreting the response.</summary>
    /// <value>The provider note value exposed by <see cref="WebSearchResponse"/>.</value>
    public string ProviderNote { get; set; } = string.Empty;
}

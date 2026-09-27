namespace LocalGPT.BusinessObjects;

/// <summary>Describes one user-approved public web search request.</summary>
public sealed class WebSearchRequest
{
    /// <summary>Gets or sets the query sent to the configured search provider.</summary>
    public string Query { get; set; } = string.Empty;
    /// <summary>Gets or sets the provider key. DuckDuckGo is the shipped default.</summary>
    public string Provider { get; set; } = "duckduckgo";
    /// <summary>Gets or sets the maximum number of bounded result items returned to the caller.</summary>
    public int MaximumResults { get; set; } = 8;
}

/// <summary>Represents one attributed public web search result item.</summary>
public sealed class WebSearchItem
{
    /// <summary>Gets or sets the human-readable result title or text.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the attributed public source URL when supplied by the provider.</summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded provider summary associated with the item.</summary>
    public string Summary { get; set; } = string.Empty;
    /// <summary>Gets or sets the provider result category.</summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>Represents one attributed response from a public search provider.</summary>
public sealed class WebSearchResponse
{
    /// <summary>Gets or sets the provider that produced this response.</summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>Gets or sets the original query.</summary>
    public string Query { get; set; } = string.Empty;
    /// <summary>Gets or sets a direct provider answer when available.</summary>
    public string Answer { get; set; } = string.Empty;
    /// <summary>Gets or sets an attributed provider abstract when available.</summary>
    public string Abstract { get; set; } = string.Empty;
    /// <summary>Gets or sets the source URL for the provider abstract when available.</summary>
    public string AbstractUrl { get; set; } = string.Empty;
    /// <summary>Gets or sets bounded attributed related results returned by the provider.</summary>
    public List<WebSearchItem> Results { get; set; } = [];
    /// <summary>Gets or sets a provider capability note that callers must preserve when interpreting the response.</summary>
    public string ProviderNote { get; set; } = string.Empty;
}

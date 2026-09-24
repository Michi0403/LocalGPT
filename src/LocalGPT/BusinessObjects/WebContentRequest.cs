namespace LocalGPT.BusinessObjects;

/// <summary>A reviewable bounded browser visit; page text is evidence, never executable instructions.</summary>
public sealed class WebContentRequest
{
    /// <summary>
    /// Gets or sets the URL that identifies the network or application endpoint associated with this web content state.
    /// </summary>
    /// <value>The URL value exposed by <see cref="WebContentRequest"/>.</value>
    public string Url { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the wait for selector value that forms part of the web content state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The wait for selector value exposed by <see cref="WebContentRequest"/>.</value>
    public string WaitForSelector { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the reveal selectors collection maintained or exposed by this web content instance for downstream processing.
    /// </summary>
    /// <value>The reveal selectors value exposed by <see cref="WebContentRequest"/>.</value>
    public List<string> RevealSelectors { get; set; } = [];
    /// <summary>
    /// Gets or sets the scroll steps value that forms part of the web content state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scroll steps value exposed by <see cref="WebContentRequest"/>.</value>
    public int ScrollSteps { get; set; } = 3;
    /// <summary>
    /// Gets or sets the maximum characters value that forms part of the web content state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum characters value exposed by <see cref="WebContentRequest"/>.</value>
    public int MaximumCharacters { get; set; } = 24000;
    /// <summary>
    /// Gets or sets the timeout seconds value that forms part of the web content state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The timeout seconds value exposed by <see cref="WebContentRequest"/>.</value>
    public int TimeoutSeconds { get; set; } = 30;
}

namespace LocalGPT.BusinessObjects;

/// <summary>
/// One runtime regular expression as loaded from the database (RegexPatterns), keyed by <see cref="Name"/>.
/// </summary>
public sealed record LocalGptRuntimeRegexDefinition
{
    /// <summary>Gets or sets the database key of the pattern.</summary>
    /// <value>RegexPatterns.Name.</value>
    public string Name { get; init; } = string.Empty;
    /// <summary>Gets or sets the pattern text.</summary>
    /// <value>RegexPatterns.Pattern.</value>
    public string Pattern { get; init; } = string.Empty;
    /// <summary>Gets or sets the comma-separated regex flags.</summary>
    /// <value>RegexPatterns.Flags.</value>
    public string Flags { get; init; } = string.Empty;
    /// <summary>Gets or sets when the row was last updated.</summary>
    /// <value>RegexPatterns.UpdatedOn.</value>
    public DateTime UpdatedOn { get; init; }
}

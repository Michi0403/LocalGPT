namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a LocalGPT runtime policy snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed record LocalGptRuntimePolicySnapshot
{
    /// <summary>
    /// Gets or sets the values collection maintained or exposed by this LocalGPT runtime policy snapshot instance for downstream processing.
    /// </summary>
    /// <value>The values value exposed by <see cref="LocalGptRuntimePolicySnapshot"/>.</value>
    public IReadOnlyDictionary<string, string> Values { get; init; } = new Dictionary<string, string>();
    /// <summary>
    /// Gets or sets the collections collection maintained or exposed by this LocalGPT runtime policy snapshot instance for downstream processing.
    /// </summary>
    /// <value>The collections value exposed by <see cref="LocalGptRuntimePolicySnapshot"/>.</value>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Collections { get; init; } = new Dictionary<string, IReadOnlyList<string>>();
    /// <summary>
    /// Gets or sets the regex patterns collection maintained or exposed by this LocalGPT runtime policy snapshot instance for downstream processing.
    /// </summary>
    /// <value>The regex patterns value exposed by <see cref="LocalGptRuntimePolicySnapshot"/>.</value>
    public IReadOnlyDictionary<string, string> RegexPatterns { get; init; } = new Dictionary<string, string>();
}

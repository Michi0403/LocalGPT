namespace LocalGPT.BusinessObjects;

/// <summary>
/// The runtime policy as defined by the database: values, collections and regular expressions keyed by their
/// database names (case-insensitive).
/// </summary>
public sealed record LocalGptRuntimePolicyDefinition
{
    /// <summary>Gets or sets the runtime values keyed by SystemVariables.Name.</summary>
    /// <value>Raw value strings.</value>
    public IReadOnlyDictionary<string, string> Values { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    /// <summary>Gets or sets the runtime collections keyed by SystemVariables.Name.</summary>
    /// <value>Collection members.</value>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Collections { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
    /// <summary>Gets or sets the runtime regular expressions keyed by RegexPatterns.Name.</summary>
    /// <value>Pattern definitions.</value>
    public IReadOnlyDictionary<string, LocalGptRuntimeRegexDefinition> RegexPatterns { get; init; } =
        new Dictionary<string, LocalGptRuntimeRegexDefinition>(StringComparer.OrdinalIgnoreCase);
}

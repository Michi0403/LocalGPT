namespace LocalGPT.BusinessObjects;

/// <summary>
/// The authoritative default runtime policy. It only provides defaults for database rows; the database
/// (SystemVariables and RegexPatterns) defines which keys exist at runtime.
/// </summary>
public sealed record LocalGptRuntimePolicySeedModel
{
    /// <summary>Database name of the value holding the LocalGPT core project identifier.</summary>
    public const string CoreProjectIdKey = "LocalGptCoreProjectId";

    /// <summary>CLR type name stored with runtime collections in SystemVariables.DataType.</summary>
    public const string CollectionDataType = "System.String[]";

    /// <summary>
    /// Gets the LocalGPT core project identifier from the seeded value <see cref="CoreProjectIdKey"/>.
    /// </summary>
    /// <value>The seeded core project identifier.</value>
    public Guid LocalGptCoreProjectId => Guid.Parse(Values.Single(item =>
        string.Equals(item.Name, CoreProjectIdKey, StringComparison.OrdinalIgnoreCase)).Value);
    /// <summary>Gets or sets the seeded runtime values.</summary>
    /// <value>One row per runtime value.</value>
    public IReadOnlyList<LocalGptRuntimeSystemVariableSeed> Values { get; init; } = [];
    /// <summary>Gets or sets the seeded runtime collections.</summary>
    /// <value>One row per runtime collection.</value>
    public IReadOnlyList<LocalGptRuntimeCollectionSeed> Collections { get; init; } = [];
    /// <summary>Gets or sets the seeded runtime regular expressions.</summary>
    /// <value>One row per runtime pattern.</value>
    public IReadOnlyList<LocalGptRuntimeRegexSeed> RegexPatterns { get; init; } = [];
    /// <summary>
    /// Gets every seeded SystemVariables row: the values plus each collection serialized as a JSON string array.
    /// </summary>
    /// <value>Rows written to SystemVariables when they are missing.</value>
    public IReadOnlyList<LocalGptRuntimeSystemVariableSeed> SystemVariables => [.. Values, .. Collections.Select(item =>
        new LocalGptRuntimeSystemVariableSeed(item.Name, System.Text.Json.JsonSerializer.Serialize(item.Values), CollectionDataType))];
}

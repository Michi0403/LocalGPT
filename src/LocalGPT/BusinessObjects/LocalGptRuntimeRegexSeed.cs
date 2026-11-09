namespace LocalGPT.BusinessObjects;

/// <summary>
/// Seed row of one runtime regular expression, keyed by its database name (RegexPatterns.Name).
/// </summary>
/// <param name="Name">Database key of the pattern.</param>
/// <param name="Pattern">Default pattern text.</param>
/// <param name="Flags">Comma-separated regex flags (i, m, s, c, compiled).</param>
public sealed record LocalGptRuntimeRegexSeed(string Name, string Pattern, string Flags);

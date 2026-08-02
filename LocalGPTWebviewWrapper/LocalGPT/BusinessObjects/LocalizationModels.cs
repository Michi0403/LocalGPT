namespace LocalGPT.BusinessObjects;

/// <summary>
/// Describes a built-in or user-supplied LocalGPT localization catalog.
/// </summary>
/// <param name="Culture">Normalized .NET culture name represented by the catalog.</param>
/// <param name="IsBuiltIn">Indicates whether LocalGPT ships a catalog for the culture.</param>
/// <param name="HasUserOverride">Indicates whether a persistent user catalog augments or overrides the built-in catalog.</param>
/// <param name="StringCount">Number of effective localized strings after fallback and overrides are merged.</param>
/// <param name="DisplayName">Culture display name resolved by the local .NET runtime.</param>
[DocumentationUpdated("2.1.21")]
public sealed record LocalizationCatalogDescriptor(
    string Culture,
    bool IsBuiltIn,
    bool HasUserOverride,
    int StringCount,
    string DisplayName);

/// <summary>
/// Reports the result of validating a user-provided localization JSON catalog.
/// </summary>
[DocumentationUpdated("2.1.21")]
public sealed class LocalizationCatalogValidationResult
{
    /// <summary>Gets or sets whether the culture name and JSON dictionary are valid.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets or sets the normalized culture name.</summary>
    public string Culture { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of non-empty localization keys in the submitted catalog.</summary>
    public int StringCount { get; set; }

    /// <summary>Gets or sets the number of English baseline keys absent from the submitted catalog.</summary>
    public int MissingBaselineKeyCount { get; set; }

    /// <summary>Gets or sets validation warnings that do not prevent import.</summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>Gets or sets validation errors that prevent import.</summary>
    public List<string> Errors { get; set; } = [];
}

/// <summary>
/// Reports the durable result of importing a localization catalog.
/// </summary>
/// <param name="Culture">Normalized culture name written by the import.</param>
/// <param name="StringCount">Number of submitted strings.</param>
/// <param name="MissingBaselineKeyCount">Number of English baseline keys supplied through fallback.</param>
[DocumentationUpdated("2.1.21")]
public sealed record LocalizationCatalogImportResult(
    string Culture,
    int StringCount,
    int MissingBaselineKeyCount);

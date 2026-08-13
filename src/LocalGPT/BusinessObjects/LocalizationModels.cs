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
    /// <value>The is valid value exposed by <see cref="LocalizationCatalogValidationResult"/>.</value>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the culture value that forms part of the localization catalog validation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The culture value exposed by <see cref="LocalizationCatalogValidationResult"/>.</value>
    public string Culture { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of non-empty localization keys in the submitted catalog.</summary>
    /// <value>The string count value exposed by <see cref="LocalizationCatalogValidationResult"/>.</value>
    public int StringCount { get; set; }

    /// <summary>Gets or sets the number of English baseline keys absent from the submitted catalog.</summary>
    /// <value>The missing baseline key count value exposed by <see cref="LocalizationCatalogValidationResult"/>.</value>
    public int MissingBaselineKeyCount { get; set; }

    /// <summary>
    /// Gets or sets the warnings collection maintained or exposed by this localization catalog validation instance for downstream processing.
    /// </summary>
    /// <value>The warnings value exposed by <see cref="LocalizationCatalogValidationResult"/>.</value>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// Gets or sets the errors collection maintained or exposed by this localization catalog validation instance for downstream processing.
    /// </summary>
    /// <value>The errors value exposed by <see cref="LocalizationCatalogValidationResult"/>.</value>
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

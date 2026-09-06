using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Localization;

/// <summary>
/// Reads built-in localization catalogs and persistent user-supplied culture overrides.
/// </summary>
[DocumentationUpdated("2.2.8")]
public interface ILocalGptLocalizationService
{
    /// <summary>
    /// Retrieves available cultures as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<string> GetAvailableCultures();

    /// <summary>
    /// Retrieves catalogs as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<LocalizationCatalogDescriptor> GetCatalogs();

    /// <summary>Gets the effective strings for a culture after English fallback and user overrides are merged.</summary>
    /// <param name="culture">Culture value supplied to the LocalGPT localization operation and used when producing its result.</param>
    /// <returns>The i read only dictionary string string produced by the operation.</returns>
    IReadOnlyDictionary<string, string> GetStrings(string? culture = null);

    /// <summary>Gets one localized value with English and caller-provided fallback behavior.</summary>
    /// <param name="key">Key value supplied to the LocalGPT localization operation and used when producing its result.</param>
    /// <param name="culture">Culture value supplied to the LocalGPT localization operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the LocalGPT localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string Get(string key, string? culture = null, string? fallback = null);

    /// <summary>Gets a localized UI sentence by deriving its maintained text-catalog key.</summary>
    /// <param name="source">English source text used as the fallback and stable catalog-key source.</param>
    /// <param name="culture">Optional requested culture; the current request culture is used when omitted.</param>
    /// <returns>The localized sentence, or the source text when no catalog value exists.</returns>
    string GetText(string source, string? culture = null);

    /// <summary>
    /// Resolves available culture as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="culture">Requested culture name.</param>
    /// <returns>The normalized available culture or en-US.</returns>
    string ResolveAvailableCulture(string? culture);

    /// <summary>
    /// Builds culture return URL as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="absoluteUri">Current absolute application URI.</param>
    /// <returns>A local path and query suitable for culture selection.</returns>
    string BuildCultureReturnUrl(string absoluteUri);

    /// <summary>Adds an explicit request culture to one validated local return URL.</summary>
    /// <param name="returnUrl">Local path and query.</param>
    /// <param name="culture">Requested available culture.</param>
    /// <returns>A local redirect URL carrying culture and UI-culture values.</returns>
    string BuildCultureRedirectUrl(string? returnUrl, string culture);

    /// <summary>Builds the absolute application endpoint used to select and persist one culture.</summary>
    /// <param name="absoluteUri">Current absolute application URI.</param>
    /// <param name="culture">Requested available culture.</param>
    /// <returns>A local culture-selection endpoint URL that performs a full page reload.</returns>
    string BuildCultureSelectionUrl(string absoluteUri, string culture);

    /// <summary>
    /// Validates catalog as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="culture">Requested .NET culture name, for example fr-FR.</param>
    /// <param name="json">UTF-8 JSON object containing string keys and values.</param>
    /// <returns>The validation result, including missing baseline keys.</returns>
    LocalizationCatalogValidationResult ValidateCatalog(string culture, string json);

    /// <summary>Formats validation errors for a UI or API status surface.</summary>
    /// <param name="validation">Validation result containing zero or more errors.</param>
    /// <returns>A single bounded human-readable error sentence.</returns>
    string FormatValidationErrors(LocalizationCatalogValidationResult validation);

    /// <summary>
    /// Imports catalog as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="culture">Requested .NET culture name.</param>
    /// <param name="json">JSON object containing string keys and values.</param>
    /// <param name="overwrite">Allows replacement of an existing user catalog.</param>
    /// <param name="cancellationToken">Cancels the asynchronous file write.</param>
    /// <returns>A task that completes with the durable import result.</returns>
    Task<LocalizationCatalogImportResult> ImportCatalogAsync(string culture, string json, bool overwrite, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implements open LocalGPT localization catalogs with English fallback, built-in defaults and persistent user overrides.
/// </summary>
/// <param name="environment">Provides the application content root containing built-in catalogs.</param>
/// <param name="logger">Writes bounded catalog discovery and validation diagnostics.</param>
/// <param name="runtimePolicy">Local gpt runtime policy data service dependency used by the local GPT localization workflow to provide the corresponding application capability.</param>
[DocumentationUpdated("2.2.8")]
public sealed class LocalGptLocalizationService(
    IWebHostEnvironment environment,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<LocalGptLocalizationService> logger) : ILocalGptLocalizationService
{

    /// <summary>Caches effective merged catalogs by normalized culture name.</summary>
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the built in localization path used by this LocalGPT localization instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The built in localization path value exposed by <see cref="LocalGptLocalizationService"/>.</value>
    private string BuiltInLocalizationPath => Path.Combine(environment.ContentRootPath, "Localization");

    /// <summary>
    /// Gets the user localization path used by this LocalGPT localization instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The user localization path value exposed by <see cref="LocalGptLocalizationService"/>.</value>
    private string UserLocalizationPath => LocalGptApplicationDataPaths.ResolveUserPath("Localization");

    /// <summary>
    /// Retrieves available cultures as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public IReadOnlyList<string> GetAvailableCultures()
    {
        try
        {
            var cultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "en-US", "de-DE" };
            AddCatalogCultures(BuiltInLocalizationPath, cultures);
            AddCatalogCultures(UserLocalizationPath, cultures);
            return cultures.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not enumerate LocalGPT localization catalogs; using built-in defaults.");
            return ["de-DE", "en-US"];
        }
    }

    /// <summary>
    /// Retrieves catalogs as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public IReadOnlyList<LocalizationCatalogDescriptor> GetCatalogs()
    {
    try
    {
            return GetAvailableCultures()
                .Select(culture => new LocalizationCatalogDescriptor(
                    culture,
                    File.Exists(GetBuiltInCatalogPath(culture)),
                    File.Exists(GetUserCatalogPath(culture)),
                    GetStrings(culture).Count,
                    GetCultureDisplayName(culture)))
                .ToArray();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(GetCatalogs)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(GetCatalogs)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves strings as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> GetStrings(string? culture = null)
    {
    try
    {
            var normalized = NormalizeCulture(culture);
            return cache.GetOrAdd(normalized, Load);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(GetStrings)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(GetStrings)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs get as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public string Get(string key, string? culture = null, string? fallback = null)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(key)) return fallback ?? string.Empty;
            if (GetStrings(culture).TryGetValue(key, out var value)) return value;
            if (GetStrings("en-US").TryGetValue(key, out value)) return value;
            return fallback ?? key;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(Get)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(Get)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves text as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public string GetText(string source, string? culture = null)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(source)) return string.Empty;
            var key = "Text." + source.Replace(" ", "␠", StringComparison.Ordinal);
            return Get(key, culture, source);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(GetText)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(GetText)} failed.");
        throw;
    }
}


    /// <summary>
    /// Resolves available culture as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public string ResolveAvailableCulture(string? culture)
    {
    try
    {
            var normalized = NormalizeCulture(culture);
            return GetAvailableCultures().FirstOrDefault(item =>
                string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase)) ?? "en-US";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(ResolveAvailableCulture)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(ResolveAvailableCulture)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds culture return URL as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public string BuildCultureReturnUrl(string absoluteUri)
    {
    try
    {
            if (!Uri.TryCreate(absoluteUri, UriKind.Absolute, out var current)) return "/";
            return BuildCultureUrl(current.AbsolutePath, current.Query, null);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(BuildCultureReturnUrl)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(BuildCultureReturnUrl)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds culture redirect URL as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public string BuildCultureRedirectUrl(string? returnUrl, string culture)
    {
    try
    {
            var selected = ResolveAvailableCulture(culture);
            var local = string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith("/", StringComparison.Ordinal) || returnUrl.StartsWith("//", StringComparison.Ordinal)
                ? "/"
                : returnUrl;
            if (!Uri.TryCreate("http://localgpt.invalid" + local, UriKind.Absolute, out var parsed))
                return "/?culture=" + Uri.EscapeDataString(selected) + "&ui-culture=" + Uri.EscapeDataString(selected);
            return BuildCultureUrl(parsed.AbsolutePath, parsed.Query, selected);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(BuildCultureRedirectUrl)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(BuildCultureRedirectUrl)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds culture selection URL as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public string BuildCultureSelectionUrl(string absoluteUri, string culture)
    {
    try
    {
            var selected = ResolveAvailableCulture(culture);
            var returnUrl = BuildCultureReturnUrl(absoluteUri);
            var endpoint = QueryHelpers.AddQueryString("/api/localization/select", "culture", selected);
            return QueryHelpers.AddQueryString(endpoint, "returnUrl", returnUrl);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(BuildCultureSelectionUrl)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(BuildCultureSelectionUrl)} failed.");
        throw;
    }
}

    /// <summary>Builds one local route while preserving non-localization query values.</summary>
    /// <param name="absolutePath">Absolute path value supplied to the LocalGPT localization operation and used when producing its result.</param>
    /// <param name="query">Query value supplied to the LocalGPT localization operation and used when producing its result.</param>
    /// <param name="culture">Culture value supplied to the LocalGPT localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildCultureUrl(string absolutePath, string query, string? culture)
    {
    try
    {
            var result = string.IsNullOrWhiteSpace(absolutePath) ? "/" : absolutePath;
            foreach (var pair in QueryHelpers.ParseQuery(query))
            {
                if (string.Equals(pair.Key, "culture", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pair.Key, "ui-culture", StringComparison.OrdinalIgnoreCase))
                    continue;
                foreach (var value in pair.Value)
                {
                    if (value is null) continue;
                    result = QueryHelpers.AddQueryString(result, pair.Key, value);
                }
            }
            if (!string.IsNullOrWhiteSpace(culture))
            {
                result = QueryHelpers.AddQueryString(result, "culture", culture);
                result = QueryHelpers.AddQueryString(result, "ui-culture", culture);
            }
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(BuildCultureUrl)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(BuildCultureUrl)} failed.");
        throw;
    }
}

    /// <summary>
    /// Validates catalog as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public LocalizationCatalogValidationResult ValidateCatalog(string culture, string json)
    {
        var result = new LocalizationCatalogValidationResult();
        try
        {
            result.Culture = NormalizeRequiredCulture(culture);
            if (string.IsNullOrWhiteSpace(json))
            {
                result.Errors.Add("The localization catalog is empty.");
                return result;
            }
            if (Encoding.UTF8.GetByteCount(json) > Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.LocalizationMaximumCatalogBytes)))
            {
                result.Errors.Add($"The localization catalog exceeds the configured LocalizationMaximumCatalogBytes policy.");
                return result;
            }

            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (data is null)
            {
                result.Errors.Add("The localization catalog must be a JSON object containing string keys and values.");
                return result;
            }
            if (data.Count > Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.LocalizationMaximumCatalogEntries)))
                result.Errors.Add("The localization catalog exceeds the configured LocalizationMaximumCatalogEntries policy.");

            var invalidKeys = data.Keys.Count(string.IsNullOrWhiteSpace);
            if (invalidKeys > 0)
                result.Errors.Add("Localization keys may not be empty or whitespace.");
            var normalizedKeys = data.Keys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .ToArray();
            if (normalizedKeys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalizedKeys.Length)
                result.Errors.Add("Localization keys must remain unique after trimming and case-insensitive comparison.");

            result.StringCount = data.Count - invalidKeys;
            var baseline = LoadCatalogFile(GetBuiltInCatalogPath("en-US"));
            result.MissingBaselineKeyCount = baseline.Keys.Count(key => !data.ContainsKey(key));
            if (result.MissingBaselineKeyCount > 0)
                result.Warnings.Add($"{result.MissingBaselineKeyCount} English baseline key(s) are absent and will use fallback text.");
            if (result.StringCount == 0)
                result.Errors.Add("The localization catalog contains no usable strings.");

            result.IsValid = result.Errors.Count == 0;
        }
        catch (CultureNotFoundException)
        {
            result.Errors.Add("The supplied culture name is not recognized by the installed .NET runtime.");
        }
        catch (JsonException exception)
        {
            logger.LogInformation(exception, "A user localization catalog failed JSON validation.");
            result.Errors.Add("The localization catalog is not a valid string-to-string JSON object.");
        }
        return result;
    }

    /// <summary>
    /// Performs format validation errors as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public string FormatValidationErrors(LocalizationCatalogValidationResult validation)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(validation);
            return validation.Errors.Count == 0
                ? "The localization catalog did not provide a validation error."
                : string.Join(" ", validation.Errors.Take(20));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(FormatValidationErrors)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(FormatValidationErrors)} failed.");
        throw;
    }
}

    /// <summary>
    /// Imports catalog as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<LocalizationCatalogImportResult> ImportCatalogAsync(string culture, string json, bool overwrite, CancellationToken cancellationToken = default)
    {
    try
    {
            var validation = ValidateCatalog(culture, json);
            if (!validation.IsValid)
                throw new InvalidDataException(string.Join(" ", validation.Errors));

            Directory.CreateDirectory(UserLocalizationPath);
            var destination = GetUserCatalogPath(validation.Culture);
            if (File.Exists(destination) && !overwrite)
                throw new IOException($"A user localization catalog for {validation.Culture} already exists. Enable overwrite to replace it.");

            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? throw new InvalidDataException("The localization catalog contained no dictionary.");
            var normalized = parsed
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(pair => pair.Key.Trim(), pair => pair.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            var serialized = JsonSerializer.Serialize(normalized, new JsonSerializerOptions { WriteIndented = true });
            var temporary = destination + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                await File.WriteAllTextAsync(temporary, serialized, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
                File.Move(temporary, destination, overwrite);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }

            if (string.Equals(validation.Culture, "en-US", StringComparison.OrdinalIgnoreCase))
                cache.Clear();
            else
                cache.TryRemove(validation.Culture, out _);
            logger.LogInformation("Imported a persistent LocalGPT localization catalog for culture {Culture} with {StringCount} entries.", validation.Culture, normalized.Count);
            return new LocalizationCatalogImportResult(validation.Culture, normalized.Count, validation.MissingBaselineKeyCount);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(ImportCatalogAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(ImportCatalogAsync)} failed.");
        throw;
    }
}

    /// <summary>Loads one effective culture catalog with English fallback and user overrides.</summary>
    /// <param name="culture">Normalized culture name.</param>
    /// <returns>The immutable effective string dictionary.</returns>
    private IReadOnlyDictionary<string, string> Load(string culture)
    {
    try
    {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Merge(GetBuiltInCatalogPath("en-US"), result, "en-US built-in");
            Merge(GetUserCatalogPath("en-US"), result, "en-US user override");
            if (!string.Equals(culture, "en-US", StringComparison.OrdinalIgnoreCase))
            {
                Merge(GetBuiltInCatalogPath(culture), result, culture + " built-in");
                Merge(GetUserCatalogPath(culture), result, culture + " user override");
            }
            logger.LogDebug("Loaded {StringCount} LocalGPT localization strings for culture {Culture}.", result.Count, culture);
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(Load)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(Load)} failed.");
        throw;
    }
}

    /// <summary>Merges one optional catalog file into an effective catalog.</summary>
    /// <param name="path">Catalog file path.</param>
    /// <param name="result">Mutable effective catalog.</param>
    /// <param name="source">Diagnostic source label.</param>
    private void Merge(string path, IDictionary<string, string> result, string source)
    {
    try
    {
            foreach (var pair in LoadCatalogFile(path)) result[pair.Key] = pair.Value;
            if (File.Exists(path)) logger.LogDebug("Merged LocalGPT localization source {Source}.", source);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(Merge)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(Merge)} failed.");
        throw;
    }
}

    /// <summary>Reads one JSON catalog without throwing for missing or invalid optional files.</summary>
    /// <param name="path">Catalog file path.</param>
    /// <returns>The parsed dictionary or an empty dictionary.</returns>
    private IReadOnlyDictionary<string, string> LoadCatalogFile(string path)
    {
        if (!File.Exists(path)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);
            var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                logger.LogWarning(
                    "Localization catalog {CatalogPath} does not contain a JSON object; the catalog is ignored.",
                    path);
                return normalized;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(property.Name)) continue;
                if (normalized.ContainsKey(property.Name))
                    logger.LogWarning(
                        "Localization catalog {CatalogPath} contains a case-insensitive duplicate key {LocalizationKey}; the later value is used defensively. Source-controlled catalogs must still pass the localization integrity guard.",
                        path,
                        property.Name);

                normalized[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString();
            }

            return normalized;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.LogWarning(ex, "Could not load LocalGPT localization catalog {CatalogPath}.", path);
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>Adds valid culture names represented by JSON files in one directory.</summary>
    /// <param name="directory">Directory to inspect.</param>
    /// <param name="cultures">Destination culture set.</param>
    private void AddCatalogCultures(string directory, ISet<string> cultures)
    {
        if (!Directory.Exists(directory)) return;
        foreach (var path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            try { cultures.Add(NormalizeRequiredCulture(name)); }
            catch (CultureNotFoundException) { logger.LogWarning("Ignored localization file with unknown culture name {FileName}.", Path.GetFileName(path)); }
        }
    }

    /// <summary>Normalizes an optional culture and falls back to English for unknown values.</summary>
    /// <param name="culture">Requested culture or null for the current UI culture.</param>
    /// <returns>A normalized supported culture name or en-US.</returns>
    private string NormalizeCulture(string? culture)
    {
        var requested = string.IsNullOrWhiteSpace(culture) ? CultureInfo.CurrentUICulture.Name : culture.Trim();
        try { return CultureInfo.GetCultureInfo(requested).Name; }
        catch (CultureNotFoundException ex)
        {
            logger.LogDebug(ex, "Unknown LocalGPT UI culture {RequestedCulture}; falling back to en-US.", requested);
            return "en-US";
        }
    }

    /// <summary>
    /// Normalizes required culture as part of the LocalGPT localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="culture">Required .NET culture name.</param>
    /// <returns>The normalized culture name.</returns>
    private string NormalizeRequiredCulture(string culture)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(culture)) throw new CultureNotFoundException(nameof(culture));
            return CultureInfo.GetCultureInfo(culture.Trim()).Name;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(NormalizeRequiredCulture)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(NormalizeRequiredCulture)} failed.");
        throw;
    }
}

    /// <summary>Resolves a culture display name for installer presentation.</summary>
    /// <param name="culture">Normalized culture name.</param>
    /// <returns>The runtime display name or the original culture string.</returns>
    private string GetCultureDisplayName(string culture)
    {
    try
    {
            try { return CultureInfo.GetCultureInfo(culture).DisplayName; }
            catch (CultureNotFoundException) { return culture; }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(GetCultureDisplayName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(GetCultureDisplayName)} failed.");
        throw;
    }
}

    /// <summary>Builds the shipped catalog path for a culture.</summary>
    /// <param name="culture">Normalized culture name.</param>
    /// <returns>The built-in JSON path.</returns>
    private string GetBuiltInCatalogPath(string culture) {
    try
    {
        return Path.Combine(BuiltInLocalizationPath, culture + ".json");
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(GetBuiltInCatalogPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(GetBuiltInCatalogPath)} failed.");
        throw;
    }
}
    /// <summary>Builds the persistent user catalog path for a culture.</summary>
    /// <param name="culture">Normalized culture name.</param>
    /// <returns>The user JSON path.</returns>
    private string GetUserCatalogPath(string culture) {
    try
    {
        return Path.Combine(UserLocalizationPath, culture + ".json");
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(GetUserCatalogPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptLocalizationService)}.{nameof(GetUserCatalogPath)} failed.");
        throw;
    }
}
}

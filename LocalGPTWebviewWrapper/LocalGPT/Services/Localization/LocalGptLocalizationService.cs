using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace LocalGPT.Services.Localization;

public interface ILocalGptLocalizationService
{
    IReadOnlyList<string> GetAvailableCultures();
    IReadOnlyDictionary<string, string> GetStrings(string? culture = null);
    string Get(string key, string? culture = null, string? fallback = null);
}

public sealed class LocalGptLocalizationService(
    IWebHostEnvironment environment,
    ILogger<LocalGptLocalizationService> logger) : ILocalGptLocalizationService
{
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> cache = new(StringComparer.OrdinalIgnoreCase);
    private string LocalizationPath => Path.Combine(environment.ContentRootPath, "Localization");

    public IReadOnlyList<string> GetAvailableCultures()
    {
        try
        {
            if (!Directory.Exists(LocalizationPath))
            {
                logger.LogWarning("LocalGPT localization directory {LocalizationPath} does not exist; using en-US only.", LocalizationPath);
                return ["en-US"];
            }

            var cultures = Directory.EnumerateFiles(LocalizationPath, "*.json", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            logger.LogDebug("Discovered {CultureCount} LocalGPT localization catalogs in {LocalizationPath}.", cultures.Length, LocalizationPath);
            if (cultures.Length == 0) return ["en-US"];
            return cultures;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not enumerate LocalGPT localization catalogs in {LocalizationPath}; using en-US only.", LocalizationPath);
            return ["en-US"];
        }
    }

    public IReadOnlyDictionary<string, string> GetStrings(string? culture = null)
    {
        var normalized = NormalizeCulture(culture);
        return cache.GetOrAdd(normalized, Load);
    }

    public string Get(string key, string? culture = null, string? fallback = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return fallback ?? string.Empty;
        if (GetStrings(culture).TryGetValue(key, out var value)) return value;
        if (GetStrings("en-US").TryGetValue(key, out value)) return value;
        return fallback ?? key;
    }

    private IReadOnlyDictionary<string, string> Load(string culture)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Merge(Path.Combine(LocalizationPath, "en-US.json"), result, "en-US");
        if (!string.Equals(culture, "en-US", StringComparison.OrdinalIgnoreCase))
            Merge(Path.Combine(LocalizationPath, culture + ".json"), result, culture);

        logger.LogDebug("Loaded {StringCount} LocalGPT localization strings for culture {Culture}.", result.Count, culture);
        return result;
    }

    private void Merge(string path, IDictionary<string, string> result, string culture)
    {
        if (!File.Exists(path))
        {
            logger.LogWarning("LocalGPT localization catalog {CatalogPath} for culture {Culture} is missing.", path, culture);
            return;
        }

        try
        {
            using var stream = File.OpenRead(path);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
            if (data is null)
            {
                logger.LogWarning("LocalGPT localization catalog {CatalogPath} for culture {Culture} contained no dictionary.", path, culture);
                return;
            }

            foreach (var pair in data.Where(pair => !string.IsNullOrWhiteSpace(pair.Key)))
                result[pair.Key] = pair.Value ?? string.Empty;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.LogWarning(ex, "Could not load LocalGPT localization catalog {CatalogPath} for culture {Culture}.", path, culture);
        }
    }

    private string NormalizeCulture(string? culture)
    {
        var requested = string.IsNullOrWhiteSpace(culture) ? CultureInfo.CurrentUICulture.Name : culture.Trim();
        try
        {
            return CultureInfo.GetCultureInfo(requested).Name;
        }
        catch (CultureNotFoundException ex)
        {
            logger.LogDebug(ex, "Unknown LocalGPT UI culture {RequestedCulture}; falling back to en-US.", requested);
            return "en-US";
        }
    }
}

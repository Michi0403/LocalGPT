using System.Reflection;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Persists user-owned DevExpress page/section layout overrides under the LocalGPT per-user data root.</summary>
public sealed class LocalGptUiLayoutService(ILogger<LocalGptUiLayoutService> logger) : ILocalGptUiLayoutService
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private IReadOnlyList<LocalGptUiLayoutSurface>? cache;

    /// <inheritdoc />
    public async Task<IReadOnlyList<LocalGptUiLayoutSurface>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (cache is not null)
                    return cache;

                var defaults = typeof(Program).Assembly
                    .GetTypes()
                    .SelectMany(type => type.GetCustomAttributes<LocalGptUiLayoutSurfaceAttribute>(inherit: false)
                        .Select(attribute => new LocalGptUiLayoutSurface
                        {
                            Key = $"{type.Assembly.GetName().Name}|{type.FullName}|{attribute.SectionName}",
                            AssemblyName = type.Assembly.GetName().Name ?? "LocalGPT",
                            ComponentName = type.FullName ?? type.Name,
                            SectionName = attribute.SectionName,
                            DisplayName = attribute.DisplayName,
                            Route = attribute.Route,
                            CanHide = attribute.CanHide,
                            Visible = true,
                            Order = 0,
                            ColSpanMd = Math.Clamp(attribute.DefaultColSpanMd, 1, 12),
                            LayoutType = NormalizeLayoutType(attribute.DefaultLayoutType),
                            InnerLayout = attribute.DefaultInnerLayout,
                            Orientation = attribute.DefaultInnerLayout.Equals("StackHorizontal", StringComparison.Ordinal) ? "Horizontal" : "Vertical",
                            StackLength = "1fr",
                            UpdatedAtUtc = DateTime.MinValue
                        }))
                    .OrderBy(surface => surface.Route, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(surface => surface.ComponentName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(surface => surface.SectionName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var path = GetStoragePath();
                if (!File.Exists(path))
                {
                    cache = defaults;
                    return cache;
                }

                var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                var persisted = JsonSerializer.Deserialize<LocalGptUiLayoutDocument>(json, jsonOptions)?.Surfaces ?? [];
                var persistedByKey = persisted
                    .Where(surface => !string.IsNullOrWhiteSpace(surface.Key))
                    .GroupBy(surface => surface.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

                foreach (var surface in defaults)
                {
                    if (!persistedByKey.TryGetValue(surface.Key, out var saved))
                        continue;

                    surface.DisplayName = string.IsNullOrWhiteSpace(saved.DisplayName) ? surface.DisplayName : saved.DisplayName;
                    surface.Visible = surface.CanHide ? saved.Visible : true;
                    surface.Order = saved.Order;
                    surface.ColSpanMd = Math.Clamp(saved.ColSpanMd, 1, 12);
                    surface.LayoutType = NormalizeLayoutType(saved.LayoutType);
                    surface.InnerLayout = saved.InnerLayout;
                    surface.Orientation = NormalizeOrientation(saved.Orientation, saved.InnerLayout);
                    surface.StackLength = saved.StackLength;
                    surface.CssClass = saved.CssClass;
                    surface.UpdatedAtUtc = saved.UpdatedAtUtc;
                    persistedByKey.Remove(surface.Key);
                }

                foreach (var orphan in persistedByKey.Values)
                {
                    orphan.ColSpanMd = Math.Clamp(orphan.ColSpanMd, 1, 12);
                    orphan.LayoutType = NormalizeLayoutType(orphan.LayoutType);
                    orphan.Orientation = NormalizeOrientation(orphan.Orientation, orphan.InnerLayout);
                    defaults.Add(orphan);
                }

                cache = defaults
                    .OrderBy(surface => surface.Route, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(surface => surface.Order)
                    .ThenBy(surface => surface.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return cache;
            }
            finally
            {
                gate.Release();
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not read LocalGPT UI layout definitions.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<LocalGptUiLayoutSurface> ResolveAsync(Type componentType, string sectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(componentType);
            if (string.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentException("A layout section name is required.", nameof(sectionName));

            var key = $"{componentType.Assembly.GetName().Name}|{componentType.FullName}|{sectionName}";
            var all = await GetAllAsync(cancellationToken).ConfigureAwait(false);
            var match = all.FirstOrDefault(surface => string.Equals(surface.Key, key, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;

            return new LocalGptUiLayoutSurface
            {
                Key = key,
                AssemblyName = componentType.Assembly.GetName().Name ?? "LocalGPT",
                ComponentName = componentType.FullName ?? componentType.Name,
                SectionName = sectionName,
                DisplayName = sectionName,
                CanHide = true,
                Visible = true,
                ColSpanMd = 12,
                LayoutType = "FormLayout",
                InnerLayout = "Content",
                Orientation = "Vertical",
                StackLength = "1fr",
                UpdatedAtUtc = DateTime.MinValue
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not resolve UI layout section {SectionName} for component {ComponentType}.", sectionName, componentType?.FullName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<LocalGptUiLayoutSurface> SaveAsync(LocalGptUiLayoutSurface surface, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(surface);
            if (string.IsNullOrWhiteSpace(surface.Key))
                throw new ArgumentException("A persisted layout surface requires a stable key.", nameof(surface));

            var declared = (await GetAllAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => string.Equals(item.Key, surface.Key, StringComparison.OrdinalIgnoreCase));
            if (declared is not null)
            {
                surface.CanHide = declared.CanHide;
                if (!surface.CanHide)
                    surface.Visible = true;
            }

            surface.ColSpanMd = Math.Clamp(surface.ColSpanMd, 1, 12);
            surface.Order = Math.Clamp(surface.Order, -10000, 10000);
            surface.LayoutType = NormalizeLayoutType(surface.LayoutType);
            surface.InnerLayout = surface.InnerLayout switch
            {
                "StackVertical" => "StackVertical",
                "StackHorizontal" => "StackHorizontal",
                _ => "Content"
            };
            surface.Orientation = NormalizeOrientation(surface.Orientation, surface.InnerLayout);
            surface.StackLength = string.IsNullOrWhiteSpace(surface.StackLength) ? "1fr" : surface.StackLength.Trim();
            surface.CssClass = surface.CssClass?.Trim() ?? string.Empty;
            surface.UpdatedAtUtc = DateTime.UtcNow;

            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var path = GetStoragePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                LocalGptUiLayoutDocument document;
                if (File.Exists(path))
                {
                    var existing = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                    document = JsonSerializer.Deserialize<LocalGptUiLayoutDocument>(existing, jsonOptions) ?? new LocalGptUiLayoutDocument();
                }
                else
                {
                    document = new LocalGptUiLayoutDocument();
                }

                document.Surfaces.RemoveAll(item => string.Equals(item.Key, surface.Key, StringComparison.OrdinalIgnoreCase));
                document.Surfaces.Add(surface);
                document.Surfaces = document.Surfaces
                    .OrderBy(item => item.ComponentName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(item => item.Order)
                    .ThenBy(item => item.SectionName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var serialized = JsonSerializer.Serialize(document, jsonOptions);
                var temporary = path + ".tmp";
                await File.WriteAllTextAsync(temporary, serialized, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
                File.Move(temporary, path, overwrite: true);
                cache = null;
                logger.LogInformation("Saved LocalGPT UI layout override {LayoutKey}; persisted path omitted from logs.", surface.Key);
            }
            finally
            {
                gate.Release();
            }

            return surface;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not save LocalGPT UI layout override {LayoutKey}.", surface?.Key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var path = GetStoragePath();
                if (!File.Exists(path))
                    return;

                var existing = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                var document = JsonSerializer.Deserialize<LocalGptUiLayoutDocument>(existing, jsonOptions) ?? new LocalGptUiLayoutDocument();
                var removed = document.Surfaces.RemoveAll(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
                if (removed == 0)
                    return;

                var serialized = JsonSerializer.Serialize(document, jsonOptions);
                var temporary = path + ".tmp";
                await File.WriteAllTextAsync(temporary, serialized, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
                File.Move(temporary, path, overwrite: true);
                cache = null;
                logger.LogInformation("Reset LocalGPT UI layout override {LayoutKey} to its declared component defaults.", key);
            }
            finally
            {
                gate.Release();
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not reset LocalGPT UI layout override {LayoutKey}.", key);
            throw;
        }
    }

    /// <summary>Normalizes the persisted DevExpress layout envelope while keeping old documents compatible.</summary>
    private string NormalizeLayoutType(string? value)
    {
        try
        {
            return value switch
            {
                "Content" => "Content",
                "GridLayout" => "GridLayout",
                "StackLayout" => "StackLayout",
                "Tabs" => "Tabs",
                "Splitter" => "Splitter",
                "Carousel" => "Carousel",
                _ => "FormLayout"
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not normalize a LocalGPT UI layout type.");
            throw;
        }
    }

    /// <summary>Normalizes orientation and migrates the two 5.0.2 stack aliases without discarding saved intent.</summary>
    private string NormalizeOrientation(string? value, string? legacyInnerLayout)
    {
        try
        {
            if (string.Equals(legacyInnerLayout, "StackHorizontal", StringComparison.Ordinal))
                return "Horizontal";
            if (string.Equals(value, "Horizontal", StringComparison.OrdinalIgnoreCase))
                return "Horizontal";
            return "Vertical";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not normalize a LocalGPT UI layout orientation.");
            throw;
        }
    }

    /// <inheritdoc />
    public string GetStoragePath()
    {
        try
        {
            return LocalGptApplicationDataPaths.ResolveUserPath("ui-layouts", "form-layouts.json");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not resolve the LocalGPT UI layout persistence path.");
            throw;
        }
    }
}

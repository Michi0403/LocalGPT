using DevExpress.Blazor;
using System.Globalization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Lists supported theme application target values.
/// </summary>
public enum ThemeApplicationTarget
{
    Shell,
    Components
}

/// <summary>
/// One successful selection in the order-sensitive Theme Fusion route. The route is intentionally
/// preserved because repeated theme swaps can create a visual result that is not described by the
/// final Base Theme and Style Layer alone.
/// </summary>
public sealed class ThemeFusionStep
{
    /// <summary>
    /// Runs the theme fusion step operation.
    /// </summary>
    public ThemeFusionStep(int sequence, ThemeApplicationTarget target, string themeName)
    {
        if (sequence < 1)
            throw new ArgumentOutOfRangeException(nameof(sequence));

        ArgumentException.ThrowIfNullOrWhiteSpace(themeName);
        Sequence = sequence;
        Target = target;
        ThemeName = themeName;
    }

    /// <summary>
    /// Gets or sets sequence.
    /// </summary>
    public int Sequence { get; }
    /// <summary>
    /// Gets or sets target.
    /// </summary>
    public ThemeApplicationTarget Target { get; }
    /// <summary>
    /// Gets or sets theme name.
    /// </summary>
    public string ThemeName { get; }
}

/// <summary>
/// Represents a theme.
/// </summary>
public sealed class Theme
{
    private const string BootstrapDarkModePostfix = "-dark";

    /// <summary>
    /// Runs the theme operation.
    /// </summary>
    public Theme(
        string name,
        ITheme devExpressTheme,
        bool isBootstrapNative,
        string? title = null,
        string? bootstrapThemeMode = null,
        string? themePath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(devExpressTheme);

        Name = name;
        DevExpressTheme = devExpressTheme;
        IsBootstrapNative = isBootstrapNative;
        Title = string.IsNullOrWhiteSpace(title)
            ? CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.Replace("-", " "))
            : title;
        BootstrapThemeMode = string.IsNullOrWhiteSpace(bootstrapThemeMode)
            ? InferBootstrapThemeMode(name, isBootstrapNative)
            : bootstrapThemeMode;
        ThemePath = string.IsNullOrWhiteSpace(themePath)
            ? InferThemePath(name, isBootstrapNative)
            : themePath;
    }

    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Gets or sets title.
    /// </summary>
    public string Title { get; }
    /// <summary>
    /// Gets or sets icon CSS class.
    /// </summary>
    public string IconCssClass => Name.ToLowerInvariant();
    /// <summary>
    /// Gets or sets is bootstrap native.
    /// </summary>
    public bool IsBootstrapNative { get; }
    /// <summary>
    /// Gets or sets bootstrap theme mode.
    /// </summary>
    public string BootstrapThemeMode { get; }
    /// <summary>
    /// Gets or sets theme path.
    /// </summary>
    public string ThemePath { get; }
    /// <summary>
    /// Gets or sets dev express theme.
    /// </summary>
    public ITheme DevExpressTheme { get; }

    /// <summary>
    /// Gets CSS class.
    /// </summary>
    public string GetCssClass(bool isActive) => isActive ? "active" : "text-body";

    /// <summary>
    /// Runs the infer bootstrap theme mode operation.
    /// </summary>
    private string InferBootstrapThemeMode(string name, bool isBootstrapNative)
    {
        if (name.Equals("blazing-dark", StringComparison.OrdinalIgnoreCase)
            || name.Equals("fluent-dark", StringComparison.OrdinalIgnoreCase)
            || isBootstrapNative && name.EndsWith(BootstrapDarkModePostfix, StringComparison.OrdinalIgnoreCase))
        {
            return "dark";
        }

        return "light";
    }

    /// <summary>
    /// Runs the infer theme path operation.
    /// </summary>
    private string InferThemePath(string name, bool isBootstrapNative)
    {
        if (!isBootstrapNative)
            return name;

        return name.EndsWith(BootstrapDarkModePostfix, StringComparison.OrdinalIgnoreCase)
            ? name[..^BootstrapDarkModePostfix.Length]
            : name;
    }
}

/// <summary>
/// Represents a theme set.
/// </summary>
public sealed class ThemeSet
{
    /// <summary>
    /// Runs the theme set operation.
    /// </summary>
    public ThemeSet(string title, params Theme[] themes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(themes);

        Title = title;
        Themes = themes;
    }

    /// <summary>
    /// Gets or sets title.
    /// </summary>
    public string Title { get; }
    /// <summary>
    /// Gets or sets themes.
    /// </summary>
    public Theme[] Themes { get; }
}

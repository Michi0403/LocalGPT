using DevExpress.Blazor;
using System.Globalization;

namespace LocalGPT.BusinessObjects;

public sealed class Theme
{
    private const string BootstrapDarkModePostfix = "-dark";

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

    public string Name { get; }
    public string Title { get; }
    public string IconCssClass => Name.ToLowerInvariant();
    public bool IsBootstrapNative { get; }
    public string BootstrapThemeMode { get; }
    public string ThemePath { get; }
    public ITheme DevExpressTheme { get; }

    public string GetCssClass(bool isActive) => isActive ? "active" : "text-body";

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

    private string InferThemePath(string name, bool isBootstrapNative)
    {
        if (!isBootstrapNative)
            return name;

        return name.EndsWith(BootstrapDarkModePostfix, StringComparison.OrdinalIgnoreCase)
            ? name[..^BootstrapDarkModePostfix.Length]
            : name;
    }
}

public sealed class ThemeSet
{
    public ThemeSet(string title, params Theme[] themes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(themes);

        Title = title;
        Themes = themes;
    }

    public string Title { get; }
    public Theme[] Themes { get; }
}

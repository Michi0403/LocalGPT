using DevExpress.Blazor;
using System.Globalization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Defines the supported theme application target values used to select or describe behavior in the surrounding workflow.
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
    /// Initializes a new <see cref="ThemeFusionStep"/> instance and captures the dependencies or initial state required by its theme fusion step workflow.
    /// </summary>
    /// <param name="sequence">Sequence value supplied to the theme fusion step operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the theme fusion step operation and used when producing its result.</param>
    /// <param name="themeName">Theme name value supplied to the theme fusion step operation and used when producing its result.</param>
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
    /// Gets the sequence value that forms part of the theme fusion step state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sequence value exposed by <see cref="ThemeFusionStep"/>.</value>
    public int Sequence { get; }
    /// <summary>
    /// Gets the target value that forms part of the theme fusion step state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target value exposed by <see cref="ThemeFusionStep"/>.</value>
    public ThemeApplicationTarget Target { get; }
    /// <summary>
    /// Gets the theme name value that forms part of the theme fusion step state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The theme name value exposed by <see cref="ThemeFusionStep"/>.</value>
    public string ThemeName { get; }
}

/// <summary>
/// Represents a theme application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class Theme
{
    /// <summary>
    /// Defines the bootstrap dark mode postfix constant used by <see cref="Theme"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string BootstrapDarkModePostfix = "-dark";

    /// <summary>
    /// Initializes a new <see cref="Theme"/> instance and captures the dependencies or initial state required by its theme workflow.
    /// </summary>
    /// <param name="name">Name value supplied to the theme operation and used when producing its result.</param>
    /// <param name="devExpressTheme">Theme dependency used by the theme workflow to provide the corresponding application capability.</param>
    /// <param name="isBootstrapNative">Value indicating whether is bootstrap native should apply to this operation.</param>
    /// <param name="title">Title value supplied to the theme operation and used when producing its result.</param>
    /// <param name="bootstrapThemeMode">Bootstrap theme mode value supplied to the theme operation and used when producing its result.</param>
    /// <param name="themePath">Theme path value supplied to the theme operation and used when producing its result.</param>
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
    /// Gets the name value that forms part of the theme state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="Theme"/>.</value>
    public string Name { get; }
    /// <summary>
    /// Gets the title value that forms part of the theme state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="Theme"/>.</value>
    public string Title { get; }
    /// <summary>
    /// Gets the icon CSS class value that forms part of the theme state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The icon CSS class value exposed by <see cref="Theme"/>.</value>
    public string IconCssClass => Name.ToLowerInvariant();
    /// <summary>
    /// Gets a value indicating whether bootstrap native applies to the theme state.
    /// </summary>
    /// <value>The is bootstrap native value exposed by <see cref="Theme"/>.</value>
    public bool IsBootstrapNative { get; }
    /// <summary>
    /// Gets the bootstrap theme mode value that forms part of the theme state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The bootstrap theme mode value exposed by <see cref="Theme"/>.</value>
    public string BootstrapThemeMode { get; }
    /// <summary>
    /// Gets the theme path used by this theme instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The theme path value exposed by <see cref="Theme"/>.</value>
    public string ThemePath { get; }
    /// <summary>
    /// Gets the DevExpress theme value that forms part of the theme state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The DevExpress theme value exposed by <see cref="Theme"/>.</value>
    public ITheme DevExpressTheme { get; }

    /// <summary>
    /// Retrieves CSS class for <see cref="Theme"/>, keeping the operation consistent with the state and invariants of the surrounding theme workflow.
    /// </summary>
    /// <param name="isActive">Value indicating whether is active should apply to this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public string GetCssClass(bool isActive) => isActive ? "active" : "text-body";

    /// <summary>
    /// Performs infer bootstrap theme mode for <see cref="Theme"/>, keeping the operation consistent with the state and invariants of the surrounding theme workflow.
    /// </summary>
    /// <param name="name">Name value supplied to the theme operation and used when producing its result.</param>
    /// <param name="isBootstrapNative">Value indicating whether is bootstrap native should apply to this operation.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Performs infer theme path for <see cref="Theme"/>, keeping the operation consistent with the state and invariants of the surrounding theme workflow.
    /// </summary>
    /// <param name="name">Name value supplied to the theme operation and used when producing its result.</param>
    /// <param name="isBootstrapNative">Value indicating whether is bootstrap native should apply to this operation.</param>
    /// <returns>The string produced by the operation.</returns>
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
/// Represents a theme set application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ThemeSet
{
    /// <summary>
    /// Initializes a new <see cref="ThemeSet"/> instance and captures the dependencies or initial state required by its theme set workflow.
    /// </summary>
    /// <param name="title">Title value supplied to the theme set operation and used when producing its result.</param>
    /// <param name="themes">Themes value supplied to the theme set operation and used when producing its result.</param>
    public ThemeSet(string title, params Theme[] themes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(themes);

        Title = title;
        Themes = themes;
    }

    /// <summary>
    /// Gets the title value that forms part of the theme set state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="ThemeSet"/>.</value>
    public string Title { get; }
    /// <summary>
    /// Gets the themes value that forms part of the theme set state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The themes value exposed by <see cref="ThemeSet"/>.</value>
    public Theme[] Themes { get; }
}

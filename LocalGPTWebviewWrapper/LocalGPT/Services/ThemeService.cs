using DevExpress.Blazor;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using DxThemes = DevExpress.Blazor.Themes;

namespace LocalGPT.Services;

/// <summary>
/// Owns LocalGPT's selectable theme catalog and active theme for one Blazor circuit.
/// DevExpress theme resources are represented as <see cref="ITheme"/> instances so startup
/// registration and runtime switching use the supported DevExpress resource pipeline.
/// </summary>
public sealed class ThemeService
{
    public const string DEFAULT_THEME_NAME = "office-white";
    public const string LocalThemeContractPath = "css/localgpt-theme-contract.css";

    private readonly ILogger<ThemeService> logger;
    private readonly IServiceActivityService serviceActivity;
    private readonly IReadOnlyDictionary<string, string> highlightJsThemeNames;
    private readonly Dictionary<string, Theme> themesByName;
    private readonly Theme defaultTheme;

    public ThemeService(
        ILogger<ThemeService> logger,
        IServiceActivityService serviceActivity)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.serviceActivity = serviceActivity ?? throw new ArgumentNullException(nameof(serviceActivity));
        highlightJsThemeNames = CreateHighlightJsThemeNames();
        ThemeSets = CreateSets();
        themesByName = ThemeSets
            .SelectMany(set => set.Themes)
            .ToDictionary(theme => theme.Name, StringComparer.OrdinalIgnoreCase);
        defaultTheme = FindThemeByName(DEFAULT_THEME_NAME)
            ?? throw new InvalidOperationException($"The required default theme '{DEFAULT_THEME_NAME}' is not configured.");
        ActiveTheme = defaultTheme;
    }

    public Theme ActiveTheme { get; private set; }
    public List<ThemeSet> ThemeSets { get; }
    public IThemeChangeRequestDispatcher? ThemeChangeRequestDispatcher { get; set; }
    public IThemeLoadNotifier? ThemeLoadNotifier { get; set; }
    public event Action<Theme>? ActiveThemeChanged;

    public Theme GetThemeOrDefault(string? themeName) => FindThemeByName(themeName) ?? defaultTheme;

    public Theme? FindThemeByName(string? themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
            return null;

        return themesByName.GetValueOrDefault(themeName.Trim());
    }

    public void SetActiveThemeByName(string? themeName) => SetActiveTheme(GetThemeOrDefault(themeName));

    public void SetActiveTheme(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        if (ReferenceEquals(ActiveTheme, theme)
            || ActiveTheme.Name.Equals(theme.Name, StringComparison.OrdinalIgnoreCase))
        {
            ActiveTheme = theme;
            return;
        }

        var previousTheme = ActiveTheme;
        ActiveTheme = theme;
        try
        {
            logger.LogInformation(
                "Application theme changed from {PreviousTheme} to {ThemeName}.",
                previousTheme.Name,
                theme.Name);
            ActiveThemeChanged?.Invoke(theme);
            serviceActivity.RecordInformation(
                nameof(ThemeService),
                nameof(SetActiveTheme),
                $"The active theme changed from {previousTheme.Name} to {theme.Name}.");
        }
        catch (Exception ex)
        {
            ActiveTheme = previousTheme;
            logger.LogError(
                ex,
                "The active-theme notification for {ThemeName} failed; the previous theme was restored.",
                theme.Name);
            serviceActivity.RecordFailure(nameof(ThemeService), nameof(SetActiveTheme), ex);
            throw;
        }
    }

    /// <summary>
    /// Compatibility helper for diagnostics and older code. Runtime switching uses
    /// IThemeChangeService and the theme's ITheme instance instead of replacing this link manually.
    /// </summary>
    public string GetThemeCssUrl(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        return theme.IsBootstrapNative
            ? "_content/DevExpress.Blazor.Themes/bootstrap-external.bs5.min.css"
            : theme.Name switch
            {
                "blazing-berry" => "_content/DevExpress.Blazor.Themes/blazing-berry.bs5.min.css",
                "blazing-dark" => "_content/DevExpress.Blazor.Themes/blazing-dark.bs5.min.css",
                "purple" => "_content/DevExpress.Blazor.Themes/purple.bs5.min.css",
                "office-white" => "_content/DevExpress.Blazor.Themes/office-white.bs5.min.css",
                _ => string.Empty
            };
    }

    /// <summary>
    /// Compatibility helper for diagnostics. Bootstrap theme files are registered on the
    /// corresponding DevExpress BootstrapExternal ITheme via AddFilePaths.
    /// </summary>
    public string GetBootstrapThemeCssUrl(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        return theme.IsBootstrapNative
            ? $"switcher-resources/css/themes/{theme.ThemePath}/bootstrap.min.css"
            : string.Empty;
    }

    public string GetHighlightJSThemeCssUrl(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var highlightThemeName = highlightJsThemeNames.GetValueOrDefault(theme.Name, "default");
        return $"css/highlight/{highlightThemeName}.css";
    }

    private IReadOnlyDictionary<string, string> CreateHighlightJsThemeNames() =>
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [DEFAULT_THEME_NAME] = "default",
            ["blazing-berry"] = "default",
            ["blazing-dark"] = "androidstudio",
            ["fluent-light"] = "default",
            ["fluent-dark"] = "androidstudio",
            ["cyborg"] = "androidstudio",
            ["default-dark"] = "androidstudio",
            ["solar"] = "androidstudio",
            ["superhero"] = "androidstudio"
        };

    private List<ThemeSet> CreateSets()
    {
        var classicThemes = new ThemeSet(
            "DevExpress Classic Themes",
            CreateClassic("blazing-berry", "Blazing Berry", DxThemes.BlazingBerry),
            CreateClassic("blazing-dark", "Blazing Dark", DxThemes.BlazingDark, "dark"),
            CreateClassic("purple", "Purple", DxThemes.Purple),
            CreateClassic(DEFAULT_THEME_NAME, "Office White", DxThemes.OfficeWhite));

        var fluentThemes = new ThemeSet(
            "DevExpress Fluent Themes",
            CreateFluent("fluent-light", "Fluent Light", ThemeMode.Light),
            CreateFluent("fluent-dark", "Fluent Dark", ThemeMode.Dark));

        var bootstrapThemes = new ThemeSet(
            "Bootstrap Themes",
            CreateBootstrap("default", "Bootstrap Default", "default", "light"),
            CreateBootstrap("default-dark", "Bootstrap Default Dark", "default", "dark"),
            CreateBootstrap("cerulean"),
            CreateBootstrap("cyborg", bootstrapMode: "dark"),
            CreateBootstrap("flatly"),
            CreateBootstrap("journal"),
            CreateBootstrap("litera"),
            CreateBootstrap("lumen"),
            CreateBootstrap("lux"),
            CreateBootstrap("pulse"),
            CreateBootstrap("simplex"),
            CreateBootstrap("solar", bootstrapMode: "dark"),
            CreateBootstrap("superhero", bootstrapMode: "dark"),
            CreateBootstrap("united"),
            CreateBootstrap("yeti"));

        return [classicThemes, fluentThemes, bootstrapThemes];
    }

    private Theme CreateClassic(string name, string title, DxTheme sourceTheme, string bootstrapMode = "light")
    {
        var devExpressTheme = sourceTheme.Clone(properties =>
        {
            properties.Name = $"LocalGPT-{name}";
            properties.AddFilePaths(LocalThemeContractPath);
        });
        return new Theme(name, devExpressTheme, false, title, bootstrapMode);
    }

    private Theme CreateFluent(string name, string title, ThemeMode mode)
    {
        var devExpressTheme = DxThemes.Fluent.Clone(properties =>
        {
            properties.Name = $"LocalGPT-{name}";
            properties.Mode = mode;
            properties.ApplyToPageElements = true;
            properties.UseBootstrapStyles = true;
            properties.AddFilePaths(LocalThemeContractPath);
        });
        return new Theme(name, devExpressTheme, false, title, mode == ThemeMode.Dark ? "dark" : "light");
    }

    private Theme CreateBootstrap(
        string name,
        string? title = null,
        string? themePath = null,
        string bootstrapMode = "light")
    {
        var resolvedThemePath = string.IsNullOrWhiteSpace(themePath) ? name : themePath;
        var bootstrapPath = $"switcher-resources/css/themes/{resolvedThemePath}/bootstrap.min.css";
        var devExpressTheme = DxThemes.BootstrapExternal.Clone(properties =>
        {
            properties.Name = $"LocalGPT-bootstrap-{name}";
            properties.AddFilePaths(bootstrapPath);
            properties.AddFilePaths(LocalThemeContractPath);
        });
        return new Theme(name, devExpressTheme, true, title, bootstrapMode, resolvedThemePath);
    }
}

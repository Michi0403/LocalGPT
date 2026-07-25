

using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    public class ThemeService()
    {
        public const string DEFAULT_THEME_NAME = "office-white";
        readonly string[] NEW_BLAZOR_THEMES = [DEFAULT_THEME_NAME, "blazing-dark", "purple", "office-white", "fluent-light", "fluent-dark"];
        readonly Dictionary<string, string> HIGHLIGHT_JS_THEME = new() {
            { DEFAULT_THEME_NAME, "default" },
            { "blazing-dark", "androidstudio" },
            { "cyborg", "androidstudio" },
            { "default-dark", "androidstudio" }
        };

        readonly Theme defaultTheme;
        public Theme ActiveTheme { get; private set; }
        public List<ThemeSet> ThemeSets { get; }
        public IThemeChangeRequestDispatcher? ThemeChangeRequestDispatcher { get; set; }
        public IThemeLoadNotifier? ThemeLoadNotifier { get; set; }
        private readonly ILogger<ThemeService> logger;
        public ThemeService(ILogger<ThemeService> logger) : this()
        {
            this.logger = logger;
            ThemeSets = CreateSets(this, logger);

            ActiveTheme = defaultTheme = FindThemeByName(DEFAULT_THEME_NAME)!;
        }

        public string GetThemeCssUrl(Theme theme)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(theme);
                if (Array.IndexOf(NEW_BLAZOR_THEMES, theme.Name) > -1)
                    return $"_content/DevExpress.Blazor.Themes/{theme.Name}.bs5.min.css";
                return $"_content/DevExpress.Blazor.Themes/bootstrap-external.bs5.min.css";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetThemeCssUrl theme {theme.ToString()}");
                return string.Empty;
            }
        }
        public string GetBootstrapThemeCssUrl(Theme theme)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(theme);
                return theme.IsBootstrapNative ? $"switcher-resources/css/themes/{theme.ThemePath}/bootstrap.min.css"
                : string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetBootstrapThemeCssUrl theme {theme.ToString()}");
                return string.Empty;
            }
        }
        public string GetHighlightJSThemeCssUrl(Theme theme)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(theme);
                var highlightjsTheme = HIGHLIGHT_JS_THEME[DEFAULT_THEME_NAME];

                if (HIGHLIGHT_JS_THEME.TryGetValue(theme.Name, out var value))
                    highlightjsTheme = value;
                return
                    $"https://cdnjs.cloudflare.com/ajax/libs/highlight.js/9.15.6/styles/{highlightjsTheme}.min.css";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetHighlightJSThemeCssUrl theme {theme.ToString()}");
                return string.Empty;
            }
        }
        public void SetActiveThemeByName(string? themeName)
        {
            try
            {
                ActiveTheme = FindThemeByName(themeName) ?? defaultTheme;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SetActiveThemeByName themeName {themeName.ToString()}");
            }
         
        }

        private Theme? FindThemeByName(string? themeName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(themeName))
                    return null;
                var themes = ThemeSets.SelectMany(ts => ts.Themes);

                foreach (var theme in themes)
                {
                    if (theme.Name.ToLower() == themeName.ToLower())
                        return theme;
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in FindThemeByName themeName {themeName?.ToString()}");
                return null;
            }
        }

        private List<ThemeSet> CreateSets(ThemeService config, ILogger logger)
        {
            try
            {
                return new List<ThemeSet>() {
                new("DevExpress Themes", NEW_BLAZOR_THEMES),
                new("Bootstrap Themes", "default", "default-dark", "cerulean", "cyborg", "flatly", "journal", "litera", "lumen", "lux", "pulse", "simplex", "solar", "superhero", "united", "yeti"),
            };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateSets config {config.ToString()}");
                return new();
            }
           
        }
    }

}

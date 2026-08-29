using DevExpress.Blazor;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using DxThemes = DevExpress.Blazor.Themes;

namespace LocalGPT.Services;

/// <summary>
/// Owns LocalGPT's selectable theme catalog and the two independently selectable theme layers
/// for one Blazor circuit. The shell theme controls LocalGPT page surfaces and Bootstrap metadata;
/// the component theme is applied through DevExpress' supported <see cref="IThemeChangeService"/>.
/// </summary>
public sealed class ThemeService
{
    /// <summary>
    /// Defines the default theme name constant used by <see cref="ThemeService"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string DEFAULT_THEME_NAME = "office-white";
    /// <summary>
    /// Defines the legacy theme cookie name constant used by <see cref="ThemeService"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string LegacyThemeCookieName = "ActiveTheme";
    /// <summary>
    /// Defines the shell theme cookie name constant used by <see cref="ThemeService"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string ShellThemeCookieName = "ActiveShellTheme";
    /// <summary>
    /// Defines the component theme cookie name constant used by <see cref="ThemeService"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string ComponentThemeCookieName = "ActiveComponentTheme";
    /// <summary>
    /// Defines the local theme contract path constant used by <see cref="ThemeService"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string LocalThemeContractPath = "css/localgpt-theme-contract.css";

    /// <summary>
    /// Stores the logger used by <see cref="ThemeService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<ThemeService> logger;
    /// <summary>
    /// Stores the service activity service dependency used by <see cref="ThemeService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IServiceActivityService serviceActivity;
    /// <summary>Database-backed operator runtime policy for Theme Fusion history.</summary>
    private readonly ILocalGptRuntimePolicyDataService runtimePolicy;
    /// <summary>
    /// Stores the in-memory highlight JavaScript theme names collection maintained internally by <see cref="ThemeService"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyDictionary<string, string> highlightJsThemeNames;
    /// <summary>
    /// Stores the in-memory themes by name collection maintained internally by <see cref="ThemeService"/> for its current workflow state.
    /// </summary>
    private readonly Dictionary<string, Theme> themesByName;
    /// <summary>
    /// Stores the internal default theme state used by <see cref="ThemeService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Theme defaultTheme;
    /// <summary>
    /// Stores the in-memory fusion route collection maintained internally by <see cref="ThemeService"/> for its current workflow state.
    /// </summary>
    private readonly List<ThemeFusionStep> fusionRoute = [];
    /// <summary>
    /// Stores the internal active shell theme state used by <see cref="ThemeService"/> while executing its surrounding workflow.
    /// </summary>
    private Theme activeShellTheme;
    /// <summary>
    /// Stores the internal active component theme state used by <see cref="ThemeService"/> while executing its surrounding workflow.
    /// </summary>
    private Theme activeComponentTheme;
    /// <summary>
    /// Stores the internal next fusion route sequence state used by <see cref="ThemeService"/> while executing its surrounding workflow.
    /// </summary>
    private int nextFusionRouteSequence = 1;

    /// <summary>
    /// Initializes a new <see cref="ThemeService"/> instance and captures the dependencies or initial state required by its theme workflow.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="serviceActivity">Service activity service dependency used by the theme workflow to provide the corresponding application capability.</param>
    /// <param name="runtimePolicy">Database-backed operator runtime policy.</param>
    public ThemeService(
        ILogger<ThemeService> logger,
        IServiceActivityService serviceActivity,
        ILocalGptRuntimePolicyDataService runtimePolicy)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.serviceActivity = serviceActivity ?? throw new ArgumentNullException(nameof(serviceActivity));
        this.runtimePolicy = runtimePolicy ?? throw new ArgumentNullException(nameof(runtimePolicy));
        highlightJsThemeNames = CreateHighlightJsThemeNames();
        ThemeSets = CreateSets();
        themesByName = ThemeSets
            .SelectMany(set => set.Themes)
            .ToDictionary(theme => theme.Name, StringComparer.OrdinalIgnoreCase);
        defaultTheme = FindThemeByName(DEFAULT_THEME_NAME)
            ?? throw new InvalidOperationException($"The required default theme '{DEFAULT_THEME_NAME}' is not configured.");
        activeShellTheme = defaultTheme;
        activeComponentTheme = defaultTheme;
    }

    /// <summary>
    /// Gets the active shell theme value that forms part of the theme state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active shell theme value exposed by <see cref="ThemeService"/>.</value>
    public Theme ActiveShellTheme => activeShellTheme;
    /// <summary>
    /// Gets the active component theme value that forms part of the theme state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active component theme value exposed by <see cref="ThemeService"/>.</value>
    public Theme ActiveComponentTheme => activeComponentTheme;

    /// <summary>
    /// Compatibility alias for older diagnostics and components. The former single theme now maps
    /// to the DevExpress component theme; new code should select the shell and component layers explicitly.
    /// </summary>
    /// <value>The active theme value exposed by <see cref="ThemeService"/>.</value>
    public Theme ActiveTheme => ActiveComponentTheme;
    /// <summary>
    /// Gets the max fusion route steps value that forms part of the theme state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max fusion route steps value exposed by <see cref="ThemeService"/>.</value>
    public int MaxFusionRouteSteps => Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ThemeMaximumFusionRouteSteps));

    /// <summary>
    /// Gets or sets a value indicating whether initialized applies to the theme state.
    /// </summary>
    /// <value>The is initialized value exposed by <see cref="ThemeService"/>.</value>
    public bool IsInitialized { get; private set; }
    /// <summary>
    /// Gets the theme sets collection maintained or exposed by this theme instance for downstream processing.
    /// </summary>
    /// <value>The theme sets value exposed by <see cref="ThemeService"/>.</value>
    public List<ThemeSet> ThemeSets { get; }
    /// <summary>
    /// Gets the fusion route collection maintained or exposed by this theme instance for downstream processing.
    /// </summary>
    /// <value>The fusion route value exposed by <see cref="ThemeService"/>.</value>
    public IReadOnlyList<ThemeFusionStep> FusionRoute => fusionRoute;
    /// <summary>
    /// Gets or sets the theme change request dispatcher value that forms part of the theme state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The theme change request dispatcher value exposed by <see cref="ThemeService"/>.</value>
    public IThemeChangeRequestDispatcher? ThemeChangeRequestDispatcher { get; set; }
    /// <summary>
    /// Gets or sets the theme load notifier value that forms part of the theme state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The theme load notifier value exposed by <see cref="ThemeService"/>.</value>
    public IThemeLoadNotifier? ThemeLoadNotifier { get; set; }
    /// <summary>
    /// Occurs when active shell theme changed changes or completes in <see cref="ThemeService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action<Theme>? ActiveShellThemeChanged;
    /// <summary>
    /// Occurs when active component theme changed changes or completes in <see cref="ThemeService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action<Theme>? ActiveComponentThemeChanged;
    /// <summary>
    /// Occurs when active theme changed changes or completes in <see cref="ThemeService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action<Theme>? ActiveThemeChanged;

    /// <summary>
    /// Retrieves theme or default as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="themeName">Theme name value supplied to the theme operation and used when producing its result.</param>
    /// <returns>The theme produced by the operation.</returns>
    public Theme GetThemeOrDefault(string? themeName) {
    try
    {
        return FindThemeByName(themeName) ?? defaultTheme;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(GetThemeOrDefault)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(GetThemeOrDefault)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves theme title as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="themeName">Theme name value supplied to the theme operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string GetThemeTitle(string? themeName) {
    try
    {
        return FindThemeByName(themeName)?.Title
        ?? (string.IsNullOrWhiteSpace(themeName) ? "Unknown theme" : themeName.Trim());
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(GetThemeTitle)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(GetThemeTitle)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs replace fusion route as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="steps">Theme fusion step dependency used by the theme workflow to provide the corresponding application capability.</param>
    public void ReplaceFusionRoute(IEnumerable<ThemeFusionStep>? steps)
    {
        try
        {
            fusionRoute.Clear();
            nextFusionRouteSequence = 1;

            if (steps is not null)
            {
                foreach (var step in steps.TakeLast(MaxFusionRouteSteps))
                {
                    if (FindThemeByName(step.ThemeName) is null
                        || !Enum.IsDefined(step.Target))
                    {
                        continue;
                    }

                    fusionRoute.Add(new ThemeFusionStep(
                        nextFusionRouteSequence++,
                        step.Target,
                        step.ThemeName));
                }
            }

            logger.LogInformation(
                "Theme Fusion route restored with {StepCount} selection steps.",
                fusionRoute.Count);
        }
        catch (Exception ex)
        {
            fusionRoute.Clear();
            nextFusionRouteSequence = 1;
            logger.LogError(ex, "Theme Fusion route restoration failed; the route was cleared.");
            serviceActivity.RecordFailure(nameof(ThemeService), nameof(ReplaceFusionRoute), ex);
            throw;
        }
    }

    /// <summary>
    /// Ensures fusion route seeded as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void EnsureFusionRouteSeeded()
    {
        try
        {
            if (fusionRoute.Count > 0)
                return;

            RecordFusionStep(ThemeApplicationTarget.Shell, activeShellTheme);
            RecordFusionStep(ThemeApplicationTarget.Components, activeComponentTheme);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Theme Fusion route seeding failed.");
            serviceActivity.RecordFailure(nameof(ThemeService), nameof(EnsureFusionRouteSeeded), ex);
            throw;
        }
    }

    /// <summary>
    /// Performs record fusion step as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="target">Target value supplied to the theme operation and used when producing its result.</param>
    /// <param name="theme">Theme value supplied to the theme operation and used when producing its result.</param>
    /// <returns>The theme fusion step produced by the operation.</returns>
    public ThemeFusionStep RecordFusionStep(ThemeApplicationTarget target, Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        try
        {
            if (!Enum.IsDefined(target))
                throw new ArgumentOutOfRangeException(nameof(target));

            if (FindThemeByName(theme.Name) is null)
                throw new InvalidOperationException("Only catalog themes can be added to the Theme Fusion route.");

            if (fusionRoute.Count >= MaxFusionRouteSteps)
                fusionRoute.RemoveAt(0);

            var step = new ThemeFusionStep(nextFusionRouteSequence++, target, theme.Name);
            fusionRoute.Add(step);

            logger.LogInformation(
                "Theme Fusion route step {RouteStep}: {ThemeTarget} selected {ThemeName}.",
                step.Sequence,
                target,
                theme.Name);
            serviceActivity.RecordInformation(
                nameof(ThemeService),
                nameof(RecordFusionStep),
                $"Theme Fusion route step {step.Sequence}: {target} selected {theme.Name}.");
            return step;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Theme Fusion route recording failed; selected theme details were omitted from the error message.");
            serviceActivity.RecordFailure(nameof(ThemeService), nameof(RecordFusionStep), ex);
            throw;
        }
    }

    /// <summary>
    /// Performs reset fusion route to current selection as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void ResetFusionRouteToCurrentSelection()
    {
        try
        {
            fusionRoute.Clear();
            nextFusionRouteSequence = 1;
            RecordFusionStep(ThemeApplicationTarget.Shell, activeShellTheme);
            RecordFusionStep(ThemeApplicationTarget.Components, activeComponentTheme);
            logger.LogInformation(
                "Theme Fusion route reset to current Base Theme {ShellTheme} and Style Layer {ComponentTheme}.",
                activeShellTheme.Name,
                activeComponentTheme.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Theme Fusion route reset failed.");
            serviceActivity.RecordFailure(nameof(ThemeService), nameof(ResetFusionRouteToCurrentSelection), ex);
            throw;
        }
    }

    /// <summary>
    /// Retrieves theme layer CSS class as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="shellThemeName">Shell theme name value supplied to the theme operation and used when producing its result.</param>
    /// <param name="componentThemeName">Component theme name value supplied to the theme operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string GetThemeLayerCssClass(string? shellThemeName, string? componentThemeName)
    {
        try
        {
            var shellToken = GetThemeCssToken(shellThemeName);
            var componentToken = GetThemeCssToken(componentThemeName);
            return $"theme-{shellToken} component-theme-{componentToken}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Theme layer CSS class generation failed; theme names were omitted from logs.");
            serviceActivity.RecordFailure(nameof(ThemeService), nameof(GetThemeLayerCssClass), ex);
            return $"theme-{DEFAULT_THEME_NAME} component-theme-{DEFAULT_THEME_NAME}";
        }
    }

    /// <summary>
    /// Retrieves theme CSS token as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="themeName">Theme name value supplied to the theme operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string GetThemeCssToken(string? themeName)
    {
        try
        {
            var validatedTheme = GetThemeOrDefault(themeName);
            return validatedTheme.Name.Replace(" ", "-", StringComparison.Ordinal).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Theme CSS token generation failed; theme details were omitted from logs.");
            serviceActivity.RecordFailure(nameof(ThemeService), nameof(GetThemeCssToken), ex);
            throw;
        }
    }

    /// <summary>
    /// Finds theme by name as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="themeName">Theme name value supplied to the theme operation and used when producing its result.</param>
    /// <returns>The theme produced by the operation.</returns>
    public Theme? FindThemeByName(string? themeName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(themeName))
                return null;

            return themesByName.GetValueOrDefault(themeName.Trim());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Theme lookup failed; the requested theme name was omitted from logs.");
            serviceActivity.RecordFailure(nameof(ThemeService), nameof(FindThemeByName), ex);
            throw;
        }
    }

    /// <summary>
    /// Performs initialize themes as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="shellThemeName">Shell theme name value supplied to the theme operation and used when producing its result.</param>
    /// <param name="componentThemeName">Component theme name value supplied to the theme operation and used when producing its result.</param>
    public void InitializeThemes(string? shellThemeName, string? componentThemeName)
    {
        try
        {
            if (IsInitialized)
                return;

            activeShellTheme = GetThemeOrDefault(shellThemeName);
            activeComponentTheme = GetThemeOrDefault(componentThemeName);
            IsInitialized = true;
            logger.LogInformation(
                "Theme state initialized with shell {ShellTheme} and DevExpress components {ComponentTheme}.",
                activeShellTheme.Name,
                activeComponentTheme.Name);
            serviceActivity.RecordInformation(
                nameof(ThemeService),
                nameof(InitializeThemes),
                $"Theme Fusion initialized with base {activeShellTheme.Name} and style layer {activeComponentTheme.Name}.");
        }
        catch (Exception ex)
        {
            activeShellTheme = defaultTheme;
            activeComponentTheme = defaultTheme;
            IsInitialized = true;
            logger.LogError(ex, "Theme Fusion initialization failed; LocalGPT restored the Base Theme and Style Layer to the default.");
            serviceActivity.RecordFailure(nameof(ThemeService), nameof(InitializeThemes), ex);
            throw;
        }
    }

    /// <summary>
    /// Sets active shell theme by name as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="themeName">Theme name value supplied to the theme operation and used when producing its result.</param>
    public void SetActiveShellThemeByName(string? themeName) {
    try
    {
        SetActiveShellTheme(GetThemeOrDefault(themeName));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(SetActiveShellThemeByName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(SetActiveShellThemeByName)} failed.");
        throw;
    }
}
    /// <summary>
    /// Sets active component theme by name as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="themeName">Theme name value supplied to the theme operation and used when producing its result.</param>
    public void SetActiveComponentThemeByName(string? themeName) {
    try
    {
        SetActiveComponentTheme(GetThemeOrDefault(themeName));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(SetActiveComponentThemeByName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(SetActiveComponentThemeByName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Sets active shell theme as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="theme">Theme value supplied to the theme operation and used when producing its result.</param>
    public void SetActiveShellTheme(Theme theme) {
    try
    {
        SetActiveThemeCore(
            theme,
            ThemeApplicationTarget.Shell,
            ref activeShellTheme,
            changedTheme => ActiveShellThemeChanged?.Invoke(changedTheme));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(SetActiveShellTheme)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(SetActiveShellTheme)} failed.");
        throw;
    }
}

    /// <summary>
    /// Sets active component theme as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="theme">Theme value supplied to the theme operation and used when producing its result.</param>
    public void SetActiveComponentTheme(Theme theme) {
    try
    {
        SetActiveThemeCore(
            theme,
            ThemeApplicationTarget.Components,
            ref activeComponentTheme,
            changedTheme => ActiveComponentThemeChanged?.Invoke(changedTheme));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(SetActiveComponentTheme)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(SetActiveComponentTheme)} failed.");
        throw;
    }
}

    /// <summary>
    /// Backward-compatible single-theme setter. It intentionally updates both layers.
    /// </summary>
    /// <param name="themeName">Theme name value supplied to the theme operation and used when producing its result.</param>
    public void SetActiveThemeByName(string? themeName) {
    try
    {
        SetActiveTheme(GetThemeOrDefault(themeName));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(SetActiveThemeByName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(SetActiveThemeByName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Sets active theme as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="theme">Theme value supplied to the theme operation and used when producing its result.</param>
    public void SetActiveTheme(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var previousShell = activeShellTheme;
        var previousComponent = activeComponentTheme;
        try
        {
            SetActiveShellTheme(theme);
            SetActiveComponentTheme(theme);
            ActiveThemeChanged?.Invoke(theme);
        }
        catch (Exception ex)
        {
            activeShellTheme = previousShell;
            activeComponentTheme = previousComponent;
            logger.LogError(ex, "The compatibility theme change failed; both prior theme layers were restored.");
            serviceActivity.RecordFailure(nameof(ThemeService), nameof(SetActiveTheme), ex);
            throw;
        }
    }

    /// <summary>
    /// Sets active theme core as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="theme">Theme value supplied to the theme operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the theme operation and used when producing its result.</param>
    /// <param name="activeTheme">Active theme value supplied to the theme operation and used when producing its result.</param>
    /// <param name="changed">Changed value supplied to the theme operation and used when producing its result.</param>
    private void SetActiveThemeCore(
        Theme theme,
        ThemeApplicationTarget target,
        ref Theme activeTheme,
        Action<Theme>? changed)
    {
        ArgumentNullException.ThrowIfNull(theme);

        if (ReferenceEquals(activeTheme, theme)
            || activeTheme.Name.Equals(theme.Name, StringComparison.OrdinalIgnoreCase))
        {
            activeTheme = theme;
            IsInitialized = true;
            return;
        }

        var previousTheme = activeTheme;
        activeTheme = theme;
        IsInitialized = true;
        try
        {
            logger.LogInformation(
                "{ThemeTarget} theme changed from {PreviousTheme} to {ThemeName}.",
                target,
                previousTheme.Name,
                theme.Name);
            changed?.Invoke(theme);
            serviceActivity.RecordInformation(
                nameof(ThemeService),
                target == ThemeApplicationTarget.Shell ? nameof(SetActiveShellTheme) : nameof(SetActiveComponentTheme),
                $"The {target} theme changed from {previousTheme.Name} to {theme.Name}.");
        }
        catch (Exception ex)
        {
            activeTheme = previousTheme;
            logger.LogError(
                ex,
                "The {ThemeTarget} theme notification for {ThemeName} failed; the previous theme was restored.",
                target,
                theme.Name);
            serviceActivity.RecordFailure(
                nameof(ThemeService),
                target == ThemeApplicationTarget.Shell ? nameof(SetActiveShellTheme) : nameof(SetActiveComponentTheme),
                ex);
            throw;
        }
    }

    /// <summary>
    /// Compatibility helper for diagnostics and older code. Runtime component switching uses
    /// IThemeChangeService and the theme's ITheme instance instead of replacing this link manually.
    /// </summary>
    /// <param name="theme">Theme value supplied to the theme operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string GetThemeCssUrl(Theme theme)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(GetThemeCssUrl)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(GetThemeCssUrl)} failed.");
        throw;
    }
}

    /// <summary>
    /// Compatibility helper for diagnostics. Bootstrap theme files are registered on the
    /// corresponding DevExpress BootstrapExternal ITheme via AddFilePaths.
    /// </summary>
    /// <param name="theme">Theme value supplied to the theme operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string GetBootstrapThemeCssUrl(Theme theme)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(theme);
            return theme.IsBootstrapNative
                ? $"switcher-resources/css/themes/{theme.ThemePath}/bootstrap.min.css"
                : string.Empty;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(GetBootstrapThemeCssUrl)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(GetBootstrapThemeCssUrl)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves highlight JavaScript theme CSS URL as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="theme">Theme value supplied to the theme operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string GetHighlightJSThemeCssUrl(Theme theme)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(theme);
            var highlightThemeName = highlightJsThemeNames.GetValueOrDefault(theme.Name, "default");
            return $"css/highlight/{highlightThemeName}.css";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(GetHighlightJSThemeCssUrl)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(GetHighlightJSThemeCssUrl)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates highlight JavaScript theme names as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The i read only dictionary string string produced by the operation.</returns>
    private IReadOnlyDictionary<string, string> CreateHighlightJsThemeNames() {
    try
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(CreateHighlightJsThemeNames)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(CreateHighlightJsThemeNames)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates sets as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<ThemeSet> CreateSets()
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(CreateSets)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(CreateSets)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates classic as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the theme operation and used when producing its result.</param>
    /// <param name="title">Title value supplied to the theme operation and used when producing its result.</param>
    /// <param name="sourceTheme">Source theme value supplied to the theme operation and used when producing its result.</param>
    /// <param name="bootstrapMode">Bootstrap mode value supplied to the theme operation and used when producing its result.</param>
    /// <returns>The theme produced by the operation.</returns>
    private Theme CreateClassic(string name, string title, DxTheme sourceTheme, string bootstrapMode = "light")
    {
    try
    {
            var devExpressTheme = sourceTheme.Clone(properties =>
            {
                properties.Name = $"LocalGPT-{name}";
                properties.AddFilePaths(LocalThemeContractPath);
            });
            return new Theme(name, devExpressTheme, false, title, bootstrapMode);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(CreateClassic)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(CreateClassic)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates fluent as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the theme operation and used when producing its result.</param>
    /// <param name="title">Title value supplied to the theme operation and used when producing its result.</param>
    /// <param name="mode">Mode value supplied to the theme operation and used when producing its result.</param>
    /// <returns>The theme produced by the operation.</returns>
    private Theme CreateFluent(string name, string title, ThemeMode mode)
    {
    try
    {
            var devExpressTheme = DxThemes.Fluent.Clone(properties =>
            {
                properties.Name = $"LocalGPT-{name}";
                properties.Mode = mode;
                properties.ApplyToPageElements = false;
                properties.UseBootstrapStyles = true;
                properties.AddFilePaths(LocalThemeContractPath);
            });
            return new Theme(name, devExpressTheme, false, title, mode == ThemeMode.Dark ? "dark" : "light");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(CreateFluent)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(CreateFluent)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates bootstrap as part of the theme service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the theme operation and used when producing its result.</param>
    /// <param name="title">Title value supplied to the theme operation and used when producing its result.</param>
    /// <param name="themePath">Theme path value supplied to the theme operation and used when producing its result.</param>
    /// <param name="bootstrapMode">Bootstrap mode value supplied to the theme operation and used when producing its result.</param>
    /// <returns>The theme produced by the operation.</returns>
    private Theme CreateBootstrap(
        string name,
        string? title = null,
        string? themePath = null,
        string bootstrapMode = "light")
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(CreateBootstrap)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ThemeService)}.{nameof(CreateBootstrap)} failed.");
        throw;
    }
}
}

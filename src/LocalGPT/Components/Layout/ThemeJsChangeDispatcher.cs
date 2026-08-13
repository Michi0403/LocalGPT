using DevExpress.Blazor;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.JSInterop;

namespace LocalGPT.Components.Layout;

/// <summary>
/// Bridges LocalGPT's independently selectable shell and DevExpress component themes to the browser.
/// Browser persistence is owned by the JavaScript module; DevExpress remains the owner of component
/// theme resources.
/// </summary>
public sealed class ThemeJsChangeDispatcher : ComponentBase, IThemeChangeRequestDispatcher, IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets or sets the initial shell theme name value that forms part of the theme JavaScript change dispatcher state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The initial shell theme name value exposed by <see cref="ThemeJsChangeDispatcher"/>.</value>
    [Parameter]
    public required string InitialShellThemeName { get; set; }

    /// <summary>
    /// Gets or sets the initial component theme name value that forms part of the theme JavaScript change dispatcher state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The initial component theme name value exposed by <see cref="ThemeJsChangeDispatcher"/>.</value>
    [Parameter]
    public required string InitialComponentThemeName { get; set; }

    /// <summary>
    /// Gets or sets the JavaScript runtime value that forms part of the theme JavaScript change dispatcher state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The JavaScript runtime value exposed by <see cref="ThemeJsChangeDispatcher"/>.</value>
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    /// <summary>
    /// Gets or sets the navigation manager value that forms part of the theme JavaScript change dispatcher state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The navigation manager value exposed by <see cref="ThemeJsChangeDispatcher"/>.</value>
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// Gets or sets the file version provider value that forms part of the theme JavaScript change dispatcher state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The file version provider value exposed by <see cref="ThemeJsChangeDispatcher"/>.</value>
    [Inject]
    private IFileVersionProvider FileVersionProvider { get; set; } = default!;

    /// <summary>
    /// Gets or sets the DevExpress theme change service value that forms part of the theme JavaScript change dispatcher state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The DevExpress theme change service value exposed by <see cref="ThemeJsChangeDispatcher"/>.</value>
    [Inject]
    private IThemeChangeService DevExpressThemeChangeService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the themes value that forms part of the theme JavaScript change dispatcher state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The themes value exposed by <see cref="ThemeJsChangeDispatcher"/>.</value>
    [Inject]
    private ThemeService Themes { get; set; } = default!;

    /// <summary>
    /// Gets or sets the logger value that forms part of the theme JavaScript change dispatcher state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The logger value exposed by <see cref="ThemeJsChangeDispatcher"/>.</value>
    [Inject]
    private ILogger<ThemeJsChangeDispatcher> Logger { get; set; } = default!;

    /// <summary>
    /// Gets or sets the notifier value that forms part of the theme JavaScript change dispatcher state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The notifier value exposed by <see cref="ThemeJsChangeDispatcher"/>.</value>
    [Inject]
    private INotificationService Notifier { get; set; } = default!;

    /// <summary>
    /// Gets or sets the component activity value that forms part of the theme JavaScript change dispatcher state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The component activity value exposed by <see cref="ThemeJsChangeDispatcher"/>.</value>
    [Inject]
    private IComponentActivityService ComponentActivity { get; set; } = default!;

    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to change gate state owned by <see cref="ThemeJsChangeDispatcher"/>.
    /// </summary>
    private readonly SemaphoreSlim _changeGate = new(1, 1);
    /// <summary>
    /// Stores the JavaScript object reference dependency used by <see cref="ThemeJsChangeDispatcher"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private IJSObjectReference? _module;
    /// <summary>
    /// Stores the internal disposed state used by <see cref="ThemeJsChangeDispatcher"/> while executing its surrounding workflow.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Handles the after render async lifecycle or event notification for <see cref="ThemeJsChangeDispatcher"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="firstRender">Value indicating whether first render should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await _changeGate.WaitAsync().ConfigureAwait(true) /* renderer-affine lifecycle continuation */;
        try
        {
            Themes.ThemeChangeRequestDispatcher = this;

            var fallbackShellTheme = Themes.GetThemeOrDefault(InitialShellThemeName);
            var fallbackComponentTheme = Themes.GetThemeOrDefault(InitialComponentThemeName);
            Themes.SetActiveShellTheme(fallbackShellTheme);
            Themes.SetActiveComponentTheme(fallbackComponentTheme);
            await DevExpressThemeChangeService
                .SetTheme(fallbackComponentTheme.DevExpressTheme).ConfigureAwait(true) /* renderer-affine lifecycle continuation */
                ;

            await EnsureModuleAsync().ConfigureAwait(true) /* renderer-affine lifecycle continuation */;
            var browserState = await _module!
                .InvokeAsync<BrowserThemeState?>("readThemeState").ConfigureAwait(true) /* renderer-affine lifecycle continuation */
                ;

            var shellTheme = Themes.GetThemeOrDefault(
                browserState?.ShellThemeName ?? fallbackShellTheme.Name);
            var componentTheme = Themes.GetThemeOrDefault(
                browserState?.ComponentThemeName ?? fallbackComponentTheme.Name);

            Themes.SetActiveShellTheme(shellTheme);
            Themes.SetActiveComponentTheme(componentTheme);
            await DevExpressThemeChangeService
                .SetTheme(componentTheme.DevExpressTheme).ConfigureAwait(true) /* renderer-affine lifecycle continuation */
                ;

            Themes.ReplaceFusionRoute(ConvertBrowserFusionRoute(browserState?.FusionRoute));
            Themes.EnsureFusionRouteSeeded();
            await PersistFusionRouteAsync().ConfigureAwait(true) /* renderer-affine lifecycle continuation */;
            await ApplyClientThemeStateAsync().ConfigureAwait(true) /* renderer-affine lifecycle continuation */;
            await NotifyLoadedAsync(shellTheme, ThemeApplicationTarget.Shell).ConfigureAwait(true) /* renderer-affine lifecycle continuation */;
            await NotifyLoadedAsync(componentTheme, ThemeApplicationTarget.Components).ConfigureAwait(true) /* renderer-affine lifecycle continuation */;

            ComponentActivity.RecordInformation(
                nameof(ThemeJsChangeDispatcher),
                "ThemeDispatcherReady",
                $"Theme dispatcher initialized with shell {shellTheme.Name} and components {componentTheme.Name}.");
        }
        catch (JSDisconnectedException)
        {
            Logger.LogDebug("Theme dispatcher initialization ended because the browser circuit disconnected.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Theme dispatcher initialization failed.");
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), "ThemeDispatcherReady", ex);
            Notifier.ShowWarning(
                "ComponentSafetyToasts",
                "The saved themes could not be fully restored. LocalGPT kept usable defaults and will retry when a theme is selected.",
                "Theme warning");
        }
        finally
        {
            _changeGate.Release();
        }

        await base.OnAfterRenderAsync(firstRender).ConfigureAwait(true) /* renderer-affine lifecycle continuation */;
    }

    /// <summary>
    /// Performs request theme change for <see cref="ThemeJsChangeDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding theme JavaScript change dispatcher workflow.
    /// </summary>
    /// <param name="theme">Theme value supplied to the theme JavaScript change dispatcher operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the theme JavaScript change dispatcher operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task RequestThemeChangeAsync(Theme theme, ThemeApplicationTarget target)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _changeGate.WaitAsync().ConfigureAwait(false);
        try
        {
            await EnsureModuleAsync().ConfigureAwait(false);

            var previousShellTheme = Themes.ActiveShellTheme;
            var previousComponentTheme = Themes.ActiveComponentTheme;
            try
            {
                if (target == ThemeApplicationTarget.Components)
                {
                    await DevExpressThemeChangeService
                        .SetTheme(theme.DevExpressTheme).ConfigureAwait(false)
                        ;
                    Themes.SetActiveComponentTheme(theme);
                }
                else
                {
                    Themes.SetActiveShellTheme(theme);
                }

                await ApplyClientThemeStateAsync().ConfigureAwait(false);
                Themes.RecordFusionStep(target, theme);
                await PersistFusionRouteAsync().ConfigureAwait(false);
                await NotifyLoadedAsync(theme, target).ConfigureAwait(false);

                ComponentActivity.RecordInformation(
                    nameof(ThemeJsChangeDispatcher),
                    nameof(RequestThemeChangeAsync),
                    $"The {target} theme {theme.Name} was applied and persisted.");
            }
            catch (JSDisconnectedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await RestoreThemeLayerAsync(
                        target,
                        previousShellTheme,
                        previousComponentTheme,
                        ex).ConfigureAwait(false)
                    ;
            }
        }
        catch (JSDisconnectedException)
        {
            Logger.LogDebug("Theme change ended because the browser circuit disconnected.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "The {ThemeTarget} theme change could not start.", target);
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), nameof(RequestThemeChangeAsync), ex);
            Notifier.ShowError(
                "ComponentSafetyToasts",
                "The selected theme could not be applied or stored. See local logs for details.",
                "Theme change failed");
        }
        finally
        {
            _changeGate.Release();
        }
    }

    /// <summary>
    /// Performs reset fusion route for <see cref="ThemeJsChangeDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding theme JavaScript change dispatcher workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task ResetFusionRouteAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _changeGate.WaitAsync().ConfigureAwait(false);
        try
        {
            await EnsureModuleAsync().ConfigureAwait(false);
            Themes.ResetFusionRouteToCurrentSelection();
            await _module!
                .InvokeVoidAsync(
                    "resetFusionRoute",
                    Themes.ActiveShellTheme.Name,
                    Themes.ActiveComponentTheme.Name).ConfigureAwait(false)
                ;

            ComponentActivity.RecordInformation(
                nameof(ThemeJsChangeDispatcher),
                nameof(ResetFusionRouteAsync),
                "Theme Fusion route reset to the current Base Theme and Style Layer; a clean reload was requested.");
        }
        catch (JSDisconnectedException)
        {
            Logger.LogDebug("Theme Fusion route reset completed while the requested page reload disconnected the circuit.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Theme Fusion route reset failed.");
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), nameof(ResetFusionRouteAsync), ex);
            Notifier.ShowError(
                "ComponentSafetyToasts",
                "The Theme Fusion route could not be reset. See local logs for details.",
                "Theme route reset failed");
        }
        finally
        {
            _changeGate.Release();
        }
    }

    /// <summary>
    /// Ensures module for <see cref="ThemeJsChangeDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding theme JavaScript change dispatcher workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task EnsureModuleAsync()
    {
        try
        {
            if (_module is not null)
                return;

            var versionedThemeModulePath = FileVersionProvider.AddFileVersionToPath(
                "/",
                "switcher-resources/theme-controller.js");
            var themeModuleUri = NavigationManager
                .ToAbsoluteUri(versionedThemeModulePath)
                .AbsoluteUri;
            var importedModule = await JsRuntime
                .InvokeAsync<IJSObjectReference>("import", themeModuleUri).ConfigureAwait(false)
                ;
            _module = importedModule
                ?? throw new InvalidOperationException("The theme JavaScript module import returned no module reference.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Theme JavaScript module initialization failed.");
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), nameof(EnsureModuleAsync), ex);
            throw;
        }
    }

    /// <summary>
    /// Applies client theme state async.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ApplyClientThemeStateAsync()
    {
        try
        {
            await EnsureModuleAsync().ConfigureAwait(false);
            await _module!
                .InvokeVoidAsync(
                    "applyThemeState",
                    Themes.ActiveShellTheme.Name,
                    Themes.ActiveShellTheme.BootstrapThemeMode,
                    Themes.GetHighlightJSThemeCssUrl(Themes.ActiveShellTheme),
                    Themes.ActiveComponentTheme.Name,
                    null,
                    null).ConfigureAwait(false)
                ;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Applying or persisting the browser theme state failed.");
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), nameof(ApplyClientThemeStateAsync), ex);
            throw;
        }
    }

    /// <summary>
    /// Persists fusion route for <see cref="ThemeJsChangeDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding theme JavaScript change dispatcher workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task PersistFusionRouteAsync()
    {
        try
        {
            await EnsureModuleAsync().ConfigureAwait(false);
            await _module!
                .InvokeVoidAsync("persistFusionRoute", Themes.FusionRoute).ConfigureAwait(false)
                ;
        }
        catch (JSDisconnectedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Theme Fusion route persistence failed; the active themes remain usable for this circuit.");
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), nameof(PersistFusionRouteAsync), ex);
        }
    }

    /// <summary>
    /// Converts browser fusion route for <see cref="ThemeJsChangeDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding theme JavaScript change dispatcher workflow.
    /// </summary>
    /// <param name="browserSteps">Browser theme fusion step dependency used by the theme JavaScript change dispatcher workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<ThemeFusionStep> ConvertBrowserFusionRoute(
        IReadOnlyList<BrowserThemeFusionStep>? browserSteps)
    {
        try
        {
            if (browserSteps is null || browserSteps.Count == 0)
                return [];

            var route = new List<ThemeFusionStep>(Math.Min(browserSteps.Count, ThemeService.MaxFusionRouteSteps));
            foreach (var browserStep in browserSteps.TakeLast(ThemeService.MaxFusionRouteSteps))
            {
                if (string.IsNullOrWhiteSpace(browserStep.ThemeName)
                    || !Enum.TryParse<ThemeApplicationTarget>(browserStep.Target, true, out var target))
                {
                    continue;
                }

                route.Add(new ThemeFusionStep(route.Count + 1, target, browserStep.ThemeName));
            }

            return route;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "The browser Theme Fusion route could not be converted into the LocalGPT runtime route.");
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), nameof(ConvertBrowserFusionRoute), ex);
            return [];
        }
    }

    /// <summary>
    /// Performs notify loaded for <see cref="ThemeJsChangeDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding theme JavaScript change dispatcher workflow.
    /// </summary>
    /// <param name="theme">Theme value supplied to the theme JavaScript change dispatcher operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the theme JavaScript change dispatcher operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task NotifyLoadedAsync(Theme theme, ThemeApplicationTarget target)
    {
        try
        {
            if (Themes.ThemeLoadNotifier is not null)
            {
                await Themes.ThemeLoadNotifier
                    .NotifyThemeLoadedAsync(theme, target).ConfigureAwait(false)
                    ;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "The {ThemeTarget} theme UI notification failed after the theme was applied.", target);
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), nameof(NotifyLoadedAsync), ex);
        }
    }

    /// <summary>
    /// Performs restore theme layer for <see cref="ThemeJsChangeDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding theme JavaScript change dispatcher workflow.
    /// </summary>
    /// <param name="target">Target value supplied to the theme JavaScript change dispatcher operation and used when producing its result.</param>
    /// <param name="previousShellTheme">Previous shell theme value supplied to the theme JavaScript change dispatcher operation and used when producing its result.</param>
    /// <param name="previousComponentTheme">Previous component theme value supplied to the theme JavaScript change dispatcher operation and used when producing its result.</param>
    /// <param name="originalException">Original exception value supplied to the theme JavaScript change dispatcher operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RestoreThemeLayerAsync(
        ThemeApplicationTarget target,
        Theme previousShellTheme,
        Theme previousComponentTheme,
        Exception originalException)
    {
        try
        {
            if (target == ThemeApplicationTarget.Components)
            {
                await DevExpressThemeChangeService
                    .SetTheme(previousComponentTheme.DevExpressTheme).ConfigureAwait(false)
                    ;
                Themes.SetActiveComponentTheme(previousComponentTheme);
            }
            else
            {
                Themes.SetActiveShellTheme(previousShellTheme);
            }

            await ApplyClientThemeStateAsync().ConfigureAwait(false);
            await NotifyLoadedAsync(
                    target == ThemeApplicationTarget.Shell ? previousShellTheme : previousComponentTheme,
                    target).ConfigureAwait(false)
                ;

            Logger.LogError(
                originalException,
                "The {ThemeTarget} theme change failed; LocalGPT restored the previous theme layer.",
                target);
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), nameof(RequestThemeChangeAsync), originalException);
            Notifier.ShowError(
                "ComponentSafetyToasts",
                "The selected theme could not be applied. LocalGPT restored the previous theme layer.",
                "Theme change failed");
        }
        catch (Exception rollbackException)
        {
            Logger.LogCritical(
                rollbackException,
                "Theme rollback failed after the {ThemeTarget} theme could not be applied.",
                target);
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), nameof(RestoreThemeLayerAsync), rollbackException);
            Notifier.ShowError(
                "ComponentSafetyToasts",
                "The selected theme and automatic rollback both failed. Reload the page to restore saved themes.",
                "Theme rollback failed");
        }
    }

    /// <summary>
    /// Releases resources owned by <see cref="ThemeJsChangeDispatcher"/> and leaves the theme JavaScript change dispatcher workflow in a safely disposed state.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        try
        {
            if (ReferenceEquals(Themes.ThemeChangeRequestDispatcher, this))
                Themes.ThemeChangeRequestDispatcher = null;

            if (_module is not null)
            {
                try
                {
                    await _module.DisposeAsync().ConfigureAwait(false);
                }
                catch (JSDisconnectedException)
                {
                    Logger.LogDebug("Theme JavaScript module disposal ended after browser disconnect.");
                }
                finally
                {
                    _module = null;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Theme dispatcher disposal failed.");
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), nameof(DisposeAsync), ex);
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Releases resources owned by <see cref="ThemeJsChangeDispatcher"/> and leaves the theme JavaScript change dispatcher workflow in a safely disposed state.
    /// </summary>
    /// <returns>The void i disposable produced by the operation.</returns>
    void IDisposable.Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        try
        {
            if (ReferenceEquals(Themes.ThemeChangeRequestDispatcher, this))
                Themes.ThemeChangeRequestDispatcher = null;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Theme dispatcher synchronous disposal failed.");
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), "Dispose", ex);
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }


}

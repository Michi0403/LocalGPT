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
    [Parameter]
    public required string InitialShellThemeName { get; set; }

    [Parameter]
    public required string InitialComponentThemeName { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private IFileVersionProvider FileVersionProvider { get; set; } = default!;

    [Inject]
    private IThemeChangeService DevExpressThemeChangeService { get; set; } = default!;

    [Inject]
    private ThemeService Themes { get; set; } = default!;

    [Inject]
    private ILogger<ThemeJsChangeDispatcher> Logger { get; set; } = default!;

    [Inject]
    private INotificationService Notifier { get; set; } = default!;

    [Inject]
    private IComponentActivityService ComponentActivity { get; set; } = default!;

    private readonly SemaphoreSlim _changeGate = new(1, 1);
    private IJSObjectReference? _module;
    private bool _disposed;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await _changeGate.WaitAsync().ConfigureAwait(true);
        try
        {
            Themes.ThemeChangeRequestDispatcher = this;

            var fallbackShellTheme = Themes.GetThemeOrDefault(InitialShellThemeName);
            var fallbackComponentTheme = Themes.GetThemeOrDefault(InitialComponentThemeName);
            Themes.SetActiveShellTheme(fallbackShellTheme);
            Themes.SetActiveComponentTheme(fallbackComponentTheme);
            await DevExpressThemeChangeService
                .SetTheme(fallbackComponentTheme.DevExpressTheme)
                .ConfigureAwait(true);

            await EnsureModuleAsync().ConfigureAwait(true);
            var browserState = await _module!
                .InvokeAsync<BrowserThemeState?>("readThemeState")
                .ConfigureAwait(true);

            var shellTheme = Themes.GetThemeOrDefault(
                browserState?.ShellThemeName ?? fallbackShellTheme.Name);
            var componentTheme = Themes.GetThemeOrDefault(
                browserState?.ComponentThemeName ?? fallbackComponentTheme.Name);

            Themes.SetActiveShellTheme(shellTheme);
            Themes.SetActiveComponentTheme(componentTheme);
            await DevExpressThemeChangeService
                .SetTheme(componentTheme.DevExpressTheme)
                .ConfigureAwait(true);

            await ApplyClientThemeStateAsync().ConfigureAwait(true);
            await NotifyLoadedAsync(shellTheme, ThemeApplicationTarget.Shell).ConfigureAwait(true);
            await NotifyLoadedAsync(componentTheme, ThemeApplicationTarget.Components).ConfigureAwait(true);

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

        await base.OnAfterRenderAsync(firstRender).ConfigureAwait(true);
    }

    public async Task RequestThemeChangeAsync(Theme theme, ThemeApplicationTarget target)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _changeGate.WaitAsync().ConfigureAwait(true);
        try
        {
            await EnsureModuleAsync().ConfigureAwait(true);

            var previousShellTheme = Themes.ActiveShellTheme;
            var previousComponentTheme = Themes.ActiveComponentTheme;
            try
            {
                if (target == ThemeApplicationTarget.Components)
                {
                    await DevExpressThemeChangeService
                        .SetTheme(theme.DevExpressTheme)
                        .ConfigureAwait(true);
                    Themes.SetActiveComponentTheme(theme);
                }
                else
                {
                    Themes.SetActiveShellTheme(theme);
                }

                await ApplyClientThemeStateAsync().ConfigureAwait(true);
                await NotifyLoadedAsync(theme, target).ConfigureAwait(true);

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
                        ex)
                    .ConfigureAwait(true);
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

    private async Task EnsureModuleAsync()
    {
        try
        {
            if (_module is not null)
                return;

            var themeModulePath = FileVersionProvider.AddFileVersionToPath(
                "/",
                "switcher-resources/theme-controller.js");
            var importedModule = await JsRuntime
                .InvokeAsync<IJSObjectReference>("import", themeModulePath)
                .ConfigureAwait(true);
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

    private async Task ApplyClientThemeStateAsync()
    {
        try
        {
            await EnsureModuleAsync().ConfigureAwait(true);
            await _module!
                .InvokeVoidAsync(
                    "applyThemeState",
                    Themes.ActiveShellTheme.Name,
                    Themes.ActiveShellTheme.BootstrapThemeMode,
                    Themes.GetHighlightJSThemeCssUrl(Themes.ActiveShellTheme),
                    Themes.ActiveComponentTheme.Name,
                    null,
                    null)
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Applying or persisting the browser theme state failed.");
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), nameof(ApplyClientThemeStateAsync), ex);
            throw;
        }
    }

    private async Task NotifyLoadedAsync(Theme theme, ThemeApplicationTarget target)
    {
        try
        {
            if (Themes.ThemeLoadNotifier is not null)
            {
                await Themes.ThemeLoadNotifier
                    .NotifyThemeLoadedAsync(theme, target)
                    .ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "The {ThemeTarget} theme UI notification failed after the theme was applied.", target);
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), nameof(NotifyLoadedAsync), ex);
        }
    }

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
                    .SetTheme(previousComponentTheme.DevExpressTheme)
                    .ConfigureAwait(true);
                Themes.SetActiveComponentTheme(previousComponentTheme);
            }
            else
            {
                Themes.SetActiveShellTheme(previousShellTheme);
            }

            await ApplyClientThemeStateAsync().ConfigureAwait(true);
            await NotifyLoadedAsync(
                    target == ThemeApplicationTarget.Shell ? previousShellTheme : previousComponentTheme,
                    target)
                .ConfigureAwait(true);

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
                    await _module.DisposeAsync().ConfigureAwait(true);
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

    private sealed class BrowserThemeState
    {
        public string? ShellThemeName { get; set; }
        public string? ComponentThemeName { get; set; }
    }
}

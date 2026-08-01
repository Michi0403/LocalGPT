using DevExpress.Blazor;
using DevExpress.Blazor.Internal;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.JSInterop;

namespace LocalGPT.Components.Layout;

/// <summary>
/// Bridges LocalGPT's two theme layers to the browser and DevExpress runtime.
/// The browser persists both validated names; DevExpress changes only the inner component layer.
/// </summary>
public sealed class ThemeJsChangeDispatcher : ComponentBase, IThemeChangeRequestDispatcher, IAsyncDisposable, IDisposable
{
    [Parameter]
    public required string InitialShellThemeName { get; set; }

    [Parameter]
    public required string InitialComponentThemeName { get; set; }

    [Inject]
    private ISafeJSRuntime? JsRuntime { get; set; }

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

    private Theme? _pendingTheme;
    private ThemeApplicationTarget? _pendingTarget;
    private IJSObjectReference? _module;
    private DotNetObjectReference<ThemeJsChangeDispatcher>? _dotNetReference;
    private bool _disposed;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            Themes.ThemeChangeRequestDispatcher = this;

            BrowserThemeState? browserState = null;
            if (JsRuntime is not null)
            {
                var themeModulePath = FileVersionProvider.AddFileVersionToPath(
                    "/",
                    "switcher-resources/theme-controller.js");
                _module = await JsRuntime
                    .InvokeAsync<IJSObjectReference>("import", themeModulePath)
                    .ConfigureAwait(true);
                _dotNetReference = DotNetObjectReference.Create(this);
                browserState = await _module
                    .InvokeAsync<BrowserThemeState?>("readThemeState")
                    .ConfigureAwait(true);
            }

            var shellTheme = Themes.GetThemeOrDefault(
                browserState?.ShellThemeName ?? InitialShellThemeName);
            var componentTheme = Themes.GetThemeOrDefault(
                browserState?.ComponentThemeName ?? InitialComponentThemeName);

            Themes.SetActiveShellTheme(shellTheme);
            Themes.SetActiveComponentTheme(componentTheme);
            await DevExpressThemeChangeService
                .SetTheme(componentTheme.DevExpressTheme)
                .ConfigureAwait(true);

            if (_module is not null)
                await ApplyClientThemeStateAsync(null).ConfigureAwait(true);

            if (Themes.ThemeLoadNotifier is not null)
            {
                await Themes.ThemeLoadNotifier
                    .NotifyThemeLoadedAsync(shellTheme, ThemeApplicationTarget.Shell)
                    .ConfigureAwait(true);
                await Themes.ThemeLoadNotifier
                    .NotifyThemeLoadedAsync(componentTheme, ThemeApplicationTarget.Components)
                    .ConfigureAwait(true);
            }

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
                "The saved themes could not be fully restored. LocalGPT kept usable defaults.",
                "Theme warning");
        }

        await base.OnAfterRenderAsync(firstRender).ConfigureAwait(true);
    }

    public async Task RequestThemeChangeAsync(Theme theme, ThemeApplicationTarget target)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_pendingTheme?.Name.Equals(theme.Name, StringComparison.OrdinalIgnoreCase) == true
            && _pendingTarget == target)
        {
            return;
        }

        var previousShellTheme = Themes.ActiveShellTheme;
        var previousComponentTheme = Themes.ActiveComponentTheme;
        _pendingTheme = theme;
        _pendingTarget = target;

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

            if (_module is not null)
                await ApplyClientThemeStateAsync(target).ConfigureAwait(true);
            else
                await ThemeLoadedAsync(target.ToString()).ConfigureAwait(true);
        }
        catch (JSDisconnectedException)
        {
            _pendingTheme = null;
            _pendingTarget = null;
            Logger.LogDebug("Theme change ended because the browser circuit disconnected.");
        }
        catch (Exception ex)
        {
            _pendingTheme = null;
            _pendingTarget = null;
            var restored = false;
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

                if (_module is not null)
                    await ApplyClientThemeStateAsync(null).ConfigureAwait(true);
                restored = true;
            }
            catch (Exception rollbackException)
            {
                Logger.LogCritical(
                    rollbackException,
                    "Theme rollback failed after the {ThemeTarget} theme could not be applied.",
                    target);
                ComponentActivity.RecordFailure(
                    nameof(ThemeJsChangeDispatcher),
                    "RollbackThemeChange",
                    rollbackException);
            }

            Logger.LogError(
                ex,
                "The {ThemeTarget} theme change failed; selected theme details were omitted from logs.",
                target);
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), "RequestThemeChange", ex);
            Notifier.ShowError(
                "ComponentSafetyToasts",
                restored
                    ? "The selected theme could not be applied. LocalGPT restored the previous theme layer."
                    : "The selected theme and automatic rollback both failed. Reload the page to restore the saved themes.",
                "Theme change failed");
        }
    }

    [JSInvokable]
    public async Task ThemeLoadedAsync(string targetName)
    {
        try
        {
            if (!Enum.TryParse<ThemeApplicationTarget>(targetName, true, out var target))
                return;

            var loadedTheme = _pendingTheme;
            var loadedTarget = _pendingTarget;
            _pendingTheme = null;
            _pendingTarget = null;
            if (loadedTheme is null || loadedTarget != target)
                return;

            if (target == ThemeApplicationTarget.Shell)
                Themes.SetActiveShellTheme(loadedTheme);
            else
                Themes.SetActiveComponentTheme(loadedTheme);

            if (Themes.ThemeLoadNotifier is not null)
            {
                await Themes.ThemeLoadNotifier
                    .NotifyThemeLoadedAsync(loadedTheme, target)
                    .ConfigureAwait(true);
            }

            ComponentActivity.RecordInformation(
                nameof(ThemeJsChangeDispatcher),
                "ThemeLoaded",
                $"The {target} theme {loadedTheme.Name} finished loading.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Theme-loaded callback failed; theme details were omitted from logs.");
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), nameof(ThemeLoadedAsync), ex);
            throw;
        }
    }

    private ValueTask ApplyClientThemeStateAsync(ThemeApplicationTarget? callbackTarget)
    {
        if (_module is null || _dotNetReference is null)
            return ValueTask.CompletedTask;

        return _module.InvokeVoidAsync(
            "applyThemeState",
            Themes.ActiveShellTheme.Name,
            Themes.ActiveShellTheme.BootstrapThemeMode,
            Themes.GetHighlightJSThemeCssUrl(Themes.ActiveShellTheme),
            Themes.ActiveComponentTheme.Name,
            callbackTarget?.ToString(),
            _dotNetReference);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (ReferenceEquals(Themes.ThemeChangeRequestDispatcher, this))
            Themes.ThemeChangeRequestDispatcher = null;

        _pendingTheme = null;
        _pendingTarget = null;
        _dotNetReference?.Dispose();
        _dotNetReference = null;

        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync().ConfigureAwait(true);
            }
            catch (JSDisconnectedException)
            {
                // The browser circuit already ended.
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Theme JavaScript module disposal failed.");
                ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), nameof(DisposeAsync), ex);
            }
            finally
            {
                _module = null;
            }
        }

        GC.SuppressFinalize(this);
    }

    void IDisposable.Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (ReferenceEquals(Themes.ThemeChangeRequestDispatcher, this))
            Themes.ThemeChangeRequestDispatcher = null;
        _pendingTheme = null;
        _pendingTarget = null;
        _dotNetReference?.Dispose();
        _dotNetReference = null;
        GC.SuppressFinalize(this);
    }

    private sealed record BrowserThemeState(string? ShellThemeName, string? ComponentThemeName);
}

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
/// Bridges LocalGPT's theme selector to DevExpress' supported runtime theme service.
/// JavaScript is used only for the Bootstrap color-mode attribute, the persisted cookie,
/// and the optional Highlight.js stylesheet. DevExpress owns its component theme resources.
/// </summary>
public sealed class ThemeJsChangeDispatcher : ComponentBase, IThemeChangeRequestDispatcher, IAsyncDisposable, IDisposable
{
    [Parameter]
    public required string InitialThemeName { get; set; }

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
            Themes.SetActiveThemeByName(InitialThemeName);
            await DevExpressThemeChangeService
                .SetTheme(Themes.ActiveTheme.DevExpressTheme)
                .ConfigureAwait(true);

            if (JsRuntime is not null)
            {
                var themeModulePath = FileVersionProvider.AddFileVersionToPath(
                    "/",
                    "switcher-resources/theme-controller.js");
                _module = await JsRuntime
                    .InvokeAsync<IJSObjectReference>("import", themeModulePath)
                    .ConfigureAwait(true);
                _dotNetReference = DotNetObjectReference.Create(this);
                await ApplyClientThemeStateAsync(Themes.ActiveTheme).ConfigureAwait(true);
            }

            ComponentActivity.RecordInformation(
                nameof(ThemeJsChangeDispatcher),
                "ThemeDispatcherReady",
                $"Theme dispatcher initialized for {Themes.ActiveTheme.Name}.");
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
                "The saved theme could not be fully restored. LocalGPT kept a usable default theme.",
                "Theme warning");
        }

        await base.OnAfterRenderAsync(firstRender).ConfigureAwait(true);
    }

    public async Task RequestThemeChangeAsync(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_pendingTheme?.Name.Equals(theme.Name, StringComparison.OrdinalIgnoreCase) == true)
            return;

        var previousTheme = Themes.ActiveTheme;
        _pendingTheme = theme;

        try
        {
            await DevExpressThemeChangeService
                .SetTheme(theme.DevExpressTheme)
                .ConfigureAwait(true);
            Themes.SetActiveTheme(theme);

            if (_module is not null)
                await ApplyClientThemeStateAsync(theme).ConfigureAwait(true);
            else
                await ThemeLoadedAsync().ConfigureAwait(true);
        }
        catch (JSDisconnectedException)
        {
            _pendingTheme = null;
            Logger.LogDebug("Theme change ended because the browser circuit disconnected.");
        }
        catch (Exception ex)
        {
            _pendingTheme = null;
            var restored = false;
            try
            {
                await DevExpressThemeChangeService
                    .SetTheme(previousTheme.DevExpressTheme)
                    .ConfigureAwait(true);
                Themes.SetActiveTheme(previousTheme);
                restored = true;
            }
            catch (Exception rollbackException)
            {
                Logger.LogCritical(
                    rollbackException,
                    "Theme rollback to {PreviousTheme} failed after {ThemeName} could not be applied.",
                    previousTheme.Name,
                    theme.Name);
                ComponentActivity.RecordFailure(
                    nameof(ThemeJsChangeDispatcher),
                    "RollbackThemeChange",
                    rollbackException);
            }

            Logger.LogError(
                ex,
                "Theme change from {PreviousTheme} to {ThemeName} failed.",
                previousTheme.Name,
                theme.Name);
            ComponentActivity.RecordFailure(nameof(ThemeJsChangeDispatcher), "RequestThemeChange", ex);
            Notifier.ShowError(
                "ComponentSafetyToasts",
                restored
                    ? "The selected theme could not be applied. LocalGPT restored the previous theme."
                    : "The selected theme and automatic rollback both failed. Reload the page to restore the saved default theme.",
                "Theme change failed");
        }
    }

    [JSInvokable]
    public async Task ThemeLoadedAsync()
    {
        var loadedTheme = _pendingTheme;
        _pendingTheme = null;
        if (loadedTheme is null)
            return;

        Themes.SetActiveTheme(loadedTheme);
        if (Themes.ThemeLoadNotifier is not null)
            await Themes.ThemeLoadNotifier.NotifyThemeLoadedAsync(loadedTheme).ConfigureAwait(true);

        ComponentActivity.RecordInformation(
            nameof(ThemeJsChangeDispatcher),
            "ThemeLoaded",
            $"Theme {loadedTheme.Name} finished loading.");
    }

    private ValueTask ApplyClientThemeStateAsync(Theme theme)
    {
        if (_module is null || _dotNetReference is null)
            return ValueTask.CompletedTask;

        return _module.InvokeVoidAsync(
            "applyThemeState",
            theme.Name,
            theme.BootstrapThemeMode,
            Themes.GetHighlightJSThemeCssUrl(theme),
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
        _dotNetReference?.Dispose();
        _dotNetReference = null;
        GC.SuppressFinalize(this);
    }
}

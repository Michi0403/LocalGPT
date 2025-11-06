using DevExpress.Blazor.Internal;
using LocalGPT.BusinessObjects;
using LocalGPT.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LocalGPT.Components.Layout
{
    public class ThemeJsChangeDispatcher : ComponentBase, IThemeChangeRequestDispatcher, IAsyncDisposable, IDisposable
    {
        [Parameter]
        public required string InitialThemeName { get; set; }
        [Inject]
        private ISafeJSRuntime? JsRuntime { get; set; }
        [Inject]
        private ThemeService Themes { get; set; } = new ThemeService();

        private Theme? _pendingTheme;
        private IJSObjectReference? _module;
        private bool disposedValue;







        protected override async Task OnAfterRenderAsync(bool firstRender)
        {

            if (firstRender && JsRuntime != null)
                _module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./switcher-resources/theme-controller.js").ConfigureAwait(false);
            Themes.ThemeChangeRequestDispatcher = this;
            if (Themes.ActiveTheme == null)
                Themes.SetActiveThemeByName(InitialThemeName);
            await base.OnAfterRenderAsync(firstRender).ConfigureAwait(false);
        }

        public async void RequestThemeChange(Theme theme)
        {
            try
            {
                if (_pendingTheme == theme) return;
                _pendingTheme = theme;

                if (_module != null)
                    await _module.InvokeVoidAsync("ThemeController.setStylesheetLinks",
                        theme.Name,
                        Themes.GetBootstrapThemeCssUrl(theme),
                        theme.BootstrapThemeMode,
                        Themes.GetThemeCssUrl(theme),
                        Themes.GetHighlightJSThemeCssUrl(theme),
                        DotNetObjectReference.Create(this)).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error requesting theme:{theme.Name} change: {ex.ToString()}");

            }

        }

        [JSInvokable]
        public async Task ThemeLoadedAsync()
        {
            try
            {
                if (Themes.ThemeLoadNotifier != null && _pendingTheme != null)
                {
                    await Themes.ThemeLoadNotifier.NotifyThemeLoadedAsync(_pendingTheme).ConfigureAwait(false);
                }
                _pendingTheme = null;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error notifying theme loaded for:{_pendingTheme?.Name} change: {ex.ToString()}");

            }

        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                GC.SuppressFinalize(this);
                if (_module != null)
                    await _module.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {

            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Themes.ThemeChangeRequestDispatcher == this)
                    Themes.ThemeChangeRequestDispatcher = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _pendingTheme = null;
                }



                disposedValue = true;
            }
        }








        void IDisposable.Dispose()
        {

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

}

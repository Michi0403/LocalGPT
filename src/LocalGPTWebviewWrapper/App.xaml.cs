using LocalGPT;
using Microsoft.AspNetCore.Builder;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebView2_WinUI3_Sample
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Stores the internal window state used by <see cref="App"/> while executing its surrounding workflow.
        /// </summary>
        private Window? _window;
        /// <summary>
        /// Stores the internal web app state used by <see cref="App"/> while executing its surrounding workflow.
        /// </summary>
        private WebApplication? _webApp;
        /// <summary>
        /// Stores the internal owns web host state used by <see cref="App"/> while executing its surrounding workflow.
        /// </summary>
        private bool _ownsWebHost;
        /// <summary>
        /// Stores the internal base URL state used by <see cref="App"/> while executing its surrounding workflow.
        /// </summary>
        private string _baseUrl = string.Empty;

        /// <summary>
        /// Initializes the singleton application object.
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Starts or reconnects to the loopback LocalGPT host and then opens the WebView shell.
        /// The installer-provided positional port argument is forwarded unchanged to LocalGPT.Program.
        /// </summary>
        /// <param name="args">Args value supplied to the app operation and used when producing its result.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                var startupArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
                _webApp = LocalGPT.Program.BuildWebApp(startupArgs);

                try
                {
                    // Do not use ConfigureAwait(false) in the WinUI launch path: window creation and
                    // activation must continue on the UI dispatcher thread.
                    await _webApp.StartAsync();
                    _ownsWebHost = true;
                }
                catch (Exception startupException)
                {
                    if (!await IsExistingLocalGptReachableAsync(LocalGPT.Program.BaseUrl))
                        throw;

                    // A previous LocalGPT desktop process can still own the installer-selected port.
                    // Reuse that verified LocalGPT /health endpoint rather than showing a dead shell.
                    Debug.WriteLine($"LocalGPT host already active at {LocalGPT.Program.BaseUrl}: {startupException.Message}");
                    await _webApp.DisposeAsync();
                    _webApp = null;
                    _ownsWebHost = false;
                }

                _baseUrl = LocalGPT.Program.BaseUrl;
                OpenMainWindow(_baseUrl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await DisposeOwnedHostAsync();
                OpenStartupFailureWindow(ex);
            }
        }

        /// <summary>
        /// Opens main window for <see cref="App"/>, keeping the operation consistent with the state and invariants of the surrounding app workflow.
        /// </summary>
        /// <param name="baseUrl">Base url value supplied to the app operation and used when producing its result.</param>
        private void OpenMainWindow(string baseUrl)
        {
            _window = new MainWindow(baseUrl)
            {
                Title = "LocalGPT by Michi0403"
            };

            var iconPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "favicon.ico");
            if (File.Exists(iconPath))
                _window.AppWindow.SetIcon(iconPath);

            _window.Closed += async (_, _) => await DisposeOwnedHostAsync();
            _window.Activate();
        }

        /// <summary>
        /// Opens startup failure window for <see cref="App"/>, keeping the operation consistent with the state and invariants of the surrounding app workflow.
        /// </summary>
        /// <param name="exception">Exception value supplied to the app operation and used when producing its result.</param>
        private void OpenStartupFailureWindow(Exception exception)
        {
            var message = $"LocalGPT could not start.\n\n{exception.GetType().Name}: {exception.Message}\n\n" +
                          $"Expected address: {LocalGPT.Program.BaseUrl}\n" +
                          "Review the LocalGPT application log and verify that the installer-selected port is not owned by another application.";

            _window = new Window
            {
                Title = "LocalGPT startup failed",
                Content = new ScrollViewer
                {
                    Padding = new Thickness(24),
                    Content = new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        IsTextSelectionEnabled = true
                    }
                }
            };
            _window.Activate();
        }

        /// <summary>
        /// Performs dispose owned host for <see cref="App"/>, keeping the operation consistent with the state and invariants of the surrounding app workflow.
        /// </summary>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task DisposeOwnedHostAsync()
        {
            if (!_ownsWebHost || _webApp is null)
                return;

            try
            {
                await _webApp.StopAsync();
                await _webApp.DisposeAsync();
            }
            finally
            {
                _ownsWebHost = false;
                _webApp = null;
            }
        }

        /// <summary>
        /// Determines whether existing LocalGPT reachable for <see cref="App"/>, keeping the operation consistent with the state and invariants of the surrounding app workflow.
        /// </summary>
        /// <param name="baseUrl">Base url value supplied to the app operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        private static async Task<bool> IsExistingLocalGptReachableAsync(string baseUrl)
        {
            try
            {
                using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
                using var response = await http.GetAsync("/health", cancellation.Token);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
            {
                Debug.WriteLine($"Existing LocalGPT health probe failed: {ex}");
                return false;
            }
        }
    }
}

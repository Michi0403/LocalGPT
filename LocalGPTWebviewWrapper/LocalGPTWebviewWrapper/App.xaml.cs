using LocalGPT;
using Microsoft.AspNetCore.Builder;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using WinRT.Interop;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WebView2_WinUI3_Sample
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {

        private Window _window;
        private WebApplication _webApp;
        private string _baseUrl = "";

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            // If you're shipping a fixed-version WebView2 Runtime with your app, un-comment the
            // following lines of code, and change the version number to the version number of the
            // WebView2 Runtime that you're packaging and shipping to users:

            // StorageFolder localFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            // String fixedPath = Path.Combine(localFolder.Path, "FixedRuntime\\130.0.2849.39");
            // Debug.WriteLine($"Launch path [{localFolder.Path}]");
            // Debug.WriteLine($"FixedRuntime path [{fixedPath}]");
            // Environment.SetEnvironmentVariable("WEBVIEW2_BROWSER_EXECUTABLE_FOLDER", fixedPath);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _webApp = LocalGPT.Program.BuildWebApp();
            await _webApp.StartAsync();          // non-blocking
            _baseUrl = $"https://localhost:{LocalGPT.Program.Port}";

            // Optionally: wait for /health before showing UI (keeps initial nav smooth)
            await WaitForHealthAsync(_baseUrl);

            _window = new MainWindow(_baseUrl);
            _window.Title = "WebView2 Hosts Blazor Backend";
            // ✅ Set window icon (shows in taskbar, Alt+Tab, and title)
            var appWindow = _window.AppWindow;

            // Set your icon file (must be an .ico, not .png)
            string iconPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "favicon.ico");
            if (File.Exists(iconPath))
            {
                appWindow.SetIcon(iconPath);
            }
            _window.Activate();

            _window.Closed += async (_, __) =>
            {
                if (_webApp != null)
                {
                    await _webApp.StopAsync();
                    await _webApp.DisposeAsync();
                }
            };
            //_window = new MainWindow();
            //_window.Activate();
        }

        private static async Task WaitForHealthAsync(string baseUrl)
        {
            using var http = new HttpClient(new HttpClientHandler
            {
                // dev only: trust localhost dev cert
                ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true
            });

            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var resp = await http.GetAsync($"{baseUrl}/health");
                    if (resp.IsSuccessStatusCode) return;
                }
                catch { /* retry */ }
                await Task.Delay(200);
            }
        }
    }
}

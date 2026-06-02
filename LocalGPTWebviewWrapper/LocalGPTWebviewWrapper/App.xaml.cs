using LocalGPT;
using Microsoft.AspNetCore.Builder;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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
            WriteStartupDiagnostic(args.Arguments);
            _webApp = LocalGPT.Program.BuildWebApp();
            await _webApp.StartAsync();          // non-blocking
            _baseUrl = $"http://127.0.0.1:{LocalGPT.Program.Port}";

            // Optionally: wait for /health before showing UI (keeps initial nav smooth)
            await WaitForHealthAsync(_baseUrl);

            var runWebView2Diagnostics =
                (args.Arguments?.Contains("--webview2-smoke", StringComparison.OrdinalIgnoreCase) ?? false) ||
                string.Equals(Environment.GetEnvironmentVariable("LOCALGPT_WEBVIEW2_SMOKE"), "1", StringComparison.OrdinalIgnoreCase) ||
                IsWebView2SmokeFlagPresent();
            var exitAfterWebView2Diagnostics =
                string.Equals(Environment.GetEnvironmentVariable("LOCALGPT_WEBVIEW2_SMOKE_EXIT"), "1", StringComparison.OrdinalIgnoreCase) ||
                IsWebView2SmokeFlagExitRequested();

            _window = new MainWindow(_baseUrl, runWebView2Diagnostics, exitAfterWebView2Diagnostics);
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

        private static bool IsWebView2SmokeFlagPresent()
        {
            return GetWebView2SmokeFlagPaths().Any(File.Exists);
        }

        private static bool IsWebView2SmokeFlagExitRequested()
        {
            var exitRequested = false;
            foreach (var path in GetWebView2SmokeFlagPaths().Where(File.Exists))
            {
                try
                {
                    var content = File.ReadAllText(path);
                    File.Delete(path);
                    exitRequested |= content.Contains("exit", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    // A stale flag should not block normal app launch.
                }
            }

            return exitRequested;
        }

        private static string[] GetWebView2SmokeFlagPaths()
        {
            return GetRuntimeDirectories()
                .Select(directory => Path.Combine(directory, "webview2-smoke.flag"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string[] GetRuntimeDirectories()
        {
            var directories = new List<string>();
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var localAppDataEnvironment = Environment.GetEnvironmentVariable("LOCALAPPDATA");

            AddRuntimeDirectory(directories, localAppData);
            AddRuntimeDirectory(directories, localAppDataEnvironment);

            var packageFamilyName = GetPackageFamilyName();
            if (!string.IsNullOrWhiteSpace(localAppDataEnvironment) &&
                !string.IsNullOrWhiteSpace(packageFamilyName))
            {
                AddRuntimeDirectory(
                    directories,
                    Path.Combine(localAppDataEnvironment, "Packages", packageFamilyName, "LocalCache", "Local"));
            }

            return directories
                .Where(directory => !string.IsNullOrWhiteSpace(directory))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static void AddRuntimeDirectory(ICollection<string> directories, string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
                return;

            directories.Add(Path.Combine(baseDirectory, "LocalGPT", "runtime"));
        }

        private static string GetPackageFamilyName()
        {
            try
            {
                return Windows.ApplicationModel.Package.Current.Id.FamilyName;
            }
            catch
            {
                return null;
            }
        }

        private static void WriteStartupDiagnostic(string arguments)
        {
            try
            {
                foreach (var directory in GetRuntimeDirectories())
                {
                    Directory.CreateDirectory(directory);
                    var path = Path.Combine(directory, $"webview2-startup-{Environment.ProcessId}.json");
                    File.WriteAllText(path, JsonSerializer.Serialize(new
                    {
                        Environment.ProcessId,
                        Arguments = arguments,
                        LocalApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        LocalAppDataEnvironment = Environment.GetEnvironmentVariable("LOCALAPPDATA"),
                        PackageFamilyName = GetPackageFamilyName(),
                        StartedAtUtc = DateTimeOffset.UtcNow
                    }, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch
            {
                // Startup diagnostics must never block app launch.
            }
        }
    }
}

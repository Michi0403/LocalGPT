using DevExpress.XtraRichEdit.Import.Html;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WinRT.Interop;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WebView2_WinUI3_Sample
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly string _baseUrl;
        //public MainWindow()
        //{
        //    this.InitializeComponent();

        //    Closed += (obj, eventArgs) =>
        //    {
        //        if (WebView2 != null)
        //        {
        //            // Ensure that WebView2 resources are released when
        //            // the MainWindow is closed. This fixes an issue where
        //            // the sample app was not properly shutting down when run
        //            // in debug mode from within Visual Studio due to an
        //            // an expected winrt::hresult_error exception in the WinUI3
        //            // WebView2::CoreWebView2HasInvalidState method.
        //            // (https://github.com/microsoft/microsoft-ui-xaml/blob/main/src/controls/dev/WebView2/WebView2.cpp)
        //            //
        //            // See here for more details regarding the WebView2
        //            // lifecycle in WinUI3 and the Close() method.
        //            // https://github.com/microsoft/microsoft-ui-xaml/issues/4752#issuecomment-819687363
        //            WebView2.Close();
        //        }
        //    };

        //    //AddressBar.Text = "https://developer.microsoft.com/en-us/microsoft-edge/webview2/";

        //    WebView2.NavigationCompleted += WebView2_NavigationCompleted;
        //    WebView2.CoreWebView2Initialized += WebView2_CoreWebView2Initialized;

        //    //WebView2.Source = new Uri(AddressBar.Text);
        //    StatusUpdate("Ready");
        //    SetTitle();
        //}

        public MainWindow(
            string baseUrl,
            bool runDiagnostics = false,
            bool exitAfterDiagnostics = false,
            bool isE2E = false)
        {
            InitializeComponent();
            _baseUrl = baseUrl;
            Closed += (_, __) => WebView2?.Close();

            //AddressBar.Text = _baseUrl;          // prefill with local server
            //WebView2.NavigationCompleted += WebView2_NavigationCompleted;
            WebView2.CoreWebView2Initialized += WebView2_CoreWebView2Initialized;
            WebView2.RequestedTheme = ElementTheme.Default;

            var initialUrl = _baseUrl;

            _ = InitializeWebViewAsync(initialUrl);
            SetTitle();
        }

        private async Task InitializeWebViewAsync(string initialUrl)
        {
            try
            {
                
                await WebView2.EnsureCoreWebView2Async(  );
                WebView2.Source = new Uri(initialUrl);
                StatusUpdate("Ready");
            }
            catch (Exception ex)
            {
                StatusUpdate($"Error initializing WebView2: {ex.Message}");
            }
        }

        private void StatusUpdate(string message)
        {
            Debug.WriteLine(message);
        }

        private void WebView2_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            if (args.Exception != null)
            {
                StatusUpdate($"Error initializing WebView2: {args.Exception.Message}");
            }
            else
            {
                //if (sender.CoreWebView2 is not null)
                //    sender.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;

                SetTitle(sender);
            }
        }

        private void CoreWebView2_DownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs args)
        {
            try
            {
                var downloadsDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    "LocalGPT");
                Directory.CreateDirectory(downloadsDirectory);

                var fileName = Path.GetFileName(args.ResultFilePath);
                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = "LocalGPT-artifact";

                args.ResultFilePath = Path.Combine(downloadsDirectory, fileName);
                StatusUpdate($"Downloading artifact to {args.ResultFilePath}");
            }
            catch (Exception ex)
            {
                StatusUpdate($"Could not configure WebView2 download path: {ex.Message}");
            }
        }

        private async void WebView2_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            StatusUpdate("Navigation complete");
          
            // Update the address bar with the full URL that was navigated to.
            //AddressBar.Text = sender.Source.ToString();
        }

    
        
        private static IEnumerable<string> GetRuntimeFlagPaths(string fileName)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var localAppDataEnvironment = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            foreach (var directory in GetRuntimeDirectories(localAppData, localAppDataEnvironment))
                yield return Path.Combine(directory, fileName);
        }

        private static IEnumerable<string> GetRuntimeDirectories(params string[] baseDirectories)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var baseDirectory in baseDirectories)
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                    continue;

                var runtimeDirectory = Path.Combine(baseDirectory, "LocalGPT", "runtime");
                if (seen.Add(runtimeDirectory))
                    yield return runtimeDirectory;
            }

            var packageFamilyName = GetPackageFamilyName();
            var localAppDataEnvironment = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrWhiteSpace(packageFamilyName) &&
                !string.IsNullOrWhiteSpace(localAppDataEnvironment))
            {
                var packageRuntimeDirectory = Path.Combine(
                    localAppDataEnvironment,
                    "Packages",
                    packageFamilyName,
                    "LocalCache",
                    "Local",
                    "LocalGPT",
                    "runtime");
                if (seen.Add(packageRuntimeDirectory))
                    yield return packageRuntimeDirectory;
            }
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

        private bool TryCreateUri(String potentialUri, out Uri result)
        {
            StatusUpdate("TryCreateUri");

            Uri uri;
            if ((Uri.TryCreate(potentialUri, UriKind.Absolute, out uri) || Uri.TryCreate("http://" + potentialUri, UriKind.Absolute, out uri)) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                result = uri;
                return true;
            }
            else
            {
                StatusUpdate("Unable to configure URI");
                result = null;
                return false;
            }
        }

        private void SetTitle(WebView2 webView2 = null)
        {
            var packageDisplayName = "LocalGPT";
            try
            {
                packageDisplayName = Windows.ApplicationModel.Package.Current.DisplayName;
            }
            catch
            {
                // Unpackaged/debug launches do not always have package identity.
            }
            var webView2Version = (webView2 != null) ? " - " + "LocalGPT by Michi0403" : string.Empty;
            Title = $"{packageDisplayName}{webView2Version}";
        }


        private sealed class WebView2DiagnosticSnapshot
        {
            public string RunId { get; set; } = string.Empty;
            public DateTimeOffset CapturedAtUtc { get; set; }
            public string RequestedUri { get; set; } = string.Empty;
            public bool IsSuccess { get; set; }
            public string WebErrorStatus { get; set; } = string.Empty;
            public string PageJson { get; set; } = string.Empty;
            public string Error { get; set; } = string.Empty;
        }
    }
}

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
using System.Text.Json;
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
        private readonly bool _runDiagnostics;
        private readonly bool _exitAfterDiagnostics;
        private readonly Queue<string> _diagnosticRoutes = new();
        private readonly string _diagnosticRunId = DateTime.Now.ToString("yyyyMMdd-HHmmss");
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

        public MainWindow(string baseUrl, bool runDiagnostics = false, bool exitAfterDiagnostics = false)
        {
            InitializeComponent();
            _baseUrl = baseUrl;
            _runDiagnostics = runDiagnostics;
            _exitAfterDiagnostics = exitAfterDiagnostics;
            if (_runDiagnostics)
            {
                _diagnosticRoutes.Enqueue("/Chat");
                _diagnosticRoutes.Enqueue("/minecraft-mod-builder");
            }

            // Let system theme decide (don’t force dark)
        
            Closed += (_, __) => WebView2?.Close();

            //AddressBar.Text = _baseUrl;          // prefill with local server
            WebView2.NavigationCompleted += WebView2_NavigationCompleted;
            WebView2.CoreWebView2Initialized += WebView2_CoreWebView2Initialized;
            WebView2.RequestedTheme = ElementTheme.Default;
            
            WebView2.Source = new Uri(_baseUrl); // initial navigation
            StatusUpdate("Ready");
            SetTitle();
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
                SetTitle(sender);
            }
        }

        private async void WebView2_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            StatusUpdate("Navigation complete");
            if (_runDiagnostics)
                await CaptureWebView2DiagnosticsAsync(sender, args);

            // Update the address bar with the full URL that was navigated to.
            //AddressBar.Text = sender.Source.ToString();
        }

        private async Task CaptureWebView2DiagnosticsAsync(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            var snapshot = new WebView2DiagnosticSnapshot
            {
                RunId = _diagnosticRunId,
                CapturedAtUtc = DateTimeOffset.UtcNow,
                RequestedUri = sender.Source?.ToString() ?? string.Empty,
                IsSuccess = args.IsSuccess,
                WebErrorStatus = args.WebErrorStatus.ToString()
            };

            try
            {
                if (sender.CoreWebView2 != null && args.IsSuccess)
                {
                    snapshot.PageJson = await sender.CoreWebView2.ExecuteScriptAsync("""
                        (() => JSON.stringify({
                            url: location.href,
                            title: document.title,
                            readyState: document.readyState,
                            bodyText: (document.body && document.body.innerText ? document.body.innerText : '').slice(0, 4000),
                            hasDxAiChatSurface: !!document.querySelector('.demo-chat, dxbl-ai-chat, .dxbl-aichat'),
                            hasMinecraftBuilderText: (document.body && document.body.innerText ? document.body.innerText : '').includes('Minecraft Mod Builder'),
                            hasSetupText: (document.body && document.body.innerText ? document.body.innerText : '').includes('Setup')
                        }))()
                        """);
                }
            }
            catch (Exception ex)
            {
                snapshot.Error = ex.Message;
            }

            await WriteDiagnosticSnapshotAsync(snapshot);

            if (_diagnosticRoutes.Count > 0)
            {
                var nextRoute = _diagnosticRoutes.Dequeue();
                sender.Source = new Uri($"{_baseUrl}{nextRoute}");
                return;
            }

            if (_exitAfterDiagnostics)
                Close();
        }

        private static async Task WriteDiagnosticSnapshotAsync(WebView2DiagnosticSnapshot snapshot)
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LocalGPT",
                "WebView2Diagnostics");
            Directory.CreateDirectory(directory);

            var routeName = snapshot.RequestedUri
                .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(":", "_")
                .Replace("/", "_")
                .Replace("\\", "_");
            var path = Path.Combine(directory, $"webview2-{snapshot.RunId}-{routeName}.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
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

        //private void TryNavigate()
        //{
        //    StatusUpdate("TryNavigate");

        //    Uri destinationUri;
        //    if (TryCreateUri(AddressBar.Text, out destinationUri))
        //    {
        //        WebView2.Source = destinationUri;
        //    }
        //    else
        //    {
        //        StatusUpdate("URI couldn't be figured out use it as a bing search term");

        //        //String bingString = $"https://www.bing.com/search?q={Uri.EscapeDataString(AddressBar.Text)}";
        //        if (TryCreateUri(bingString, out destinationUri))
        //        {
        //            //AddressBar.Text = destinationUri.AbsoluteUri;
        //            WebView2.Source = destinationUri;
        //        }
        //        else
        //        {
        //            StatusUpdate("URI couldn't be configured as bing search term, giving up");
        //        }
        //    }
        //}

        //private void Go_OnClick(object sender, RoutedEventArgs e)
        //{
        //    StatusUpdate("Go_OnClick: " + AddressBar.Text);

        //    TryNavigate();
        //}

        //private void AddressBar_KeyDown(object sender, KeyRoutedEventArgs e)
        //{
        //    if (e.Key == Windows.System.VirtualKey.Enter)
        //    {
        //        StatusUpdate("AddressBar_KeyDown [Enter]: " + AddressBar.Text);

        //        e.Handled = true;
        //        TryNavigate();
        //    }
        //}

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
            var webView2Version = (webView2 != null) ? " - " + GetWebView2Version(webView2) : string.Empty;
            Title = $"{packageDisplayName}{webView2Version}";
        }

        private string GetWebView2Version(WebView2 webView2)
        {
            var runtimeVersion = webView2.CoreWebView2.Environment.BrowserVersionString;

            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions();
            var targetVersionMajorAndRest = options.TargetCompatibleBrowserVersion;
            var versionList = targetVersionMajorAndRest.Split('.');
            if (versionList.Length != 4)
            {
                return "Invalid SDK build version";
            }
            var sdkVersion = versionList[2] + "." + versionList[3];

            return $"{runtimeVersion}; {sdkVersion}";
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

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WebView2_WinUI3_Sample
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        /// <summary>
        /// Stores the internal base URL state used by <see cref="MainWindow"/> while executing its surrounding workflow.
        /// </summary>
        private readonly string _baseUrl;

        /// <summary>
        /// Initializes a new <see cref="MainWindow"/> instance and captures the dependencies or initial state required by its main window workflow.
        /// </summary>
        /// <param name="baseUrl">Base url value supplied to the main window operation and used when producing its result.</param>
        public MainWindow(string baseUrl)
        {
            InitializeComponent();
            _baseUrl = baseUrl;

            // Let system theme decide (don’t force dark)
        
            Closed += (_, __) => WebView2?.Close();

            //AddressBar.Text = _baseUrl;          // prefill with local server
            WebView2.NavigationCompleted += WebView2_NavigationCompleted;
            WebView2.CoreWebView2Initialized += WebView2_CoreWebView2Initialized;
            WebView2.RequestedTheme = ElementTheme.Default;
            
            WebView2.Source = new Uri(_baseUrl); // initial navigation
            SetTitle();
        }

        /// <summary>
        /// Performs status update for <see cref="MainWindow"/>, keeping the operation consistent with the state and invariants of the surrounding main window workflow.
        /// </summary>
        /// <param name="message">Message value supplied to the main window operation and used when producing its result.</param>
        private void StatusUpdate(string message)
        {
            Debug.WriteLine(message);
        }

        /// <summary>
        /// Performs web view2 core web view2 initialized for <see cref="MainWindow"/>, keeping the operation consistent with the state and invariants of the surrounding main window workflow.
        /// </summary>
        /// <param name="sender">Sender value supplied to the main window operation and used when producing its result.</param>
        /// <param name="args">Args value supplied to the main window operation and used when producing its result.</param>
        private void WebView2_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            if (args.Exception != null)
            {
                StatusUpdate($"Error initializing WebView2: {args.Exception.Message}");
            }
            else
            {
                try
                {
                    if (sender.CoreWebView2 is not null)
                    {
                        sender.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                        sender.CoreWebView2.Settings.AreDevToolsEnabled = true;
                        sender.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
                        sender.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                    }
                    SetTitle(sender);
                }
                catch (Exception exception)
                {
                    StatusUpdate($"Could not enable WebView developer controls: {exception}");
                }
            }
        }


        /// <summary>
        /// Performs core web view2 web message received for <see cref="MainWindow"/>, keeping the operation consistent with the state and invariants of the surrounding main window workflow.
        /// </summary>
        /// <param name="sender">Sender value supplied to the main window operation and used when producing its result.</param>
        /// <param name="args">Args value supplied to the main window operation and used when producing its result.</param>
        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                var message = args.TryGetWebMessageAsString();
                if (string.Equals(message, "localgpt-open-devtools", StringComparison.Ordinal))
                    WebView2.CoreWebView2?.OpenDevToolsWindow();
            }
            catch (Exception exception)
            {
                StatusUpdate($"Could not process a WebView host message: {exception}");
            }
        }

        /// <summary>
        /// Performs web view2 navigation completed for <see cref="MainWindow"/>, keeping the operation consistent with the state and invariants of the surrounding main window workflow.
        /// </summary>
        /// <param name="sender">Sender value supplied to the main window operation and used when producing its result.</param>
        /// <param name="args">Args value supplied to the main window operation and used when producing its result.</param>
        private void WebView2_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            StatusUpdate("Navigation complete");

            // Update the address bar with the full URL that was navigated to.
            //AddressBar.Text = sender.Source.ToString();
        }

        /// <summary>
        /// Attempts to create URI for <see cref="MainWindow"/>, keeping the operation consistent with the state and invariants of the surrounding main window workflow.
        /// </summary>
        /// <param name="potentialUri">Potential uri value supplied to the main window operation and used when producing its result.</param>
        /// <param name="result">Result value supplied to the main window operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        private bool TryCreateUri(string potentialUri, out Uri? result)
        {
            StatusUpdate("TryCreateUri");

            Uri? uri;
            if ((Uri.TryCreate(potentialUri, UriKind.Absolute, out uri) || Uri.TryCreate("http://" + potentialUri, UriKind.Absolute, out uri)) &&
                uri is not null &&
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

        /// <summary>
        /// Sets title for <see cref="MainWindow"/>, keeping the operation consistent with the state and invariants of the surrounding main window workflow.
        /// </summary>
        /// <param name="webView2">Web view2 value supplied to the main window operation and used when producing its result.</param>
        private void SetTitle(WebView2? webView2 = null)
        {
            Title = $"LocalGPT by Michi0403";
        }

        /// <summary>
        /// Retrieves web view2 version for <see cref="MainWindow"/>, keeping the operation consistent with the state and invariants of the surrounding main window workflow.
        /// </summary>
        /// <param name="webView2">Web view2 value supplied to the main window operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
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
    }
}

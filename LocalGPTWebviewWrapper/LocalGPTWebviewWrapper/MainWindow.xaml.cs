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
        private readonly bool _runDiagnostics;
        private readonly bool _exitAfterDiagnostics;
        private readonly bool _runGpuCouncilDiagnostics;
        private readonly bool _runFeatureRequestDiagnostics;
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
            _runGpuCouncilDiagnostics = string.Equals(
                Environment.GetEnvironmentVariable("LOCALGPT_WEBVIEW2_SMOKE_GPU_COUNCIL"),
                "1",
                StringComparison.OrdinalIgnoreCase) || ConsumeRuntimeFlag("webview2-smoke-gpu-council.flag");
            _runFeatureRequestDiagnostics = string.Equals(
                Environment.GetEnvironmentVariable("LOCALGPT_WEBVIEW2_SMOKE_FEATURE_REQUEST"),
                "1",
                StringComparison.OrdinalIgnoreCase) || ConsumeRuntimeFlag("webview2-smoke-feature-request.flag");
            if (_runDiagnostics)
            {
                _diagnosticRoutes.Enqueue("/Chat");
                _diagnosticRoutes.Enqueue("/model-council");
                _diagnosticRoutes.Enqueue("/database");
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
                if (sender.CoreWebView2 is not null)
                    sender.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;

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
                    await Task.Delay(TimeSpan.FromMilliseconds(1500));
                    await sender.CoreWebView2.ExecuteScriptAsync($"window.__localGptDiagRunGpuCouncil = {(_runGpuCouncilDiagnostics ? "true" : "false")};");
                    await sender.CoreWebView2.ExecuteScriptAsync($"window.__localGptDiagRunFeatureRequest = {(_runFeatureRequestDiagnostics ? "true" : "false")};");
                    await sender.CoreWebView2.ExecuteScriptAsync("""
                        (async () => {
                            const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));
                            const text = () => document.body && document.body.innerText ? document.body.innerText : '';
                            const buttons = () => Array.from(document.querySelectorAll('button'));
                            const findButton = (buttonText) => buttons()
                                .find(item => item.innerText && item.innerText.includes(buttonText));
                            const isButtonReady = (button) => !!button &&
                                !button.disabled &&
                                button.getAttribute('aria-disabled') !== 'true' &&
                                !button.classList.contains('dxbl-disabled');
                            const clickButton = (buttonText) => {
                                const button = findButton(buttonText);
                                if (!isButtonReady(button)) {
                                    return false;
                                }

                                button.click();
                                return true;
                            };
                            const waitFor = async (predicate, timeoutMs) => {
                                const deadline = Date.now() + timeoutMs;
                                while (Date.now() < deadline) {
                                    if (predicate()) {
                                        return true;
                                    }

                                    await sleep(500);
                                }

                                return false;
                            };
                            const labelInput = (labelText) => {
                                const label = Array.from(document.querySelectorAll('label'))
                                    .find(item => item.innerText && item.innerText.includes(labelText));
                                return label ? label.querySelector('input, select, textarea') : null;
                            };
                            const setInput = (labelText, value) => {
                                const input = labelInput(labelText);
                                if (!input) {
                                    return false;
                                }

                                input.value = value;
                                input.dispatchEvent(new Event('input', { bubbles: true }));
                                input.dispatchEvent(new Event('change', { bubbles: true }));
                                return true;
                            };
                            const setCheckbox = (labelText, checked) => {
                                const input = labelInput(labelText);
                                if (!input) {
                                    return false;
                                }

                                if (input.checked !== checked) {
                                    input.click();
                                }

                                return input.checked === checked;
                            };
                            const setCouncilModelSelection = (modelName) => {
                                let found = false;
                                for (const row of Array.from(document.querySelectorAll('.candidate-row'))) {
                                    const input = row.querySelector('input[type="checkbox"]');
                                    if (!input) {
                                        continue;
                                    }

                                    const shouldSelect = (row.innerText || '').includes(modelName);
                                    if (shouldSelect) {
                                        found = true;
                                    }

                                    if (input.checked !== shouldSelect) {
                                        input.click();
                                    }
                                }

                                return found;
                            };
                            const fetchCouncilArtifacts = async () => {
                                const artifacts = [];
                                for (const link of Array.from(document.querySelectorAll('a[href*="/__artifacts/council/"]')).slice(0, 8)) {
                                    const artifact = {
                                        name: (link.innerText || link.getAttribute('download') || '').trim(),
                                        href: link.href,
                                        status: null,
                                        bytes: null,
                                        ok: false,
                                        error: null
                                    };

                                    try {
                                        const response = await fetch(link.href);
                                        const buffer = await response.arrayBuffer();
                                        artifact.status = response.status;
                                        artifact.bytes = buffer.byteLength;
                                        artifact.ok = response.ok;
                                    }
                                    catch (error) {
                                        artifact.error = error && error.message ? error.message : String(error);
                                    }

                                    artifacts.push(artifact);
                                }

                                window.__localGptDiagCouncilArtifactDownloads = artifacts;
                                return artifacts;
                            };

                            window.__localGptDiagClickedCouncilFeatureChat = false;
                            window.__localGptDiagClickedCouncilLowGpuPreset = false;
                            window.__localGptDiagCouncilFeatureRequestSmoke = null;
                            window.__localGptDiagCouncilArtifactDownloads = [];
                            window.__localGptDiagMinecraftBuilderSmoke = null;
                            if (location.pathname.toLowerCase().includes('/model-council')) {
                                const featureSmoke = {
                                    clickedLowGpuPreset: false,
                                    clickedFeatureRequestChat: false,
                                    selectedGptOss: false,
                                    setMaxOutputTokens: false,
                                    setTimeoutSeconds: false,
                                    setCpuOnly: false,
                                    setGenerateArtifacts: false,
                                    clickedRunCouncil: false,
                                    artifactSectionVisible: false,
                                    error: null
                                };
                                window.__localGptDiagCouncilFeatureRequestSmoke = featureSmoke;

                                window.__localGptDiagClickedCouncilLowGpuPreset = clickButton('Low GPU Preset');
                                featureSmoke.clickedLowGpuPreset = window.__localGptDiagClickedCouncilLowGpuPreset;

                                window.__localGptDiagClickedCouncilFeatureChat = clickButton('Feature Request Chat');
                                featureSmoke.clickedFeatureRequestChat = window.__localGptDiagClickedCouncilFeatureChat;

                                if (window.__localGptDiagRunFeatureRequest) {
                                    try {
                                        await sleep(750);
                                        featureSmoke.selectedGptOss = setCouncilModelSelection('gpt-oss:20b');
                                        featureSmoke.setMaxOutputTokens = setInput('Max output tokens', '384');
                                        featureSmoke.setTimeoutSeconds = setInput('Timeout seconds', '600');
                                        featureSmoke.setCpuOnly = setCheckbox('CPU-only Ollama', true);
                                        featureSmoke.setGenerateArtifacts = setCheckbox('Generate code + DLL', true);
                                        featureSmoke.clickedRunCouncil = clickButton('Run Council');
                                        featureSmoke.artifactSectionVisible = await waitFor(
                                            () => text().includes('Downloadable Examples') && !!document.querySelector('a[href*="/__artifacts/council/"]'),
                                            600000);
                                        if (featureSmoke.artifactSectionVisible) {
                                            await fetchCouncilArtifacts();
                                        }
                                    }
                                    catch (error) {
                                        featureSmoke.error = error && error.message ? error.message : String(error);
                                    }
                                }
                            }

                            if (location.pathname.toLowerCase().includes('/minecraft-mod-builder')) {
                                const smoke = {
                                    clickedCreateDatapackZip: false,
                                    clickedCreateWorkspace: false,
                                    clickedRunCommand: false,
                                    clickedAskCouncil: false,
                                    setCouncilModel: false,
                                    setGpuCouncil: false,
                                    workspaceVisible: false,
                                    commandResultVisible: false,
                                    downloadHref: null,
                                    downloadStatus: null,
                                    downloadBytes: null,
                                    downloadOk: false,
                                    councilResultVisible: false,
                                    error: null
                                };
                                window.__localGptDiagMinecraftBuilderSmoke = smoke;

                                const findDownloadLink = () => Array.from(document.querySelectorAll('a'))
                                    .find(item => (item.innerText && item.innerText.includes('Download Datapack ZIP'))
                                        || (item.href && item.href.includes('/__artifacts/minecraft/')));

                                try {
                                    smoke.clickedCreateDatapackZip = clickButton('Create Datapack ZIP');
                                    if (smoke.clickedCreateDatapackZip) {
                                        smoke.commandResultVisible = await waitFor(
                                            () => text().includes('Command Result') || text().includes('Exit code') || !!findDownloadLink(),
                                            180000);
                                        smoke.workspaceVisible = text().includes('Workspace') && text().includes('Run Command');
                                    }
                                    else {
                                        smoke.clickedCreateWorkspace = clickButton('Create Workspace');
                                        smoke.workspaceVisible = await waitFor(() => text().includes('Workspace') && text().includes('Run Command'), 30000);

                                        await waitFor(() => isButtonReady(findButton('Run Command')), 10000);
                                        smoke.clickedRunCommand = clickButton('Run Command');
                                        smoke.commandResultVisible = await waitFor(() => text().includes('Command Result') || text().includes('Exit code'), 90000);
                                    }

                                    if (!findDownloadLink())
                                        await waitFor(() => !!findDownloadLink(), 30000);

                                    const downloadLink = findDownloadLink();
                                    if (downloadLink) {
                                        smoke.downloadHref = downloadLink.href;
                                        const response = await fetch(downloadLink.href);
                                        const buffer = await response.arrayBuffer();
                                        smoke.downloadStatus = response.status;
                                        smoke.downloadOk = response.ok;
                                        smoke.downloadBytes = buffer.byteLength;
                                    }

                                    if (window.__localGptDiagRunGpuCouncil) {
                                        smoke.setCouncilModel = setInput('Council models', 'gpt-oss:20b');
                                        smoke.setGpuCouncil = setCheckbox('CPU-only council', false);
                                        smoke.clickedAskCouncil = clickButton('Ask AI Council');
                                        smoke.councilResultVisible = await waitFor(() => text().includes('AI Council Log') || text().includes('Council plan saved'), 420000);
                                    }
                                }
                                catch (error) {
                                    smoke.error = error && error.message ? error.message : String(error);
                                }
                            }
                            return window.__localGptDiagClickedCouncilFeatureChat;
                        })()
                        """);
                    var path = sender.Source?.AbsolutePath.ToLowerInvariant() ?? string.Empty;
                    if (path.Contains("/minecraft-mod-builder", StringComparison.Ordinal))
                    {
                        await WaitForJavaScriptConditionAsync(
                            sender.CoreWebView2,
                            """
                            (() => {
                                const smoke = window.__localGptDiagMinecraftBuilderSmoke;
                                if (!smoke) return false;
                                if (smoke.error) return true;
                                if (window.__localGptDiagRunGpuCouncil) {
                                    return !!smoke.councilResultVisible;
                                }
                                return !!smoke.downloadOk || !!smoke.downloadStatus || !!smoke.error;
                            })()
                            """,
                            _runGpuCouncilDiagnostics ? TimeSpan.FromMinutes(8) : TimeSpan.FromMinutes(3));
                    }
                    else if (path.Contains("/model-council", StringComparison.Ordinal) && _runFeatureRequestDiagnostics)
                    {
                        await WaitForJavaScriptConditionAsync(
                            sender.CoreWebView2,
                            """
                            (() => {
                                const smoke = window.__localGptDiagCouncilFeatureRequestSmoke;
                                if (!smoke) return false;
                                if (smoke.error) return true;
                                return smoke.artifactSectionVisible && Array.isArray(window.__localGptDiagCouncilArtifactDownloads) && window.__localGptDiagCouncilArtifactDownloads.length > 0;
                            })()
                            """,
                            TimeSpan.FromMinutes(11));
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(800));
                    }

                    snapshot.PageJson = await sender.CoreWebView2.ExecuteScriptAsync("""
                        (() => {
                            const bodyText = () => (document.body && document.body.innerText ? document.body.innerText : '');
                            const text = bodyText();
                            const prompt = document.querySelector('textarea')?.value ?? '';
                            const minecraftSmoke = window.__localGptDiagMinecraftBuilderSmoke;
                            const minecraftArtifact = {
                                href: minecraftSmoke ? minecraftSmoke.downloadHref : null,
                                status: minecraftSmoke ? minecraftSmoke.downloadStatus : null,
                                bytes: minecraftSmoke ? minecraftSmoke.downloadBytes : null,
                                ok: minecraftSmoke ? !!minecraftSmoke.downloadOk : false,
                                error: minecraftSmoke ? minecraftSmoke.error : null
                            };
                            const minecraftDownloadLink = Array.from(document.querySelectorAll('a'))
                                .find(item => (item.innerText && item.innerText.includes('Download Datapack ZIP'))
                                    || (item.href && item.href.includes('/__artifacts/minecraft/')));
                            if (minecraftDownloadLink) {
                                minecraftArtifact.href = minecraftArtifact.href || minecraftDownloadLink.href;
                            }
                            const councilArtifacts = Array.isArray(window.__localGptDiagCouncilArtifactDownloads)
                                ? window.__localGptDiagCouncilArtifactDownloads
                                : [];
                            if (councilArtifacts.length === 0) {
                                for (const link of Array.from(document.querySelectorAll('a[href*="/__artifacts/council/"]')).slice(0, 8)) {
                                    councilArtifacts.push({
                                        name: (link.innerText || link.getAttribute('download') || '').trim(),
                                        href: link.href,
                                        status: null,
                                        bytes: null,
                                        ok: false,
                                        error: null
                                    });
                                }
                            }
                            const readLabeledInput = (labelText) => {
                                const label = Array.from(document.querySelectorAll('label'))
                                    .find(item => item.innerText && item.innerText.includes(labelText));
                                const input = label ? label.querySelector('input, select, textarea') : null;
                                if (!input) {
                                    return null;
                                }

                                if (input.type === 'checkbox') {
                                    return input.checked;
                                }

                                return input.value ?? null;
                            };
                            return {
                                url: location.href,
                                title: document.title,
                                readyState: document.readyState,
                                bodyText: text.slice(0, 4000),
                                hasDxAiChatSurface: !!document.querySelector('.demo-chat, dxbl-ai-chat, .dxbl-aichat'),
                                hasCouncilSurface: text.includes('AI Council') && text.includes('Run Council'),
                                hasCouncilFeatureRequestChat: text.includes('Feature Request Chat'),
                                clickedCouncilFeatureChat: !!window.__localGptDiagClickedCouncilFeatureChat,
                                clickedCouncilLowGpuPreset: !!window.__localGptDiagClickedCouncilLowGpuPreset,
                                runGpuCouncilDiagnostics: !!window.__localGptDiagRunGpuCouncil,
                                runFeatureRequestDiagnostics: !!window.__localGptDiagRunFeatureRequest,
                                councilLowGpuSettings: {
                                    reviewRounds: readLabeledInput('Review rounds'),
                                    parallelModels: readLabeledInput('Parallel models'),
                                    maxOutputTokens: readLabeledInput('Max output tokens'),
                                    contextTokens: readLabeledInput('Context tokens'),
                                    timeoutSeconds: readLabeledInput('Timeout seconds'),
                                    keepAlive: readLabeledInput('Ollama keep-alive'),
                                    cpuOnlyOllama: readLabeledInput('CPU-only Ollama')
                                },
                                councilPromptPreview: prompt.slice(0, 1200),
                                hasCouncilImplementationPrompt: prompt.includes('implementation-request council chat'),
                                hasCouncilArtifactSection: text.includes('Downloadable Examples'),
                                councilArtifactDownloads: councilArtifacts,
                                councilFeatureRequestSmoke: window.__localGptDiagCouncilFeatureRequestSmoke,
                                hasDatabaseEditor: text.includes('SQLite Database') && text.includes('Council Knowledge'),
                                hasLiveSqliteTableEditor: text.includes('Live SQLite Tables'),
                                hasDxGridSurface: !!document.querySelector('.dxbl-grid'),
                                hasMinecraftBuilderText: text.includes('Minecraft Mod Builder'),
                                minecraftBuilderCommandResultVisible: text.includes('Command Result') || text.includes('Exit code'),
                                minecraftBuilderExitCodeZero: text.includes('Exit code 0'),
                                minecraftBuilderDownloadVisible: text.includes('Download Datapack ZIP') || !!minecraftDownloadLink,
                                minecraftBuilderArtifact: minecraftArtifact,
                                minecraftBuilderSmoke: window.__localGptDiagMinecraftBuilderSmoke,
                                hasSetupText: text.includes('Setup')
                            };
                        })()
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

        private static async Task WaitForJavaScriptConditionAsync(CoreWebView2 webView, string script, TimeSpan timeout)
        {
            var deadline = DateTimeOffset.UtcNow.Add(timeout);
            while (DateTimeOffset.UtcNow < deadline)
            {
                var result = await webView.ExecuteScriptAsync(script);
                if (string.Equals(result, "true", StringComparison.OrdinalIgnoreCase))
                    return;

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
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

        private static bool ConsumeRuntimeFlag(string fileName)
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LocalGPT",
                "runtime",
                fileName);

            if (!File.Exists(path))
                return false;

            try
            {
                File.Delete(path);
            }
            catch
            {
                // A stale flag should not block diagnostics.
            }

            return true;
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

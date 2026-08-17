using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using static Microsoft.AspNetCore.Components.Web.RenderMode;
using LocalGPT;
using LocalGPT.Components;
using LocalGPT.Components.Layout;
using LocalGPT.BusinessObjects;
using LocalGPT.Services;
using DevExpress.Blazor;
using DevExpress.Blazor.Office;
using DevExpress.Blazor.RichEdit;
using DevExpress.Blazor.PivotTable;
using DevExpress.Blazor.PdfViewer;
using DevExpress.Blazor.Reporting.Models;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using DevExpress.AIIntegration.Blazor.Chat;
using Microsoft.Extensions.AI;
using Markdig;
using System.Dynamic;
using System.Globalization;
using LocalGPT.Components.Shared;
using Microsoft.AspNetCore.Components;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects.Enums;

namespace LocalGPT.Components.Pages
{
    public partial class Install
    {
    private string ActiveInstallSection { get; set; } = "providers";
    private bool InstallSectionUserSelected;
    private IReadOnlyList<WorkbenchNavItem> InstallSections =>
    [
        new("providers", T("Install.Workbench.Nav.Providers", "AI providers"), T("Install.Workbench.Nav.ProvidersHelp", "Hosts, models and provider connection settings"), ConfiguredProviderHosts.Count.ToString(CultureInfo.InvariantCulture)),
        new("remote", T("Install.Workbench.Nav.RemoteEndpoint", "Remote endpoint"), T("Install.Workbench.Nav.RemoteEndpointHelp", "LAN / VPN / smartphone listener")),
        new("tls", T("Install.Workbench.Nav.Certificate", "TLS certificate"), T("Install.Workbench.Nav.CertificateHelp", "Create and select the HTTPS certificate")),
        new("guide", T("Install.Workbench.Nav.Guide", "Setup guide"), T("Install.Workbench.Nav.GuideHelp", "First-run status and quick starts")),
        new("toolchains", T("Install.Workbench.Nav.Toolchains", "Toolchains"), T("Install.Workbench.Nav.ToolchainsHelp", "Compilers and runtime discovery")),
        new("languages", T("Install.Workbench.Nav.Languages", "Languages"), T("Install.Workbench.Nav.LanguagesHelp", "Runtime language catalogs")),
        new("log", T("Install.Workbench.Nav.Log", "Setup log"), T("Install.Workbench.Nav.LogHelpShort", "Operational setup messages"))
    ];

    private Task OnInstallSectionChanged(string key)
    {
        InstallSectionUserSelected = true;
        ActiveInstallSection = key;
        return Task.CompletedTask;
    }

    private AICoreOptions Model = new();
    private readonly HashSet<string> RemovedOllamaEndpoints = new(StringComparer.OrdinalIgnoreCase);
    private string? ExplicitOllamaPrimaryEndpoint;
    private LoggingCoreOptions LoggingModel = new();
    private DatabaseLoggerCoreOptions DatabaseLoggerModel = new();
    private IReadOnlyList<CoreLogLevel> LogLevels { get; } = Enum.GetValues<CoreLogLevel>();
    private RemoteWebEndpointOptions NetworkModel = new();
    private NetworkCertificateCreateRequest CertificateRequest = new();
    private IReadOnlyList<NetworkCertificateKeySize> CertificateKeySizes { get; } = Enum.GetValues<NetworkCertificateKeySize>();
    private IReadOnlyList<NetworkCertificateHash> CertificateHashes { get; } = Enum.GetValues<NetworkCertificateHash>();
    private IReadOnlyList<System.Security.Cryptography.X509Certificates.StoreLocation> CertificateStoreLocations { get; } = Enum.GetValues<System.Security.Cryptography.X509Certificates.StoreLocation>();
    private IReadOnlyList<System.Security.Cryptography.X509Certificates.StoreName> CertificateStoreNames { get; } = Enum.GetValues<System.Security.Cryptography.X509Certificates.StoreName>();
    private bool UseCreatedCertificateForRemoteEndpoint = true;
    private bool IsCertificateBusy;
    private string CertificateStatus = string.Empty;
    private string NetworkEndpointPreview => !NetworkModel.Enabled || NetworkModel.Port <= 0
        ? "disabled (loopback-only LocalGPT remains active)"
        : $"{(string.IsNullOrWhiteSpace(NetworkModel.CertificatePath) ? "http" : "https")}://{(string.IsNullOrWhiteSpace(NetworkModel.Address) ? "0.0.0.0" : NetworkModel.Address)}:{NetworkModel.Port}";
    private string _log = "";
    private string toastName = "InstallToasts";
    private bool IsDiscovering;
    private bool IsConnectivityChecking;
    private bool IsSaving;
    private bool IsOllamaProcessBusy;
    private bool IsOnboardingLoading;
    private bool IsLocalizationImporting;
    private bool IsToolchainBusy;
    private bool IsSavingHostHardware;
    private readonly Dictionary<string, ConfiguredAiHostHardwareDraft> HostHardwareDrafts = new(StringComparer.OrdinalIgnoreCase);
    private bool OverwriteLocalizationCatalog;
    private string LocalizationCulture = "fr-FR";
    private string LocalizationImportStatus = string.Empty;
    private string ToolchainSearchRoots = string.Empty;
    private string ToolchainStatus = string.Empty;
    private FirstRunOnboardingStatus? OnboardingStatus;
    private IReadOnlyList<LocalizationCatalogDescriptor> LocalizationCatalogs = Array.Empty<LocalizationCatalogDescriptor>();
    private IReadOnlyList<ProjectCompilerInstallation> CompilerInstallations = Array.Empty<ProjectCompilerInstallation>();
    private OllamaProcessStatus OllamaProcessStatus = new(false, false, null, Array.Empty<OllamaProcessInfo>(), string.Empty, "Ollama process status not checked yet.");
    private string ConnectivityStatus = "Connectivity not checked yet. Refresh to verify configured and local Ollama / OpenAI-compatible hosts.";
    private DateTimeOffset? LastConnectivityCheck;
    private IReadOnlyList<LocalAiHostDiscoveryResult> DiscoveredHosts = Array.Empty<LocalAiHostDiscoveryResult>();
    private IReadOnlyList<ProviderModelReference> DiscoveredProviderModels => DiscoveredHosts
        .Where(host => host.IsReachable)
        .SelectMany(host => host.Models.Select(model => ToProviderModel(host, model)))
        .ToList();
    private List<OllamaCoreOptions> AdditionalOllamaModels => Model.OllamaCores;
    private List<ChatGPTLocalCoreOptions> AdditionalOpenAiCompatibleHosts => Model.ChatGPTLocalCores;
    private IReadOnlyList<ConfiguredProviderHostView> ConfiguredProviderHosts => BuildConfiguredProviderHosts();
    private bool HasReachableAiHost => DiscoveredHosts.Any(host => host.IsReachable);
    private string ConnectivityAlertCss => HasReachableAiHost ? "alert alert-success" : "alert alert-warning";
    /// <summary>Resolves one installer UI string with a maintained fallback.</summary>
    private string T(string key, string fallback) => Localization.Get(key, fallback: fallback);

    private string LastCheckedLabel => LastConnectivityCheck is null
        ? string.Empty
        : string.Format(
            CultureInfo.CurrentUICulture,
            T("Install.Connectivity.LastChecked", "Last checked {0}."),
            LastConnectivityCheck.Value.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentUICulture));

    private string ConfiguredProviderSummary => string.Format(
        CultureInfo.CurrentUICulture,
        ConfiguredProviderHosts.Count == 1
            ? T("Install.ConfiguredProviders.SummaryOne", "{0} host/model binding available to Chat and AI Council.")
            : T("Install.ConfiguredProviders.SummaryMany", "{0} host/model bindings available to Chat and AI Council."),
        ConfiguredProviderHosts.Count);

    protected override void OnInitialized()
    {
        ApplyRequestedInstallSection();
        try
        {
            var current = Opts.CurrentValue.AICore ?? new AICoreOptions();
            // Provider settings are edited transactionally. Never alias IOptionsMonitor.CurrentValue into the UI;
            // otherwise typing a new endpoint mutates the live registry before Save and can replace another host.
            Model = ProviderConfigurationRegistry.CreateDetachedDraft(current);

            LoggingModel = Opts.CurrentValue.LoggingCore ?? new LoggingCoreOptions
            {
                CoreLogLevel = CoreLogLevel.Information,
                FileCore = new FileLoggerCoreOptions { CoreLogLevel = CoreLogLevel.Error, FilePath = string.Empty },
                EmailCore = new EmailLoggerCoreOptions { CoreLogLevel = CoreLogLevel.None },
                DatabaseCore = new DatabaseLoggerCoreOptions { CoreLogLevel = CoreLogLevel.Warning }
            };
            LoggingModel.FileCore ??= new FileLoggerCoreOptions { CoreLogLevel = CoreLogLevel.Error, FilePath = string.Empty };
            LoggingModel.EmailCore ??= new EmailLoggerCoreOptions { CoreLogLevel = CoreLogLevel.None };
            DatabaseLoggerModel = LoggingModel.DatabaseCore ?? new DatabaseLoggerCoreOptions { CoreLogLevel = CoreLogLevel.Warning };
            LoggingModel.DatabaseCore = DatabaseLoggerModel;
            NetworkModel = Opts.CurrentValue.LocalGPT?.RemoteEndpoint ?? new RemoteWebEndpointOptions();
            CertificateRequest = NetworkCertificates.CreateDefaultRequest();
            if (!string.IsNullOrWhiteSpace(NetworkModel.CertificatePath))
            {
                CertificateRequest.OutputPath = NetworkModel.CertificatePath;
                CertificateRequest.Password = NetworkModel.CertificatePassword;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Install.OnInitialized failed: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            _ = Append("(Init) " + ex.Message);
        }
    }


    private void ApplyRequestedInstallSection()
    {
        try
        {
            var fragment = new Uri(Nav.Uri).Fragment.TrimStart('#');
            ActiveInstallSection = fragment switch
            {
                "provider-studio" => "providers",
                "network-endpoint" => "remote",
                "tls-certificate" => "tls",
                "setup-guide" => "guide",
                "toolchains" => "toolchains",
                "localization" => "languages",
                "setup-log" => "log",
                _ => ActiveInstallSection
            };
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Could not map the Install URL fragment to a workbench section.");
        }
    }

    private IReadOnlyList<ConfiguredProviderHostView> BuildConfiguredProviderHosts()
    {
        var result = new List<ConfiguredProviderHostView>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddOpenAiCompatible(ChatGPTLocalCoreOptions? option, bool primary, string origin)
        {
            if (option is null || (string.IsNullOrWhiteSpace(option.Endpoint) && string.IsNullOrWhiteSpace(option.ModelName)))
                return;
            var endpoint = NormalizeProviderEndpoint(option.Endpoint);
            var key = $"openai-compatible|{endpoint}|{option.ModelName}";
            if (!seen.Add(key)) return;
            result.Add(new ConfiguredProviderHostView(key, "OpenAI-compatible / LM Studio", endpoint, option.ModelName ?? string.Empty, origin, primary, option, null));
        }

        void AddOllama(OllamaCoreOptions? option, bool primary, string origin)
        {
            if (option is null || (string.IsNullOrWhiteSpace(option.Uri) && string.IsNullOrWhiteSpace(option.ModelName)))
                return;
            var endpoint = NormalizeProviderEndpoint(option.Uri);
            var key = $"ollama|{endpoint}|{option.ModelName}";
            if (!seen.Add(key)) return;
            result.Add(new ConfiguredProviderHostView(key, "Ollama", endpoint, option.ModelName ?? string.Empty, origin, primary, null, option));
        }

        AddOpenAiCompatible(Model.ChatGPTLocalCore, true, "Primary local OpenAI-compatible host");
        foreach (var option in Model.ChatGPTLocalCores)
            AddOpenAiCompatible(option, false, "Additional local OpenAI-compatible host");
        AddOllama(Model.OllamaCore, true, "Primary Ollama host");
        foreach (var option in Model.OllamaCores)
            AddOllama(option, false, "Additional Ollama host");

        if (Model.OpenAICore is { } openAi && (!string.IsNullOrWhiteSpace(openAi.ApiKey) || !string.IsNullOrWhiteSpace(openAi.ModelName)))
            result.Add(new ConfiguredProviderHostView("openai-cloud", "OpenAI Cloud", "OpenAI API", openAi.ModelName ?? string.Empty, "Cloud provider", true, null, null));
        if (Model.OpenAIServiceCore is { } azure && (!string.IsNullOrWhiteSpace(azure.Endpoint) || !string.IsNullOrWhiteSpace(azure.DeploymentName)))
            result.Add(new ConfiguredProviderHostView("azure-openai", "Azure OpenAI", azure.Endpoint ?? string.Empty, azure.DeploymentName ?? string.Empty, "Cloud provider", true, null, null));

        return result;
    }

    private sealed record ConfiguredProviderHostView(
        string Key,
        string Provider,
        string Endpoint,
        string ModelName,
        string Origin,
        bool IsPrimary,
        ChatGPTLocalCoreOptions? OpenAiHost,
        OllamaCoreOptions? OllamaHost);

    private void NormalizeLocalEndpoint()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Model.ChatGPTLocalCore?.Endpoint)) return;

            var ep = Model.ChatGPTLocalCore.Endpoint.Trim().TrimEnd('/');
            if (Uri.TryCreate(ep, UriKind.Absolute, out var uri) && (string.IsNullOrWhiteSpace(uri.AbsolutePath) || uri.AbsolutePath == "/"))
                ep += "/v1";
            Model.ChatGPTLocalCore.Endpoint = ep;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to normalize local endpoint: {Message}", ex.Message);
            _ = Append("Normalization warning: " + ex.Message);
        }
    }

    private async Task Append(string text)
    {
        try
        {
            _log = $"[{DateTime.Now:T}] {text}\n" + _log;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Logging must never recurse into the UI log path when the interactive circuit is unavailable.
            Logger.LogWarning(ex, "Install activity log UI refresh failed: {Message}", ex.Message);
        }
    }

    private async Task TestLocal()
    {
        try
        {
            NormalizeLocalEndpoint();
            var (ok, msg) = await Probe.TestLocalOpenAICompatAsync(Model.ChatGPTLocalCore!, default).ConfigureAwait(false);
            await Append((ok ? "OK" : "FAIL") + " Local: " + msg).ConfigureAwait(false);
            if (ok) Notifier.ShowSuccess(toastName, "Local endpoint is reachable.", "Local Test OK");
            else    Notifier.ShowWarning(toastName, msg, "Local Test Failed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "TestLocal failed: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            await Append("(Local Test) " + ex.Message).ConfigureAwait(false);
        }
    }

    private async Task StartLocal()
    {
        try
        {
            NormalizeLocalEndpoint();
            var (ok, msg) = await Probe.TryStartLocalAsync(Model.ChatGPTLocalCore!, default).ConfigureAwait(false);
            await Append((ok ? "Started" : "Failed") + ": " + msg).ConfigureAwait(false);
            if (ok) Notifier.ShowSuccess(toastName, "Local server started.", "Start OK");
            else    Notifier.ShowWarning(toastName, msg, "Start Failed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "StartLocal failed: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            await Append("(Start Local) " + ex.Message).ConfigureAwait(false);
        }
    }

    private async Task TestOllama()
    {
        try
        {
            var (ok, msg) = await Probe.TestOllamaAsync(Model.OllamaCore!, default).ConfigureAwait(false);
            LastConnectivityCheck = DateTimeOffset.Now;
            ConnectivityStatus = ok
                ? $"Ollama reachable at {Model.OllamaCore?.Uri}. Selected model: {Model.OllamaCore?.ModelName}."
                : $"Ollama not reachable at {Model.OllamaCore?.Uri}: {msg}";
            await Append((ok ? "OK" : "FAIL") + " Ollama: " + msg).ConfigureAwait(false);
            if (ok) Notifier.ShowSuccess(toastName, "Ollama reachable.", "Ollama Test OK");
            else    Notifier.ShowWarning(toastName, msg, "Ollama Test Failed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "TestOllama failed: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            await Append("(Ollama Test) " + ex.Message).ConfigureAwait(false);
        }
    }

    private bool initialSetupRefreshStarted;

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !initialSetupRefreshStarted)
        {
            initialSetupRefreshStarted = true;
            TaskRunner.Run(
                nameof(Install),
                "InitialSetupRefresh",
                InitializeSetupAfterRenderAsync);
        }

        return Task.CompletedTask;
    }

    private async Task InitializeSetupAfterRenderAsync(CancellationToken cancellationToken)
    {
        try
        {
            LocalizationCatalogs = Localization.GetCatalogs();
            cancellationToken.ThrowIfCancellationRequested();
            await RefreshOnboardingAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await RefreshCompilerInstallationsAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await RefreshOllamaProcessStatusAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await RefreshConnectivityStatusAsync(showToast: false).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await LoadHostHardwareProfilesAsync(cancellationToken).ConfigureAwait(false);
            ComponentActivity.RecordInformation(nameof(Install), nameof(InitializeSetupAfterRenderAsync), "Install background initialization completed without holding the interactive renderer.");
        }
        catch (OperationCanceledException)
        {
            Logger.LogDebug("Install background initialization was cancelled.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Install background initialization failed: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            await Append("(InitializeSetupAfterRenderAsync) " + ex.Message).ConfigureAwait(false);
        }
    }



    /// <summary>Loads durable physical-host hardware definitions into the Install-page drafts.</summary>
    private async Task LoadHostHardwareProfilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var profiles = await HostHardware.GetAllAsync(cancellationToken).ConfigureAwait(false);
            await InvokeAsync(() =>
            {
                foreach (var configured in ConfiguredProviderHosts)
                {
                    var hostKey = HostHardware.GetHostKey(configured.Endpoint);
                    var profile = profiles.FirstOrDefault(item => item.HostKey.Equals(hostKey, StringComparison.OrdinalIgnoreCase));
                    if (profile is not null)
                        HostHardwareDrafts[hostKey] = HostHardware.CreateDraft(configured.Endpoint, profile);
                }
                StateHasChanged();
            }).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                Logger.LogDebug(exception, "Loading configured-host hardware into Install was cancelled.");
            else
                Logger.LogError(exception, "Loading configured-host hardware into Install failed.");
        }
    }

    /// <summary>Gets the editable hardware draft shared by provider bindings that resolve to the same physical host.</summary>
    private ConfiguredAiHostHardwareDraft GetHostHardwareDraft(string endpoint)
    {
        try
        {
            var hostKey = HostHardware.GetHostKey(endpoint);
            if (!HostHardwareDrafts.TryGetValue(hostKey, out var draft))
            {
                draft = HostHardware.CreateDraft(endpoint);
                HostHardwareDrafts[hostKey] = draft;
            }
            if (!draft.Endpoint.Equals(endpoint, StringComparison.OrdinalIgnoreCase))
                draft.Endpoint = endpoint;
            return draft;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Preparing configured-host hardware form failed; endpoint details were omitted.");
            throw;
        }
    }

    /// <summary>Saves user-confirmed hardware for a configured physical AI host.</summary>
    private async Task SaveHostHardwareAsync(string endpoint)
    {
        IsSavingHostHardware = true;
        try
        {
            var saved = await HostHardware.SaveAsync(GetHostHardwareDraft(endpoint)).ConfigureAwait(false);
            HostHardwareDrafts[HostHardware.GetHostKey(endpoint)] = HostHardware.CreateDraft(endpoint, saved);
            Notifier.ShowSuccess(toastName, "Configured-host hardware saved in the LocalGPT database.", "Host hardware");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Saving configured-host hardware from Install failed; endpoint and values were omitted.");
            Notifier.ShowError(toastName, "Host hardware could not be saved. See local logs for details.", "Host hardware");
        }
        finally
        {
            IsSavingHostHardware = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Runs read-only local hardware discovery for a loopback provider host.</summary>
    private async Task DetectHostHardwareAsync(string endpoint)
    {
        IsSavingHostHardware = true;
        try
        {
            var saved = await HostHardware.DetectLocalAsync(endpoint).ConfigureAwait(false);
            HostHardwareDrafts[HostHardware.GetHostKey(endpoint)] = HostHardware.CreateDraft(endpoint, saved);
            Notifier.ShowSuccess(toastName, saved.IsUserConfirmed ? "Existing confirmed host hardware kept unchanged." : "Local hardware discovery refreshed.", "Host hardware");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Detecting configured-host hardware from Install failed; endpoint details were omitted.");
            Notifier.ShowError(toastName, "Local host hardware detection failed. Manual values and report import remain available.", "Host hardware");
        }
        finally
        {
            IsSavingHostHardware = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Reads a bounded local HWiNFO text file selected by the user and imports it for the configured host.</summary>
    private async Task ImportHostHardwareFileAsync(string endpoint, Microsoft.AspNetCore.Components.Forms.InputFileChangeEventArgs args)
    {
        try
        {
            var file = args.File;
            using var stream = file.OpenReadStream(8 * 1024 * 1024);
            using var reader = new StreamReader(stream);
            var draft = GetHostHardwareDraft(endpoint);
            draft.HwInfoReportText = await reader.ReadToEndAsync().ConfigureAwait(false);
            await ImportHostHardwareAsync(endpoint).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Reading selected configured-host HWiNFO report failed; file content and endpoint were omitted.");
            Notifier.ShowError(toastName, "The selected HWiNFO report could not be read or imported.", "Host hardware");
        }
    }

    /// <summary>Imports deterministic facts from the pasted HWiNFO report into the configured physical host.</summary>
    private async Task ImportHostHardwareAsync(string endpoint)
    {
        IsSavingHostHardware = true;
        try
        {
            var draft = GetHostHardwareDraft(endpoint);
            var saved = await HostHardware.ImportHwInfoAsync(endpoint, draft.HwInfoReportText).ConfigureAwait(false);
            HostHardwareDrafts[HostHardware.GetHostKey(endpoint)] = HostHardware.CreateDraft(endpoint, saved);
            Notifier.ShowSuccess(toastName, "HWiNFO hardware facts imported and stored for this physical host.", "Host hardware");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Importing configured-host HWiNFO report from Install failed; report content was omitted.");
            Notifier.ShowError(toastName, exception is InvalidDataException ? exception.Message : "HWiNFO import failed. See local logs for details.", "Host hardware");
        }
        finally
        {
            IsSavingHostHardware = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Returns whether an endpoint belongs to the LocalGPT machine and is therefore eligible for local probing.</summary>
    private bool IsLoopbackProviderEndpoint(string endpoint)
    {
        try
        {
            return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && uri.IsLoopback;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Checking provider endpoint locality failed; endpoint details were omitted.");
            return false;
        }
    }


    /// <summary>Refreshes the persistent installer guide and Council quick-start catalog.</summary>
    private async Task RefreshOnboardingAsync()
    {
        try
        {
            IsOnboardingLoading = true;
            OnboardingStatus = await Onboarding.GetStatusAsync(refreshConnectivity: false).ConfigureAwait(false);
            if (!OnboardingStatus.IsCompleted && !InstallSectionUserSelected && string.IsNullOrWhiteSpace(new Uri(Nav.Uri).Fragment))
                ActiveInstallSection = "guide";
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Could not load the persistent installer onboarding surface.");
            Notifier.ShowError(toastName, "The setup guide could not be loaded. Review LocalGPT logs.", "Setup guide");
        }
        finally
        {
            IsOnboardingLoading = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Records that the guide was reviewed without hiding it from Install.</summary>
    private async Task CompleteOnboardingAsync()
    {
        try
        {
            IsOnboardingLoading = true;
            await Onboarding.CompleteAsync(userConfirmed: true).ConfigureAwait(false);
            OnboardingStatus = await Onboarding.GetStatusAsync(refreshConnectivity: false).ConfigureAwait(false);
            Notifier.ShowSuccess(toastName, "The setup guide was marked reviewed and remains available on Install.", "Setup reviewed");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Could not record the onboarding review state.");
            Notifier.ShowError(toastName, "The setup review state could not be saved.", "Setup guide");
        }
        finally
        {
            IsOnboardingLoading = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Loads compiler and runtime toolchains stored in the LocalGPT database.</summary>
    /// <returns>A task that completes after the installer list is refreshed.</returns>
    private async Task RefreshCompilerInstallationsAsync()
    {
        try
        {
            IsToolchainBusy = true;
            CompilerInstallations = await ProjectMaintenance.GetCompilerInstallationsAsync().ConfigureAwait(false) /* renderer-affine toolchain list */;
            ToolchainStatus = $"{CompilerInstallations.Count} stored toolchain profile(s).";
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Could not load stored compiler installations; executable paths were omitted from logs.");
            ToolchainStatus = T("Install.Toolchains.LoadFailed", "Stored toolchains could not be loaded. Review LocalGPT logs.");
            Notifier.ShowError(toastName, ToolchainStatus, T("Install.Toolchains.Title", "Compilers and runtime toolchains"));
        }
        finally
        {
            IsToolchainBusy = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Discovers compiler executables from PATH, common locations and optional user roots.</summary>
    /// <returns>A task that completes after detected profiles are saved and displayed.</returns>
    private async Task DiscoverToolchainsAsync()
    {
        try
        {
            IsToolchainBusy = true;
            ToolchainStatus = T("Install.Toolchains.Discovering", "Discovering toolchains...");
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            var discovered = await ProjectMaintenance.DiscoverCompilerInstallationsAsync(new DiscoverProjectCompilersRequest
            {
                CustomSearchRootsText = ToolchainSearchRoots,
                SaveDiscovered = true,
                UserConfirmed = true
            }).ConfigureAwait(false);
            CompilerInstallations = await ProjectMaintenance.GetCompilerInstallationsAsync().ConfigureAwait(false) /* renderer-affine discovery refresh */;
            ToolchainStatus = $"Discovered {discovered.Count} candidate(s); {CompilerInstallations.Count} profile(s) are stored.";
            Notifier.ShowSuccess(toastName, ToolchainStatus, T("Install.Toolchains.Title", "Compilers and runtime toolchains"));
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Compiler discovery failed; search roots and executable paths were omitted from logs.");
            ToolchainStatus = T("Install.Toolchains.DiscoveryFailed", "Toolchain discovery failed. Review LocalGPT logs.");
            Notifier.ShowError(toastName, ToolchainStatus, T("Install.Toolchains.Title", "Compilers and runtime toolchains"));
        }
        finally
        {
            IsToolchainBusy = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Runs the bounded version probe for one stored compiler installation.</summary>
    /// <param name="compilerId">Stored compiler identifier.</param>
    /// <returns>A task that completes after validation and list refresh.</returns>
    private async Task ValidateCompilerAsync(Guid compilerId)
    {
        try
        {
            IsToolchainBusy = true;
            var compiler = await ProjectMaintenance.ValidateCompilerInstallationAsync(compilerId, userConfirmed: true).ConfigureAwait(false) /* renderer-affine validation result */;
            ToolchainStatus = compiler.LastValidationSucceeded
                ? $"Validated {compiler.Name}: {compiler.Version}."
                : $"Validation failed for {compiler.Name}.";
            CompilerInstallations = await ProjectMaintenance.GetCompilerInstallationsAsync().ConfigureAwait(false) /* renderer-affine validation refresh */;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Compiler validation failed for {CompilerId}; executable path was omitted from logs.", compilerId);
            ToolchainStatus = T("Install.Toolchains.ValidationFailed", "Compiler validation failed. Review LocalGPT logs.");
            Notifier.ShowError(toastName, ToolchainStatus, T("Install.Toolchains.Title", "Compilers and runtime toolchains"));
        }
        finally
        {
            IsToolchainBusy = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Marks one stored compiler as the default for its language.</summary>
    /// <param name="compiler">Compiler profile selected by the user.</param>
    /// <returns>A task that completes after the default profile is saved.</returns>
    private async Task MakeCompilerDefaultAsync(ProjectCompilerInstallation compiler)
    {
        ArgumentNullException.ThrowIfNull(compiler);
        try
        {
            IsToolchainBusy = true;
            await ProjectMaintenance.SaveCompilerInstallationAsync(new SaveProjectCompilerInstallationRequest
            {
                Id = compiler.Id,
                Name = compiler.Name,
                Language = compiler.Language,
                ExecutablePath = compiler.ExecutablePath,
                CompilerHomePath = compiler.CompilerHomePath,
                Version = compiler.Version,
                Architecture = compiler.Architecture,
                DiscoverySource = compiler.DiscoverySource,
                ToolchainKind = compiler.ToolchainKind,
                DetectedPlatform = compiler.DetectedPlatform,
                ValidationArguments = compiler.ValidationArguments,
                EnvironmentVariablesJson = compiler.EnvironmentVariablesJson,
                EnvironmentVariables = compiler.EnvironmentVariables,
                KnowledgeProfileKey = compiler.KnowledgeProfileKey,
                KnowledgeEntryId = compiler.KnowledgeEntryId,
                VersionKnowledgeEntryId = compiler.VersionKnowledgeEntryId,
                IsEnabled = compiler.IsEnabled,
                IsDefaultForLanguage = true,
                UserConfirmed = true
            }).ConfigureAwait(false);
            CompilerInstallations = await ProjectMaintenance.GetCompilerInstallationsAsync().ConfigureAwait(false) /* renderer-affine default refresh */;
            ToolchainStatus = $"{compiler.Name} is now the default {compiler.Language} toolchain.";
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Setting compiler {CompilerId} as language default failed.", compiler.Id);
            ToolchainStatus = T("Install.Toolchains.DefaultFailed", "The default toolchain could not be saved.");
        }
        finally
        {
            IsToolchainBusy = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Deletes one unreferenced stored compiler profile.</summary>
    /// <param name="compilerId">Stored compiler identifier.</param>
    /// <returns>A task that completes after the delete attempt.</returns>
    private async Task DeleteCompilerAsync(Guid compilerId)
    {
        try
        {
            IsToolchainBusy = true;
            await ProjectMaintenance.DeleteCompilerInstallationAsync(compilerId, userConfirmed: true).ConfigureAwait(false) /* renderer-affine delete result */;
            CompilerInstallations = await ProjectMaintenance.GetCompilerInstallationsAsync().ConfigureAwait(false) /* renderer-affine delete refresh */;
            ToolchainStatus = T("Install.Toolchains.Removed", "The compiler profile was removed.");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Deleting compiler {CompilerId} failed.", compilerId);
            ToolchainStatus = exception.Message;
            Notifier.ShowError(toastName, ToolchainStatus, T("Install.Toolchains.Title", "Compilers and runtime toolchains"));
        }
        finally
        {
            IsToolchainBusy = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Validates and imports one user-selected localization JSON file.</summary>
    /// <param name="args">Selected browser file.</param>
    private async Task ImportLocalizationFileAsync(InputFileChangeEventArgs args)
    {
        try
        {
            IsLocalizationImporting = true;
            LocalizationImportStatus = string.Empty;
            var file = args.File;
            if (string.IsNullOrWhiteSpace(LocalizationCulture))
                LocalizationCulture = Path.GetFileNameWithoutExtension(file.Name);
            var stream = file.OpenReadStream(4 * 1024 * 1024);
            await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync().ConfigureAwait(false);
            var validation = Localization.ValidateCatalog(LocalizationCulture, json);
            if (!validation.IsValid)
            {
                LocalizationImportStatus = Localization.FormatValidationErrors(validation);
                Notifier.ShowWarning(toastName, LocalizationImportStatus, "Language catalog rejected");
                return;
            }

            var imported = await Localization.ImportCatalogAsync(
                validation.Culture,
                json,
                OverwriteLocalizationCatalog).ConfigureAwait(false);
            LocalizationCatalogs = Localization.GetCatalogs();
            var catalogDescriptor = LocalizationCatalogs.FirstOrDefault(item => string.Equals(item.Culture, imported.Culture, StringComparison.OrdinalIgnoreCase));
            await FeaturePersistence.SaveLocalizationCatalogAsync(new SaveFeatureRecordRequest<LocalizationCatalogRegistration>
            {
                UserConfirmed = true,
                Record = new LocalizationCatalogRegistration
                {
                    CultureName = imported.Culture,
                    DisplayName = catalogDescriptor?.DisplayName ?? imported.Culture,
                    CatalogPath = $"user-localization://{imported.Culture}",
                    StringCount = imported.StringCount,
                    MissingBaselineKeyCount = imported.MissingBaselineKeyCount,
                    IsUserOverride = true,
                    IsEnabled = true
                }
            }).ConfigureAwait(false);
            LocalizationCulture = imported.Culture;
            LocalizationImportStatus = $"Imported {imported.StringCount} strings for {imported.Culture}. {imported.MissingBaselineKeyCount} missing baseline keys use English fallback.";
            Notifier.ShowSuccess(toastName, LocalizationImportStatus, "Language catalog imported");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Localization catalog import failed; submitted text was omitted from logs.");
            LocalizationImportStatus = exception.Message;
            Notifier.ShowError(toastName, "The language catalog could not be imported. " + exception.Message, "Language catalog");
        }
        finally
        {
            IsLocalizationImporting = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Activates the selected imported culture and reloads Install through the localization controller.</summary>
    private void ActivateSelectedCulture()
    {
        var route = $"/api/localization/select?culture={Uri.EscapeDataString(LocalizationCulture)}&returnUrl={Uri.EscapeDataString("/install")}";
        Nav.NavigateTo(route, forceLoad: true);
    }

    /// <summary>Opens the generated documentation route.</summary>
    private void OpenDocumentation() => NavigateToRoute("/help");

    /// <summary>Opens the editable Council-team catalog.</summary>
    private void OpenCouncilTeams() => NavigateToRoute("/council-teams");

    /// <summary>Opens one direct Council starter through a fresh full Chat navigation.</summary>
    /// <param name="quickStart">Selected maintained quick-start descriptor.</param>
    private void OpenQuickStartInChat(CouncilQuickStart quickStart)
    {
        ArgumentNullException.ThrowIfNull(quickStart);
        try
        {
            Nav.NavigateTo(quickStart.Route, forceLoad: true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Installer direct Council starter navigation failed for {StarterKey}.", quickStart.Key);
            Notifier.ShowError(toastName, "The selected Council starter could not be opened.", "Council starter");
        }
    }

    /// <summary>Navigates to one application-relative installer destination.</summary>
    /// <param name="route">Application-relative route.</param>
    private void NavigateToRoute(string route)
    {
        try
        {
            Nav.NavigateTo(route, forceLoad: false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Installer navigation failed for route {Route}.", route);
            Notifier.ShowError(toastName, "The requested LocalGPT page could not be opened.", "Navigation");
        }
    }
}
}

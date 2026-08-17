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
    private async Task RefreshOllamaProcessStatusAsync()
    {
        try
        {
            IsOllamaProcessBusy = true;
            OllamaProcessStatus = await OllamaProcesses.GetStatusAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not refresh Ollama process status; executable paths were omitted from logs.");
            OllamaProcessStatus = OllamaProcessStatus with { Message = $"Ollama process status failed: {ex.Message}" };
        }
        finally
        {
            IsOllamaProcessBusy = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private Task StartOllamaAsync() => RunOllamaProcessActionAsync(
        cancellationToken => OllamaProcesses.StartAsync(cancellationToken),
        "Start Ollama");

    private Task StopOllamaAsync() => RunOllamaProcessActionAsync(
        cancellationToken => OllamaProcesses.StopAsync(cancellationToken),
        "Stop Ollama");

    private Task RestartOllamaAsync() => RunOllamaProcessActionAsync(
        cancellationToken => OllamaProcesses.RestartAsync(cancellationToken),
        "Restart Ollama");

    private async Task RunOllamaProcessActionAsync(
        Func<CancellationToken, Task<OllamaProcessStatus>> action,
        string operation)
    {
        try
        {
            IsOllamaProcessBusy = true;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            OllamaProcessStatus = await action(default).ConfigureAwait(false);
            await Append($"{operation}: {OllamaProcessStatus.Message}").ConfigureAwait(false);

            if (OllamaProcessStatus.IsRunning)
                Notifier.ShowSuccess(toastName, OllamaProcessStatus.Message, operation);
            else if (CouncilText.StartsWithText(operation, "Stop"))
                Notifier.ShowSuccess(toastName, OllamaProcessStatus.Message, operation);
            else
                Notifier.ShowWarning(toastName, OllamaProcessStatus.Message, operation);

            await RefreshConnectivityStatusAsync(showToast: false).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ollama process operation {Operation} failed; executable paths were omitted from logs.", operation);
            Notifier.ShowError(toastName, "The Ollama process operation failed. Review LocalGPT logs.", operation);
        }
        finally
        {
            IsOllamaProcessBusy = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private async Task DiscoverLocalAiHosts()
    {
        try
        {
            IsDiscovering = true;
            await Append("Scanning configured providers plus standard localhost discovery endpoints...").ConfigureAwait(false);

            DiscoveredHosts = await Probe.DiscoverLocalHostsAsync(default).ConfigureAwait(false);

            var reachable = DiscoveredHosts.Count(h => h.IsReachable);
            var models = DiscoveredHosts.Sum(h => h.Models.Count);

            await Append($"Discovery found {reachable} reachable host(s) and {models} model(s).").ConfigureAwait(false);
            LastConnectivityCheck = DateTimeOffset.Now;
            ConnectivityStatus = BuildConnectivityStatus(reachable, models);

            if (reachable > 0)
                Notifier.ShowSuccess(toastName, $"Found {reachable} reachable AI host(s).", "Discovery Complete");
            else
                Notifier.ShowWarning(toastName, "No configured or standard local AI hosts responded.", "Discovery Complete");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DiscoverLocalAiHosts failed: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            await Append("(Discovery) " + ex.Message).ConfigureAwait(false);
        }
        finally
        {
            IsDiscovering = false;
        }
    }

    private Task RefreshConnectivityStatusFromButton()
    {
        try
        {
            return RefreshConnectivityStatusAsync(showToast: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DiscoverLocalAiHosts failed: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            return Task.CompletedTask;
        }
    }

    private async Task RefreshConnectivityStatusAsync(bool showToast)
    {
        try
        {
            IsConnectivityChecking = true;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            DiscoveredHosts = await Probe.DiscoverLocalHostsAsync(default).ConfigureAwait(false);

            var reachable = DiscoveredHosts.Count(host => host.IsReachable);
            var models = DiscoveredHosts.Sum(host => host.Models.Count);
            LastConnectivityCheck = DateTimeOffset.Now;
            ConnectivityStatus = BuildConnectivityStatus(reachable, models);
            await Append($"Connectivity refresh: {ConnectivityStatus}").ConfigureAwait(false);

            if (!showToast)
                return;

            if (reachable > 0)
                Notifier.ShowSuccess(toastName, ConnectivityStatus, "Connectivity OK");
            else
                Notifier.ShowWarning(toastName, ConnectivityStatus, "Connectivity Warning");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RefreshConnectivityStatusAsync failed: {Message}", ex.Message);
            LastConnectivityCheck = DateTimeOffset.Now;
            ConnectivityStatus = "Connectivity check failed: " + ex.Message;
            if (showToast)
                Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            await Append("(Connectivity) " + ex.Message).ConfigureAwait(false);
        }
        finally
        {
            IsConnectivityChecking = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private string BuildConnectivityStatus(int reachable, int models)
    {
        try
        {
            if (reachable <= 0)
                return "No configured or standard local Ollama / OpenAI-compatible host is reachable. Verify the endpoint, provider bind address and firewall, then refresh.";

            var hostSummaries = DiscoveredHosts
                .Where(host => host.IsReachable)
                .Select(host => $"{host.Provider} at {host.Endpoint} ({host.Models.Count} model(s))");
            return $"Reachable: {CouncilText.FormatJoinedList(hostSummaries, "; ")}. Total detected models: {models}.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            return string.Empty;
        }
    }

    private ProviderModelReference ToProviderModel(LocalAiHostDiscoveryResult host, LocalAiModelInfo model)
    {
        var isOllama = host.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase);
        var identity = new ProviderModelIdentity();
        var endpoint = isOllama
            ? identity.NormalizeEndpoint(host.Endpoint)
            : identity.NormalizeOpenAiCompatibleEndpoint(host.Endpoint);
        return new ProviderModelReference
        {
            ProviderKind = isOllama ? ProviderModelKinds.Ollama : ProviderModelKinds.OpenAICompatible,
            ProviderName = host.Provider,
            Endpoint = endpoint,
            ModelName = model.Name,
            IsLocal = Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) &&
                (endpointUri.IsLoopback || endpointUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)),
            IsReachable = host.IsReachable,
            IsConfigured = isOllama
                ? IsOllamaModelConfigured(endpoint, model.Name)
                : IsOpenAiCompatibleModelConfigured(endpoint, model.Name),
            IsLoaded = model.IsLoaded,
            SupportsBenchmark = host.IsReachable,
            Details = host.Status
        };
    }

    private Task OnInstallBenchmarkAppliedAsync(ProviderModelBenchmarkAppliedEvent applied)
    {
        ConnectivityStatus = $"Benchmark recommendation applied for {applied.Model.SelectionKey} as preset {applied.Preset.Name}. Save provider settings separately only when you changed the active setup model.";
        Notifier.ShowSuccess(toastName, ConnectivityStatus, "Benchmark settings applied");
        return InvokeAsync(StateHasChanged);
    }

    private async Task ApplyDiscoveredModel(LocalAiHostDiscoveryResult host, LocalAiModelInfo model)
    {
        try
        {
            if (string.Equals(host.Provider, "Ollama", StringComparison.OrdinalIgnoreCase))
            {
                var binding = UpsertOllamaHostBinding(host.Endpoint, model.Name);
                await Append($"Selected Ollama model {model.Name} at {host.Endpoint} as {(binding.IsPrimary ? "the primary" : "an additional")} endpoint-qualified host binding.").ConfigureAwait(false);
            }
            else
            {
                var binding = UpsertOpenAiCompatibleHostBinding(host.Provider, host.Endpoint, model.Name);
                await Append($"Selected {host.Provider} model {model.Name} at {host.Endpoint} as {(binding.IsPrimary ? "the primary" : "an additional")} endpoint-qualified host binding.").ConfigureAwait(false);
            }

            Notifier.ShowSuccess(toastName, $"{host.Provider}: {model.Name}", "Model Selected");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ApplyDiscoveredModel failed: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            await Append("(Selection) " + ex.Message).ConfigureAwait(false);
        }
    }

    private async Task UseOllamaGptOss20b()
    {
        try
        {
            var binding = UpsertOllamaHostBinding("http://localhost:11434", "gpt-oss:20b");
            await Append($"Selected Ollama gpt-oss:20b at http://localhost:11434 as {(binding.IsPrimary ? "the primary" : "an additional")} endpoint-qualified host binding.").ConfigureAwait(false);
            Notifier.ShowSuccess(toastName, "Ollama gpt-oss:20b selected.", "Model Selected");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UseOllamaGptOss20b failed: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    private void AddOpenAiCompatibleHost()
    {
        Model.ChatGPTLocalCores ??= new List<ChatGPTLocalCoreOptions>();
        Model.ChatGPTLocalCores.Add(new ChatGPTLocalCoreOptions
        {
            Endpoint = "http://127.0.0.1:1234/v1",
            ApiKey = "local-no-key",
            ModelName = string.Empty,
            AutoStartServer = false
        });
    }

    private (bool IsPrimary, OllamaCoreOptions Binding) UpsertOllamaHostBinding(string endpoint, string modelName)
    {
        try
        {
            var identity = new ProviderModelIdentity();
            var normalizedEndpoint = identity.NormalizeEndpoint(endpoint);
            RemovedOllamaEndpoints.Remove(normalizedEndpoint);
            Model.OllamaCores ??= new List<OllamaCoreOptions>();

            if (string.IsNullOrWhiteSpace(Model.OllamaCore?.Uri)
                || identity.NormalizeEndpoint(Model.OllamaCore.Uri).Equals(normalizedEndpoint, StringComparison.OrdinalIgnoreCase))
            {
                Model.OllamaCore ??= new OllamaCoreOptions();
                Model.OllamaCore.Uri = normalizedEndpoint;
                Model.OllamaCore.ModelName = modelName.Trim();
                return (true, Model.OllamaCore);
            }

            var existing = Model.OllamaCores.FirstOrDefault(item =>
                !string.IsNullOrWhiteSpace(item.Uri)
                && identity.NormalizeEndpoint(item.Uri).Equals(normalizedEndpoint, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                existing = new OllamaCoreOptions
                {
                    Uri = normalizedEndpoint,
                    ModelName = modelName.Trim(),
                    ResponseProtocol = ChatResponseProtocol.Auto
                };
                Model.OllamaCores.Add(existing);
            }
            else
            {
                existing.Uri = normalizedEndpoint;
                existing.ModelName = modelName.Trim();
            }

            return (false, existing);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not upsert the endpoint-qualified Ollama host binding for {EndpointHost}.", GetEndpointHostLabel(endpoint));
            throw;
        }
    }

    private (bool IsPrimary, ChatGPTLocalCoreOptions Binding) UpsertOpenAiCompatibleHostBinding(string providerName, string endpoint, string modelName)
    {
        try
        {
            var identity = new ProviderModelIdentity();
            var normalizedEndpoint = identity.NormalizeOpenAiCompatibleEndpoint(endpoint);
            Model.ChatGPTLocalCores ??= new List<ChatGPTLocalCoreOptions>();

            if (string.IsNullOrWhiteSpace(Model.ChatGPTLocalCore?.Endpoint)
                || identity.NormalizeOpenAiCompatibleEndpoint(Model.ChatGPTLocalCore.Endpoint).Equals(normalizedEndpoint, StringComparison.OrdinalIgnoreCase))
            {
                Model.ChatGPTLocalCore ??= new ChatGPTLocalCoreOptions();
                Model.ChatGPTLocalCore.Endpoint = normalizedEndpoint;
                Model.ChatGPTLocalCore.ApiKey = string.IsNullOrWhiteSpace(Model.ChatGPTLocalCore.ApiKey)
                    ? (string.Equals(providerName, "LM Studio", StringComparison.OrdinalIgnoreCase) ? "lm-studio" : "local-key")
                    : Model.ChatGPTLocalCore.ApiKey;
                Model.ChatGPTLocalCore.ModelName = modelName.Trim();
                return (true, Model.ChatGPTLocalCore);
            }

            var existing = Model.ChatGPTLocalCores.FirstOrDefault(item =>
                !string.IsNullOrWhiteSpace(item.Endpoint)
                && identity.NormalizeOpenAiCompatibleEndpoint(item.Endpoint).Equals(normalizedEndpoint, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                existing = new ChatGPTLocalCoreOptions
                {
                    Endpoint = normalizedEndpoint,
                    ApiKey = string.Equals(providerName, "LM Studio", StringComparison.OrdinalIgnoreCase) ? "lm-studio" : "local-key",
                    ModelName = modelName.Trim(),
                    AutoStartServer = false
                };
                Model.ChatGPTLocalCores.Add(existing);
            }
            else
            {
                existing.Endpoint = normalizedEndpoint;
                existing.ApiKey = string.IsNullOrWhiteSpace(existing.ApiKey)
                    ? (string.Equals(providerName, "LM Studio", StringComparison.OrdinalIgnoreCase) ? "lm-studio" : "local-key")
                    : existing.ApiKey;
                existing.ModelName = modelName.Trim();
            }

            return (false, existing);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not upsert the endpoint-qualified OpenAI-compatible host binding for {EndpointHost}.", GetEndpointHostLabel(endpoint));
            throw;
        }
    }

    private bool IsOllamaModelConfigured(string normalizedEndpoint, string modelName)
    {
        try
        {
            var identity = new ProviderModelIdentity();
            bool Matches(OllamaCoreOptions? option) => option is not null
                && !string.IsNullOrWhiteSpace(option.Uri)
                && !string.IsNullOrWhiteSpace(option.ModelName)
                && identity.NormalizeEndpoint(option.Uri).Equals(normalizedEndpoint, StringComparison.OrdinalIgnoreCase)
                && option.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase);
            return Matches(Model.OllamaCore) || (Model.OllamaCores?.Any(Matches) ?? false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not evaluate the configured Ollama model identity.");
            return false;
        }
    }

    private bool IsOpenAiCompatibleModelConfigured(string normalizedEndpoint, string modelName)
    {
        try
        {
            var identity = new ProviderModelIdentity();
            bool Matches(ChatGPTLocalCoreOptions? option) => option is not null
                && !string.IsNullOrWhiteSpace(option.Endpoint)
                && !string.IsNullOrWhiteSpace(option.ModelName)
                && identity.NormalizeOpenAiCompatibleEndpoint(option.Endpoint).Equals(normalizedEndpoint, StringComparison.OrdinalIgnoreCase)
                && option.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase);
            return Matches(Model.ChatGPTLocalCore) || (Model.ChatGPTLocalCores?.Any(Matches) ?? false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not evaluate the configured OpenAI-compatible model identity.");
            return false;
        }
    }

    private async Task MakeConfiguredProviderPrimaryAsync(ConfiguredProviderHostView configured)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(configured);
            if (configured.IsPrimary)
                return;

            if (configured.OllamaHost is not null)
            {
                var selected = configured.OllamaHost;
                ExplicitOllamaPrimaryEndpoint = new ProviderModelIdentity().NormalizeEndpoint(selected.Uri);
                if (!string.IsNullOrWhiteSpace(ExplicitOllamaPrimaryEndpoint))
                    RemovedOllamaEndpoints.Remove(ExplicitOllamaPrimaryEndpoint);
                var previousPrimary = Model.OllamaCore;
                Model.OllamaCores.Remove(selected);
                Model.OllamaCore = selected;
                if (previousPrimary is not null && !string.IsNullOrWhiteSpace(previousPrimary.Uri))
                    AddDistinctOllamaAdditionalBinding(previousPrimary);
            }
            else if (configured.OpenAiHost is not null)
            {
                var selected = configured.OpenAiHost;
                var previousPrimary = Model.ChatGPTLocalCore;
                Model.ChatGPTLocalCores.Remove(selected);
                Model.ChatGPTLocalCore = selected;
                if (previousPrimary is not null && !string.IsNullOrWhiteSpace(previousPrimary.Endpoint))
                    AddDistinctOpenAiAdditionalBinding(previousPrimary);
            }
            else
            {
                throw new InvalidOperationException("Only local Ollama and OpenAI-compatible host bindings can be promoted to primary.");
            }

            await Save().ConfigureAwait(false);
            Notifier.ShowSuccess(toastName, configured.Endpoint, "Primary provider updated");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not promote configured provider {ProviderKey} to primary.", configured.Key);
            Notifier.ShowError(toastName, "The provider could not be promoted. See local application logs for details.", "Primary provider failed");
        }
    }

    private void AddDistinctOllamaAdditionalBinding(OllamaCoreOptions option)
    {
        try
        {
            var identity = new ProviderModelIdentity();
            var endpoint = identity.NormalizeEndpoint(option.Uri);
            Model.OllamaCores ??= new List<OllamaCoreOptions>();
            if (Model.OllamaCores.Any(item =>
                identity.NormalizeEndpoint(item.Uri).Equals(endpoint, StringComparison.OrdinalIgnoreCase)
                && item.ModelName.Equals(option.ModelName, StringComparison.OrdinalIgnoreCase)))
                return;
            Model.OllamaCores.Add(option);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not preserve the previous primary Ollama binding as an additional endpoint-qualified host.");
            throw;
        }
    }

    private void AddDistinctOpenAiAdditionalBinding(ChatGPTLocalCoreOptions option)
    {
        try
        {
            var identity = new ProviderModelIdentity();
            var endpoint = identity.NormalizeOpenAiCompatibleEndpoint(option.Endpoint);
            Model.ChatGPTLocalCores ??= new List<ChatGPTLocalCoreOptions>();
            if (Model.ChatGPTLocalCores.Any(item =>
                identity.NormalizeOpenAiCompatibleEndpoint(item.Endpoint).Equals(endpoint, StringComparison.OrdinalIgnoreCase)
                && item.ModelName.Equals(option.ModelName, StringComparison.OrdinalIgnoreCase)))
                return;
            Model.ChatGPTLocalCores.Add(option);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not preserve the previous primary OpenAI-compatible binding as an additional endpoint-qualified host.");
            throw;
        }
    }

    private string GetEndpointHostLabel(string? endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            return string.IsNullOrWhiteSpace(endpoint) ? "unknown" : "invalid-endpoint";
        return $"{uri.Host}:{uri.Port}";
    }

    private async Task RemoveConfiguredProviderHostAsync(ConfiguredProviderHostView configured)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(configured);

            if (string.Equals(configured.Key, "openai-cloud", StringComparison.OrdinalIgnoreCase))
            {
                Model.OpenAICore = new OpenAICompatOptions
                {
                    Endpoint = string.Empty,
                    ApiKey = string.Empty,
                    ModelName = string.Empty
                };
            }
            else if (string.Equals(configured.Key, "azure-openai", StringComparison.OrdinalIgnoreCase))
            {
                Model.OpenAIServiceCore = new OpenAIServiceCoreOptions
                {
                    Endpoint = string.Empty,
                    Key = string.Empty,
                    DeploymentName = string.Empty
                };
            }
            else if (configured.OpenAiHost is not null)
            {
                if (configured.IsPrimary)
                {
                    Model.ChatGPTLocalCore = new ChatGPTLocalCoreOptions
                    {
                        Endpoint = string.Empty,
                        ApiKey = string.Empty,
                        ModelName = string.Empty,
                        AutoStartServer = false
                    };
                }
                else
                {
                    Model.ChatGPTLocalCores.Remove(configured.OpenAiHost);
                }
            }
            else if (configured.OllamaHost is not null)
            {
                var removedEndpoint = new ProviderModelIdentity().NormalizeEndpoint(configured.OllamaHost.Uri);
                if (!string.IsNullOrWhiteSpace(removedEndpoint))
                    RemovedOllamaEndpoints.Add(removedEndpoint);
                if (string.Equals(ExplicitOllamaPrimaryEndpoint, removedEndpoint, StringComparison.OrdinalIgnoreCase))
                    ExplicitOllamaPrimaryEndpoint = null;

                if (configured.IsPrimary)
                {
                    Model.OllamaCore = new OllamaCoreOptions
                    {
                        Uri = string.Empty,
                        ModelName = string.Empty
                    };
                }
                else
                {
                    Model.OllamaCores.Remove(configured.OllamaHost);
                }
            }
            else
            {
                throw new InvalidOperationException("The selected configured provider has no removable configuration binding.");
            }

            await Save().ConfigureAwait(false);
            Notifier.ShowSuccess(toastName, configured.Provider, "Saved provider removed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RemoveConfiguredProviderHostAsync failed for configured provider {ProviderKey}.", configured.Key);
            Notifier.ShowError(toastName, "The provider could not be removed. See local application logs for details.", "Remove provider failed");
        }
    }

    private async Task RemoveOpenAiCompatibleHostAsync(ChatGPTLocalCoreOptions host)
    {
        Model.ChatGPTLocalCores?.Remove(host);
        await Save().ConfigureAwait(false);
    }

    private void AddOllamaModel()
    {
        try
        {
            Model.OllamaCores.Add(new OllamaCoreOptions
            {
                Uri = string.Empty,
                ModelName = string.Empty
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AddOllamaModel failed: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    private async Task RemoveOllamaModelAsync(OllamaCoreOptions ollama)
    {
        try
        {
            var identity = new ProviderModelIdentity();
            var removedEndpoint = identity.NormalizeEndpoint(ollama.Uri);
            var primaryEndpoint = identity.NormalizeEndpoint(Model.OllamaCore?.Uri);
            if (!string.IsNullOrWhiteSpace(removedEndpoint)
                && !removedEndpoint.Equals(primaryEndpoint, StringComparison.OrdinalIgnoreCase))
                RemovedOllamaEndpoints.Add(removedEndpoint);
            Model.OllamaCores.Remove(ollama);
            await Save().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RemoveOllamaModelAsync failed: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    private async Task TestOpenAiCompatibleHostAsync(ChatGPTLocalCoreOptions host)
    {
        try
        {
            var (ok, msg) = await Probe.TestLocalOpenAICompatAsync(host, default).ConfigureAwait(false);
            await Append((ok ? "OK" : "FAIL") + $" OpenAI-compatible {host.Endpoint}: " + msg).ConfigureAwait(false);
            if (ok) Notifier.ShowSuccess(toastName, host.Endpoint, "Provider reachable");
            else Notifier.ShowWarning(toastName, msg, "Provider test failed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "TestOpenAiCompatibleHostAsync failed for provider endpoint host.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    private async Task TestOllamaHostAsync(OllamaCoreOptions host)
    {
        try
        {
            var (ok, msg) = await Probe.TestOllamaAsync(host, default).ConfigureAwait(false);
            await Append((ok ? "OK" : "FAIL") + $" Ollama {host.Uri}: " + msg).ConfigureAwait(false);
            if (ok) Notifier.ShowSuccess(toastName, host.Uri, "Provider reachable");
            else Notifier.ShowWarning(toastName, msg, "Provider test failed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "TestOllamaHostAsync failed for provider endpoint host.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    private bool IsConfiguredHost(LocalAiHostDiscoveryResult host)
    {
        try
        {
            var identity = new ProviderModelIdentity();
            if (string.Equals(host.Provider, "Ollama", StringComparison.OrdinalIgnoreCase))
            {
                var endpoint = identity.NormalizeEndpoint(host.Endpoint);
                return string.Equals(identity.NormalizeEndpoint(Model.OllamaCore?.Uri), endpoint, StringComparison.OrdinalIgnoreCase)
                    || Model.OllamaCores.Any(item => string.Equals(identity.NormalizeEndpoint(item.Uri), endpoint, StringComparison.OrdinalIgnoreCase));
            }

            var openAiEndpoint = identity.NormalizeOpenAiCompatibleEndpoint(host.Endpoint);
            return string.Equals(identity.NormalizeOpenAiCompatibleEndpoint(Model.ChatGPTLocalCore?.Endpoint), openAiEndpoint, StringComparison.OrdinalIgnoreCase)
                || Model.ChatGPTLocalCores.Any(item => string.Equals(identity.NormalizeOpenAiCompatibleEndpoint(item.Endpoint), openAiEndpoint, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not compare discovered provider host {EndpointHost} with the configured endpoint registry.", GetEndpointHostLabel(host.Endpoint));
            return false;
        }
    }

    private async Task RemoveConfiguredHostAsync(LocalAiHostDiscoveryResult host)
    {
        try
        {
            var identity = new ProviderModelIdentity();
            if (string.Equals(host.Provider, "Ollama", StringComparison.OrdinalIgnoreCase))
            {
                var endpoint = identity.NormalizeEndpoint(host.Endpoint);
                if (!string.IsNullOrWhiteSpace(endpoint))
                    RemovedOllamaEndpoints.Add(endpoint);
                if (string.Equals(ExplicitOllamaPrimaryEndpoint, endpoint, StringComparison.OrdinalIgnoreCase))
                    ExplicitOllamaPrimaryEndpoint = null;
                if (string.Equals(identity.NormalizeEndpoint(Model.OllamaCore?.Uri), endpoint, StringComparison.OrdinalIgnoreCase))
                    Model.OllamaCore = new OllamaCoreOptions { Uri = string.Empty, ModelName = string.Empty };
                Model.OllamaCores.RemoveAll(item => string.Equals(identity.NormalizeEndpoint(item.Uri), endpoint, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var endpoint = identity.NormalizeOpenAiCompatibleEndpoint(host.Endpoint);
                if (string.Equals(identity.NormalizeOpenAiCompatibleEndpoint(Model.ChatGPTLocalCore?.Endpoint), endpoint, StringComparison.OrdinalIgnoreCase))
                    Model.ChatGPTLocalCore = new ChatGPTLocalCoreOptions { Endpoint = string.Empty, ApiKey = string.Empty, ModelName = string.Empty, AutoStartServer = false };
                Model.ChatGPTLocalCores.RemoveAll(item => string.Equals(identity.NormalizeOpenAiCompatibleEndpoint(item.Endpoint), endpoint, StringComparison.OrdinalIgnoreCase));
            }

            await Save().ConfigureAwait(false);
            Notifier.ShowSuccess(toastName, host.Endpoint, "Saved provider removed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RemoveConfiguredHostAsync failed for provider endpoint host.");
            Notifier.ShowError(toastName, "The provider could not be removed. See local application logs for details.", "Remove provider failed");
        }
    }

    private string NormalizeProviderEndpoint(string? endpoint)
        => string.IsNullOrWhiteSpace(endpoint)
            ? string.Empty
            : new ProviderModelIdentity().NormalizeEndpoint(endpoint);

    private async Task TestOpenAI()
    {
        try
        {
            var (ok, msg) = await Probe.TestOpenAIAsync(Model.OpenAICore!, default).ConfigureAwait(false);
            await Append((ok ? "OK" : "FAIL") + " OpenAI: " + msg).ConfigureAwait(false);
            if (ok) Notifier.ShowSuccess(toastName, "OpenAI reachable.", "OpenAI Test OK");
            else    Notifier.ShowWarning(toastName, msg, "OpenAI Test Failed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "TestOpenAI failed: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            await Append("(OpenAI Test) " + ex.Message).ConfigureAwait(false);
        }
    }

    private async Task TestAzure()
    {
        try
        {
            var (ok, msg) = await Probe.TestAzureAsync(Model.OpenAIServiceCore!, default).ConfigureAwait(false);
            await Append((ok ? "OK" : "FAIL") + " Azure: " + msg).ConfigureAwait(false);
            if (ok) Notifier.ShowSuccess(toastName, "Azure endpoint reachable.", "Azure Test OK");
            else    Notifier.ShowWarning(toastName, msg, "Azure Test Failed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "TestAzure failed: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            await Append("(Azure Test) " + ex.Message).ConfigureAwait(false);
        }
    }

    private async Task Save()
    {
        var persistedSuccessfully = false;
        try
        {
            IsSaving = true;
            NormalizeLocalEndpoint();

            var root = Opts.CurrentValue;
            ApplyModelsToConfiguration(root);
            await Writer.SaveAsync(root).ConfigureAwait(false);

            // Refresh connectivity for status only. Discovery must never rewrite provider
            // configuration; selecting a discovered model remains an explicit user action.
            await RefreshConnectivityStatusAsync(showToast: false).ConfigureAwait(false);
            NormalizeLocalEndpoint();
            ApplyModelsToConfiguration(root);
            await Writer.SaveAsync(root).ConfigureAwait(false);
            persistedSuccessfully = true;

            await Append("Saved durable provider and network endpoint settings. A changed network listener becomes active after restart.").ConfigureAwait(false);
            Notifier.ShowSuccess(toastName, "Provider/network settings saved. Restart LocalGPT to apply listener changes.", "Saved");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Save failed: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            await Append("(Save) " + ex.Message).ConfigureAwait(false);
        }
        finally
        {
            if (persistedSuccessfully)
            {
                RemovedOllamaEndpoints.Clear();
                ExplicitOllamaPrimaryEndpoint = null;
            }
            IsSaving = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private void ApplyModelsToConfiguration(LocalGPT.BusinessObjects.ConfigurationRoot root)
    {
        root.AICore ??= new AICoreOptions();
        ProviderConfigurationRegistry.ApplyDetachedDraft(
            root.AICore,
            Model,
            RemovedOllamaEndpoints,
            ExplicitOllamaPrimaryEndpoint);
        Model = ProviderConfigurationRegistry.CreateDetachedDraft(root.AICore);
        root.LoggingCore = LoggingModel;
        root.LoggingCore.DatabaseCore = DatabaseLoggerModel;
        root.LocalGPT ??= new LocalGptHostOptions();
        root.LocalGPT.RemoteEndpoint = new RemoteWebEndpointOptions
        {
            Enabled = NetworkModel.Enabled,
            Address = string.IsNullOrWhiteSpace(NetworkModel.Address) ? "0.0.0.0" : NetworkModel.Address.Trim(),
            Port = NetworkModel.Port,
            CertificatePath = NetworkModel.CertificatePath?.Trim() ?? string.Empty,
            CertificatePassword = NetworkModel.CertificatePassword ?? string.Empty
        };
    }

    private async Task CreateNetworkCertificateAsync()
    {
        try
        {
            IsCertificateBusy = true;
            CertificateStatus = "Creating certificate...";
            var result = await NetworkCertificates.CreateAsync(CertificateRequest).ConfigureAwait(false);
            CertificateStatus = $"Created {result.Thumbprint} at {result.PfxPath}; {result.StoreDescription}; valid until {result.NotAfter:O}.";
            if (UseCreatedCertificateForRemoteEndpoint)
            {
                NetworkModel.CertificatePath = result.PfxPath;
                NetworkModel.CertificatePassword = CertificateRequest.Password ?? string.Empty;
                if (NetworkModel.Port <= 0) NetworkModel.Port = 5443;
                NetworkModel.Enabled = true;
            }
            await Append(CertificateStatus).ConfigureAwait(false);
            Notifier.ShowSuccess(toastName, "Certificate created. Save setup settings and restart LocalGPT to activate HTTPS.", "TLS certificate ready");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Certificate creation failed: {Message}", ex.Message);
            CertificateStatus = ex.Message;
            Notifier.ShowError(toastName, "Certificate creation failed. See local application logs for details.", "TLS certificate failed");
            await Append("(Certificate) " + ex.Message).ConfigureAwait(false);
        }
        finally
        {
            IsCertificateBusy = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private void GoChat()
    {
        try
        {
            Nav.NavigateTo("/Chat", forceLoad: false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Navigation to /Chat failed: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            _ = Append("(Nav) " + ex.Message);
        }
    }

    
    }
}

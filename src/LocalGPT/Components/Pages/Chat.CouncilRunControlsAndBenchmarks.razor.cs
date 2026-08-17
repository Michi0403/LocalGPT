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
using Microsoft.AspNetCore.WebUtilities;

namespace LocalGPT.Components.Pages
{
    public partial class Chat
    {

    private Task OnHardwareRoadsChanged()
    {
        if (EditingRunningCouncilConfiguration && ActiveCouncilConfigurationRunId is Guid runId)
        {
            if (UpdateActiveCouncilConfiguration(runId))
            {
                LoadCouncilRunConfiguration(runId);
                modelStatus = $"Applied model settings to running Council {ActiveCouncilRunShortId} revision {ActiveCouncilConfigurationRevision}. Other runs, future sessions, and saved presets were not changed.";
            }
            else
            {
                modelStatus = "The running Council ended before these session-only model settings could be applied.";
            }
            return Task.CompletedTask;
        }

        SelectedModelPreset = null;
        SelectedHardwarePerformancePreset = null;
        SavePreparationConfiguration();
        modelStatus = $"Custom hardware roads active at {CouncilResourceLoadPercent}% session load for future Council runs.";
        return Task.CompletedTask;
    }

    /// <summary>Updates the Council provider timeout without changing other saved or running sessions.</summary>
    private Task OnCouncilModelTimeoutChangedAsync(int value)
    {
        value = Math.Clamp(value, 30, 1800);
        if (EditingRunningCouncilConfiguration && ActiveCouncilConfigurationRunId is Guid runId)
        {
            ActiveCouncilModelTimeoutSeconds = value;
            if (UpdateActiveCouncilConfiguration(runId))
            {
                LoadCouncilRunConfiguration(runId);
                modelStatus = $"Running Council {ActiveCouncilRunShortId} now uses a {value}s model timeout for provider requests that have not started yet.";
            }
            else
            {
                modelStatus = "The running Council ended before the model timeout could be applied.";
            }
            return Task.CompletedTask;
        }

        CouncilModelTimeoutSeconds = value;
        SelectedModelPreset = null;
        SavePreparationConfiguration();
        modelStatus = $"Future Council runs will use a {value}s model response timeout.";
        return Task.CompletedTask;
    }

    /// <summary>Updates the visible per-host Council concurrency ceiling while keeping per-road lane limits independently configurable.</summary>
    private Task OnCouncilMaxParallelModelsChangedAsync(int value)
    {
        value = Math.Max(1, value);
        if (EditingRunningCouncilConfiguration && ActiveCouncilConfigurationRunId is Guid runId)
        {
            ActiveCouncilMaxParallelModels = value;
            if (UpdateActiveCouncilConfiguration(runId))
            {
                LoadCouncilRunConfiguration(runId);
                modelStatus = $"Running Council {ActiveCouncilRunShortId} will use up to {value} request(s) per AI host when the next phase constructs its hardware-road gates; per-model lane limits can reduce that further.";
            }
            else
            {
                modelStatus = "The running Council ended before the per-host concurrency ceiling could be applied.";
            }
            return Task.CompletedTask;
        }

        CouncilMaxParallelModels = value;
        SelectedModelPreset = null;
        SavePreparationConfiguration();
        modelStatus = $"Future Council runs may use up to {value} parallel request(s) per AI host in hardware-road mode; per-model lane limits still apply.";
        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies the simple host/road scheduling choice without requiring the advanced per-model road editor.
    /// </summary>
    private Task OnCouncilSchedulingModeChanged(ChangeEventArgs args)
    {
        try
        {
            var allowParallelRoads = string.Equals(args.Value?.ToString(), "road-parallel", StringComparison.OrdinalIgnoreCase);
            if (EditingRunningCouncilConfiguration && ActiveCouncilConfigurationRunId is Guid runId)
            {
                ActiveCouncilAllowParallelHardwareRoads = allowParallelRoads;
                if (UpdateActiveCouncilConfiguration(runId))
                {
                    LoadCouncilRunConfiguration(runId);
                    modelStatus = $"Running Council {ActiveCouncilRunShortId} now uses {(allowParallelRoads ? "hardware-road parallel" : "host-balanced")} scheduling for model requests that have not started yet.";
                }
                else
                {
                    modelStatus = "The running Council ended before the scheduling policy could be applied.";
                }

                return Task.CompletedTask;
            }

            CouncilAllowParallelHardwareRoads = allowParallelRoads;
            SelectedModelPreset = null;
            SavePreparationConfiguration();
            modelStatus = $"Future Council runs will use {(allowParallelRoads ? "hardware-road parallel" : "host-balanced")} scheduling.";
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, $"{nameof(OnCouncilSchedulingModeChanged)} failed while applying the user-selected Council scheduling policy.");
            Notifier.ShowError(toastName, "The load-balancing policy could not be changed. See local logs for details.", "Scheduling failed");
            return Task.CompletedTask;
        }
    }

    private Task OnCouncilResourceLoadChanged(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), out var value))
            return Task.CompletedTask;

        value = Math.Clamp((int)Math.Round(value / 5d) * 5, 0, 100);
        if (EditingRunningCouncilConfiguration && ActiveCouncilConfigurationRunId is Guid runId)
        {
            ActiveCouncilResourceLoadPercent = value;
            UpdateActiveCouncilConfiguration(runId);
            LoadCouncilRunConfiguration(runId);
            modelStatus = $"Running Council {ActiveCouncilRunShortId} now uses session settings revision {ActiveCouncilConfigurationRevision} at {value}% load for each model request that has not started yet.";
        }
        else
        {
            CouncilResourceLoadPercent = value;
            SelectedModelPreset = null;
            SavePreparationConfiguration();
            modelStatus = $"Future Council runs will use {value}% session load.";
        }

        return Task.CompletedTask;
    }

    private void ApplyCouncilRunConfigurationSnapshot(CouncilRunConfigurationSnapshot snapshot)
    {
        ActiveCouncilConfigurationRunId = snapshot.RunId;
        ActiveCouncilConfigurationRevision = snapshot.Revision;
        ActiveCouncilConfigurationParticipants = snapshot.Participants.ToList();
        ActiveCouncilModelRoutes = snapshot.ModelRoutes.Select(CloneRoute).ToList();
        ActiveCouncilResourceLoadPercent = snapshot.ResourceLoadPercent;
        ActiveCouncilMaxOutputTokens = snapshot.RequestedMaxOutputTokens;
        ActiveCouncilMaxContextTokens = snapshot.RequestedMaxContextTokens;
        ActiveCouncilFallbackOllamaNumGpu = snapshot.FallbackOllamaNumGpu;
        ActiveCouncilAllowParallelHardwareRoads = snapshot.AllowParallelHardwareRoads;
        ActiveCouncilMaxParallelModels = Math.Max(1, snapshot.MaxParallelModels);
        ActiveCouncilModelTimeoutSeconds = Math.Clamp(snapshot.ModelTimeoutSeconds, 30, 1800);
    }

    private void LoadCouncilRunConfiguration(Guid? runId)
    {
        if (runId is not Guid activeRunId)
        {
            ActiveCouncilConfigurationRunId = null;
            ActiveCouncilConfigurationRevision = 0;
            ActiveCouncilConfigurationParticipants.Clear();
            ActiveCouncilModelRoutes.Clear();
            ActiveCouncilMaxOutputTokens = 0;
            ActiveCouncilMaxContextTokens = 0;
            ActiveCouncilFallbackOllamaNumGpu = null;
            ActiveCouncilAllowParallelHardwareRoads = true;
            ActiveCouncilMaxParallelModels = 1;
            ActiveCouncilModelTimeoutSeconds = 1800;
            return;
        }

        var snapshot = CouncilRunConfigurations.Get(activeRunId);
        if (snapshot is not null &&
            snapshot.IsRunning &&
            ActiveCouncilConfigurationRunId == activeRunId &&
            ActiveCouncilConfigurationRevision == snapshot.Revision)
        {
            return;
        }

        if (snapshot is null || !snapshot.IsRunning)
        {
            if (ActiveCouncilConfigurationRunId == activeRunId)
            {
                ActiveCouncilConfigurationRunId = null;
                ActiveCouncilConfigurationRevision = 0;
                ActiveCouncilConfigurationParticipants.Clear();
                ActiveCouncilModelRoutes.Clear();
                ActiveCouncilMaxOutputTokens = 0;
                ActiveCouncilMaxContextTokens = 0;
                ActiveCouncilFallbackOllamaNumGpu = null;
                ActiveCouncilAllowParallelHardwareRoads = true;
            ActiveCouncilMaxParallelModels = 1;
            ActiveCouncilModelTimeoutSeconds = 1800;
            }
            return;
        }

        ApplyCouncilRunConfigurationSnapshot(snapshot);
    }

    private void OnCouncilRunConfigurationChanged(Guid runId)
    {
        if (isDisposed || SelectedCouncilRunId != runId)
            return;

        try
        {
            TaskRunner.Run(
                nameof(Chat),
                "RefreshCouncilRunConfiguration",
                _ => InvokeAsync(() =>
                {
                    LoadCouncilRunConfiguration(runId);
                    StateHasChanged();
                }),
                componentLifetimeCts.Token);
        }
        catch (ObjectDisposedException) when (isDisposed)
        {
        }
    }


    private void SynchronizeSelectedCouncilRoutes()
    {
        try
        {
            var synchronized = CouncilHardwareRoads
                .Synchronize(SelectedCouncilModelNames, CouncilModelRoutes)
                .Select(CloneRoute)
                .ToList();

            foreach (var route in synchronized)
            {
                var candidate = OllamaCandidates.FirstOrDefault(item =>
                    item.SelectionKey.Equals(route.ModelName, StringComparison.OrdinalIgnoreCase));
                if (candidate is null)
                    continue;

                route.ModelName = candidate.SelectionKey;
                route.ProviderKind = candidate.ProviderKind;
                route.ProviderName = candidate.Provider;
                route.ProviderEndpoint = candidate.Endpoint;
                route.ProviderModelName = candidate.ModelName;
                if (!candidate.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase))
                    route.OllamaNumGpu = null;
            }

            CouncilModelRoutes = synchronized;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not synchronize provider-qualified Council routes for the current preparation selection.");
            throw;
        }
    }

    private List<OneWireCouncilModelRoute> CreateProviderQualifiedCouncilRoutes()
    {
        var routes = CouncilModelRoutes.Select(CloneRoute).ToList();
        foreach (var route in routes)
        {
            var candidate = OllamaCandidates.FirstOrDefault(item =>
                item.SelectionKey.Equals(route.ModelName, StringComparison.OrdinalIgnoreCase));
            if (candidate is null)
                continue;
            route.ModelName = candidate.SelectionKey;
            route.ProviderKind = candidate.ProviderKind;
            route.ProviderName = candidate.Provider;
            route.ProviderEndpoint = candidate.Endpoint;
            route.ProviderModelName = candidate.ModelName;
            if (!candidate.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase))
                route.OllamaNumGpu = null;
        }
        return routes;
    }

    private OneWireCouncilModelRoute CloneRoute(OneWireCouncilModelRoute route) => new()
    {
        ModelName = route.ModelName,
        ProviderKind = route.ProviderKind,
        ProviderName = route.ProviderName,
        ProviderEndpoint = route.ProviderEndpoint,
        ProviderModelName = route.ProviderModelName,
        HardwareKind = route.HardwareKind,
        HardwareIndex = route.HardwareIndex,
        HardwareName = route.HardwareName,
        MinOutputTokens = route.MinOutputTokens,
        MaxOutputTokens = route.MaxOutputTokens,
        MinContextTokens = route.MinContextTokens,
        MaxContextTokens = route.MaxContextTokens,
        OllamaNumGpu = route.OllamaNumGpu,
        LoadPercentOverride = route.LoadPercentOverride,
        SelfReportedDxFunctions = [.. route.SelfReportedDxFunctions],
        SelfReportedControllerMethods = [.. route.SelfReportedControllerMethods],
        SelfReportedOrganicCapabilities = [.. route.SelfReportedOrganicCapabilities],
        SelfReportedSkills = [.. route.SelfReportedSkills],
        IsEnabled = route.IsEnabled,
        MaxConcurrentModelsOnLane = route.MaxConcurrentModelsOnLane
    };

    private void SetCouncilHostSelection(CouncilProviderHostGroup host, bool isSelected)
    {
        try
        {
            foreach (var candidate in host.Models)
            {
                if (isSelected)
                {
                    if (!SelectedCouncilModelNames.Contains(candidate.SelectionKey, StringComparer.OrdinalIgnoreCase))
                        SelectedCouncilModelNames.Add(candidate.SelectionKey);
                }
                else
                {
                    SelectedCouncilModelNames.RemoveAll(value => value.Equals(candidate.SelectionKey, StringComparison.OrdinalIgnoreCase));
                }
            }
            SelectedModelPreset = null;
            SynchronizeSelectedCouncilRoutes();
            SavePreparationConfiguration();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Provider-host Council selection failed for {Provider} at {Endpoint}.", host.ProviderName, host.EndpointLabel);
            Notifier.ShowError(toastName, "The provider host selection could not be changed. See local logs for details.", "Selection failed");
        }
    }

    private void ToggleCouncilModel(string selectionKey, bool isChecked)
    {
        try
        {
            if (isChecked)
            {
                if (!SelectedCouncilModelNames.Contains(selectionKey, StringComparer.OrdinalIgnoreCase))
                    SelectedCouncilModelNames.Add(selectionKey);
            }
            else
            {
                SelectedCouncilModelNames.RemoveAll(name => name.Equals(selectionKey, StringComparison.OrdinalIgnoreCase));
            }

            SelectedModelPreset = null;
            SynchronizeSelectedCouncilRoutes();
            SavePreparationConfiguration();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Provider-qualified council model selection failed.");
            Notifier.ShowError(toastName, "The council model selection could not be changed. See local logs for details.", "Selection failed");
        }
    }

    private void RemoveUnavailableCouncilSelection(string selectionKey)
    {
        try
        {
            SelectedCouncilModelNames.RemoveAll(value => value.Equals(selectionKey, StringComparison.OrdinalIgnoreCase));
            DiagnosticCouncilModelNames.RemoveAll(value => value.Equals(selectionKey, StringComparison.OrdinalIgnoreCase));
            SelectedModelPreset = null;
            SynchronizeSelectedCouncilRoutes();
            SavePreparationConfiguration();
            modelSelectionNotice = L("Chat.Council.UnavailableRoutes.Removed", "Unavailable Council route removed from the current selection.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Removing unavailable provider-qualified Council selection failed.");
            Notifier.ShowError(toastName, L("Chat.Council.UnavailableRoutes.RemoveFailed", "The unavailable Council route could not be removed. See local logs."), L("Common.Error", "Error"));
        }
    }

    private void RemoveAllUnavailableCouncilSelections()
    {
        try
        {
            var unavailable = UnavailableCouncilSelections;
            SelectedCouncilModelNames.RemoveAll(value => unavailable.Contains(value, StringComparer.OrdinalIgnoreCase));
            DiagnosticCouncilModelNames.RemoveAll(value => unavailable.Contains(value, StringComparer.OrdinalIgnoreCase));
            SelectedModelPreset = null;
            SynchronizeSelectedCouncilRoutes();
            SavePreparationConfiguration();
            modelSelectionNotice = L("Chat.Council.UnavailableRoutes.RemovedAll", "Unavailable Council routes removed from the current selection.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Removing all unavailable provider-qualified Council selections failed.");
            Notifier.ShowError(toastName, L("Chat.Council.UnavailableRoutes.RemoveFailed", "The unavailable Council route could not be removed. See local logs."), L("Common.Error", "Error"));
        }
    }

    private List<string> ReconcileAvailableProviderSelectionKeys(IEnumerable<string> values, string selectionScope)
    {
        try
        {
            var normalized = new List<string>();
            var unavailable = new List<string>();
            var identity = new ProviderModelIdentity();
            foreach (var raw in values.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                var value = raw.Trim();
                var exact = OllamaCandidates.FirstOrDefault(candidate =>
                    candidate.SelectionKey.Equals(value, StringComparison.OrdinalIgnoreCase));
                if (exact is not null)
                {
                    if (!normalized.Contains(exact.SelectionKey, StringComparer.OrdinalIgnoreCase))
                        normalized.Add(exact.SelectionKey);
                    continue;
                }

                if (identity.LooksProviderQualified(value))
                {
                    unavailable.Add(value);
                    if (!normalized.Contains(value, StringComparer.OrdinalIgnoreCase))
                        normalized.Add(value);
                    continue;
                }

                var byModel = OllamaCandidates
                    .Where(candidate => candidate.ModelName.Equals(value, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (byModel.Count == 1)
                {
                    if (!normalized.Contains(byModel[0].SelectionKey, StringComparer.OrdinalIgnoreCase))
                        normalized.Add(byModel[0].SelectionKey);
                    continue;
                }

                // A bare legacy value is retained when no unique mapping is possible. The runtime
                // will reject ambiguity rather than substituting a provider host silently.
                if (!normalized.Contains(value, StringComparer.OrdinalIgnoreCase))
                    normalized.Add(value);
            }

            if (unavailable.Count > 0)
            {
                hadUnavailableProviderSelections = true;
                modelSelectionNotice = CouncilText.ProviderUnavailableSelectionNotice(unavailable, selectionScope, Logger);
            }

            return normalized;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not reconcile provider-qualified {SelectionScope} selections against the refreshed host catalog.", selectionScope);
            throw;
        }
    }

    private List<string> NormalizeProviderSelectionKeys(IEnumerable<string> values)
    {
        var normalized = new List<string>();
        foreach (var raw in values.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var value = raw.Trim();
            var exact = OllamaCandidates.FirstOrDefault(candidate =>
                candidate.SelectionKey.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                if (!normalized.Contains(exact.SelectionKey, StringComparer.OrdinalIgnoreCase))
                    normalized.Add(exact.SelectionKey);
                continue;
            }

            var byModel = OllamaCandidates
                .Where(candidate => candidate.ModelName.Equals(value, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var resolved = byModel.Count == 1 ? byModel[0].SelectionKey : value;
            if (!normalized.Contains(resolved, StringComparer.OrdinalIgnoreCase))
                normalized.Add(resolved);
        }
        return normalized;
    }

    private async Task SelectProviderModelFromPanelAsync(ProviderModelReference model)
    {
        if (ChatClientProvider is null)
            return;
        var session = ModelsList.FirstOrDefault(item =>
            item.SelectionKey.Equals(model.SelectionKey, StringComparison.OrdinalIgnoreCase));
        if (session is null)
        {
            modelStatus = $"The session for {model.SelectionKey} is no longer available. Refresh provider models.";
            return;
        }
        await OnModelChanged(session).ConfigureAwait(false);
        modelStatus = $"Using {model.SelectionKey} in Chat.";
    }

    private async Task OnBenchmarkPerformancePresetSavedAsync(HardwarePerformancePreset preset)
    {
        await LoadHardwarePerformancePresetsAsync(componentLifetimeCts.Token).ConfigureAwait(false);
        modelStatus = $"Benchmark saved hardware performance preset '{preset.Name}'. Select it in Hardware spooler to apply it without changing Council membership.";
    }

    private async Task OnChatBenchmarkAppliedAsync(ProviderModelBenchmarkAppliedEvent applied)
    {
        if (!SelectedCouncilModelNames.Contains(applied.Model.SelectionKey, StringComparer.OrdinalIgnoreCase))
            SelectedCouncilModelNames.Add(applied.Model.SelectionKey);
        CouncilModelRoutes.RemoveAll(route =>
            route.ModelName.Equals(applied.Route.ModelName, StringComparison.OrdinalIgnoreCase));
        CouncilModelRoutes.Add(CloneRoute(applied.Route));
        await RefreshModelPresetsAfterBenchmarkAsync(applied.Preset).ConfigureAwait(false);
        SavePreparationConfiguration();
        modelStatus = $"Applied benchmark recommendation for {applied.Model.SelectionKey} as preset {applied.Preset.Name}.";
    }

    private async Task OnChatBenchmarkCouncilAppliedAsync(ProviderModelBenchmarkBatchAppliedEvent applied)
    {
        foreach (var model in applied.Models)
        {
            if (!SelectedCouncilModelNames.Contains(model.SelectionKey, StringComparer.OrdinalIgnoreCase))
                SelectedCouncilModelNames.Add(model.SelectionKey);
        }
        foreach (var route in applied.Routes)
        {
            CouncilModelRoutes.RemoveAll(existing =>
                existing.ModelName.Equals(route.ModelName, StringComparison.OrdinalIgnoreCase));
            CouncilModelRoutes.Add(CloneRoute(route));
        }
        await RefreshModelPresetsAfterBenchmarkAsync(applied.Preset).ConfigureAwait(false);
        SavePreparationConfiguration();
        modelStatus = $"Applied {applied.Routes.Count} provider-qualified Benchmark Council recommendation(s) as preset {applied.Preset.Name}.";
    }
    }
}

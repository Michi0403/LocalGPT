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
    /// <summary>
    /// Renders the chat Razor component and coordinates the component-local state, commands, and presentation behavior used by the surrounding LocalGPT interface.
    /// </summary>
    public partial class Chat
    {


    /// <summary>
    /// Loads hardware performance presets for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task LoadHardwarePerformancePresetsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Guid? selectedId = null;
            await InvokeAsync(() => selectedId = SelectedHardwarePerformancePreset?.Id).ConfigureAwait(false);
            selectedId ??= CouncilRunConfigurations.GetPreparation()?.HardwarePerformancePresetId;
            var loadedPresets = (await HardwarePerformancePresets
                .GetPresetsAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false)).ToList();
            await InvokeAsync(() =>
            {
                HardwarePerformancePresetItems = loadedPresets;
                SelectedHardwarePerformancePreset = selectedId is Guid id
                    ? loadedPresets.FirstOrDefault(item => item.Id == id)
                    : null;
                HardwarePerformancePresetName = SelectedHardwarePerformancePreset?.Name ?? HardwarePerformancePresetName;
                StateHasChanged();
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || isDisposed)
        {
            Logger.LogDebug("Hardware performance preset refresh was cancelled during Chat teardown.");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not load hardware performance presets.");
            ComponentActivity.RecordWarning(nameof(Chat), "LoadHardwarePerformancePresets", "Hardware performance profiles could not be loaded; manual hardware roads remain available.");
        }
    }

    /// <summary>
    /// Refreshes hardware performance presets for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task RefreshHardwarePerformancePresetsAsync() =>
        LoadHardwarePerformancePresetsAsync(componentLifetimeCts.Token);

    /// <summary>
    /// Handles the hardware performance preset changed async lifecycle or event notification for <see cref="Chat"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the chat operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task OnHardwarePerformancePresetChangedAsync(ChangeEventArgs args)
    {
        try
        {
            if (!Guid.TryParse(Convert.ToString(args.Value, CultureInfo.InvariantCulture), out var presetId))
            {
                SelectedHardwarePerformancePreset = null;
                HardwarePerformancePresetName = string.Empty;
                if (EditingRunningCouncilConfiguration && ActiveCouncilConfigurationRunId is Guid activeRunId)
                    CouncilRunConfigurations.UpdateHardwarePerformancePresetIdentity(activeRunId, null);
                else
                    SavePreparationConfiguration();
                modelStatus = "Custom hardware settings are active. Current roads were not reset.";
                return;
            }

            var preset = HardwarePerformancePresetItems.FirstOrDefault(item => item.Id == presetId);
            if (preset is null)
                return;

            int applied;
            if (EditingRunningCouncilConfiguration && ActiveCouncilConfigurationRunId is Guid runId)
            {
                applied = await HardwarePerformancePresets
                    .ApplyPresetToRunAsync(preset.Id, runId, userConfirmed: true, componentLifetimeCts.Token)
                    .ConfigureAwait(false);
                await InvokeAsync(() =>
                {
                    LoadCouncilRunConfiguration(runId);
                    SelectedHardwarePerformancePreset = preset;
                    HardwarePerformancePresetName = preset.Name;
                    modelStatus = $"Applied hardware performance preset '{preset.Name}' to {applied} matching provider-qualified model road(s) in running Council {ActiveCouncilRunShortId}. Council membership was not changed.";
                    StateHasChanged();
                }).ConfigureAwait(false);
            }
            else
            {
                // Persist the current participant selection first; the service then owns all provider-qualified
                // matching, road copying, normalization and session token-ceiling changes.
                SavePreparationConfiguration();
                applied = await HardwarePerformancePresets
                    .ApplyPresetToPreparationAsync(preset.Id, userConfirmed: true, componentLifetimeCts.Token)
                    .ConfigureAwait(false);
                var preparation = CouncilRunConfigurations.GetPreparation();
                await InvokeAsync(() =>
                {
                    if (preparation is not null)
                        ApplyPreparationConfiguration(preparation);
                    SelectedHardwarePerformancePreset = preset;
                    HardwarePerformancePresetName = preset.Name;
                    modelStatus = $"Applied hardware performance preset '{preset.Name}' to {applied} matching provider-qualified model road(s), including the profile session token ceilings. Council membership was not changed.";
                    StateHasChanged();
                }).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (componentLifetimeCts.IsCancellationRequested || isDisposed)
        {
            Logger.LogDebug("Hardware performance preset application was cancelled during Chat teardown.");
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning(ex, "The selected hardware performance preset could not be applied to the current Council configuration.");
            await InvokeAsync(() =>
            {
                SelectedHardwarePerformancePreset = null;
                modelStatus = ex.Message;
                Notifier.ShowWarning(toastName, ex.Message, "Performance preset not applied");
                StateHasChanged();
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not apply hardware performance preset.");
            await InvokeAsync(() => Notifier.ShowError(toastName, "The hardware performance preset could not be applied. See local logs.", "Performance preset failed")).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Applies a hardware performance preset selected from the compact prompt-line DevExpress selector by delegating to the same service-backed path used by Chat configuration.
    /// </summary>
    /// <param name="preset">Service-backed preset selected by the user, or <see langword="null"/> when no preset is selected.</param>
    /// <returns>A task that completes after the shared hardware-preset application path has finished.</returns>
    private Task OnQuickHardwarePerformancePresetChangedAsync(HardwarePerformancePreset? preset) =>
        OnHardwarePerformancePresetChangedAsync(new ChangeEventArgs { Value = preset?.Id.ToString() ?? string.Empty });

    /// <summary>
    /// Persists hardware performance preset for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SaveHardwarePerformancePresetAsync()
    {
        await RunUiActionAsync(async () =>
        {
            isHardwarePerformancePresetBusy = true;
            try
            {
                var preset = new HardwarePerformancePreset
                {
                    Id = SelectedHardwarePerformancePreset?.Id ?? Guid.NewGuid(),
                    Name = HardwarePerformancePresetName,
                    Description = EditingRunningCouncilConfiguration
                        ? $"Saved from running Council {ActiveCouncilRunShortId} hardware spooler settings."
                        : "Saved from Chat configuration hardware spooler settings.",
                    ModelRoutesJson = JsonSerializer.Serialize(CouncilEditorRoutes.Select(CloneRoute).ToList()),
                    ResourceLoadPercent = CouncilEditorResourceLoadPercent,
                    SourceKind = "Manual",
                    IsDefault = SelectedHardwarePerformancePreset?.IsDefault ?? HardwarePerformancePresetItems.Count == 0,
                    IsUserApproved = true
                };
                var saved = await HardwarePerformancePresets
                    .SavePresetAsync(preset, userConfirmed: true, componentLifetimeCts.Token)
                    .ConfigureAwait(false);
                await LoadHardwarePerformancePresetsAsync(componentLifetimeCts.Token).ConfigureAwait(false);
                SelectedHardwarePerformancePreset = HardwarePerformancePresetItems.FirstOrDefault(item => item.Id == saved.Id) ?? saved;
                HardwarePerformancePresetName = saved.Name;
                Notifier.ShowSuccess(toastName, "The hardware performance preset was saved to SQLite.", "Performance preset saved");
            }
            finally
            {
                isHardwarePerformancePresetBusy = false;
            }
        }, "Save hardware performance preset").ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes hardware performance preset for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task DeleteHardwarePerformancePresetAsync()
    {
        if (SelectedHardwarePerformancePreset is null)
            return;
        await RunUiActionAsync(async () =>
        {
            isHardwarePerformancePresetBusy = true;
            try
            {
                var id = SelectedHardwarePerformancePreset.Id;
                await HardwarePerformancePresets
                    .DeletePresetAsync(id, userConfirmed: true, componentLifetimeCts.Token)
                    .ConfigureAwait(false);
                SelectedHardwarePerformancePreset = null;
                HardwarePerformancePresetName = string.Empty;
                await LoadHardwarePerformancePresetsAsync(componentLifetimeCts.Token).ConfigureAwait(false);
                Notifier.ShowSuccess(toastName, "The hardware performance preset was deleted. Current road values were left unchanged.", "Performance preset deleted");
            }
            finally
            {
                isHardwarePerformancePresetBusy = false;
            }
        }, "Delete hardware performance preset").ConfigureAwait(false);
    }

    /// <summary>
    /// Refreshes the service-backed Council model-preset list without reapplying defaults or replacing the current manual Council preparation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous refresh.</param>
    /// <returns>A task that completes after the latest model-preset rows have been loaded.</returns>
    private async Task RefreshModelPresetItemsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Guid? selectedId = null;
            await InvokeAsync(() => selectedId = SelectedModelPreset?.Id).ConfigureAwait(false);
            var loadedPresets = (await ModelPresetService
                .GetPresetsAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false)).ToList();
            await InvokeAsync(() =>
            {
                ModelPresets = loadedPresets;
                SelectedModelPreset = selectedId is Guid id
                    ? loadedPresets.FirstOrDefault(item => item.Id == id)
                    : null;
                if (SelectedModelPreset is not null)
                    ModelPresetName = SelectedModelPreset.Name;
                StateHasChanged();
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || isDisposed)
        {
            Logger.LogDebug("Council model preset refresh was cancelled during Chat teardown.");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not refresh Council model presets from the model preset service.");
            ComponentActivity.RecordWarning(nameof(Chat), "RefreshModelPresetItems", "Council model presets could not be refreshed; the current in-memory selection remains available.");
        }
    }

    /// <summary>
    /// Loads model presets for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous preset load.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task LoadModelPresetsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var loadedPresets = (await ModelPresetService
                .GetPresetsAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false)).ToList();
            await InvokeAsync(() =>
            {
                ModelPresets = loadedPresets;
                var requestedPreset = string.IsNullOrWhiteSpace(RequestedModelPresetName)
                    ? null
                    : loadedPresets.FirstOrDefault(item => string.Equals(item.Name, RequestedModelPresetName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (requestedPreset is not null && DiagnosticCouncilModelNames.Count == 0)
                {
                    ApplyModelPreset(requestedPreset);
                    modelStatus = $"Loaded quick-start model preset '{requestedPreset.Name}'.";
                }
                else
                {
                    var preparation = CouncilRunConfigurations.GetPreparation();
                    if (preparation is not null && DiagnosticCouncilModelNames.Count == 0)
                    {
                        ApplyPreparationConfiguration(preparation);
                    }
                    else
                    {
                        var defaultPreset = loadedPresets.FirstOrDefault(item => item.IsDefault);
                        if (SelectedModelPreset is null && defaultPreset is not null && DiagnosticCouncilModelNames.Count == 0)
                            ApplyModelPreset(defaultPreset);
                    }
                }
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || isDisposed)
        {
            Logger.LogDebug("Council model preset load was cancelled during Chat teardown.");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not load council model presets.");
            ComponentActivity.RecordWarning(nameof(Chat), "LoadModelPresets", "Council model presets could not be loaded; the current manual selection remains available.");
        }
    }

    /// <summary>
    /// Handles the model preset changed async lifecycle or event notification for <see cref="Chat"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the chat operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task OnModelPresetChangedAsync(ChangeEventArgs args)
    {
        try
        {
            if (!Guid.TryParse(Convert.ToString(args.Value, CultureInfo.InvariantCulture), out var presetId))
            {
                SelectedModelPreset = null;
                ModelPresetName = string.Empty;
                SavePreparationConfiguration();
                return;
            }

            var preset = ModelPresets.FirstOrDefault(item => item.Id == presetId);
            if (preset is null)
                return;
            ApplyModelPreset(preset);
            modelStatus = $"Loaded model preset '{preset.Name}'.";
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not apply a council model preset.");
            Notifier.ShowError(toastName, "The model preset could not be applied. See local logs.", "Preset failed");
        }
    }

    /// <summary>
    /// Applies a Council model preset selected from the compact prompt-line DevExpress selector through the same path used by Chat configuration.
    /// </summary>
    /// <param name="preset">Service-backed Council model preset selected by the user, or <see langword="null"/> when no preset is selected.</param>
    /// <returns>A task that completes after the shared model-preset application path has finished.</returns>
    private Task OnQuickModelPresetChangedAsync(CouncilModelPreset? preset) =>
        OnModelPresetChangedAsync(new ChangeEventArgs { Value = preset?.Id.ToString() ?? string.Empty });

    /// <summary>
    /// Applies model preset for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <param name="preset">Preset value supplied to the chat operation and used when producing its result.</param>
    private void ApplyModelPreset(CouncilModelPreset preset)
    {
        SelectedModelPreset = preset;
        ModelPresetName = preset.Name;
        SelectedCouncilModelNames = NormalizeProviderSelectionKeys(
            JsonSerializer.Deserialize<List<string>>(preset.ModelNamesJson) ?? []);
        CouncilModelRoutes = ParseModelRoutes(preset.ModelRoutesJson);
        SynchronizeSelectedCouncilRoutes();
        var loadOverrides = CouncilModelRoutes.Where(route => route.LoadPercentOverride.HasValue).Select(route => route.LoadPercentOverride!.Value).Distinct().ToList();
        if (loadOverrides.Count == 1) CouncilResourceLoadPercent = Math.Clamp((int)Math.Round(loadOverrides[0] / 5d) * 5, 0, 100);
        CouncilMaxOutputTokens = Math.Clamp(preset.MaxOutputTokens, Catalog.MinCouncilOutputTokens, Catalog.MaxCouncilOutputTokens);
        CouncilMaxContextTokens = Math.Clamp(preset.MaxContextTokens, Catalog.MinCouncilContextTokens, Catalog.MaxCouncilContextTokens);
        IncludeCouncilMemory = preset.IncludeMemory;
        GenerateCouncilArtifacts = preset.GenerateArtifacts;
        CreateProjectPerCouncilRun = preset.CreateProjectPerRun;
        CouncilAllowParallelHardwareRoads = preset.AllowParallelHardwareRoads;
        CouncilMaxParallelModels = Math.Max(1, preset.MaxParallelModels);
        if (preset.OllamaNumGpu == 0)
            OllamaAccelerationMode = Catalog.OllamaModeSafeCpu;
        else if (preset.OllamaNumGpu is > 0)
        {
            OllamaAccelerationMode = Catalog.OllamaModeLimitedGpu;
            LimitedGpuLayers = Math.Clamp(preset.OllamaNumGpu.Value, 1, 99);
        }
        else
            OllamaAccelerationMode = Catalog.OllamaModeAutoGpu;

        SavePreparationConfiguration();
    }

    /// <summary>
    /// Persists model preset for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SaveModelPresetAsync()
    {
        await RunUiActionAsync(async () =>
        {
            isModelPresetBusy = true;
            try
            {
                var preset = new CouncilModelPreset
                {
                    Id = SelectedModelPreset?.Id ?? Guid.NewGuid(),
                    Name = ModelPresetName,
                    Description = "Saved from the DXChat council model selector.",
                    ModelNamesJson = JsonSerializer.Serialize(SelectedCouncilModelNames),
                    ModelRoutesJson = JsonSerializer.Serialize(CreateProviderQualifiedCouncilRoutes()),
                    AllowParallelHardwareRoads = CouncilAllowParallelHardwareRoads,
                    MaxOutputTokens = CouncilMaxOutputTokens,
                    MaxContextTokens = CouncilMaxContextTokens,
                    MaxParallelModels = Math.Max(1, CouncilMaxParallelModels),
                    OllamaNumGpu = ResolveOllamaNumGpu(),
                    IncludeMemory = IncludeCouncilMemory,
                    GenerateArtifacts = GenerateCouncilArtifacts,
                    CreateProjectPerRun = CreateProjectPerCouncilRun,
                    IsDefault = SelectedModelPreset?.IsDefault ?? ModelPresets.Count == 0,
                    IsUserApproved = true
                };
                var savedPreset = await ModelPresetService.SavePresetAsync(preset, userConfirmed: true).ConfigureAwait(false);
                await LoadModelPresetsAsync().ConfigureAwait(false);
                SelectedModelPreset = ModelPresets.FirstOrDefault(item => item.Id == savedPreset.Id);
                Notifier.ShowSuccess(toastName, "The model preset was saved to SQLite.", "Preset saved");
            }
            finally
            {
                isModelPresetBusy = false;
            }
        }, "Save model preset").ConfigureAwait(false);
    }

    /// <summary>
    /// Performs archive model preset for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ArchiveModelPresetAsync()
    {
        if (SelectedModelPreset is null)
            return;
        await RunUiActionAsync(async () =>
        {
            isModelPresetBusy = true;
            try
            {
                await ModelPresetService.ArchivePresetAsync(SelectedModelPreset.Id, userConfirmed: true).ConfigureAwait(false);
                SelectedModelPreset = null;
                ModelPresetName = string.Empty;
                await LoadModelPresetsAsync().ConfigureAwait(false);
                Notifier.ShowSuccess(toastName, "The model preset was archived.", "Preset archived");
            }
            finally
            {
                isModelPresetBusy = false;
            }
        }, "Archive model preset").ConfigureAwait(false);
    }

    /// <summary>
    /// Persists preparation configuration for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    private void SavePreparationConfiguration()
    {
        if (DiagnosticCouncilModelNames.Count > 0)
            return;

        CouncilRunConfigurations.SavePreparation(new CouncilPreparationConfiguration(
            SelectedCouncilModelNames.ToList(),
            CreateProviderQualifiedCouncilRoutes(),
            CouncilResourceLoadPercent,
            ResolveCouncilOutputTokens(),
            ResolveCouncilContextTokens(),
            ResolveOllamaNumGpu(),
            CouncilAllowParallelHardwareRoads,
            Math.Max(1, CouncilMaxParallelModels),
            Math.Clamp(CouncilModelTimeoutSeconds, 30, 1800),
            Math.Clamp(CouncilCritiqueRounds, 0, 3),
            IncludeCouncilMemory,
            CreateProjectPerCouncilRun,
            string.IsNullOrWhiteSpace(SelectedCouncilTeamKey) ? "general" : SelectedCouncilTeamKey)
        {
            ModelPresetId = SelectedModelPreset?.Id,
            HardwarePerformancePresetId = SelectedHardwarePerformancePreset?.Id
        });
    }

    /// <summary>
    /// Applies preparation configuration for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
    private void ApplyPreparationConfiguration(CouncilPreparationConfiguration configuration)
    {
        SelectedModelPreset = configuration.ModelPresetId is Guid modelPresetId
            ? ModelPresets.FirstOrDefault(item => item.Id == modelPresetId)
            : null;
        ModelPresetName = SelectedModelPreset?.Name ?? string.Empty;
        SelectedHardwarePerformancePreset = configuration.HardwarePerformancePresetId is Guid hardwarePresetId
            ? HardwarePerformancePresetItems.FirstOrDefault(item => item.Id == hardwarePresetId)
            : null;
        HardwarePerformancePresetName = SelectedHardwarePerformancePreset?.Name ?? string.Empty;
        SelectedCouncilModelNames = NormalizeProviderSelectionKeys(configuration.ModelNames);
        CouncilModelRoutes = configuration.ModelRoutes.Select(CloneRoute).ToList();
        SynchronizeSelectedCouncilRoutes();
        CouncilResourceLoadPercent = Math.Clamp((int)Math.Round(configuration.ResourceLoadPercent / 5d) * 5, 0, 100);
        CouncilMaxOutputTokens = Math.Clamp(configuration.MaxOutputTokens, Catalog.MinCouncilOutputTokens, Catalog.MaxCouncilOutputTokens);
        CouncilMaxContextTokens = Math.Clamp(configuration.MaxContextTokens, Catalog.MinCouncilContextTokens, Catalog.MaxCouncilContextTokens);
        CouncilCritiqueRounds = Math.Clamp(configuration.CritiqueRounds, 0, 3);
        CouncilAllowParallelHardwareRoads = configuration.AllowParallelHardwareRoads;
        CouncilMaxParallelModels = Math.Max(1, configuration.MaxParallelModels);
        CouncilModelTimeoutSeconds = Math.Clamp(configuration.ModelTimeoutSeconds, 30, 1800);
        IncludeCouncilMemory = configuration.IncludeMemory;
        CreateProjectPerCouncilRun = configuration.CreateProjectPerRun;
        var requestedTeamKey = string.IsNullOrWhiteSpace(configuration.CouncilTeamKey) ? "general" : configuration.CouncilTeamKey;
        SelectedCouncilTeamKey = CouncilTeams.Any(team => string.Equals(team.Key, requestedTeamKey, StringComparison.OrdinalIgnoreCase))
            ? requestedTeamKey
            : CouncilTeams.FirstOrDefault(team => team.Key == "general")?.Key ?? CouncilTeams.FirstOrDefault()?.Key ?? "general";
        if (configuration.OllamaNumGpu == 0)
        {
            OllamaAccelerationMode = Catalog.OllamaModeSafeCpu;
        }
        else if (configuration.OllamaNumGpu is > 0)
        {
            OllamaAccelerationMode = Catalog.OllamaModeLimitedGpu;
            LimitedGpuLayers = Math.Clamp(configuration.OllamaNumGpu.Value, 1, 99);
        }
        else
        {
            OllamaAccelerationMode = Catalog.OllamaModeAutoGpu;
        }

        modelStatus = "Restored the last Council preparation settings for this LocalGPT process.";
    }

    /// <summary>Restores the detailed and compact Chat configuration selectors from the authoritative configuration captured by a live Council session.</summary>
    /// <param name="snapshot">Running Council configuration snapshot being rejoined by this browser circuit.</param>
    private void ApplyRejoinedCouncilPreparationSnapshot(CouncilRunConfigurationSnapshot snapshot)
    {
        var preparation = new CouncilPreparationConfiguration(
            snapshot.Participants,
            snapshot.ModelRoutes,
            snapshot.ResourceLoadPercent,
            snapshot.RequestedMaxOutputTokens,
            snapshot.RequestedMaxContextTokens,
            snapshot.FallbackOllamaNumGpu,
            snapshot.AllowParallelHardwareRoads,
            snapshot.MaxParallelModels,
            snapshot.ModelTimeoutSeconds,
            snapshot.CritiqueRounds,
            snapshot.IncludeMemory,
            snapshot.CreateProjectPerRun,
            snapshot.CouncilTeamKey)
        {
            ModelPresetId = snapshot.ModelPresetId,
            HardwarePerformancePresetId = snapshot.HardwarePerformancePresetId
        };

        ApplyPreparationConfiguration(preparation);
        modelStatus = $"Restored the configuration captured by running Council {ShortCouncilRunId(snapshot.RunId)} for this Chat circuit.";
    }

    /// <summary>
    /// Updates active council configuration for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool UpdateActiveCouncilConfiguration(Guid runId) =>
        CouncilRunConfigurations.Update(
            runId,
            ActiveCouncilModelRoutes,
            ActiveCouncilResourceLoadPercent,
            ActiveCouncilMaxOutputTokens,
            ActiveCouncilMaxContextTokens,
            ActiveCouncilFallbackOllamaNumGpu,
            ActiveCouncilAllowParallelHardwareRoads,
            ActiveCouncilMaxParallelModels,
            ActiveCouncilModelTimeoutSeconds);

    /// <summary>
    /// Handles the council max output tokens changed async lifecycle or event notification for <see cref="Chat"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the chat operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task OnCouncilMaxOutputTokensChangedAsync(int value)
    {
        value = Math.Clamp(value, Catalog.MinCouncilOutputTokens, Catalog.MaxCouncilOutputTokens);
        if (EditingRunningCouncilConfiguration && ActiveCouncilConfigurationRunId is Guid runId)
        {
            ActiveCouncilMaxOutputTokens = value;
            UpdateActiveCouncilConfiguration(runId);
            LoadCouncilRunConfiguration(runId);
            modelStatus = $"Running Council {ActiveCouncilRunShortId} now uses an output ceiling of {value:N0} tokens for model requests that have not started yet.";
        }
        else
        {
            CouncilMaxOutputTokens = value;
            SelectedModelPreset = null;
            SynchronizeSelectedCouncilRoutes();
            SavePreparationConfiguration();
            modelStatus = $"Future Council sessions will use an output ceiling of {value:N0} tokens.";
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the council max context tokens changed async lifecycle or event notification for <see cref="Chat"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the chat operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task OnCouncilMaxContextTokensChangedAsync(int value)
    {
        value = Math.Clamp(value, Catalog.MinCouncilContextTokens, Catalog.MaxCouncilContextTokens);
        if (EditingRunningCouncilConfiguration && ActiveCouncilConfigurationRunId is Guid runId)
        {
            ActiveCouncilMaxContextTokens = value;
            UpdateActiveCouncilConfiguration(runId);
            LoadCouncilRunConfiguration(runId);
            modelStatus = $"Running Council {ActiveCouncilRunShortId} now uses a context ceiling of {value:N0} tokens for model requests that have not started yet.";
        }
        else
        {
            CouncilMaxContextTokens = value;
            SelectedModelPreset = null;
            SavePreparationConfiguration();
            modelStatus = $"Future Council sessions will use a context ceiling of {value:N0} tokens.";
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the limited GPU layers changed lifecycle or event notification for <see cref="Chat"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the chat operation and used when producing its result.</param>
    private void OnLimitedGpuLayersChanged(int value)
    {
        LimitedGpuLayers = Math.Clamp(value, 1, 99);
        SelectedModelPreset = null;
        SavePreparationConfiguration();
        modelStatus = $"Limited GPU mode will use {LimitedGpuLayers} Ollama GPU layer(s).";
    }

    /// <summary>
    /// Handles the council critique rounds changed lifecycle or event notification for <see cref="Chat"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the chat operation and used when producing its result.</param>
    private void OnCouncilCritiqueRoundsChanged(int value)
    {
        CouncilCritiqueRounds = Math.Clamp(value, 0, 3);
        SelectedModelPreset = null;
        SavePreparationConfiguration();
        modelStatus = $"Future Council runs will use {CouncilCritiqueRounds} peer review round(s).";
    }
    }
}

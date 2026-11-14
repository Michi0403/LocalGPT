using LocalGPT.BusinessObjects;
using System.Text.Json;

namespace LocalGPT.Components.Pages;

/// <summary>Owns the Data Management preference editor without changing provider host configuration.</summary>
public partial class DataManagement
{
    /// <summary>Currently discovered provider-qualified model identities.</summary>
    private IReadOnlyList<MultiModelCouncilModelCandidate> PreferredModelCandidates { get; set; } = [];

    /// <summary>Ordered user selection, including unavailable saved model routes.</summary>
    private List<string> PreferredSelectionKeys { get; set; } = [];

    /// <summary>Indicates model discovery or preference persistence is in progress.</summary>
    private bool PreferredModelsBusy { get; set; }

    /// <summary>Reloads exact persisted preference keys and current model discovery.</summary>
    /// <returns>Asynchronous refresh operation.</returns>
    private async Task LoadPreferredModelsCoreAsync()
    {
        PreferredModelsBusy = true;
        try
        {
            PreferredSelectionKeys = (PreferredOptions.CurrentValue.AICore?.PreferredCouncilModelKeys ?? [])
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            try
            {
                PreferredModelCandidates = await PreferredModelRuntime.GetCandidatesAsync().ConfigureAwait(true);
            }
            catch (Exception error)
            {
                PreferredModelCandidates = [];
                Logger.LogWarning(error, "Preferred-model discovery failed; database management remains available.");
                Notifier.ShowWarning(ToastName, "Model discovery is temporarily unavailable; saved preferences remain visible.", "Council models");
            }
        }
        finally
        {
            PreferredModelsBusy = false;
        }
    }

    /// <summary>Refreshes installed model status and discards unsaved preference changes.</summary>
    /// <returns>Asynchronous refresh operation.</returns>
    private async Task RefreshPreferredModelsAsync()
    {
        if (Busy || PreferredModelsBusy) return;
        try
        {
            await LoadPreferredModelsCoreAsync().ConfigureAwait(true);
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Refreshing preferred Council models failed.");
            Notifier.ShowError(ToastName, "Model discovery failed; inspect application logs.", "Council models");
        }
    }

    /// <summary>Formats the provider-qualified selection and availability for screen readers.</summary>
    /// <param name="candidate">Discovered provider model.</param>
    /// <returns>Readable model/host/status label.</returns>
    private string PreferredModelDisplay(MultiModelCouncilModelCandidate candidate) =>
        $"{candidate.Provider} · {candidate.ModelName} · {candidate.Endpoint} " +
        (candidate.IsInstalled ? "(installed)" : candidate.IsConfigured ? "(configured)" : "(discovered)");

    /// <summary>Edits only the staged preferred-model pool, without mutating persistent options.</summary>
    /// <param name="key">Exact provider-qualified model key.</param>
    /// <param name="selected">Whether the model should be preferred.</param>
    private void TogglePreferredModel(string key, bool selected)
    {
        PreferredSelectionKeys.RemoveAll(item => string.Equals(item, key, StringComparison.OrdinalIgnoreCase));
        if (selected)
            PreferredSelectionKeys.Add(key);
    }

    /// <summary>Checks whether the selected model can move one priority position without changing its order.</summary>
    /// <param name="key">Provider-qualified model identity in the staged selection.</param>
    /// <param name="direction">-1 for higher priority, +1 for lower priority.</param>
    /// <returns>True when the target priority index exists.</returns>
    private bool CanMovePreferredModel(string key, int direction)
    {
        var current = PreferredSelectionKeys.FindIndex(value =>
            string.Equals(value, key, StringComparison.OrdinalIgnoreCase));
        var target = current + direction;
        return current >= 0 && target >= 0 && target < PreferredSelectionKeys.Count;
    }

    /// <summary>Moves one preferred model in the persisted priority ordering.</summary>
    /// <param name="key">Exact provider-qualified model identity.</param>
    /// <param name="direction">-1 moves earlier, +1 moves later.</param>
    private void MovePreferredModel(string key, int direction)
    {
        var current = PreferredSelectionKeys.FindIndex(value => string.Equals(value, key, StringComparison.OrdinalIgnoreCase));
        var next = current + direction;
        if (current < 0 || next < 0 || next >= PreferredSelectionKeys.Count) return;
        (PreferredSelectionKeys[current], PreferredSelectionKeys[next]) =
            (PreferredSelectionKeys[next], PreferredSelectionKeys[current]);
    }

    /// <summary>Saves selected model keys using the existing configuration writer, keeping unrelated settings intact.</summary>
    /// <returns>Asynchronous save operation.</returns>
    private async Task SavePreferredModelsAsync()
    {
        if (Busy || PreferredModelsBusy) return;
        PreferredModelsBusy = true;
        try
        {
            var original = PreferredOptions.CurrentValue;
            var aiCore = JsonSerializer.Deserialize<AICoreOptions>(
                JsonSerializer.Serialize(original.AICore ?? new AICoreOptions())) ?? new AICoreOptions();
            aiCore.PreferredCouncilModelKeys = PreferredSelectionKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var updated = new LocalGPT.BusinessObjects.ConfigurationRoot
            {
                AICore = aiCore,
                LoggingCore = original.LoggingCore,
                PythonCore = original.PythonCore,
                ToolchainEnvironment = original.ToolchainEnvironment,
                ConnectionStringsCore = original.ConnectionStringsCore,
                LocalGPT = original.LocalGPT
            };
            await PreferredConfigurationWriter.SaveAsync(updated).ConfigureAwait(true);
            Logger.LogInformation("Saved {ModelCount} preferred provider-qualified Council models.", aiCore.PreferredCouncilModelKeys.Count);
            Notifier.ShowSuccess(ToastName, "Preferred Council models saved.", "Council models");
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Saving preferred Council model routes failed.");
            Notifier.ShowError(ToastName, "Could not save model preferences; inspect local logs.", "Council models");
        }
        finally
        {
            PreferredModelsBusy = false;
        }
    }
}

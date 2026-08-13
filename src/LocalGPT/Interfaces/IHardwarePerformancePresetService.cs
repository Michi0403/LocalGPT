using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Owns persistence, normalization and benchmark synthesis for reusable hardware-spooler performance profiles.
/// </summary>
public interface IHardwarePerformancePresetService
{
    /// <summary>Returns stored performance profiles ordered for user selection.</summary>
    /// <param name="includeArchived">Value indicating whether include archived should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<HardwarePerformancePreset>> GetPresetsAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    /// <summary>Returns one stored performance profile, or null when it does not exist.</summary>
    /// <param name="presetId">Identifier of the preset to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The hardware performance preset produced by the operation.</returns>
    Task<HardwarePerformancePreset?> GetPresetAsync(
        Guid presetId,
        CancellationToken cancellationToken = default);

    /// <summary>Saves a manually prepared or service-generated performance profile after explicit user confirmation.</summary>
    /// <param name="preset">Preset value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The hardware performance preset produced by the operation.</returns>
    Task<HardwarePerformancePreset> SavePresetAsync(
        HardwarePerformancePreset preset,
        bool userConfirmed,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a completed provider benchmark into one reusable hardware profile. The stored min/max ranges are
    /// based on successful measured profiles while each route keeps the Council recommendation as its load override.
    /// </summary>
    /// <param name="report">Report value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <param name="presetName">Preset name value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The hardware performance preset produced by the operation.</returns>
    Task<HardwarePerformancePreset> SaveBenchmarkResultAsync(
        ProviderModelBenchmarkReport report,
        string presetName,
        bool userConfirmed,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes preset as part of the hardware performance preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="presetId">Identifier of the preset to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task DeletePresetAsync(
        Guid presetId,
        bool userConfirmed,
        CancellationToken cancellationToken = default);

    /// <summary>Applies a stored performance profile to the saved preparation configuration for the next Council run without changing Council membership.</summary>
    /// <param name="presetId">Identifier of the stored performance preset.</param>
    /// <param name="userConfirmed">Whether the user explicitly approved changing the prepared hardware roads.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The number of provider-qualified model routes that matched and were updated.</returns>
    Task<int> ApplyPresetToPreparationAsync(
        Guid presetId,
        bool userConfirmed,
        CancellationToken cancellationToken = default);

    /// <summary>Applies a stored performance profile to one running Council configuration without changing participants or unrelated run settings.</summary>
    /// <param name="presetId">Identifier of the stored performance preset.</param>
    /// <param name="runId">Identifier of the running Council to update.</param>
    /// <param name="userConfirmed">Whether the user explicitly approved changing the running hardware roads.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The number of provider-qualified model routes that matched and were updated.</returns>
    Task<int> ApplyPresetToRunAsync(
        Guid presetId,
        Guid runId,
        bool userConfirmed,
        CancellationToken cancellationToken = default);
}

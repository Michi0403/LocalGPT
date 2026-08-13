using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for model preset behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IModelPresetService
{
    /// <summary>
    /// Retrieves presets as part of the model preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="includeArchived">Value indicating whether include archived should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<CouncilModelPreset>> GetPresetsAsync(bool includeArchived = false, CancellationToken cancellationToken = default);
    /// <summary>
    /// Persists preset as part of the model preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="preset">Preset value supplied to the model preset operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council model preset produced by the operation.</returns>
    Task<CouncilModelPreset> SavePresetAsync(CouncilModelPreset preset, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs archive preset as part of the model preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="presetId">Identifier of the preset to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task ArchivePresetAsync(Guid presetId, bool userConfirmed, CancellationToken cancellationToken = default);
}

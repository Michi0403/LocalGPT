using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the model preset service contract.
/// </summary>
public interface IModelPresetService
{
    /// <summary>
    /// Gets presets async.
    /// </summary>
    Task<IReadOnlyList<CouncilModelPreset>> GetPresetsAsync(bool includeArchived = false, CancellationToken cancellationToken = default);
    /// <summary>
    /// Saves preset async.
    /// </summary>
    Task<CouncilModelPreset> SavePresetAsync(CouncilModelPreset preset, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the archive preset async operation.
    /// </summary>
    Task ArchivePresetAsync(Guid presetId, bool userConfirmed, CancellationToken cancellationToken = default);
}

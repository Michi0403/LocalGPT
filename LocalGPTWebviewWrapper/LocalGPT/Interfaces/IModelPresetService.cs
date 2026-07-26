using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IModelPresetService
{
    Task<IReadOnlyList<CouncilModelPreset>> GetPresetsAsync(bool includeArchived = false, CancellationToken cancellationToken = default);
    Task<CouncilModelPreset> SavePresetAsync(CouncilModelPreset preset, bool userConfirmed, CancellationToken cancellationToken = default);
    Task ArchivePresetAsync(Guid presetId, bool userConfirmed, CancellationToken cancellationToken = default);
}

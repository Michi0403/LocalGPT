using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Explicitly approved Godot C# starter generation and bidirectional workspace synchronization.</summary>
public interface IGameGodotWorkspaceService
{
    Task<GameStudioDraft> SeedGodotFilesAsync(Guid projectId, Guid draftId, bool userConfirmed, CancellationToken cancellationToken = default);
    Task<GameStudioDraft> UpdateSceneResourceAsync(Guid projectId, Guid draftId, bool userConfirmed, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ExportAsync(Guid projectId, Guid draftId, bool userConfirmed, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ImportChangedAsync(Guid projectId, Guid draftId, bool userConfirmed, CancellationToken cancellationToken = default);
}

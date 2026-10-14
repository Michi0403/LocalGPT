using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Explicitly approved Godot C# starter generation and bidirectional workspace synchronization.</summary>
public interface IGameGodotWorkspaceService
{
    /// <summary>
    /// Performs seed godot files as part of the game godot workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    Task<GameStudioDraft> SeedGodotFilesAsync(Guid projectId, Guid draftId, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Updates scene resource as part of the game godot workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    Task<GameStudioDraft> UpdateSceneResourceAsync(Guid projectId, Guid draftId, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs export as part of the game godot workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<string>> ExportAsync(Guid projectId, Guid draftId, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Imports changed as part of the game godot workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<string>> ImportChangedAsync(Guid projectId, Guid draftId, bool userConfirmed, CancellationToken cancellationToken = default);
}

using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the project-owned game authoring/build boundary. Project services produce validated runtime definitions;
/// GameDirector consumes those definitions without owning project authoring policy.
/// </summary>
public interface IGameProjectService
{
    /// <summary>Reads the durable editable authoring profile owned by one Game project.</summary>
    /// <param name="projectId">Identifier of the owning LocalGPT project.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The persisted authoring profile, or <c>null</c> when the project has not saved one yet.</returns>
    Task<LocalGptGameProjectProfile?> GetProfileAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>Creates or updates the durable project-owned authoring profile without starting the runtime.</summary>
    /// <param name="projectId">Identifier of the owning LocalGPT project.</param>
    /// <param name="request">User- or AI-authored game settings approved by the caller.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The persisted authoring profile.</returns>
    Task<LocalGptGameProjectProfile> SaveProfileAsync(
        Guid projectId,
        SaveGameProjectProfileRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Builds and persists the runtime definition for one LocalGPT project whose project type is Game.</summary>
    /// <param name="projectId">Identifier of the owning LocalGPT project.</param>
    /// <param name="request">Approval to compile the already-persisted Game-project design baseline.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The persisted build result and compiled runtime definition.</returns>
    Task<ProjectGameBuildResult> BuildAsync(
        Guid projectId,
        BuildProjectGameRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Reads the latest approved built game definition for one project.</summary>
    /// <param name="projectId">Identifier of the owning LocalGPT project.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The latest build, or <c>null</c> when the project has not been built yet.</returns>
    Task<ProjectGameBuildResult?> GetBuildAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>Launches the latest built definition for one game project through the shared GameDirector runtime.</summary>
    /// <param name="projectId">Identifier of the owning LocalGPT project.</param>
    /// <param name="request">Launch-time chat/Council ownership context.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The authoritative game-session snapshot.</returns>
    Task<CouncilGameSessionSnapshot> LaunchAsync(
        Guid projectId,
        LaunchProjectGameRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Resolves a project by exact identifier, identifier prefix, or name and launches its latest game build.</summary>
    /// <param name="selector">Human-entered project selector.</param>
    /// <param name="request">Launch-time chat/Council ownership context.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The authoritative game-session snapshot.</returns>
    Task<CouncilGameSessionSnapshot> LaunchBySelectorAsync(
        string selector,
        LaunchProjectGameRequest request,
        CancellationToken cancellationToken = default);
}

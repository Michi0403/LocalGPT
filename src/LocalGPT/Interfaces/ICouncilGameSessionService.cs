using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for council game session behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ICouncilGameSessionService
{
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="ICouncilGameSessionService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    event Action<Guid>? Changed;

    /// <summary>
    /// Performs start as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    Task<CouncilGameSessionSnapshot> StartAsync(
        StartCouncilGameRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs get as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    Task<CouncilGameSessionSnapshot?> GetAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves active as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="conversationId">Identifier of the conversation to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    Task<CouncilGameSessionSnapshot?> GetActiveAsync(
        Guid? conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves the newest running game owned by one Council run.</summary>
    /// <param name="councilRunId">Identifier of the Council run that owns the game.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the lookup.</param>
    /// <returns>The newest running game owned by the Council run, or <c>null</c> when none exists.</returns>
    Task<CouncilGameSessionSnapshot?> GetActiveForCouncilRunAsync(
        Guid councilRunId,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves the running game of one specific family owned by a Council run.</summary>
    /// <param name="councilRunId">Identifier of the Council run that owns the game.</param>
    /// <param name="gameKey">Stable game-family key that must match the running session.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the lookup.</param>
    /// <returns>The newest matching running game owned by the Council run, or <c>null</c> when none exists.</returns>
    Task<CouncilGameSessionSnapshot?> GetActiveForCouncilRunAsync(
        Guid councilRunId,
        string gameKey,
        CancellationToken cancellationToken = default);

    /// <summary>Ends the game runtime only, leaving the owning chat, Council/provider sessions and terminal surface available.</summary>
    /// <param name="sessionId">Identifier of the game session to end.</param>
    /// <param name="endedBy">Bounded actor label recorded as the last game-session action owner.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The ended game snapshot, or <c>null</c> when the session no longer exists.</returns>
    Task<CouncilGameSessionSnapshot?> EndAsync(
        Guid sessionId,
        string endedBy = "Current User",
        CancellationToken cancellationToken = default);

    /// <summary>Ends every running ASCII game owned by one Council run while leaving unrelated games untouched.</summary>
    /// <param name="councilRunId">Identifier of the Council run whose owned game sessions should end.</param>
    /// <param name="endedBy">Bounded actor label recorded as the last game-session action owner.</param>
    /// <returns>The number of running game sessions that were ended.</returns>
    int EndByCouncilRun(Guid councilRunId, string endedBy = "Council lifecycle");

    /// <summary>
    /// Performs list as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="includeCompleted">Value indicating whether include completed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<CouncilGameSessionSnapshot>> ListAsync(
        bool includeCompleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Previews control as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game director decision produced by the operation.</returns>
    Task<CouncilGameDirectorDecision> PreviewControlAsync(
        CouncilGameControlRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies control as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    Task<CouncilGameSessionSnapshot> ApplyControlAsync(
        CouncilGameControlRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs submit frame as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    Task<CouncilGameSessionSnapshot> SubmitFrameAsync(
        SubmitCouncilGameFrameRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the shared ASCII palette contract and compact model-facing authoring guidance.</summary>
    CouncilAsciiPaletteSnapshot GetAsciiPalette();

    /// <summary>Reads the full or cropped authoritative ASCII display.</summary>
    Task<CouncilGameDisplaySnapshot> GetDisplayAsync(ReadCouncilGameDisplayRequest request, CancellationToken cancellationToken = default);

    /// <summary>Writes text into the authoritative ASCII display for the renderer that owns the turn.</summary>
    Task<CouncilGameSessionSnapshot> WriteTextAsync(WriteCouncilGameTextRequest request, CancellationToken cancellationToken = default);

    /// <summary>Writes one cell into the authoritative ASCII display for the renderer that owns the turn.</summary>
    Task<CouncilGameSessionSnapshot> SetCellAsync(SetCouncilGameCellRequest request, CancellationToken cancellationToken = default);

    /// <summary>Fills a clipped rectangular region of the authoritative ASCII display.</summary>
    Task<CouncilGameSessionSnapshot> FillRegionAsync(FillCouncilGameRegionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Blits multiline ASCII content into the authoritative display.</summary>
    Task<CouncilGameSessionSnapshot> BlitRegionAsync(BlitCouncilGameRegionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Submits pregenerated full-screen frames for local client-side animation.</summary>
    Task<CouncilGameSessionSnapshot> SubmitAnimationAsync(SubmitCouncilGameAnimationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Initializes or advances exactly one deterministic Kernel Creature Tournament exchange.</summary>
    Task<CouncilKernelTournamentResolution> AdvanceKernelTournamentAsync(
        CouncilKernelTournamentAdvanceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets input gate as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    Task<CouncilGameSessionSnapshot> SetInputGateAsync(
        SetCouncilGameInputGateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets control mode as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="mode">Mode value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="autoplayEnabled">Value indicating whether autoplay enabled should apply to this operation.</param>
    /// <param name="autoplayDelayMilliseconds">Autoplay delay milliseconds value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council game session snapshot produced by the operation.</returns>
    Task<CouncilGameSessionSnapshot> SetControlModeAsync(
        Guid sessionId,
        CouncilGameControlMode mode,
        bool autoplayEnabled,
        int autoplayDelayMilliseconds = 1200,
        CancellationToken cancellationToken = default);
}

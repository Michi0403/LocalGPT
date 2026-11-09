using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Builds council-driven game steps from Council workflow answers: entrants from identity steps, the asset check,
/// presentation frames and exchange evidence, as declared by the game's <c>council.lineup</c> rule row.
/// </summary>
public interface IGameCouncilLineupService
{
    /// <summary>
    /// Reads the lineup settings of a ruleset.
    /// </summary>
    /// <param name="ruleset">The game ruleset.</param>
    /// <returns>The settings, or <c>null</c> when the game declares no Council lineup.</returns>
    GameCouncilLineupSettings? ReadSettings(GameRuleset ruleset);

    /// <summary>
    /// Builds the engine request of one Council system step.
    /// </summary>
    /// <param name="game">The active game session.</param>
    /// <param name="steps">The Council run's steps so far.</param>
    /// <param name="pairs">The owner/member model pairs of the run.</param>
    /// <param name="initializeOnly">Whether this step initializes the lineup.</param>
    /// <param name="drawAsset">Asks one member for an asset drawing; <c>null</c> skips the asset check.</param>
    /// <param name="cancellationToken">Cancels the step.</param>
    /// <returns>The engine request.</returns>
    Task<GameCouncilAdvanceRequest> BuildAsync(
        CouncilGameSessionSnapshot game,
        IReadOnlyList<MultiModelCouncilStep> steps,
        IReadOnlyList<(string Owner, string Member)> pairs,
        bool initializeOnly,
        Func<GameAssetDraftRequest, CancellationToken, Task<GameAssetDraft?>>? drawAsset,
        CancellationToken cancellationToken = default);
}

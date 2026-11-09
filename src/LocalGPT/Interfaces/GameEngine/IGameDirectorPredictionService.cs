using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Produces the advisory subdirector predictions the GameDirector attaches to a reviewed proposal. Every prediction is
/// one <c>director.prediction</c> rule row of the running game; predictions never change session state.
/// </summary>
public interface IGameDirectorPredictionService
{
    /// <summary>Builds the predictions declared by the session's ruleset for the proposed action.</summary>
    /// <param name="context">Immutable session snapshot and the normalized proposal.</param>
    /// <returns>The predictions in rule order; empty when the game declares none.</returns>
    IReadOnlyList<CouncilGameSubdirectorPrediction> Predict(CouncilGameDirectorContext context);
}

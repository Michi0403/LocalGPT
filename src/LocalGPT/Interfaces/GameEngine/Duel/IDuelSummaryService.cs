using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine.Duel;

/// <summary>
/// Builds the Markdown transcript summary of a duel step from the ruleset's summary texts.
/// </summary>
public interface IDuelSummaryService
{
    /// <summary>
    /// Builds the summary.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="consequence">The consequence line of the step.</param>
    /// <param name="initialized">Whether the step only initialized the bracket.</param>
    /// <returns>The Markdown summary.</returns>
    string Build(GameEngineContext context, DuelSettings settings, string consequence, bool initialized);
}

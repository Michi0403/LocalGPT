using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Duel;

/// <summary>
/// View <c>duel-stage</c>: the resting frame of council-driven duel games between engine steps. The step frames
/// themselves are produced by the duel progression; this view shows the centred title and the waiting state.
/// </summary>
/// <param name="texts">Reads the waiting texts.</param>
/// <param name="textUtility">Fits rows.</param>
/// <param name="logger">Receives view diagnostics.</param>
public sealed class DuelStageView(
    IGameTextService texts,
    IGameTextUtilityService textUtility,
    ILogger<DuelStageView> logger) : IGameViewPrimitive
{
    /// <inheritdoc />
    public string Key => "duel-stage";

    /// <inheritdoc />
    public GameViewFrame Render(GameEngineContext context)
    {
        try
        {
            var session = context.Session;
            var width = session.FrameWidth;
            var height = session.FrameHeight;
            var lines = Enumerable.Range(0, height).Select(_ => new string(' ', width)).ToArray();
            var title = texts.Get(context.Ruleset, "view.waiting.title");
            var subtitle = texts.Get(context.Ruleset, session.Progression.Initialized ? "view.waiting.ready" : "view.waiting.preparing");
            var center = Math.Max(0, (width - title.Length) / 2);
            var row = Math.Max(1, height / 2 - 2);
            lines[row] = textUtility.Fit(new string('=', center) + title + new string('=', Math.Max(0, width - center - title.Length)), width);
            lines[Math.Min(height - 1, row + 2)] = textUtility.Fit(subtitle.PadLeft(Math.Max(subtitle.Length, (width + subtitle.Length) / 2)), width);
            return new GameViewFrame { Text = string.Join(Environment.NewLine, lines) };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rendering the duel stage of session {SessionId} failed.", context.Session.Id);
            throw;
        }
    }
}

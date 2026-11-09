using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Produces and normalizes the authoritative terminal frame of a shared game session.</summary>
public sealed partial class CouncilGameSessionService
{
    /// <summary>Renderer name of frames produced by the generic game engine.</summary>
    private const string PreviewRendererName = "LocalGPT deterministic preview renderer";

    /// <summary>Renderer name of frames produced by the curated Game Studio scene engine.</summary>
    private const string SceneRendererName = "LocalGPT curated scene engine";

    /// <summary>
    /// Renders the current authoritative frame: authored scenes through the scene engine, every other game through the view
    /// primitive its ruleset names. Style runs a view returns are kept; otherwise the snapshot derives semantic runs.
    /// </summary>
    /// <param name="session">Session whose frame text and frame style runs are replaced.</param>
    private void RenderAuthoritativeFrame(CouncilGameSessionState session)
    {
        try
        {
            session.FrameStyleRuns.Clear();
            if (session.Scene is not null && session.SceneClock is not null)
            {
                session.FrameText = gameSceneEngine.Preview(session.Scene, session.SceneClock).Text;
                return;
            }

            var frame = engine.Render(session);
            session.FrameText = frame.Text;
            if (frame.StyleRuns is { Count: > 0 } runs)
                session.FrameStyleRuns = NormalizeAsciiStyleRuns(runs, session.FrameWidth, session.FrameHeight, CouncilAsciiColorMode.TerminalDefault);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rendering the authoritative frame of game session {GameSessionId} failed.", session.Id);
            throw;
        }
    }

    /// <summary>Normalizes frame text to exactly <paramref name="height"/> lines of exactly <paramref name="width"/> characters.</summary>
    /// <param name="frame">Frame text with any line endings.</param>
    /// <param name="width">Terminal width in characters.</param>
    /// <param name="height">Terminal height in lines.</param>
    /// <returns>The normalized frame joined with the platform line separator.</returns>
    private string NormalizeFrame(string frame, int width, int height)
    {
        try
        {
            var sourceLines = (frame ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n');
            var result = new List<string>(height);
            for (var row = 0; row < height; row++)
                result.Add(textUtility.Fit(row < sourceLines.Length ? sourceLines[row] : string.Empty, width));
            return string.Join(Environment.NewLine, result);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing a game frame failed.");
            throw;
        }
    }
}

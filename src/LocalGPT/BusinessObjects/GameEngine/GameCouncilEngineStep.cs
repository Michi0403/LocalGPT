namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// Transcript-facing result of one <c>SystemGameEngineResolution</c> Council step: who the engine is (from the game's
/// texts), what it reports, and whether the game is complete or was already ended.
/// </summary>
public sealed class GameCouncilEngineStep
{
    /// <summary>Gets or sets the display name the engine step is recorded under (text <c>council.engine-name</c>).</summary>
    /// <value>The engine member name of the transcript step.</value>
    public string EngineName { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider model name recorded for the engine step (text <c>council.engine-model</c>).</summary>
    /// <value>The in-process engine model label.</value>
    public string EngineModel { get; set; } = string.Empty;

    /// <summary>Gets or sets the Markdown summary written to the Council transcript.</summary>
    /// <value>The engine summary.</value>
    public string SummaryMarkdown { get; set; } = string.Empty;

    /// <summary>Gets or sets the progress line shown while the Council run continues.</summary>
    /// <value>The progress message; empty when nothing is reported.</value>
    public string ProgressMessage { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the game reached its final result in this step.</summary>
    /// <value><c>true</c> when the game is complete.</value>
    public bool Completed { get; set; }

    /// <summary>Gets or sets whether the game had already been ended, so the step only reported that without changing it.</summary>
    /// <value><c>true</c> when the run's game session was no longer running.</value>
    public bool GameEnded { get; set; }
}

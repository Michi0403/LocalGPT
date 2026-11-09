namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// Asks a council-driven game session to initialize its entrants or to resolve one authoritative exchange from Council evidence.
/// </summary>
public sealed class GameCouncilAdvanceRequest
{
    /// <summary>
    /// Gets or sets the game session to advance.
    /// </summary>
    /// <value>The session identifier.</value>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Gets or sets whether this step only initializes the entrants and the opening presentation.
    /// </summary>
    /// <value><c>true</c> for the lineup step; <c>false</c> for an exchange step.</value>
    public bool InitializeOnly { get; set; }

    /// <summary>
    /// Gets or sets the entrants assembled from the Council's identity steps.
    /// </summary>
    /// <value>The entrant seeds in pairing order.</value>
    public IReadOnlyList<GameEntrantSeed> Entrants { get; set; } = [];

    /// <summary>
    /// Gets or sets complete ASCII frames written by a presentation role for the opening.
    /// </summary>
    /// <value>Presentation frames shown before the engine frames.</value>
    public IReadOnlyList<string> PresentationFrames { get; set; } = [];

    /// <summary>
    /// Gets or sets the command evidence of the current exchange.
    /// </summary>
    /// <value>The Council answers mapped to entrant sides.</value>
    public IReadOnlyList<GameCouncilEvidence> Evidence { get; set; } = [];
}

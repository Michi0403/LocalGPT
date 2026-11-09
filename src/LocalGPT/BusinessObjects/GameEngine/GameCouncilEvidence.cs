namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// One Council answer handed to a council-driven game as command evidence for an entrant side.
/// </summary>
public sealed class GameCouncilEvidence
{
    /// <summary>
    /// Gets or sets the entrant side the evidence speaks for: <c>owner</c> or <c>member</c>.
    /// </summary>
    /// <value>The evidence side declared by the game's council rule.</value>
    public string Side { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Council model whose answer this is.
    /// </summary>
    /// <value>The answering model selection name.</value>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plain answer text.
    /// </summary>
    /// <value>The decoded answer content.</value>
    public string Content { get; set; } = string.Empty;
}

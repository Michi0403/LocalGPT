namespace LocalGPT.BusinessObjects.GameEngine.Duel;

/// <summary>
/// A tagged value read from one side's Council evidence and exposed as a storyboard token.
/// </summary>
public sealed class DuelEvidenceTag
{
    /// <summary>Gets or sets the token name, prefixed with <c>left.</c> or <c>right.</c> in frames.</summary>
    /// <value>The token name.</value>
    public string Token { get; set; } = string.Empty;

    /// <summary>Gets or sets the evidence side, <c>owner</c> or <c>member</c>.</summary>
    /// <value>The evidence side.</value>
    public string Side { get; set; } = string.Empty;

    /// <summary>Gets or sets the line tag, for example <c>VOICE</c>.</summary>
    /// <value>The tag.</value>
    public string Tag { get; set; } = string.Empty;
}

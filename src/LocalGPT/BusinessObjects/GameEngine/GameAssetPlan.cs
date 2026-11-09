namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// Which workflow steps supply the drawers of a Council-driven game's asset check: the entrant's own member answers in
/// the role of <see cref="SelfStepKey"/>, later attempts go to the members of the role of <see cref="ArtistStepKey"/>.
/// </summary>
public sealed class GameAssetPlan
{
    /// <summary>Gets or sets the workflow-step key whose role the entrant's member draws in.</summary>
    /// <value>The self-drawing step key.</value>
    public string SelfStepKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the workflow-step key whose role members draw for entrants without a usable asset.</summary>
    /// <value>The artist step key.</value>
    public string ArtistStepKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the workflow-step key recorded on drawing turns.</summary>
    /// <value>The asset-check step key.</value>
    public string AssetStepKey { get; set; } = string.Empty;
}

using System.Text.Json;

namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// How a council-driven game maps Council roles and workflow steps to entrants, evidence, presentation frames and the asset check.
/// Read from the game's rule row with the trigger <c>council.lineup</c>.
/// </summary>
public sealed class GameCouncilLineupSettings
{
    /// <summary>Gets or sets the Council role of entrant owners.</summary>
    /// <value>The owner role name.</value>
    public string OwnerRole { get; set; } = string.Empty;

    /// <summary>Gets or sets the Council role of team members.</summary>
    /// <value>The member role name.</value>
    public string MemberRole { get; set; } = string.Empty;

    /// <summary>Gets or sets the workflow step whose answer a member draws its asset in.</summary>
    /// <value>The self-drawing step key.</value>
    public string AssetSelfStep { get; set; } = string.Empty;

    /// <summary>Gets or sets the workflow step whose role provides artists for missing assets.</summary>
    /// <value>The artist step key.</value>
    public string AssetArtistStep { get; set; } = string.Empty;

    /// <summary>Gets or sets the workflow step key recorded for asset drawing turns.</summary>
    /// <value>The asset step key.</value>
    public string AssetStepKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the similarity at which a drawing counts as a copy.</summary>
    /// <value>A similarity in (0, 1].</value>
    public double AssetCopySimilarity { get; set; } = 0.85d;

    /// <summary>Gets or sets the maximum drawing attempts per entrant.</summary>
    /// <value>The attempt budget.</value>
    public int AssetMaximumDrawAttempts { get; set; }

    /// <summary>Gets or sets the raw lineup parameters.</summary>
    /// <value>The rule parameters.</value>
    public JsonElement Parameters { get; set; }
}

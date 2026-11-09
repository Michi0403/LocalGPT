namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// Asks one Council member for an asset drawing of a game entrant during the game's asset check.
/// </summary>
/// <param name="MemberModelName">Member that owns the asset.</param>
/// <param name="OwnerModelName">Owner paired with the member.</param>
/// <param name="Texts">Entrant texts available to the drawing prompt.</param>
/// <param name="OtherAssets">Assets of the other entrants, which the drawing must not copy.</param>
/// <param name="Attempt">0 for the member itself, 1 and later for the configured artist members in order.</param>
public sealed record GameAssetDraftRequest(
    string MemberModelName,
    string OwnerModelName,
    IReadOnlyDictionary<string, string> Texts,
    string OtherAssets,
    int Attempt);

namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// Result of one asset drawing turn.
/// </summary>
/// <param name="DrawerModelName">Member that drew.</param>
/// <param name="Content">Visible answer of the drawing turn; empty when the turn failed.</param>
public sealed record GameAssetDraft(
    string DrawerModelName,
    string Content);

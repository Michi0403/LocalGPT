namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// How an action moves the camera of an authored Game Studio scene, in quarter turns of the four-way scene heading.
/// </summary>
/// <param name="HeadingQuarterTurns">Quarter turns added to the heading (counter-clockwise); 0 keeps it.</param>
/// <param name="MoveQuarterOffset">Quarter offset from the heading to move along, or <c>null</c> when the action does not move.</param>
public sealed record GameSceneControlStep(int HeadingQuarterTurns, int? MoveQuarterOffset);

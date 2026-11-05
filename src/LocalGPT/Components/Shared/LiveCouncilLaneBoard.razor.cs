namespace LocalGPT.Components.Shared;

/// <summary>
/// Renders the graphical participant lanes of a server-owned Council inside its chat message: one card per member with a state dot
/// (streaming, waiting, finished, closed), phase/role/route, status, an expandable stream panel and access to the full answer evidence.
/// The board refreshes itself at most once per second and renders only expanded streams, off the renderer dispatcher.
/// </summary>
public partial class LiveCouncilLaneBoard
{
}

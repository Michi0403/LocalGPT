using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Builds and poses ASCII actor rigs from the rig parameters of archetype rows.
/// </summary>
public interface IGameRigService
{
    /// <summary>
    /// Builds a deterministic rig for an actor.
    /// </summary>
    /// <param name="archetype">The archetype row carrying the rig parameters.</param>
    /// <param name="identity">The identity hashed into the rig.</param>
    /// <param name="descriptor">Free text matched against the rig's keyword glyph rules.</param>
    /// <returns>The rig.</returns>
    CouncilAsciiActorRig Build(GameProjectArchetype archetype, string identity, string descriptor);

    /// <summary>
    /// Renders a posed rig.
    /// </summary>
    /// <param name="archetype">The archetype row carrying links, markers and poses.</param>
    /// <param name="rig">The rig.</param>
    /// <param name="pose">The pose key.</param>
    /// <param name="facingRight">Whether the actor faces right.</param>
    /// <returns>The rig rows.</returns>
    List<string> Render(GameProjectArchetype archetype, CouncilAsciiActorRig rig, string pose, bool facingRight);
}

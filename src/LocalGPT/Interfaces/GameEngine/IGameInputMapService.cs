using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Turns the keyboard and gamepad labels of a game's input bindings ("W / ArrowUp", "Right trigger", "D-pad Left")
/// into the physical inputs the browser controller listens to, so no game action is named in client code.
/// </summary>
public interface IGameInputMapService
{
    /// <summary>Builds the physical input map of the bindings.</summary>
    /// <param name="bindings">Input bindings of the running game.</param>
    /// <returns>One mapping per binding that names at least one recognized input.</returns>
    IReadOnlyList<GameInputMapping> Build(IReadOnlyList<RuntimeInputBindingDefinition> bindings);

    /// <summary>Builds a stable signature of a session's bindings so the browser map is refreshed only when it changes.</summary>
    /// <param name="sessionId">Identifier of the game session; empty when no session is shown.</param>
    /// <param name="bindings">Input bindings of the running game.</param>
    /// <returns>The signature; empty when no session is shown.</returns>
    string Signature(Guid sessionId, IReadOnlyList<RuntimeInputBindingDefinition> bindings);
}

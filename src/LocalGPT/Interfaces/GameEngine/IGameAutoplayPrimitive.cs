using LocalGPT.BusinessObjects;
using System.Text.Json;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// An autoplay primitive: chooses the next legal action for the AI player from a snapshot.
/// </summary>
public interface IGameAutoplayPrimitive
{
    /// <summary>
    /// Gets the primitive key referenced by <c>AutoplayKey</c>.
    /// </summary>
    /// <value>The stable primitive key.</value>
    string Key { get; }

    /// <summary>
    /// Chooses the next action.
    /// </summary>
    /// <param name="snapshot">The session snapshot including its ruleset.</param>
    /// <param name="autoplayParameters">The autoplay parameters of the ruleset.</param>
    /// <returns>A legal action key.</returns>
    string SelectAction(CouncilGameSessionSnapshot snapshot, JsonElement autoplayParameters);
}

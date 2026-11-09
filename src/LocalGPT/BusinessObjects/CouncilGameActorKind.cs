using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Identifies the runtime actor that proposed an action.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilGameActorKind
{
    /// <summary>
    /// Selects the player option for <see cref="CouncilGameActorKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Player,
    /// <summary>
    /// Selects the creature option for <see cref="CouncilGameActorKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Creature,
    /// <summary>
    /// Selects the reactive object option for <see cref="CouncilGameActorKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ReactiveObject,
    /// <summary>
    /// Selects the director option for <see cref="CouncilGameActorKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Director
}

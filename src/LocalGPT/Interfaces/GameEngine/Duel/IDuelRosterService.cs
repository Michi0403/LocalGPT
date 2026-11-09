using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine.Duel;

/// <summary>
/// Owns the entrant owners and their team members: creation, active member, switching, bench rest and team health.
/// </summary>
public interface IDuelRosterService
{
    /// <summary>
    /// Creates the entrants from Council seeds and starts the bracket state.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="entrants">The Council entrant seeds.</param>
    void Initialize(GameEngineContext context, DuelSettings settings, IReadOnlyList<GameEntrantSeed> entrants);

    /// <summary>
    /// Lists the entrant owners in entrant order.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="settings">The duel settings.</param>
    /// <returns>The owners.</returns>
    List<GameActorState> Owners(CouncilGameSessionState session, DuelSettings settings);

    /// <summary>
    /// Finds an actor by key.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="key">The actor key.</param>
    /// <returns>The actor, or <c>null</c>.</returns>
    GameActorState? Find(CouncilGameSessionState session, string? key);

    /// <summary>
    /// Lists an owner's team members in slot order.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="owner">The owner.</param>
    /// <returns>The members.</returns>
    List<GameActorState> Members(CouncilGameSessionState session, GameActorState owner);

    /// <summary>
    /// Finds an owner's active member.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="owner">The owner.</param>
    /// <returns>The active member, or <c>null</c> for an empty team.</returns>
    GameActorState? ActiveMember(CouncilGameSessionState session, GameActorState owner);

    /// <summary>
    /// Reads a member's health.
    /// </summary>
    /// <param name="settings">The duel settings.</param>
    /// <param name="member">The member.</param>
    /// <returns>The health.</returns>
    int Health(DuelSettings settings, GameActorState? member);

    /// <summary>
    /// Sums the non-negative health of an owner's team.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="owner">The owner.</param>
    /// <returns>The team health.</returns>
    int TeamHealth(CouncilGameSessionState session, DuelSettings settings, GameActorState owner);

    /// <summary>
    /// Tests whether an owner still has a member with health.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="owner">The owner.</param>
    /// <returns><c>true</c> when a member can still fight.</returns>
    bool HasHealthyMember(CouncilGameSessionState session, DuelSettings settings, GameActorState owner);

    /// <summary>
    /// Re-synchronizes the active flags of an owner's team with its active member.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="owner">The owner.</param>
    void Sync(CouncilGameSessionState session, GameActorState owner);

    /// <summary>
    /// Switches to a healthy benched member, preferably the requested one.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="requestedTarget">A name or key fragment of the requested member.</param>
    /// <param name="countAgainstLimit">Whether the switch uses the owner's switch budget.</param>
    /// <returns><c>true</c> when a switch happened.</returns>
    bool Switch(CouncilGameSessionState session, DuelSettings settings, GameActorState owner, string requestedTarget, bool countAgainstLimit);

    /// <summary>
    /// Lets benched members with health rest and recover.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="owner">The owner.</param>
    void RestBench(CouncilGameSessionState session, DuelSettings settings, GameActorState owner);
}

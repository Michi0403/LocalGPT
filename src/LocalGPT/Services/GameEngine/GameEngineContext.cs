using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine;

/// <summary>
/// The per-call context handed to engine primitives: the session being interpreted and the engine that runs nested triggers.
/// </summary>
/// <param name="session">The session being interpreted.</param>
/// <param name="engine">The engine, so primitives can run nested triggers or regenerate levels.</param>
public sealed class GameEngineContext(CouncilGameSessionState session, IGameEngineRuntimeService engine)
{
    /// <summary>
    /// Gets the session being interpreted.
    /// </summary>
    /// <value>The mutable session state; callers hold its sync root.</value>
    public CouncilGameSessionState Session { get; } = session;

    /// <summary>
    /// Gets the ruleset of the session.
    /// </summary>
    /// <value>The declarative ruleset.</value>
    public GameRuleset Ruleset => Session.Ruleset;

    /// <summary>
    /// Gets the engine that runs nested triggers.
    /// </summary>
    /// <value>The generic engine.</value>
    public IGameEngineRuntimeService Engine { get; } = engine;
}

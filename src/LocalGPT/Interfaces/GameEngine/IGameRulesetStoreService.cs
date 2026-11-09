using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Persistence of game rulesets in the project system: composes rulesets from a profile and its template,
/// resolves launchable games by key or alias, and replaces authored rule rows.
/// </summary>
public interface IGameRulesetStoreService
{
    /// <summary>
    /// Composes the ruleset of a game project profile; empty profile keys and empty row tables inherit the template game.
    /// </summary>
    /// <param name="profileId">The profile identifier.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The composed ruleset.</returns>
    Task<GameRuleset> LoadRulesetAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a launchable game definition by game key or game alias without persisting a build.
    /// </summary>
    /// <param name="gameKeyOrAlias">The game key or alias.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The in-memory definition, or <c>null</c> when no game matches.</returns>
    Task<ProjectGameDefinition?> ResolveDefinitionAsync(string gameKeyOrAlias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the launchable games.
    /// </summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The catalog in launcher order.</returns>
    Task<IReadOnlyList<GameCatalogEntry>> ListCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the game a Council team plays: by default team, or by a runtime-class prefix alias of the game.
    /// </summary>
    /// <param name="teamKey">The team key.</param>
    /// <param name="runtimeClassKeys">The runtime classes assigned to the team's roles.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The catalog entry, or <c>null</c> when the team plays no game.</returns>
    Task<GameCatalogEntry?> FindForTeamAsync(string teamKey, IReadOnlyList<string> runtimeClassKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the rule rows of a game project and marks seeded games as user-modified.
    /// </summary>
    /// <param name="projectId">The game project.</param>
    /// <param name="request">The rows.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The composed ruleset after the write.</returns>
    Task<GameRuleset> SaveRulesetAsync(Guid projectId, SaveGameRulesetRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the own rule rows of a game project without template inheritance.
    /// </summary>
    /// <param name="projectId">The game project.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The own rows.</returns>
    Task<SaveGameRulesetRequest> GetOwnRowsAsync(Guid projectId, CancellationToken cancellationToken = default);
}

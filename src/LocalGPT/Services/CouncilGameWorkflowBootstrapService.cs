using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services;

/// <summary>
/// Ensures that a Council team which explicitly opts into the required ASCII surface and a known
/// game runtime-class family owns one authoritative game session before model workflow execution starts.
/// Persisted team/runtime-class configuration remains the source of truth for game behavior and difficulty.
/// </summary>
/// <param name="teams">Council team configuration service dependency used by the council game workflow bootstrap workflow to provide the corresponding application capability.</param>
/// <param name="games">Council game session service dependency used by the council game workflow bootstrap workflow to provide the corresponding application capability.</param>
/// <param name="gameCatalog">Game catalog that maps a team's declared runtime-class families to the game project that owns them.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CouncilGameWorkflowBootstrapService(
    ICouncilTeamConfigurationService teams,
    ICouncilGameSessionService games,
    IGameRulesetStoreService gameCatalog,
    ILogger<CouncilGameWorkflowBootstrapService> logger)
{
    /// <summary>Ensures the configured Council run owns its required game session, when the selected team declares one.</summary>
    /// <param name="request">Prepared Council request whose run identifier owns the game.</param>
    /// <param name="cancellationToken">Cancels the configuration lookup or game bootstrap.</param>
    /// <returns>The existing or newly-created game snapshot, or <c>null</c> when the selected team is not an ASCII-game team.</returns>
    public async Task<CouncilGameSessionSnapshot?> EnsureSessionAsync(
        MultiModelCouncilRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            var team = await teams.FindTeamAsync(request.CouncilTeamKey, cancellationToken).ConfigureAwait(false);
            if (team is null || !team.PreferredCapabilities.Contains(AsciiChatTextService.RequiredSurfaceCapability, StringComparer.OrdinalIgnoreCase))
                return null;

            var runtimeClassKeys = team.Roles
                .SelectMany(role => role.RuntimeClassKeys)
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (runtimeClassKeys.Length == 0)
            {
                logger.LogDebug(
                    "Council team {TeamKey} requires the shared ASCII presentation surface without declaring a game runtime-class family; run {RunId} will use transcript-owned ASCII presentation only.",
                    team.Key,
                    request.RunId);
                return null;
            }

            var game = await gameCatalog.FindForTeamAsync(team.Key, runtimeClassKeys, cancellationToken).ConfigureAwait(false);
            if (game is null)
            {
                logger.LogWarning(
                    "Council team {TeamKey} requires the ASCII surface and declares runtime classes, but none belong to a supported deterministic game family; no game bootstrap was performed for run {RunId}.",
                    team.Key,
                    request.RunId);
                return null;
            }

            var existing = await games.GetActiveForCouncilRunAsync(request.RunId, game.GameKey, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
                return existing;

            var snapshot = await games.StartAsync(new StartCouncilGameRequest
            {
                GameKey = game.GameKey,
                TeamKey = team.Key,
                CouncilRunId = request.RunId,
                ConversationId = request.ContinueConversationId,
                ControlMode = CouncilGameControlMode.Shared,
                AutoplayEnabled = false,
                FrameWidth = game.FrameWidth,
                FrameHeight = game.FrameHeight,
                StartedBy = "LocalGPT Council workflow bootstrap"
            }, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Council workflow prebootstrapped game {GameKey} session {SessionId} for team {TeamKey} and run {RunId}.",
                snapshot.GameKey,
                snapshot.Id,
                team.Key,
                request.RunId);
            return snapshot;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Council game workflow bootstrap was canceled for run {RunId}.", request.RunId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Council game workflow bootstrap failed for run {RunId} and team {TeamKey}.", request.RunId, request.CouncilTeamKey);
            throw;
        }
    }
}

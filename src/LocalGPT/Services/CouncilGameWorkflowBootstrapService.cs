using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Ensures that a Council team which explicitly opts into the required ASCII surface and a known
/// game runtime-class family owns one authoritative game session before model workflow execution starts.
/// Persisted team/runtime-class configuration remains the source of truth for game behavior and difficulty.
/// </summary>
public sealed class CouncilGameWorkflowBootstrapService(
    ICouncilTeamConfigurationService teams,
    ICouncilGameSessionService games,
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

            var existing = await games.GetActiveForCouncilRunAsync(request.RunId, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
                return existing;

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

            var gameKey = ResolveGameKey(runtimeClassKeys);
            if (string.IsNullOrWhiteSpace(gameKey))
            {
                logger.LogWarning(
                    "Council team {TeamKey} requires the ASCII surface and declares runtime classes, but none belong to a supported deterministic game family; no game bootstrap was performed for run {RunId}.",
                    team.Key,
                    request.RunId);
                return null;
            }

            var snapshot = await games.StartAsync(new StartCouncilGameRequest
            {
                GameKey = gameKey,
                TeamKey = team.Key,
                CouncilRunId = request.RunId,
                ConversationId = request.ContinueConversationId,
                ControlMode = CouncilGameControlMode.Shared,
                AutoplayEnabled = false,
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

    /// <summary>Maps declared runtime-class namespaces to the deterministic game runtime that owns those classes.</summary>
    private string? ResolveGameKey(IEnumerable<string> runtimeClassKeys)
    {
        try
        {
            var keys = runtimeClassKeys.ToArray();
            if (keys.Any(key => key.StartsWith("games.ascii.kernel-tournament.", StringComparison.OrdinalIgnoreCase)))
                return "kernel-creature-tournament";
            if (keys.Any(key => key.StartsWith("games.ascii.doom.", StringComparison.OrdinalIgnoreCase)))
                return "ascii-doom";
            if (keys.Any(key => key.StartsWith("games.green-dragon.", StringComparison.OrdinalIgnoreCase)))
                return "green-dragon";
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Resolving a Council game key from runtime-class configuration failed.");
            throw;
        }
    }
}

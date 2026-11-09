using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Council;

/// <summary>
/// Default <see cref="IGameCouncilBridgeService"/>: reads the run's Council-driven game, its lineup rule and its texts,
/// so the Council service never names a game, a role, a step key or a game text itself.
/// </summary>
/// <param name="gameSessions">Authoritative game sessions.</param>
/// <param name="engine">Generic game engine (turn-model checks).</param>
/// <param name="lineup">Builds engine turns from Council answers.</param>
/// <param name="texts">Reads the game's texts.</param>
/// <param name="actorQuery">Reads actor texts.</param>
/// <param name="logger">Receives bridge diagnostics.</param>
public sealed class GameCouncilBridgeService(
    ICouncilGameSessionService gameSessions,
    IGameEngineRuntimeService engine,
    IGameCouncilLineupService lineup,
    IGameTextService texts,
    IGameActorQueryService actorQuery,
    ILogger<GameCouncilBridgeService> logger) : IGameCouncilBridgeService
{
    /// <inheritdoc />
    public async Task<CouncilGameSessionSnapshot?> FindRunGameAsync(Guid councilRunId, bool includeEnded, CancellationToken cancellationToken = default)
    {
        try
        {
            if (councilRunId == Guid.Empty)
                return null;
            var sessions = await gameSessions.ListAsync(includeEnded, cancellationToken).ConfigureAwait(false);
            return sessions
                .Where(session => session.CouncilRunId == councilRunId && engine.IsCouncilDriven(session.Ruleset))
                .OrderByDescending(session => string.Equals(session.Status, "Running", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(session => session.UpdatedAtUtc)
                .FirstOrDefault();
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "Finding the Council-driven game of run {CouncilRunId} was cancelled.", councilRunId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Finding the Council-driven game of run {CouncilRunId} failed.", councilRunId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> LimitToActiveEntrantsAsync(Guid councilRunId, string roleName, IReadOnlyList<string> roleParticipants, CancellationToken cancellationToken = default)
    {
        try
        {
            if (roleParticipants.Count <= 2)
                return roleParticipants;
            var game = await FindRunGameAsync(councilRunId, includeEnded: false, cancellationToken).ConfigureAwait(false);
            if (game is null || !string.Equals(game.Status, "Running", StringComparison.OrdinalIgnoreCase) || game.ActiveEntrantKeys.Count != 2)
                return roleParticipants;
            var settings = lineup.ReadSettings(game.Ruleset);
            if (settings is null)
                return roleParticipants;

            var owners = game.Actors
                .Where(actor => game.ActiveEntrantKeys.Contains(actor.Key, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (owners.Count != 2)
                return roleParticipants;

            var ownerSide = string.Equals(roleName, settings.OwnerRole, StringComparison.OrdinalIgnoreCase);
            var desiredModels = owners.Select(owner => actorQuery.Text(owner, ownerSide ? "model" : "memberModel")).ToList();
            var identity = new ProviderModelIdentity();
            var selected = roleParticipants
                .Where(participant => desiredModels.Any(model => identity.AreEquivalentSelectionKeys(model, participant)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (selected.Count != desiredModels.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                return roleParticipants;

            logger.LogInformation(
                "Council-driven game {GameKey} round {Round} limited role {Role} from {ConfiguredCount} configured member(s) to the {ActiveCount} member(s) of the current pairing.",
                game.GameKey,
                Math.Max(1, game.Progression.Round),
                roleName,
                roleParticipants.Count,
                selected.Count);
            return selected;
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "Limiting role {Role} to the active game entrants was cancelled.", roleName);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Limiting role {Role} of run {CouncilRunId} to the active game entrants failed.", roleName, councilRunId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<GameCouncilEngineStep> ResolveAsync(
        Guid councilRunId,
        IReadOnlyList<MultiModelCouncilStep> steps,
        IReadOnlyList<(string Role, string Participant, string PairedRole, string PairedParticipant)> pairings,
        bool initializeOnly,
        Func<GameAssetDraftRequest, CancellationToken, Task<GameAssetDraft?>>? drawAsset,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var game = await FindRunGameAsync(councilRunId, includeEnded: true, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("This Council workflow step requires the Council-driven game session its run bootstraps; none exists for the run.");
            if (!string.Equals(game.Status, "Running", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Council-driven game {GameKey} of run {CouncilRunId} was ended before this engine step; the session is not changed.", game.GameKey, councilRunId);
                return new GameCouncilEngineStep
                {
                    EngineName = texts.Get(game.Ruleset, "council.engine-name"),
                    EngineModel = texts.Get(game.Ruleset, "council.engine-model"),
                    SummaryMarkdown = texts.Get(game.Ruleset, "council.ended-summary"),
                    ProgressMessage = texts.Get(game.Ruleset, "council.progress.ended"),
                    Completed = true,
                    GameEnded = true
                };
            }

            var settings = lineup.ReadSettings(game.Ruleset)
                ?? throw new InvalidOperationException($"Game '{game.GameKey}' declares no Council lineup rule.");
            var pairs = pairings
                .Where(pairing => string.Equals(pairing.Role, settings.OwnerRole, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(pairing.PairedRole, settings.MemberRole, StringComparison.OrdinalIgnoreCase))
                .Select(pairing => (Owner: pairing.Participant, Member: pairing.PairedParticipant))
                .ToList();
            var advance = await lineup.BuildAsync(game, steps, pairs, initializeOnly, initializeOnly ? drawAsset : null, cancellationToken).ConfigureAwait(false);
            var resolution = await gameSessions.AdvanceCouncilTurnAsync(advance, cancellationToken).ConfigureAwait(false);
            return new GameCouncilEngineStep
            {
                EngineName = texts.Get(game.Ruleset, "council.engine-name"),
                EngineModel = texts.Get(game.Ruleset, "council.engine-model"),
                SummaryMarkdown = resolution.SummaryMarkdown,
                ProgressMessage = texts.Get(game.Ruleset, initializeOnly ? "council.progress.initialize" : "council.progress.turn"),
                Completed = resolution.Completed
            };
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "Resolving the Council-driven game step of run {CouncilRunId} was cancelled.", councilRunId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the Council-driven game step of run {CouncilRunId} failed.", councilRunId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<GameAssetPlan?> PlanAssetAsync(Guid councilRunId, CancellationToken cancellationToken = default)
    {
        try
        {
            var game = await FindRunGameAsync(councilRunId, includeEnded: false, cancellationToken).ConfigureAwait(false);
            var settings = game is null ? null : lineup.ReadSettings(game.Ruleset);
            if (settings is null || settings.AssetMaximumDrawAttempts <= 0)
                return null;
            return new GameAssetPlan
            {
                SelfStepKey = settings.AssetSelfStep,
                ArtistStepKey = settings.AssetArtistStep,
                AssetStepKey = settings.AssetStepKey
            };
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "Planning the asset check of run {CouncilRunId} was cancelled.", councilRunId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Planning the asset check of run {CouncilRunId} failed.", councilRunId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<GameAssetPrompt> BuildAssetPromptAsync(Guid councilRunId, GameAssetDraftRequest request, string drawerModelName, bool drawsSelf, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var game = await FindRunGameAsync(councilRunId, includeEnded: false, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The asset check requires the run's Council-driven game session.");
            var ruleset = game.Ruleset;
            var settings = lineup.ReadSettings(ruleset);
            var nameField = settings is null ? string.Empty : ReadNameField(settings);
            var entrantName = nameField.Length > 0 ? request.Texts.GetValueOrDefault(nameField) ?? string.Empty : string.Empty;
            var phaseLabel = texts.Get(ruleset, "asset.phase");

            // Prompt placeholders use the Council's {{Name}} form so the prompt stays compatible with the role
            // instructions the Council fills in later; they are replaced literally, never evaluated.
            var drawerLine = (drawsSelf ? texts.Get(ruleset, "asset.drawer.self") : texts.Get(ruleset, "asset.drawer.artist"))
                .Replace("{{AssetMemberModel}}", request.MemberModelName, StringComparison.Ordinal);
            var prompt = texts.Get(ruleset, "asset.prompt");
            foreach (var (key, value) in request.Texts)
                prompt = prompt.Replace("{{Entrant." + key + "}}", value, StringComparison.OrdinalIgnoreCase);
            prompt = prompt
                .Replace("{{AssetMemberModel}}", request.MemberModelName, StringComparison.Ordinal)
                .Replace("{{AssetDrawer}}", drawerLine, StringComparison.Ordinal)
                .Replace(
                    "{{AssetOtherSprites}}",
                    string.IsNullOrWhiteSpace(request.OtherAssets) ? texts.Get(ruleset, "asset.no-other") : request.OtherAssets,
                    StringComparison.Ordinal);
            var progress = texts.Format(ruleset, "asset.progress", new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["phase"] = phaseLabel,
                ["drawer"] = drawerModelName,
                ["name"] = entrantName
            });
            return new GameAssetPrompt
            {
                PhaseLabel = phaseLabel,
                EntrantName = entrantName,
                Prompt = prompt,
                ProgressMessage = progress
            };
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "Building the asset prompt of run {CouncilRunId} was cancelled.", councilRunId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the asset prompt of run {CouncilRunId} failed.", councilRunId);
            throw;
        }
    }

    /// <summary>Reads the entrant text field that holds the entrant's display name.</summary>
    /// <param name="settings">Lineup settings of the game.</param>
    /// <returns>The name field key, or an empty string.</returns>
    private string ReadNameField(GameCouncilLineupSettings settings)
    {
        try
        {
            return settings.Parameters.ValueKind == System.Text.Json.JsonValueKind.Object
                && settings.Parameters.TryGetProperty("nameField", out var value)
                && value.ValueKind == System.Text.Json.JsonValueKind.String
                    ? value.GetString() ?? string.Empty
                    : string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the lineup name field failed.");
            throw;
        }
    }
}

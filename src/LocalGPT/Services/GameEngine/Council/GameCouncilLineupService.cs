using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Council;

/// <summary>
/// Builds council-driven game steps from Council workflow answers as declared by the game's <c>council.lineup</c> rule:
/// <c>fields</c> map tagged identity answers to entrant texts, <c>spriteSource</c> names the drawing answer,
/// <c>presentationStep</c> the fenced presentation frames and <c>evidence</c> the exchange command steps per side.
/// </summary>
/// <param name="parameters">Reads the lineup parameters.</param>
/// <param name="formatter">Formats fallback names.</param>
/// <param name="reader">Reads tagged answers.</param>
/// <param name="sprites">Reads drawn sprites.</param>
/// <param name="frames">Reads presentation frames.</param>
/// <param name="similarity">Detects copied sprites.</param>
/// <param name="logger">Receives lineup diagnostics.</param>
public sealed class GameCouncilLineupService(
    IGameParameterReader parameters,
    IGameTemplateFormatter formatter,
    IGameCouncilTextReader reader,
    IGameSpriteReader sprites,
    IGameAsciiFrameReader frames,
    IGameSpriteSimilarityService similarity,
    ILogger<GameCouncilLineupService> logger) : IGameCouncilLineupService
{
    /// <inheritdoc />
    public GameCouncilLineupSettings? ReadSettings(GameRuleset ruleset)
    {
        try
        {
            var rule = ruleset.Rules
                .Where(item => item.IsEnabled && string.Equals(item.Trigger, "council.lineup", StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.SortOrder)
                .FirstOrDefault();
            if (rule is null)
                return null;
            var values = parameters.Parse(rule.ParametersJson);
            var asset = parameters.ReadObject(values, "assetCheck");
            var copySimilarity = parameters.ReadDouble(asset, "copySimilarity", 0.85d);
            return new GameCouncilLineupSettings
            {
                OwnerRole = parameters.ReadString(values, "ownerRole", string.Empty),
                MemberRole = parameters.ReadString(values, "memberRole", string.Empty),
                AssetSelfStep = parameters.ReadString(asset, "selfStep", string.Empty),
                AssetArtistStep = parameters.ReadString(asset, "artistStep", string.Empty),
                AssetStepKey = parameters.ReadString(asset, "stepKey", string.Empty),
                AssetCopySimilarity = copySimilarity is > 0d and <= 1d ? copySimilarity : 0.85d,
                AssetMaximumDrawAttempts = Math.Clamp(parameters.ReadInt(asset, "maximumDrawAttempts", 0), 0, 8),
                Parameters = values
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the Council lineup settings failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<GameCouncilAdvanceRequest> BuildAsync(
        CouncilGameSessionSnapshot game,
        IReadOnlyList<MultiModelCouncilStep> steps,
        IReadOnlyList<(string Owner, string Member)> pairs,
        bool initializeOnly,
        Func<GameAssetDraftRequest, CancellationToken, Task<GameAssetDraft?>>? drawAsset,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = ReadSettings(game.Ruleset)
                ?? throw new InvalidOperationException($"Game '{game.GameKey}' declares no Council lineup rule.");
            var values = settings.Parameters;
            var spriteParameters = parameters.ReadObject(values, "sprite");
            var entrants = new List<GameEntrantSeed>();
            foreach (var (owner, member) in pairs.DistinctBy(pair => pair.Owner + "|" + pair.Member, StringComparer.OrdinalIgnoreCase))
            {
                var seed = new GameEntrantSeed { OwnerModelName = owner, MemberModelName = member };
                foreach (var field in parameters.ReadObjects(values, "fields"))
                {
                    var key = parameters.ReadString(field, "field", string.Empty);
                    if (key.Length == 0)
                        continue;
                    if (parameters.ReadBool(field, "onlyIfEmpty", false) && !string.IsNullOrWhiteSpace(seed.Texts.GetValueOrDefault(key)))
                        continue;
                    seed.Texts[key] = reader.ReadTag(AnswerOf(steps, field, owner, member), parameters.ReadString(field, "tag", string.Empty));
                }
                var nameField = parameters.ReadString(values, "nameField", string.Empty);
                if (nameField.Length > 0 && string.IsNullOrWhiteSpace(seed.Texts.GetValueOrDefault(nameField)))
                {
                    seed.Texts[nameField] = formatter.Format(parameters.ReadString(values, "nameFallbackTemplate", "{number}"),
                        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["number"] = entrants.Count + 1 });
                }
                var drawn = sprites.Read(AnswerOf(steps, parameters.ReadObject(values, "spriteSource"), owner, member), spriteParameters);
                seed.SpriteRows = drawn.Rows;
                seed.SpriteKind = drawn.Kind;
                seed.SpritePalette = drawn.Palette;
                entrants.Add(seed);
            }

            if (initializeOnly && drawAsset is not null && entrants.Count > 1)
                await CheckAssetsAsync(settings, entrants, spriteParameters, nameField: parameters.ReadString(values, "nameField", string.Empty), drawAsset, cancellationToken).ConfigureAwait(false);

            var presentationStep = parameters.ReadString(values, "presentationStep", string.Empty);
            var presentation = steps.LastOrDefault(step => presentationStep.Length > 0 && string.Equals(step.WorkflowStepKey, presentationStep, StringComparison.OrdinalIgnoreCase));
            var presentationFrames = initializeOnly && presentation is not null
                ? frames.Read(presentation.VisibleContent ?? presentation.Content ?? string.Empty,
                    parameters.ReadInt(values, "maximumPresentationFrames", 6),
                    parameters.ReadInt(values, "maximumPresentationLength", 12_000))
                : [];

            var evidenceSteps = parameters.ReadObjects(values, "evidence")
                .Select(item => (Step: parameters.ReadString(item, "step", string.Empty), Side: parameters.ReadString(item, "side", string.Empty)))
                .Where(item => item.Step.Length > 0)
                .ToList();
            var evidence = steps
                .Select(step => (Step: step, Mapping: evidenceSteps.FirstOrDefault(item => string.Equals(item.Step, step.WorkflowStepKey, StringComparison.OrdinalIgnoreCase))))
                .Where(item => item.Mapping.Step is { Length: > 0 })
                .Select(item => new GameCouncilEvidence
                {
                    Side = item.Mapping.Side,
                    ModelName = string.IsNullOrWhiteSpace(item.Step.RecoveryTargetModelName) ? item.Step.ModelName : item.Step.RecoveryTargetModelName,
                    Content = reader.PlainModelText(item.Step.VisibleContent ?? item.Step.Content ?? string.Empty)
                })
                .ToList();
            logger.LogDebug("Built Council game step for {GameKey}: {EntrantCount} entrant(s), {FrameCount} presentation frame(s), {EvidenceCount} evidence answer(s).",
                game.GameKey, entrants.Count, presentationFrames.Count, evidence.Count);
            return new GameCouncilAdvanceRequest
            {
                SessionId = game.Id,
                InitializeOnly = initializeOnly,
                Entrants = entrants,
                PresentationFrames = presentationFrames,
                Evidence = evidence
            };
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "Building the Council game step was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the Council game step of session {SessionId} failed.", game.Id);
            throw;
        }
    }

    /// <summary>
    /// Finds the latest answer of a pair side in a workflow step.
    /// </summary>
    /// <param name="steps">The Council steps.</param>
    /// <param name="source">The source declaration with <c>step</c> and <c>side</c>.</param>
    /// <param name="owner">The owner model.</param>
    /// <param name="member">The member model.</param>
    /// <returns>The answer text, or an empty string.</returns>
    private string AnswerOf(IReadOnlyList<MultiModelCouncilStep> steps, JsonElement source, string owner, string member)
    {
        try
        {
            var stepKey = parameters.ReadString(source, "step", string.Empty);
            var model = string.Equals(parameters.ReadString(source, "side", string.Empty), "owner", StringComparison.OrdinalIgnoreCase) ? owner : member;
            var step = steps.LastOrDefault(candidate =>
                string.Equals(candidate.WorkflowStepKey, stepKey, StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(candidate.ModelName, model, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(candidate.RecoveryTargetModelName, model, StringComparison.OrdinalIgnoreCase)));
            return step?.VisibleContent ?? step?.Content ?? string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Finding a Council lineup answer failed.");
            throw;
        }
    }

    /// <summary>
    /// Asks members, then artists, for new drawings when an entrant has no usable sprite or copies an earlier one.
    /// </summary>
    /// <param name="settings">The lineup settings.</param>
    /// <param name="entrants">The entrants, updated in place.</param>
    /// <param name="spriteParameters">The sprite parameters.</param>
    /// <param name="nameField">The entrant text naming the member.</param>
    /// <param name="drawAsset">Asks one member for a drawing.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>A task completing when the check finished.</returns>
    private async Task CheckAssetsAsync(
        GameCouncilLineupSettings settings,
        List<GameEntrantSeed> entrants,
        JsonElement spriteParameters,
        string nameField,
        Func<GameAssetDraftRequest, CancellationToken, Task<GameAssetDraft?>> drawAsset,
        CancellationToken cancellationToken)
    {
        try
        {
            for (var index = 0; index < entrants.Count && settings.AssetMaximumDrawAttempts > 0; index++)
            {
                var entrant = entrants[index];
                var current = index;
                var missing = entrant.SpriteRows.Count == 0;
                if (!missing && !CopiesAnother(entrants, current, entrant.SpriteRows, onlyEarlier: true, settings.AssetCopySimilarity))
                    continue;

                var name = entrant.Texts.GetValueOrDefault(nameField) ?? string.Empty;
                logger.LogInformation("Council game asset check: entrant {EntrantName} of {MemberModel} has {Problem}; asking for a new drawing.",
                    name, entrant.MemberModelName, missing ? "no usable sprite" : "a copy of another entrant's sprite");
                for (var attempt = 0; attempt < settings.AssetMaximumDrawAttempts; attempt++)
                {
                    var otherAssets = string.Join(
                        Environment.NewLine + Environment.NewLine,
                        entrants
                            .Where((other, otherIndex) => otherIndex != current && other.SpriteRows.Count > 0)
                            .Select(other => (other.Texts.GetValueOrDefault(nameField) ?? string.Empty) + ":" + Environment.NewLine + string.Join(Environment.NewLine, other.SpriteRows)));
                    var draft = await drawAsset(new GameAssetDraftRequest(entrant.MemberModelName, entrant.OwnerModelName, entrant.Texts, otherAssets, attempt), cancellationToken).ConfigureAwait(false);
                    if (draft is null)
                        break;

                    var drawn = sprites.Read(draft.Content, spriteParameters);
                    var accepted = drawn.Rows.Count > 0 && !CopiesAnother(entrants, current, drawn.Rows, onlyEarlier: false, settings.AssetCopySimilarity);
                    logger.LogInformation("Council game asset check: drawing {Attempt} for {EntrantName} by {Drawer} was {Outcome}.",
                        attempt + 1, name, draft.DrawerModelName,
                        accepted ? "accepted" : drawn.Rows.Count == 0 ? "not a usable sprite" : "too similar to another entrant's sprite");
                    if (!accepted)
                        continue;

                    entrant.SpriteRows = drawn.Rows;
                    entrant.SpriteKind = drawn.Kind;
                    entrant.SpritePalette = drawn.Palette;
                    entrant.SpriteAuthorModelName = string.Equals(draft.DrawerModelName, entrant.MemberModelName, StringComparison.OrdinalIgnoreCase)
                        ? string.Empty
                        : draft.DrawerModelName;
                    break;
                }
            }
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "The Council game asset check was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The Council game asset check failed.");
            throw;
        }
    }

    /// <summary>
    /// Tests whether rows copy another entrant's sprite.
    /// </summary>
    /// <param name="entrants">The entrants.</param>
    /// <param name="current">The index of the checked entrant.</param>
    /// <param name="rows">The rows to test.</param>
    /// <param name="onlyEarlier">Whether only earlier entrants count.</param>
    /// <param name="copySimilarity">The copy threshold.</param>
    /// <returns><c>true</c> when the rows copy another sprite.</returns>
    private bool CopiesAnother(IReadOnlyList<GameEntrantSeed> entrants, int current, IReadOnlyList<string> rows, bool onlyEarlier, double copySimilarity)
    {
        try
        {
            return entrants
                .Where((other, otherIndex) => otherIndex != current && (!onlyEarlier || otherIndex < current) && other.SpriteRows.Count > 0)
                .Any(other => similarity.Similarity(rows, other.SpriteRows) >= copySimilarity);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Comparing entrant sprites failed.");
            throw;
        }
    }
}

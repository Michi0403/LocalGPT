using LocalGPT.BusinessObjects;

namespace LocalGPT.Services
{
    /// <summary>Bridges bounded Council role output into the deterministic Kernel Creature Tournament game session.</summary>
    public sealed partial class MultiModelCouncilService
    {
        /// <summary>
        /// Narrows live battle turns to the two trainer/creature kernels that the deterministic engine says are in the current legal match.
        /// Opening/selection steps still use the complete configured role pool, while a damaged or incomplete match projection safely falls back to that pool.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="definition">Definition value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="roleParticipants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        private async Task<IReadOnlyList<string>> LimitKernelTournamentRoundParticipantsAsync(
            MultiModelCouncilResult result,
            OrganicCouncilTeamDefinition team,
            CouncilWorkflowStepDefinition definition,
            IReadOnlyList<string> roleParticipants,
            CancellationToken cancellationToken)
        {
            if (!string.Equals(team.Key, "kernel-creature-tournament", StringComparison.OrdinalIgnoreCase)
                || !(string.Equals(definition.Key, "trainer-round-command", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(definition.Key, "fight-round", StringComparison.OrdinalIgnoreCase))
                || roleParticipants.Count <= 2)
                return roleParticipants;

            try
            {
                var game = await gameSessions.GetActiveForCouncilRunAsync(result.RunId, "kernel-creature-tournament", cancellationToken).ConfigureAwait(false);
                if (game is null || game.TournamentCurrentFighterIds.Count != 2)
                    return roleParticipants;

                var activeFighters = game.TournamentFighters
                    .Where(fighter => game.TournamentCurrentFighterIds.Contains(fighter.FighterId, StringComparer.OrdinalIgnoreCase))
                    .ToList();
                if (activeFighters.Count != 2)
                    return roleParticipants;

                var desiredModels = string.Equals(definition.Role, "Creature Trainer", StringComparison.OrdinalIgnoreCase)
                    ? activeFighters.Select(fighter => fighter.TrainerModelName).ToList()
                    : activeFighters.Select(fighter => fighter.CreatureModelName).ToList();
                var identity = new ProviderModelIdentity();
                var selected = roleParticipants
                    .Where(participant => desiredModels.Any(model => identity.AreEquivalentSelectionKeys(model, participant)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (selected.Count != desiredModels.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                    return roleParticipants;

                logger.LogInformation(
                    "Kernel Creature Tournament round {Round} limited role {Role} from {ConfiguredCount} configured member(s) to the {ActiveCount} current legal-match member(s).",
                    Math.Max(1, game.TournamentRound),
                    definition.Role,
                    roleParticipants.Count,
                    selected.Count);
                return selected;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Kernel Creature Tournament active-pair optimization could not be resolved; the configured role pool will run unchanged.");
                return roleParticipants;
            }
        }

        /// <summary>
        /// Performs run kernel tournament system step as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="definition">Definition value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="rolePairings">Council participant pairing dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="round">Round value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="phase">Phase value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="initializeOnly">Value indicating whether initialize only should apply to this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The multi model council step produced by the operation.</returns>
        private async Task<MultiModelCouncilStep> RunKernelTournamentSystemStepAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            CouncilWorkflowStepDefinition definition,
            IReadOnlyList<CouncilParticipantPairing> rolePairings,
            IReadOnlyList<string> participants,
            int round,
            string phase,
            bool initializeOnly,
            CancellationToken cancellationToken)
        {
            try
            {
                var startedAtUtc = DateTime.UtcNow;
                var game = await gameSessions.GetActiveForCouncilRunAsync(result.RunId, "kernel-creature-tournament", cancellationToken).ConfigureAwait(false);
                if (game is null || !string.Equals(game.GameKey, "kernel-creature-tournament", StringComparison.OrdinalIgnoreCase))
                {
                    var endedGame = (await gameSessions.ListAsync(includeCompleted: true, cancellationToken: cancellationToken).ConfigureAwait(false))
                        .Where(candidate => candidate.CouncilRunId == result.RunId
                            && string.Equals(candidate.GameKey, "kernel-creature-tournament", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(candidate => candidate.UpdatedAtUtc)
                        .FirstOrDefault();
                    if (endedGame is not null && !string.Equals(endedGame.Status, "Running", StringComparison.OrdinalIgnoreCase))
                    {
                        var endedSummary = "### LocalGPT Tournament Engine\nThe tournament game session was ended before this engine step. LocalGPT will not resurrect or mutate the ended deterministic session.\n\n[[TOURNAMENT_COMPLETE]]";
                        var endedStep = new MultiModelCouncilStep
                        {
                            SortOrder = result.Steps.Count + 1,
                            Round = round,
                            Phase = phase,
                            ModelName = "LocalGPT Tournament Engine",
                            ProviderName = "LocalGPT",
                            ProviderEndpoint = "in-process",
                            ProviderModelName = "kernel-creature-tournament-engine",
                            CouncilMembers = participants.ToList(),
                            Role = definition.Role,
                            Content = endedSummary,
                            VisibleContent = endedSummary,
                            StartedAtUtc = startedAtUtc,
                            CompletedAtUtc = DateTime.UtcNow
                        };
                        endedStep.DurationSeconds = Math.Max(0d, (endedStep.CompletedAtUtc - endedStep.StartedAtUtc).TotalSeconds);
                        request.ProgressMessage?.Invoke("The Kernel Creature Tournament game was ended; the Council workflow is stopping without resurrecting the session.");
                        return endedStep;
                    }

                    throw new InvalidOperationException("The Kernel Creature Tournament workflow requires its prebootstrapped deterministic game session.");
                }

                // Provider streams are HTML-encoded for the chat transcript. The tournament grid renders
                // through textContent, so it needs the plain text back: decode at most twice (double-encoded
                // transcript copies), then drop characters that do not occupy exactly one terminal cell.
                static string PlainModelText(string? content)
                {
                    var text = content ?? string.Empty;
                    for (var pass = 0; pass < 2 && text.Contains('&', StringComparison.Ordinal); pass++)
                    {
                        var decoded = System.Net.WebUtility.HtmlDecode(text);
                        if (string.Equals(decoded, text, StringComparison.Ordinal))
                            break;
                        text = decoded;
                    }
                    var plain = new System.Text.StringBuilder(text.Length);
                    foreach (var character in text)
                    {
                        if (char.IsSurrogate(character) || character is '\uFE0F' or '\u200D' or '\u200B' or '\uFEFF')
                            continue;
                        if (char.IsControl(character) && character is not '\n' and not '\r' and not '\t')
                            continue;
                        plain.Append(character);
                    }
                    return plain.ToString();
                }

                // Small models sometimes copy the prompt's angle-bracket placeholder instead of filling it.
                static bool IsTemplatePlaceholder(string value)
                {
                    var trimmed = (value ?? string.Empty).Trim();
                    return trimmed.Length >= 3 && trimmed.Length <= 120
                        && trimmed[0] == '<' && trimmed[^1] == '>'
                        && trimmed.IndexOfAny(['<', '>'], 1, trimmed.Length - 2) < 0;
                }

                // Removes echoed prompt placeholders ("<one short trainer line>") inside a value; HTML-like tags are kept.
                static string StripPromptPlaceholders(string value)
                {
                    var text = value ?? string.Empty;
                    var result = new System.Text.StringBuilder(text.Length);
                    var index = 0;
                    while (index < text.Length)
                    {
                        var open = text.IndexOf('<', index);
                        var close = open < 0 ? -1 : text.IndexOf('>', open + 1);
                        if (open < 0 || close < 0)
                        {
                            result.Append(text, index, text.Length - index);
                            break;
                        }
                        result.Append(text, index, open - index);
                        var inner = text.AsSpan(open + 1, close - open - 1);
                        var isPlaceholder = inner.Length is >= 3 and <= 120
                            && char.IsLetter(inner[0])
                            && inner.Contains(' ')
                            && inner.IndexOfAny('<', '=', '"') < 0;
                        if (!isPlaceholder)
                            result.Append(text, open, close - open + 1);
                        index = close + 1;
                    }
                    return string.Join(' ', result.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                }

                static string ReadTag(string content, string tag)
                {
                    foreach (var line in PlainModelText(content).Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n'))
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith(tag + ":", StringComparison.OrdinalIgnoreCase))
                        {
                            var value = string.Join(' ', trimmed[(tag.Length + 1)..].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                            return IsTemplatePlaceholder(value) ? string.Empty : StripPromptPlaceholders(value);
                        }
                    }
                    return string.Empty;
                }

                // The Kernel Creature member draws its own battle sprite. Pixel legend: . empty, # outline,
                // + body, - shade, * accent, o eye; a few common aliases are normalized. Rows that are not
                // legend-only but are printable ASCII are kept as member-authored ASCII art instead.
                static (List<string> Rows, string Kind, List<int> Palette) ReadSprite(string content)
                {
                    var lines = PlainModelText(content).Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
                    int[] palette = [16, 75, 25, 220, 231];
                    string[] paletteKeys = ["outline", "body", "shade", "accent", "eye"];
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (!trimmed.StartsWith("COLORS:", StringComparison.OrdinalIgnoreCase))
                            continue;
                        foreach (var token in trimmed["COLORS:".Length..].Split([' ', ',', ';', '\t'], StringSplitOptions.RemoveEmptyEntries))
                        {
                            var separator = token.IndexOfAny(['=', ':']);
                            if (separator <= 0)
                                continue;
                            var keyIndex = Array.FindIndex(paletteKeys, key => key.Equals(token[..separator].Trim(), StringComparison.OrdinalIgnoreCase));
                            if (keyIndex >= 0 && int.TryParse(token[(separator + 1)..].Trim(), out var color) && color is >= 0 and <= 255)
                                palette[keyIndex] = color;
                        }
                        break;
                    }

                    static char PixelLegend(char value) => value switch
                    {
                        '.' or ' ' or '_' or '`' => '.',
                        '#' or '@' or 'X' => '#',
                        '+' or '=' or '\u2588' => '+',
                        '-' or '~' or ':' or '\u2593' or '\u2591' => '-',
                        '*' or '%' or '^' or '\u2592' => '*',
                        'o' or 'O' or '0' => 'o',
                        _ => '\0'
                    };

                    var markerIndex = Array.FindIndex(lines, line => line.TrimStart().StartsWith("SPRITE:", StringComparison.OrdinalIgnoreCase));
                    var candidates = new List<string>();
                    var start = markerIndex >= 0 ? markerIndex : Array.FindIndex(lines, line => line.TrimStart().StartsWith("```", StringComparison.Ordinal));
                    if (start < 0)
                        return (new List<string>(), string.Empty, palette.ToList());
                    if (markerIndex >= 0)
                    {
                        var inline = lines[markerIndex].TrimStart()["SPRITE:".Length..].TrimEnd();
                        if (!string.IsNullOrWhiteSpace(inline))
                            candidates.Add(inline);
                    }
                    for (var index = start + 1; index < lines.Length && candidates.Count < 24; index++)
                    {
                        var line = lines[index].TrimEnd();
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("```", StringComparison.Ordinal))
                        {
                            if (candidates.Count > 0)
                                break;
                            continue;
                        }
                        if (trimmed.Length == 0)
                        {
                            if (candidates.Count > 0)
                                break;
                            continue;
                        }
                        var colon = trimmed.IndexOf(':');
                        if (colon > 1 && colon <= 12 && trimmed[..colon].All(char.IsLetter))
                            break;
                        candidates.Add(line);
                    }

                    if (candidates.Count >= 4 && candidates.All(row => row.Length <= 32 && row.All(cell => PixelLegend(cell) != '\0')))
                    {
                        var rows = candidates.Select(row => new string(row.Select(PixelLegend).ToArray())).ToList();
                        var width = rows.Max(row => row.Length);
                        rows = rows.Select(row => row.PadRight(width, '.')).ToList();
                        while (rows.Count > 0 && rows[0].All(cell => cell == '.')) rows.RemoveAt(0);
                        while (rows.Count > 0 && rows[^1].All(cell => cell == '.')) rows.RemoveAt(rows.Count - 1);
                        if (rows.Count > 0)
                        {
                            var firstColumn = Enumerable.Range(0, width).FirstOrDefault(column => rows.Any(row => row[column] != '.'));
                            var lastColumn = Enumerable.Range(0, width).Reverse().FirstOrDefault(column => rows.Any(row => row[column] != '.'));
                            rows = rows.Select(row => row[firstColumn..(lastColumn + 1)]).ToList();
                        }
                        var filled = rows.Sum(row => row.Count(cell => cell != '.'));
                        // A drawing needs a shape: identical rows (bars, stripes) are not a creature.
                        var distinctRows = rows.Distinct(StringComparer.Ordinal).Count();
                        if (rows.Count >= 4 && filled >= 8 && distinctRows >= 3)
                            return (rows.Take(16).Select(row => row.Length > 20 ? row[..20] : row).ToList(), "pixel", palette.ToList());
                    }

                    var art = candidates
                        .Where(row => !IsTemplatePlaceholder(row) && row.All(cell => cell >= ' ' && cell <= '~'))
                        .Take(14)
                        .Select(row => row.Length > 36 ? row[..36] : row)
                        .ToList();
                    if (art.Count >= 3 && art.Count == Math.Min(candidates.Count, 14) && art.Select(row => row.Trim()).Distinct(StringComparer.Ordinal).Count() >= 3)
                    {
                        var indent = art.Where(row => row.Trim().Length > 0).Select(row => row.Length - row.TrimStart().Length).DefaultIfEmpty(0).Min();
                        return (art.Select(row => row.Length >= indent ? row[indent..] : row).ToList(), "ascii", palette.ToList());
                    }
                    return (new List<string>(), string.Empty, palette.ToList());
                }

                static IReadOnlyList<string> ReadAsciiFrames(string content)
                {
                    var frames = new List<string>();
                    var lines = PlainModelText(content).Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
                    List<string>? frameLines = null;
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (frameLines is null)
                        {
                            if (trimmed.StartsWith("```", StringComparison.Ordinal))
                                frameLines = [];
                            continue;
                        }

                        if (trimmed.StartsWith("```", StringComparison.Ordinal))
                        {
                            var frame = string.Join('\n', frameLines).TrimEnd();
                            // A frame made only of echoed placeholders ("<complete frame 1>") is not art.
                            var echoedPlaceholderOnly = frameLines
                                .Where(frameLine => !string.IsNullOrWhiteSpace(frameLine))
                                .All(IsTemplatePlaceholder);
                            if (!string.IsNullOrWhiteSpace(frame) && !echoedPlaceholderOnly)
                                frames.Add(frame.Length > 12_000 ? frame[..12_000] : frame);
                            frameLines = null;
                            if (frames.Count >= 6)
                                break;
                            continue;
                        }

                        frameLines.Add(line);
                    }
                    return frames;
                }

                var contestantPairs = rolePairings
                    .Where(pairing => string.Equals(pairing.RoleName, "Creature Trainer", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(pairing.PairedRoleName, "Kernel Creature", StringComparison.OrdinalIgnoreCase))
                    .DistinctBy(pairing => pairing.Participant + "|" + pairing.PairedParticipant, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var contestants = new List<CouncilKernelTournamentContestantSeed>();
                foreach (var pairing in contestantPairs)
                {
                    static bool RepresentsModelSlot(MultiModelCouncilStep step, string modelName) =>
                        string.Equals(step.ModelName, modelName, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(step.RecoveryTargetModelName, modelName, StringComparison.OrdinalIgnoreCase);

                    var introduction = result.Steps.LastOrDefault(step =>
                        string.Equals(step.WorkflowStepKey, "creature-introduction", StringComparison.OrdinalIgnoreCase)
                        && RepresentsModelSlot(step, pairing.PairedParticipant));
                    var trainerSelection = result.Steps.LastOrDefault(step =>
                        string.Equals(step.WorkflowStepKey, "trainer-creature-selection", StringComparison.OrdinalIgnoreCase)
                        && RepresentsModelSlot(step, pairing.Participant));
                    var creatureName = ReadTag(introduction?.VisibleContent ?? introduction?.Content ?? string.Empty, "NAME");
                    if (string.IsNullOrWhiteSpace(creatureName))
                        creatureName = ReadTag(trainerSelection?.VisibleContent ?? trainerSelection?.Content ?? string.Empty, "NAME");
                    var trainerContent = trainerSelection?.VisibleContent ?? trainerSelection?.Content ?? string.Empty;
                    var creatureContent = introduction?.VisibleContent ?? introduction?.Content ?? string.Empty;
                    var memberSprite = ReadSprite(creatureContent);
                    contestants.Add(new CouncilKernelTournamentContestantSeed
                    {
                        TrainerModelName = pairing.Participant,
                        CreatureModelName = pairing.PairedParticipant,
                        CreatureName = string.IsNullOrWhiteSpace(creatureName) ? $"Kernel Creature {contestants.Count + 1}" : creatureName,
                        CreatureSpecies = ReadTag(trainerContent, "SPECIES"),
                        CreatureStyle = ReadTag(trainerContent, "STYLE"),
                        CreatureForm = ReadTag(creatureContent, "FORM"),
                        CreatureTrait = ReadTag(creatureContent, "TRAIT"),
                        CreatureVoice = ReadTag(creatureContent, "VOICE"),
                        TrainerGreeting = ReadTag(trainerContent, "CHALLENGE"),
                        CreatureSprite = memberSprite.Rows,
                        CreatureSpriteKind = memberSprite.Kind,
                        CreatureSpritePalette = memberSprite.Palette
                    });
                }

                var teamArtStep = result.Steps.LastOrDefault(step =>
                    string.Equals(step.WorkflowStepKey, "team-building-ascii", StringComparison.OrdinalIgnoreCase));
                var presentationFrames = initializeOnly
                    ? ReadAsciiFrames(teamArtStep?.VisibleContent ?? teamArtStep?.Content ?? string.Empty)
                    : [];

                var evidence = result.Steps
                    .Where(step => string.Equals(step.WorkflowStepKey, "trainer-round-command", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(step.WorkflowStepKey, "fight-round", StringComparison.OrdinalIgnoreCase))
                    .Select(step => new CouncilKernelTournamentRoleEvidence
                    {
                        Role = string.Equals(step.WorkflowStepKey, "trainer-round-command", StringComparison.OrdinalIgnoreCase) ? "Creature Trainer" : "Kernel Creature",
                        ModelName = string.IsNullOrWhiteSpace(step.RecoveryTargetModelName) ? step.ModelName : step.RecoveryTargetModelName,
                        Content = PlainModelText(step.VisibleContent ?? step.Content ?? string.Empty)
                    })
                    .ToList();

                var resolution = await gameSessions.AdvanceKernelTournamentAsync(new CouncilKernelTournamentAdvanceRequest
                {
                    SessionId = game.Id,
                    InitializeOnly = initializeOnly,
                    Contestants = contestants,
                    PresentationFrames = presentationFrames,
                    Evidence = evidence
                }, cancellationToken).ConfigureAwait(false);

                var systemStep = new MultiModelCouncilStep
                {
                    SortOrder = result.Steps.Count + 1,
                    Round = round,
                    Phase = phase,
                    ModelName = "LocalGPT Tournament Engine",
                    ProviderName = "LocalGPT",
                    ProviderEndpoint = "in-process",
                    ProviderModelName = "kernel-creature-tournament-engine",
                    CouncilMembers = participants.ToList(),
                    Role = definition.Role,
                    Content = resolution.SummaryMarkdown,
                    VisibleContent = resolution.SummaryMarkdown,
                    StartedAtUtc = startedAtUtc,
                    CompletedAtUtc = DateTime.UtcNow
                };
                systemStep.DurationSeconds = Math.Max(0d, (systemStep.CompletedAtUtc - systemStep.StartedAtUtc).TotalSeconds);
                request.ProgressMessage?.Invoke(initializeOnly
                    ? "The LocalGPT tournament engine initialized the authoritative bracket and one-shot lineup movie."
                    : "The LocalGPT tournament engine resolved one authoritative exchange and prepared the one-shot arena movie/subtitle.");
                return systemStep;
            }
            catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug(exception, "Kernel Creature Tournament system resolution was cancelled for Council run {CouncilRunId}.", result.RunId);
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Kernel Creature Tournament system resolution failed for Council run {CouncilRunId}.", result.RunId);
                throw;
            }
        }
    }
}

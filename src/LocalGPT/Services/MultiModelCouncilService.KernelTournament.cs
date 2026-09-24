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

                static string ReadTag(string content, string tag)
                {
                    foreach (var line in (content ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n'))
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith(tag + ":", StringComparison.OrdinalIgnoreCase))
                            return string.Join(' ', trimmed[(tag.Length + 1)..].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                    }
                    return string.Empty;
                }

                static IReadOnlyList<string> ReadAsciiFrames(string content)
                {
                    var frames = new List<string>();
                    var lines = (content ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
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
                            if (!string.IsNullOrWhiteSpace(frame))
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
                        TrainerGreeting = ReadTag(trainerContent, "CHALLENGE")
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
                        Content = step.VisibleContent ?? step.Content ?? string.Empty
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

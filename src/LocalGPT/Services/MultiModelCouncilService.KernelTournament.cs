using LocalGPT.BusinessObjects;

namespace LocalGPT.Services
{
    /// <summary>Bridges bounded Council role output into the deterministic Kernel Creature Tournament game session.</summary>
    public sealed partial class MultiModelCouncilService
    {
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
                var game = await gameSessions.GetActiveForCouncilRunAsync(result.RunId, cancellationToken).ConfigureAwait(false);
                if (game is null || !string.Equals(game.GameKey, "kernel-creature-tournament", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The Kernel Creature Tournament workflow requires its prebootstrapped deterministic game session.");

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

                var contestantPairs = rolePairings
                    .Where(pairing => string.Equals(pairing.RoleName, "Creature Trainer", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(pairing.PairedRoleName, "Kernel Creature", StringComparison.OrdinalIgnoreCase))
                    .DistinctBy(pairing => pairing.Participant + "|" + pairing.PairedParticipant, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var contestants = new List<CouncilKernelTournamentContestantSeed>();
                foreach (var pairing in contestantPairs)
                {
                    var introduction = result.Steps.LastOrDefault(step =>
                        string.Equals(step.WorkflowStepKey, "creature-introduction", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(step.ModelName, pairing.PairedParticipant, StringComparison.OrdinalIgnoreCase));
                    var trainerSelection = result.Steps.LastOrDefault(step =>
                        string.Equals(step.WorkflowStepKey, "trainer-creature-selection", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(step.ModelName, pairing.Participant, StringComparison.OrdinalIgnoreCase));
                    var creatureName = ReadTag(introduction?.VisibleContent ?? introduction?.Content ?? string.Empty, "NAME");
                    if (string.IsNullOrWhiteSpace(creatureName))
                        creatureName = ReadTag(trainerSelection?.VisibleContent ?? trainerSelection?.Content ?? string.Empty, "NAME");
                    contestants.Add(new CouncilKernelTournamentContestantSeed
                    {
                        TrainerModelName = pairing.Participant,
                        CreatureModelName = pairing.PairedParticipant,
                        CreatureName = string.IsNullOrWhiteSpace(creatureName) ? $"Kernel Creature {contestants.Count + 1}" : creatureName
                    });
                }

                var evidence = result.Steps
                    .Where(step => string.Equals(step.WorkflowStepKey, "trainer-round-command", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(step.WorkflowStepKey, "fight-round", StringComparison.OrdinalIgnoreCase))
                    .Select(step => new CouncilKernelTournamentRoleEvidence
                    {
                        Role = string.Equals(step.WorkflowStepKey, "trainer-round-command", StringComparison.OrdinalIgnoreCase) ? "Creature Trainer" : "Kernel Creature",
                        ModelName = step.ModelName,
                        Content = step.VisibleContent ?? step.Content ?? string.Empty
                    })
                    .ToList();

                var resolution = await gameSessions.AdvanceKernelTournamentAsync(new CouncilKernelTournamentAdvanceRequest
                {
                    SessionId = game.Id,
                    InitializeOnly = initializeOnly,
                    Contestants = contestants,
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

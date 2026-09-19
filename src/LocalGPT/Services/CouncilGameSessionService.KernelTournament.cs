using System.Security.Cryptography;
using System.Text;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services
{
    /// <summary>Owns deterministic Kernel Creature Tournament bracket progression and one-shot arena presentation.</summary>
    public sealed partial class CouncilGameSessionService
    {
        /// <inheritdoc />
        public Task<CouncilKernelTournamentResolution> AdvanceKernelTournamentAsync(
            CouncilKernelTournamentAdvanceRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ThrowIfDisposed();
                ArgumentNullException.ThrowIfNull(request);
                cancellationToken.ThrowIfCancellationRequested();
                if (!sessions.TryGetValue(request.SessionId, out var session))
                    throw new KeyNotFoundException($"Council game session {request.SessionId} was not found.");
                if (!string.Equals(session.GameKey, "kernel-creature-tournament", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The requested game session is not a Kernel Creature Tournament session.");

                string summary;
                bool completed;
                lock (session.SyncRoot)
                {
                    static string Compact(string? value, int maximum = 48)
                    {
                        var compact = string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                        return compact.Length <= maximum ? compact : compact[..maximum].TrimEnd();
                    }

                    static string SafeName(string? value, string fallback)
                    {
                        var compact = Compact(value, 28);
                        return string.IsNullOrWhiteSpace(compact) ? fallback : compact;
                    }

                    static int StableRange(string seed, int minimum, int maximum)
                    {
                        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
                        var value = BitConverter.ToUInt32(bytes, 0);
                        return minimum + (int)(value % (uint)Math.Max(1, maximum - minimum + 1));
                    }

                    static string ReadTaggedValue(string content, string tag)
                    {
                        foreach (var line in (content ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n'))
                        {
                            var trimmed = line.Trim();
                            if (trimmed.StartsWith(tag + ":", StringComparison.OrdinalIgnoreCase))
                                return Compact(trimmed[(tag.Length + 1)..], 72);
                        }
                        return string.Empty;
                    }

                    static string ResolveMove(string content)
                    {
                        var tagged = ReadTaggedValue(content, "MOVE");
                        if (string.IsNullOrWhiteSpace(tagged))
                            tagged = ReadTaggedValue(content, "COMMAND");
                        var source = string.IsNullOrWhiteSpace(tagged) ? content ?? string.Empty : tagged;
                        if (source.Contains("RECOVER", StringComparison.OrdinalIgnoreCase)
                            || source.Contains("HEAL", StringComparison.OrdinalIgnoreCase)
                            || source.Contains("REST", StringComparison.OrdinalIgnoreCase))
                            return "RECOVER";
                        if (source.Contains("GUARD", StringComparison.OrdinalIgnoreCase)
                            || source.Contains("DEFEND", StringComparison.OrdinalIgnoreCase)
                            || source.Contains("BLOCK", StringComparison.OrdinalIgnoreCase)
                            || source.Contains("BRACE", StringComparison.OrdinalIgnoreCase))
                            return "GUARD";
                        if (source.Contains("WAIT", StringComparison.OrdinalIgnoreCase))
                            return "WAIT";
                        return "ATTACK";
                    }

                    static string EvidenceFor(IReadOnlyList<CouncilKernelTournamentRoleEvidence> evidence, string modelName, string role)
                    {
                        return evidence
                            .Where(item => string.Equals(item.ModelName, modelName, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(item.Role, role, StringComparison.OrdinalIgnoreCase))
                            .Select(item => item.Content ?? string.Empty)
                            .LastOrDefault() ?? string.Empty;
                    }

                    static string HealthBar(int health, int startingHealth)
                    {
                        const int cells = 20;
                        var filled = Math.Clamp((int)Math.Round(cells * Math.Max(0, health) / (double)Math.Max(1, startingHealth)), 0, cells);
                        return "[" + new string('#', filled) + new string('-', cells - filled) + $"] {Math.Max(0, health),3}";
                    }

                    string MakeFrame(string headline, CouncilKernelTournamentFighterState? left, CouncilKernelTournamentFighterState? right, string body, string footer)
                    {
                        var width = Math.Min(72, Math.Max(48, session.FrameWidth));
                        var height = Math.Min(18, Math.Max(12, session.FrameHeight));
                        var rows = Enumerable.Repeat(string.Empty, height).ToArray();
                        rows[0] = "+" + new string('-', width - 2) + "+";
                        rows[1] = "| " + Compact(headline, width - 4).PadRight(width - 4) + " |";
                        rows[2] = "+" + new string('-', width - 2) + "+";
                        if (left is not null && right is not null)
                        {
                            rows[4] = Compact($"{left.CreatureName}  {HealthBar(left.Health, session.TournamentRules.StartingHealth)}", width);
                            rows[5] = Compact($"VS", width).PadLeft(width / 2 + 1);
                            rows[6] = Compact($"{right.CreatureName}  {HealthBar(right.Health, session.TournamentRules.StartingHealth)}", width);
                        }
                        rows[8] = Compact(body, width);
                        rows[Math.Min(height - 3, 10)] = Compact(footer, width);
                        rows[height - 2] = "+" + new string('-', width - 2) + "+";
                        return NormalizeFrame(string.Join(Environment.NewLine, rows), session.FrameWidth, session.FrameHeight);
                    }

                    void PrepareBracket()
                    {
                        while (string.IsNullOrWhiteSpace(session.TournamentChampionFighterId))
                        {
                            if (session.TournamentRoundFighterIds.Count == 1)
                            {
                                session.TournamentChampionFighterId = session.TournamentRoundFighterIds[0];
                                break;
                            }

                            if (session.TournamentMatchIndex < session.TournamentRoundFighterIds.Count)
                            {
                                if (session.TournamentMatchIndex == session.TournamentRoundFighterIds.Count - 1)
                                {
                                    session.TournamentNextRoundFighterIds.Add(session.TournamentRoundFighterIds[session.TournamentMatchIndex]);
                                    session.TournamentMatchIndex++;
                                    continue;
                                }
                                break;
                            }

                            session.TournamentRoundFighterIds = session.TournamentNextRoundFighterIds.ToList();
                            session.TournamentNextRoundFighterIds.Clear();
                            session.TournamentMatchIndex = 0;
                            session.TournamentExchange = 0;
                            session.TournamentRound++;
                        }
                    }

                    if (!session.TournamentInitialized)
                    {
                        var contestants = request.Contestants
                            .Where(item => !string.IsNullOrWhiteSpace(item.TrainerModelName) && !string.IsNullOrWhiteSpace(item.CreatureModelName))
                            .DistinctBy(item => item.CreatureModelName, StringComparer.OrdinalIgnoreCase)
                            .Take(8)
                            .ToList();
                        if (contestants.Count < 2)
                            throw new InvalidOperationException("The Kernel Creature Tournament requires at least two distinct trainer/creature pairings.");

                        session.TournamentFighters = contestants.Select((item, index) => new CouncilKernelTournamentFighterState
                        {
                            FighterId = $"fighter-{index + 1:00}",
                            TrainerModelName = item.TrainerModelName.Trim(),
                            CreatureModelName = item.CreatureModelName.Trim(),
                            CreatureName = SafeName(item.CreatureName, $"Kernel Creature {index + 1}"),
                            Health = session.TournamentRules.StartingHealth
                        }).ToList();
                        session.TournamentRoundFighterIds = session.TournamentFighters.Select(item => item.FighterId).ToList();
                        session.TournamentNextRoundFighterIds.Clear();
                        session.TournamentMatchIndex = 0;
                        session.TournamentRound = 1;
                        session.TournamentExchange = 0;
                        session.TournamentChampionFighterId = string.Empty;
                        session.TournamentInitialized = true;
                        PrepareBracket();
                    }

                    PrepareBracket();
                    CouncilKernelTournamentFighterState? activeLeft = null;
                    CouncilKernelTournamentFighterState? activeRight = null;
                    if (string.IsNullOrWhiteSpace(session.TournamentChampionFighterId)
                        && session.TournamentMatchIndex + 1 < session.TournamentRoundFighterIds.Count)
                    {
                        activeLeft = session.TournamentFighters.First(item => string.Equals(item.FighterId, session.TournamentRoundFighterIds[session.TournamentMatchIndex], StringComparison.OrdinalIgnoreCase));
                        activeRight = session.TournamentFighters.First(item => string.Equals(item.FighterId, session.TournamentRoundFighterIds[session.TournamentMatchIndex + 1], StringComparison.OrdinalIgnoreCase));
                    }

                    var frames = new List<string>();
                    if (request.InitializeOnly)
                    {
                        var pairingText = string.Join("  |  ", session.TournamentFighters.Select(item => item.CreatureName));
                        frames.Add(MakeFrame($"ROUND {session.TournamentRound} // BRACKET ONLINE", activeLeft, activeRight, "Arena lights online...", pairingText));
                        frames.Add(MakeFrame($"ROUND {session.TournamentRound} // CONTESTANTS", activeLeft, activeRight, "AI trainers and creature kernels are paired.", pairingText));
                        frames.Add(MakeFrame($"ROUND {session.TournamentRound} // MATCH READY", activeLeft, activeRight, "The deterministic engine owns HP, damage and advancement.", "Awaiting the first bounded trainer/creature exchange."));
                        session.AnimationSubtitle = "Bracket initialized. The AI participants provide moves and voice; LocalGPT owns every tournament consequence.";
                        summary = BuildTournamentSummary(session, "Bracket initialized by the LocalGPT tournament engine.", true);
                    }
                    else if (activeLeft is null || activeRight is null)
                    {
                        var champion = session.TournamentFighters.FirstOrDefault(item => string.Equals(item.FighterId, session.TournamentChampionFighterId, StringComparison.OrdinalIgnoreCase));
                        frames.Add(MakeFrame("TOURNAMENT COMPLETE", champion, champion, "CEREMONIAL CHAMPION", champion?.CreatureName ?? "Champion"));
                        frames.Add(MakeFrame("ARENA RESULT LOCKED", champion, champion, "All scheduled matches are resolved.", "[[TOURNAMENT_COMPLETE]]"));
                        session.AnimationSubtitle = $"Tournament complete. {champion?.CreatureName ?? "The remaining creature"} receives the imaginary ceremonial prize.";
                        summary = BuildTournamentSummary(session, session.AnimationSubtitle, false);
                    }
                    else
                    {
                        session.TournamentExchange++;
                        var leftTrainer = EvidenceFor(request.Evidence, activeLeft.TrainerModelName, "Creature Trainer");
                        var rightTrainer = EvidenceFor(request.Evidence, activeRight.TrainerModelName, "Creature Trainer");
                        var leftCreature = EvidenceFor(request.Evidence, activeLeft.CreatureModelName, "Kernel Creature");
                        var rightCreature = EvidenceFor(request.Evidence, activeRight.CreatureModelName, "Kernel Creature");
                        var leftMove = ResolveMove(leftCreature + "\n" + leftTrainer);
                        var rightMove = ResolveMove(rightCreature + "\n" + rightTrainer);
                        var leftBefore = activeLeft.Health;
                        var rightBefore = activeRight.Health;
                        var leftDamage = leftMove == "ATTACK" ? StableRange($"{session.Id:N}|{session.TournamentRound}|{session.TournamentMatchIndex}|{session.TournamentExchange}|L|{leftCreature}|{leftTrainer}", session.TournamentRules.MinimumDamage, session.TournamentRules.MaximumDamage) : 0;
                        var rightDamage = rightMove == "ATTACK" ? StableRange($"{session.Id:N}|{session.TournamentRound}|{session.TournamentMatchIndex}|{session.TournamentExchange}|R|{rightCreature}|{rightTrainer}", session.TournamentRules.MinimumDamage, session.TournamentRules.MaximumDamage) : 0;
                        if (rightMove == "GUARD") leftDamage = Math.Max(0, leftDamage - session.TournamentRules.GuardReduction);
                        if (leftMove == "GUARD") rightDamage = Math.Max(0, rightDamage - session.TournamentRules.GuardReduction);
                        if (leftMove == "RECOVER") activeLeft.Health = Math.Min(session.TournamentRules.StartingHealth, activeLeft.Health + session.TournamentRules.RecoveryAmount);
                        if (rightMove == "RECOVER") activeRight.Health = Math.Min(session.TournamentRules.StartingHealth, activeRight.Health + session.TournamentRules.RecoveryAmount);
                        activeRight.Health = Math.Max(0, activeRight.Health - leftDamage);
                        activeLeft.Health = Math.Max(0, activeLeft.Health - rightDamage);

                        var forcedDecision = session.TournamentExchange >= session.TournamentRules.MaximumExchangesPerMatch;
                        CouncilKernelTournamentFighterState? winner = null;
                        CouncilKernelTournamentFighterState? loser = null;
                        if (activeLeft.Health <= 0 || activeRight.Health <= 0 || forcedDecision)
                        {
                            if (activeLeft.Health == activeRight.Health)
                            {
                                var chooseLeft = StableRange($"{session.Id:N}|{session.TournamentRound}|{session.TournamentMatchIndex}|decision", 0, 1) == 0;
                                winner = chooseLeft ? activeLeft : activeRight;
                                loser = chooseLeft ? activeRight : activeLeft;
                            }
                            else
                            {
                                winner = activeLeft.Health > activeRight.Health ? activeLeft : activeRight;
                                loser = ReferenceEquals(winner, activeLeft) ? activeRight : activeLeft;
                            }
                            loser.Eliminated = true;
                            winner.Wins++;
                            session.TournamentNextRoundFighterIds.Add(winner.FighterId);
                            session.TournamentMatchIndex += 2;
                        }

                        var leftFlavor = ReadTaggedValue(leftCreature, "FLAVOR");
                        var rightFlavor = ReadTaggedValue(rightCreature, "FLAVOR");
                        var leftVoice = ReadTaggedValue(leftCreature, "VOICE");
                        var rightVoice = ReadTaggedValue(rightCreature, "VOICE");
                        frames.Add(MakeFrame($"ROUND {session.TournamentRound} // EXCHANGE {session.TournamentExchange}", activeLeft, activeRight, $"{activeLeft.CreatureName}: {leftMove}   {activeRight.CreatureName}: {rightMove}", "AI moves locked. Engine resolution follows."));
                        frames.Add(MakeFrame("AI MOVE", activeLeft, activeRight, string.Join(" / ", new[] { leftFlavor, rightFlavor }.Where(value => !string.IsNullOrWhiteSpace(value))), string.Join(" / ", new[] { leftVoice, rightVoice }.Where(value => !string.IsNullOrWhiteSpace(value)))));
                        frames.Add(MakeFrame("ENGINE IMPACT", activeLeft, activeRight, $"{activeLeft.CreatureName}: {leftBefore}->{activeLeft.Health} HP   {activeRight.CreatureName}: {rightBefore}->{activeRight.Health} HP", $"Damage: {leftDamage} / {rightDamage}; guard/recovery already applied."));

                        var consequence = winner is null
                            ? $"Exchange resolved: {activeLeft.CreatureName} {activeLeft.Health} HP; {activeRight.CreatureName} {activeRight.Health} HP."
                            : $"Match resolved: {winner.CreatureName} advances; {loser!.CreatureName} is eliminated from this fictional bracket.";

                        if (winner is not null)
                        {
                            session.TournamentExchange = 0;
                            PrepareBracket();
                        }
                        frames.Add(MakeFrame(winner is null ? "NEXT EXCHANGE" : "BRACKET ADVANCE", activeLeft, activeRight, consequence, string.IsNullOrWhiteSpace(session.TournamentChampionFighterId) ? "Tournament continues." : "Ceremonial champion decided."));
                        session.AnimationSubtitle = Compact(string.Join(" ", new[] { leftVoice, rightVoice, consequence }.Where(value => !string.IsNullOrWhiteSpace(value))), 360);
                        summary = BuildTournamentSummary(session, consequence, false);
                    }

                    session.AnimationFrames = frames.Select(frame => NormalizeFrame(frame, session.FrameWidth, session.FrameHeight)).ToList();
                    session.AnimationFrameStyleRuns = session.AnimationFrames
                        .Select(frame => BuildSemanticAsciiStyleRuns(session.GameKey, session.RuntimeProfile, frame, session.FrameWidth, session.FrameHeight))
                        .ToList();
                    session.AnimationSubtitleStyle = new CouncilAsciiTextStyle
                    {
                        ColorMode = CouncilAsciiColorMode.Indexed256,
                        ForegroundColor = 226,
                        BackgroundColor = 0,
                        Bold = true
                    };
                    session.AnimationDelayMilliseconds = session.TournamentRules.AnimationFrameDelayMilliseconds;
                    session.AnimationSubtitleHoldMilliseconds = session.TournamentRules.SubtitleHoldMilliseconds;
                    session.FrameText = session.AnimationFrames.FirstOrDefault() ?? RenderKernelTournamentWaiting(session);
                    session.FrameStyleRuns = session.AnimationFrameStyleRuns.Count > 0
                        ? CloneAsciiStyleRuns(session.AnimationFrameStyleRuns[0])
                        : BuildSemanticAsciiStyleRuns(session.GameKey, session.RuntimeProfile, session.FrameText, session.FrameWidth, session.FrameHeight);
                    session.FrameCaption = string.IsNullOrWhiteSpace(session.TournamentChampionFighterId)
                        ? $"Kernel Creature Tournament · round {session.TournamentRound}"
                        : "Kernel Creature Tournament · complete";
                    session.FrameRenderer = "LocalGPT Kernel Tournament Engine";
                    session.CurrentTurnOwner = "LocalGPT Tournament Engine";
                    session.LastAction = request.InitializeOnly ? "tournament-initialize" : "tournament-resolve-exchange";
                    session.LastActionBy = "LocalGPT Tournament Engine";
                    session.Turn++;
                    session.UpdatedAtUtc = DateTime.UtcNow;
                    completed = !string.IsNullOrWhiteSpace(session.TournamentChampionFighterId);
                }

                Notify(session.Id);
                logger.LogInformation("Advanced Kernel Creature Tournament session {GameSessionId}; completed={Completed}. AI move content was omitted.", session.Id, completed);
                return Task.FromResult(new CouncilKernelTournamentResolution
                {
                    Session = ToSnapshot(session),
                    SummaryMarkdown = summary,
                    Completed = completed
                });
            }
            catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation(exception, "Advancing a Kernel Creature Tournament session was cancelled.");
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Advancing a Kernel Creature Tournament session failed; AI move content was omitted.");
                throw;
            }
        }

        /// <summary>Builds the transcript-visible authoritative tournament scoreboard and continuation marker.</summary>
        private string BuildTournamentSummary(CouncilGameSessionState session, string consequence, bool initialized)
        {
            try
            {
                var builder = new StringBuilder();
                builder.AppendLine("### LocalGPT Tournament Engine");
                builder.AppendLine(consequence);
                builder.AppendLine();
                builder.AppendLine($"Rules: `{session.TournamentRuntimeClassKey}` · Round {Math.Max(1, session.TournamentRound)} · Engine-owned HP/bracket state");
                foreach (var fighter in session.TournamentFighters)
                {
                    var status = string.Equals(fighter.FighterId, session.TournamentChampionFighterId, StringComparison.OrdinalIgnoreCase)
                        ? "champion"
                        : fighter.Eliminated ? "eliminated" : "active";
                    builder.AppendLine($"- {fighter.CreatureName}: {Math.Max(0, fighter.Health)} HP · {fighter.Wins} win(s) · {status} · trainer `{fighter.TrainerModelName}` · creature `{fighter.CreatureModelName}`");
                }
                builder.AppendLine();
                if (!string.IsNullOrWhiteSpace(session.TournamentChampionFighterId))
                {
                    var champion = session.TournamentFighters.FirstOrDefault(item => string.Equals(item.FighterId, session.TournamentChampionFighterId, StringComparison.OrdinalIgnoreCase));
                    builder.AppendLine($"Champion: **{champion?.CreatureName ?? "Unknown"}** · imaginary ceremonial prize awarded.");
                    builder.AppendLine("[[TOURNAMENT_COMPLETE]]");
                }
                else
                {
                    if (session.TournamentMatchIndex + 1 < session.TournamentRoundFighterIds.Count)
                    {
                        var left = session.TournamentFighters.First(item => string.Equals(item.FighterId, session.TournamentRoundFighterIds[session.TournamentMatchIndex], StringComparison.OrdinalIgnoreCase));
                        var right = session.TournamentFighters.First(item => string.Equals(item.FighterId, session.TournamentRoundFighterIds[session.TournamentMatchIndex + 1], StringComparison.OrdinalIgnoreCase));
                        builder.AppendLine($"Current legal match: **{left.CreatureName}** vs **{right.CreatureName}**.");
                    }
                    builder.AppendLine(initialized ? "The first exchange may begin." : "The next bounded exchange may begin.");
                    builder.AppendLine("[[TOURNAMENT_CONTINUE]]");
                }
                return builder.ToString().Trim();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Building the Kernel Creature Tournament summary failed.");
                throw;
            }
        }
    }
}

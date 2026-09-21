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
                                return Compact(trimmed[(tag.Length + 1)..], 96);
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

                    static string ResolveTrainerAction(string content)
                    {
                        var tagged = ReadTaggedValue(content, "TRAINER_ACTION");
                        var source = string.IsNullOrWhiteSpace(tagged) ? string.Empty : tagged;
                        if (source.Contains("SWITCH", StringComparison.OrdinalIgnoreCase)) return "SWITCH";
                        if (source.Contains("FOCUS", StringComparison.OrdinalIgnoreCase)) return "FOCUS";
                        if (source.Contains("BRACE", StringComparison.OrdinalIgnoreCase) || source.Contains("GUARD", StringComparison.OrdinalIgnoreCase)) return "BRACE";
                        if (source.Contains("REST", StringComparison.OrdinalIgnoreCase) || source.Contains("CARE", StringComparison.OrdinalIgnoreCase)) return "REST";
                        return "NONE";
                    }

                    static string EvidenceFor(IReadOnlyList<CouncilKernelTournamentRoleEvidence> evidence, string modelName, string role)
                    {
                        return evidence
                            .Where(item => string.Equals(item.ModelName, modelName, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(item.Role, role, StringComparison.OrdinalIgnoreCase))
                            .Select(item => item.Content ?? string.Empty)
                            .LastOrDefault() ?? string.Empty;
                    }

                    static string ShortModel(string? value)
                    {
                        var model = Compact(value, 20);
                        var separator = model.LastIndexOf(" — ", StringComparison.Ordinal);
                        if (separator >= 0 && separator + 3 < model.Length)
                            model = model[(separator + 3)..];
                        var at = model.IndexOf(" @ ", StringComparison.Ordinal);
                        if (at > 0)
                            model = model[..at];
                        return Compact(model, 16);
                    }

                    static CouncilAsciiActorRig BuildTrainerRig(string identity)
                    {
                        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity ?? string.Empty));
                        return new CouncilAsciiActorRig
                        {
                            RigKey = $"trainer:{Convert.ToHexString(hash.AsSpan(0, 4)).ToLowerInvariant()}",
                            Width = 13,
                            Height = 5,
                            HeadGlyph = "oO@0"[hash[0] % 4],
                            TorsoGlyph = "*+#="[hash[1] % 4],
                            Joints =
                            [
                                new() { Name = "head", X = 6, Y = 0 },
                                new() { Name = "neck", X = 6, Y = 1 },
                                new() { Name = "leftHand", X = 3, Y = 2 },
                                new() { Name = "torso", X = 6, Y = 2 },
                                new() { Name = "rightHand", X = 9, Y = 2 },
                                new() { Name = "hip", X = 6, Y = 3 },
                                new() { Name = "leftFoot", X = 4, Y = 4 },
                                new() { Name = "rightFoot", X = 8, Y = 4 }
                            ]
                        };
                    }

                    static CouncilAsciiActorRig BuildCreatureRig(string identity, string descriptor)
                    {
                        var hash = SHA256.HashData(Encoding.UTF8.GetBytes((identity ?? string.Empty) + "|" + (descriptor ?? string.Empty)));
                        var head = descriptor.Contains("robot", StringComparison.OrdinalIgnoreCase) || descriptor.Contains("mech", StringComparison.OrdinalIgnoreCase)
                            ? 'O'
                            : descriptor.Contains("slime", StringComparison.OrdinalIgnoreCase) || descriptor.Contains("blob", StringComparison.OrdinalIgnoreCase)
                                ? 'o'
                                : descriptor.Contains("dragon", StringComparison.OrdinalIgnoreCase) || descriptor.Contains("wing", StringComparison.OrdinalIgnoreCase)
                                    ? '^'
                                    : "oO0@"[hash[0] % 4];
                        return new CouncilAsciiActorRig
                        {
                            RigKey = $"creature:{Convert.ToHexString(hash.AsSpan(0, 4)).ToLowerInvariant()}",
                            Width = 15,
                            Height = 5,
                            HeadGlyph = head,
                            TorsoGlyph = descriptor.Contains("metal", StringComparison.OrdinalIgnoreCase) ? '#' : '=',
                            Joints =
                            [
                                new() { Name = "head", X = 10, Y = 0 },
                                new() { Name = "neck", X = 9, Y = 1 },
                                new() { Name = "body", X = 7, Y = 2 },
                                new() { Name = "tail", X = 2, Y = 2 },
                                new() { Name = "frontPaw", X = 10, Y = 4 },
                                new() { Name = "rearPaw", X = 5, Y = 4 },
                                new() { Name = "wing", X = 6, Y = 0 }
                            ]
                        };
                    }

                    static string[] RenderRig(CouncilAsciiActorRig rig, string pose, bool facingRight, bool creature)
                    {
                        var width = Math.Clamp(rig.Width, 9, 20);
                        var height = Math.Clamp(rig.Height, 4, 8);
                        var canvas = Enumerable.Range(0, height).Select(_ => Enumerable.Repeat(' ', width).ToArray()).ToArray();
                        var joints = rig.Joints.ToDictionary(joint => joint.Name, joint => (joint.X, joint.Y), StringComparer.OrdinalIgnoreCase);

                        void Shift(string name, int dx, int dy)
                        {
                            if (!joints.TryGetValue(name, out var point)) return;
                            joints[name] = (Math.Clamp(point.X + dx, 0, width - 1), Math.Clamp(point.Y + dy, 0, height - 1));
                        }

                        var forward = facingRight ? 1 : -1;
                        switch (pose)
                        {
                            case "attack":
                                Shift(creature ? "head" : "rightHand", forward * 2, 0);
                                Shift(creature ? "frontPaw" : "leftHand", forward, -1);
                                break;
                            case "guard":
                                Shift(creature ? "frontPaw" : "rightHand", -forward, -1);
                                Shift(creature ? "head" : "leftHand", -forward, 0);
                                break;
                            case "recover":
                                Shift(creature ? "head" : "rightHand", 0, -1);
                                Shift(creature ? "wing" : "leftHand", 0, -1);
                                break;
                            case "hit":
                                Shift("head", -forward, 1);
                                Shift(creature ? "body" : "torso", -forward, 0);
                                break;
                            case "victory":
                                Shift(creature ? "head" : "rightHand", 0, -1);
                                Shift(creature ? "wing" : "leftHand", 0, -1);
                                break;
                            case "command":
                                Shift(creature ? "head" : "rightHand", forward, -1);
                                break;
                        }

                        if (!facingRight)
                        {
                            foreach (var key in joints.Keys.ToList())
                            {
                                var point = joints[key];
                                joints[key] = (width - 1 - point.X, point.Y);
                            }
                        }

                        void Put((int X, int Y) point, char glyph)
                        {
                            if (point.X >= 0 && point.X < width && point.Y >= 0 && point.Y < height)
                                canvas[point.Y][point.X] = glyph;
                        }

                        void Link(string first, string second)
                        {
                            if (!joints.TryGetValue(first, out var a) || !joints.TryGetValue(second, out var b)) return;
                            var x = a.X;
                            var y = a.Y;
                            var dx = Math.Abs(b.X - a.X);
                            var sx = a.X < b.X ? 1 : -1;
                            var dy = -Math.Abs(b.Y - a.Y);
                            var sy = a.Y < b.Y ? 1 : -1;
                            var err = dx + dy;
                            while (true)
                            {
                                if ((x, y) != a && (x, y) != b)
                                {
                                    var glyph = a.Y == b.Y ? '-' : a.X == b.X ? '|' : ((b.X - a.X) * (b.Y - a.Y) > 0 ? '\\' : '/');
                                    Put((x, y), glyph);
                                }
                                if (x == b.X && y == b.Y) break;
                                var e2 = 2 * err;
                                if (e2 >= dy) { err += dy; x += sx; }
                                if (e2 <= dx) { err += dx; y += sy; }
                            }
                        }

                        if (creature)
                        {
                            Link("tail", "body");
                            Link("body", "neck");
                            Link("neck", "head");
                            Link("body", "frontPaw");
                            Link("body", "rearPaw");
                            Link("body", "wing");
                            if (joints.TryGetValue("head", out var head)) Put(head, rig.HeadGlyph);
                            if (joints.TryGetValue("body", out var body)) Put(body, rig.TorsoGlyph);
                            if (joints.TryGetValue("frontPaw", out var frontPaw)) Put(frontPaw, 'v');
                            if (joints.TryGetValue("rearPaw", out var rearPaw)) Put(rearPaw, 'v');
                            if (joints.TryGetValue("tail", out var tail)) Put(tail, '~');
                            if (joints.TryGetValue("wing", out var wing)) Put(wing, '^');
                        }
                        else
                        {
                            Link("head", "neck");
                            Link("neck", "torso");
                            Link("torso", "leftHand");
                            Link("torso", "rightHand");
                            Link("torso", "hip");
                            Link("hip", "leftFoot");
                            Link("hip", "rightFoot");
                            if (joints.TryGetValue("head", out var head)) Put(head, rig.HeadGlyph);
                            if (joints.TryGetValue("torso", out var torso)) Put(torso, rig.TorsoGlyph);
                            if (joints.TryGetValue("leftHand", out var leftHand)) Put(leftHand, 'o');
                            if (joints.TryGetValue("rightHand", out var rightHand)) Put(rightHand, 'o');
                            if (joints.TryGetValue("leftFoot", out var leftFoot)) Put(leftFoot, '/');
                            if (joints.TryGetValue("rightFoot", out var rightFoot)) Put(rightFoot, '\\');
                        }

                        return canvas.Select(row => new string(row).TrimEnd()).ToArray();
                    }

                    static CouncilKernelTournamentCreatureState? ActiveCreature(CouncilKernelTournamentFighterState fighter)
                    {
                        return fighter.CreatureRoster.FirstOrDefault(creature => string.Equals(creature.CreatureId, fighter.ActiveCreatureId, StringComparison.OrdinalIgnoreCase))
                            ?? fighter.CreatureRoster.FirstOrDefault(creature => creature.IsActive)
                            ?? fighter.CreatureRoster.FirstOrDefault();
                    }

                    static int TeamHealth(CouncilKernelTournamentFighterState fighter) => fighter.CreatureRoster.Count == 0
                        ? Math.Max(0, fighter.Health)
                        : fighter.CreatureRoster.Sum(creature => Math.Max(0, creature.Health));

                    static bool HasHealthyCreature(CouncilKernelTournamentFighterState fighter) => fighter.CreatureRoster.Count == 0
                        ? fighter.Health > 0
                        : fighter.CreatureRoster.Any(creature => creature.Health > 0);

                    static void SyncActiveProjection(CouncilKernelTournamentFighterState fighter)
                    {
                        var active = ActiveCreature(fighter);
                        if (active is null) return;
                        fighter.ActiveCreatureId = active.CreatureId;
                        fighter.CreatureName = active.Name;
                        fighter.CreatureSpecies = active.Species;
                        fighter.CreatureStyle = active.Style;
                        fighter.CreatureForm = active.Form;
                        fighter.CreatureTrait = active.Trait;
                        fighter.CreatureVoice = active.Voice;
                        fighter.Health = active.Health;
                        foreach (var creature in fighter.CreatureRoster)
                            creature.IsActive = string.Equals(creature.CreatureId, active.CreatureId, StringComparison.OrdinalIgnoreCase);
                    }

                    bool SwitchCreature(CouncilKernelTournamentFighterState fighter, string requestedTarget, bool countAgainstLimit)
                    {
                        if (fighter.CreatureRoster.Count <= 1)
                            return false;
                        if (countAgainstLimit && fighter.SwitchesUsedInCurrentFight >= session.TournamentRules.MaximumCreatureSwitchesPerFight)
                            return false;

                        var current = ActiveCreature(fighter);
                        var target = !string.IsNullOrWhiteSpace(requestedTarget)
                            ? fighter.CreatureRoster.FirstOrDefault(creature => creature.Health > 0
                                && !creature.IsActive
                                && (creature.Name.Contains(requestedTarget, StringComparison.OrdinalIgnoreCase)
                                    || creature.CreatureId.Contains(requestedTarget, StringComparison.OrdinalIgnoreCase)))
                            : null;
                        target ??= fighter.CreatureRoster.FirstOrDefault(creature => creature.Health > 0 && !creature.IsActive);
                        if (target is null)
                            return false;

                        if (current is not null)
                        {
                            current.IsActive = false;
                            current.IsResting = current.Health > 0;
                        }
                        target.IsActive = true;
                        target.IsResting = false;
                        fighter.ActiveCreatureId = target.CreatureId;
                        if (countAgainstLimit)
                            fighter.SwitchesUsedInCurrentFight++;
                        SyncActiveProjection(fighter);
                        return true;
                    }

                    void RestBenchedCreatures(CouncilKernelTournamentFighterState fighter)
                    {
                        foreach (var creature in fighter.CreatureRoster.Where(creature => !creature.IsActive && creature.Health > 0))
                        {
                            creature.IsResting = true;
                            creature.RestedExchanges++;
                            creature.Health = Math.Min(session.TournamentRules.StartingHealth, creature.Health + session.TournamentRules.RestRecoveryPerExchange);
                        }
                    }

                    void AddTimeline(string kind, CouncilKernelTournamentFighterState? left, CouncilKernelTournamentFighterState? right, string detail)
                    {
                        session.TournamentTimelineSequence++;
                        session.TournamentTimeline.Add(new CouncilKernelTournamentTimelineEntry
                        {
                            Sequence = session.TournamentTimelineSequence,
                            Round = Math.Max(1, session.TournamentRound),
                            Match = Math.Max(1, session.TournamentMatchIndex / 2 + 1),
                            Exchange = Math.Max(0, session.TournamentExchange),
                            EventKind = Compact(kind, 18),
                            LeftState = left is null ? string.Empty : Compact($"{ShortModel(left.TrainerModelName)}->{left.CreatureName} {Math.Max(0, left.Health)}HP", 54),
                            RightState = right is null ? string.Empty : Compact($"{ShortModel(right.TrainerModelName)}->{right.CreatureName} {Math.Max(0, right.Health)}HP", 54),
                            Detail = Compact(detail, 140)
                        });
                        if (session.TournamentTimeline.Count > 12)
                            session.TournamentTimeline.RemoveRange(0, session.TournamentTimeline.Count - 12);
                    }

                    string TimelineLine(int offsetFromEnd)
                    {
                        var index = session.TournamentTimeline.Count - 1 - offsetFromEnd;
                        if (index < 0) return string.Empty;
                        var entry = session.TournamentTimeline[index];
                        return $"T{entry.Sequence:00} R{entry.Round}E{entry.Exchange} {entry.EventKind}: {entry.Detail}";
                    }

                    string RosterLine(CouncilKernelTournamentFighterState fighter)
                    {
                        var names = fighter.CreatureRoster.Select(creature =>
                            $"{(creature.IsActive ? '>' : creature.IsResting ? '~' : ' ')}{Compact(creature.Name, 12)}:{Math.Max(0, creature.Health)}");
                        return Compact(string.Join(" ", names), 48);
                    }

                    string MakeFrame(
                        string headline,
                        CouncilKernelTournamentFighterState? left,
                        CouncilKernelTournamentFighterState? right,
                        string body,
                        string footer,
                        string leftPose = "idle",
                        string rightPose = "idle",
                        int? leftHealth = null,
                        int? rightHealth = null,
                        string centerEffect = "VS")
                    {
                        var width = Math.Min(160, Math.Max(96, session.FrameWidth));
                        var height = Math.Min(48, Math.Max(32, session.FrameHeight));
                        var rows = Enumerable.Repeat(string.Empty, height).ToArray();
                        var interior = width - 2;
                        var gutter = 5;
                        var sideWidth = Math.Max(26, (interior - gutter) / 2);

                        static string FitRaw(string value, int maximum)
                        {
                            var raw = value ?? string.Empty;
                            return raw.Length <= maximum ? raw : raw[..maximum];
                        }

                        string Sides(string leftText, string rightText, string center = "")
                        {
                            var lhs = FitRaw(leftText, sideWidth).PadRight(sideWidth);
                            var rhs = FitRaw(rightText, sideWidth).PadLeft(sideWidth);
                            var gap = Math.Max(1, interior - lhs.Length - rhs.Length);
                            var centered = FitRaw(center, gap);
                            var leftPad = Math.Max(0, (gap - centered.Length) / 2);
                            var rightPad = Math.Max(0, gap - centered.Length - leftPad);
                            return FitRaw(lhs + new string(' ', leftPad) + centered + new string(' ', rightPad) + rhs, interior);
                        }

                        static string MiniHealth(int health, int startingHealth)
                        {
                            const int cells = 10;
                            var filled = Math.Clamp((int)Math.Round(cells * Math.Max(0, health) / (double)Math.Max(1, startingHealth)), 0, cells);
                            return "[" + new string('#', filled) + new string('-', cells - filled) + $"] {Math.Max(0, health),3}";
                        }

                        rows[0] = "+" + new string('-', width - 2) + "+";
                        rows[1] = "| " + Compact(headline, width - 4).PadRight(width - 4) + " |";
                        rows[2] = "+" + new string('-', width - 2) + "+";
                        if (left is not null && right is not null)
                        {
                            rows[3] = Sides($"Trainer {ShortModel(left.TrainerModelName)} [{left.LastTrainerAction}]", $"Trainer {ShortModel(right.TrainerModelName)} [{right.LastTrainerAction}]");
                            var leftTrainer = RenderRig(left.TrainerRig, leftPose, true, creature: false);
                            var rightTrainer = RenderRig(right.TrainerRig, rightPose, false, creature: false);
                            for (var line = 0; line < 5; line++)
                                rows[4 + line] = Sides(leftTrainer.ElementAtOrDefault(line) ?? string.Empty, rightTrainer.ElementAtOrDefault(line) ?? string.Empty, line == 2 ? centerEffect : string.Empty);

                            rows[9] = Sides(
                                $"{left.CreatureName} · {Compact(left.CreatureSpecies, 16)}",
                                $"{right.CreatureName} · {Compact(right.CreatureSpecies, 16)}");
                            var leftActive = ActiveCreature(left);
                            var rightActive = ActiveCreature(right);
                            var leftCreature = RenderRig(leftActive?.Rig ?? BuildCreatureRig(left.FighterId, left.CreatureSpecies), leftPose, true, creature: true);
                            var rightCreature = RenderRig(rightActive?.Rig ?? BuildCreatureRig(right.FighterId, right.CreatureSpecies), rightPose, false, creature: true);
                            for (var line = 0; line < 5; line++)
                                rows[10 + line] = Sides(leftCreature.ElementAtOrDefault(line) ?? string.Empty, rightCreature.ElementAtOrDefault(line) ?? string.Empty, line == 2 ? centerEffect : string.Empty);

                            rows[15] = Sides(
                                MiniHealth(leftHealth ?? left.Health, session.TournamentRules.StartingHealth),
                                MiniHealth(rightHealth ?? right.Health, session.TournamentRules.StartingHealth),
                                "HP");
                            rows[16] = Sides(RosterLine(left), RosterLine(right), "TEAM");
                            rows[17] = Sides(
                                $"switches {left.SwitchesUsedInCurrentFight}/{session.TournamentRules.MaximumCreatureSwitchesPerFight}",
                                $"switches {right.SwitchesUsedInCurrentFight}/{session.TournamentRules.MaximumCreatureSwitchesPerFight}",
                                "TRAINER");
                        }
                        rows[18] = Compact(body, interior);
                        rows[19] = Compact(footer, interior);
                        rows[20] = Compact(TimelineLine(0), interior);
                        rows[21] = Compact(TimelineLine(1), interior);
                        rows[height - 1] = "+" + new string('-', width - 2) + "+";
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
                            CreatureSpecies = Compact(item.CreatureSpecies, 36),
                            CreatureStyle = Compact(item.CreatureStyle, 36),
                            CreatureForm = Compact(item.CreatureForm, 48),
                            CreatureTrait = Compact(item.CreatureTrait, 48),
                            CreatureVoice = Compact(item.CreatureVoice, 72),
                            TrainerRig = BuildTrainerRig(item.TrainerModelName),
                            Health = session.TournamentRules.StartingHealth
                        }).ToList();

                        var usedCreatureNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        for (var fighterIndex = 0; fighterIndex < session.TournamentFighters.Count; fighterIndex++)
                        {
                            var fighter = session.TournamentFighters[fighterIndex];
                            if (!usedCreatureNames.Add(fighter.CreatureName))
                            {
                                var qualifiedName = SafeName($"{fighter.CreatureName}-{ShortModel(fighter.CreatureModelName)}", $"Kernel Creature {fighterIndex + 1}");
                                if (!usedCreatureNames.Add(qualifiedName))
                                {
                                    qualifiedName = SafeName($"{fighter.CreatureName}-{fighterIndex + 1}", $"Kernel Creature {fighterIndex + 1}");
                                    usedCreatureNames.Add(qualifiedName);
                                }
                                fighter.CreatureName = qualifiedName;
                            }

                            var suffixes = new[] { string.Empty, " Echo", " Vanguard", " Warden", " Nimbus" };
                            var rosterCount = Math.Clamp(session.TournamentRules.CreaturesPerTrainer, 1, suffixes.Length);
                            fighter.CreatureRoster = Enumerable.Range(0, rosterCount).Select(slot =>
                            {
                                var name = slot == 0 ? fighter.CreatureName : SafeName(fighter.CreatureName + suffixes[slot], $"{fighter.CreatureName}-{slot + 1}");
                                var form = slot == 0 ? fighter.CreatureForm : Compact($"reserve-{slot + 1} {fighter.CreatureForm}", 48);
                                var trait = slot == 0 ? fighter.CreatureTrait : Compact($"resting partner; {fighter.CreatureTrait}", 48);
                                var descriptor = string.Join(' ', fighter.CreatureSpecies, fighter.CreatureStyle, form, trait);
                                return new CouncilKernelTournamentCreatureState
                                {
                                    CreatureId = $"{fighter.FighterId}-creature-{slot + 1:00}",
                                    Name = name,
                                    Species = fighter.CreatureSpecies,
                                    Style = fighter.CreatureStyle,
                                    Form = form,
                                    Trait = trait,
                                    Voice = fighter.CreatureVoice,
                                    Health = session.TournamentRules.StartingHealth,
                                    IsActive = slot == 0,
                                    IsResting = slot != 0,
                                    Rig = BuildCreatureRig($"{fighter.CreatureModelName}|{slot}", descriptor)
                                };
                            }).ToList();
                            fighter.ActiveCreatureId = fighter.CreatureRoster[0].CreatureId;
                            SyncActiveProjection(fighter);
                        }

                        session.TournamentTimeline.Clear();
                        session.TournamentTimelineSequence = 0;
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
                        var presentationFrames = request.PresentationFrames
                            .Where(frame => !string.IsNullOrWhiteSpace(frame))
                            .Take(6)
                            .ToList();
                        frames.AddRange(presentationFrames);
                        var pairingText = string.Join("  |  ", session.TournamentFighters.Select(item => item.CreatureName));
                        AddTimeline("READY", activeLeft, activeRight, $"Bracket ready; {session.TournamentRules.CreaturesPerTrainer} creature slots per trainer, {session.TournamentRules.MaximumCreatureSwitchesPerFight} trainer switches per fight.");
                        frames.Add(MakeFrame($"ROUND {session.TournamentRound} // BRACKET ONLINE", activeLeft, activeRight, "Arena lights online...", pairingText));
                        frames.Add(MakeFrame($"ROUND {session.TournamentRound} // CONTESTANTS", activeLeft, activeRight, "Persistent trainer and creature rigs are joint-anchored for every ASCII pose.", pairingText));
                        frames.Add(MakeFrame($"ROUND {session.TournamentRound} // MATCH READY", activeLeft, activeRight, "The deterministic engine owns HP, damage, legal switches and advancement.", "Trainer actions, creature commands, roster rest and exact state are shown together."));
                        session.AnimationSubtitle = presentationFrames.Count > 0
                            ? "ASCII Team Artist reveal complete. Bracket initialized; LocalGPT still owns every tournament consequence."
                            : "Bracket initialized. The AI participants provide bounded commands and voice; LocalGPT owns every tournament consequence.";
                        summary = BuildTournamentSummary(session, presentationFrames.Count > 0
                            ? "ASCII team reveal completed; bracket initialized by the LocalGPT tournament engine."
                            : "Bracket initialized by the LocalGPT tournament engine.", true);
                    }
                    else if (activeLeft is null || activeRight is null)
                    {
                        var champion = session.TournamentFighters.FirstOrDefault(item => string.Equals(item.FighterId, session.TournamentChampionFighterId, StringComparison.OrdinalIgnoreCase));
                        AddTimeline("COMPLETE", champion, champion, $"Champion {champion?.CreatureName ?? "unknown"} locked by deterministic bracket state.");
                        frames.Add(MakeFrame("TOURNAMENT COMPLETE", champion, champion, "CEREMONIAL CHAMPION", champion?.CreatureName ?? "Champion", "victory", "victory"));
                        frames.Add(MakeFrame("ARENA RESULT LOCKED", champion, champion, "All scheduled matches are resolved.", "[[TOURNAMENT_COMPLETE]]", "victory", "victory"));
                        session.AnimationSubtitle = $"Tournament complete. {champion?.CreatureName ?? "The remaining creature"} receives the imaginary ceremonial prize.";
                        summary = BuildTournamentSummary(session, session.AnimationSubtitle, false);
                    }
                    else
                    {
                        if (session.TournamentExchange == 0)
                        {
                            activeLeft.SwitchesUsedInCurrentFight = 0;
                            activeRight.SwitchesUsedInCurrentFight = 0;
                            activeLeft.LastTrainerAction = "NONE";
                            activeRight.LastTrainerAction = "NONE";
                        }

                        session.TournamentExchange++;
                        var leftTrainer = EvidenceFor(request.Evidence, activeLeft.TrainerModelName, "Creature Trainer");
                        var rightTrainer = EvidenceFor(request.Evidence, activeRight.TrainerModelName, "Creature Trainer");
                        var leftCreature = EvidenceFor(request.Evidence, activeLeft.CreatureModelName, "Kernel Creature");
                        var rightCreature = EvidenceFor(request.Evidence, activeRight.CreatureModelName, "Kernel Creature");
                        var leftTrainerAction = ResolveTrainerAction(leftTrainer);
                        var rightTrainerAction = ResolveTrainerAction(rightTrainer);
                        activeLeft.LastTrainerAction = leftTrainerAction;
                        activeRight.LastTrainerAction = rightTrainerAction;

                        var leftSwitchTarget = ReadTaggedValue(leftTrainer, "SWITCH");
                        var rightSwitchTarget = ReadTaggedValue(rightTrainer, "SWITCH");
                        var leftSwitched = leftTrainerAction == "SWITCH" && SwitchCreature(activeLeft, leftSwitchTarget, countAgainstLimit: true);
                        var rightSwitched = rightTrainerAction == "SWITCH" && SwitchCreature(activeRight, rightSwitchTarget, countAgainstLimit: true);
                        if (leftSwitched || rightSwitched)
                            AddTimeline("SWITCH", activeLeft, activeRight, $"Trainer switch: left={(leftSwitched ? activeLeft.CreatureName : "none")}; right={(rightSwitched ? activeRight.CreatureName : "none")}.");

                        var leftMove = ResolveMove(leftCreature + "\n" + leftTrainer);
                        var rightMove = ResolveMove(rightCreature + "\n" + rightTrainer);
                        var leftBefore = activeLeft.Health;
                        var rightBefore = activeRight.Health;

                        if (leftTrainerAction == "REST")
                        {
                            var active = ActiveCreature(activeLeft);
                            if (active is not null)
                            {
                                active.Health = Math.Min(session.TournamentRules.StartingHealth, active.Health + Math.Max(1, session.TournamentRules.RecoveryAmount / 2));
                                SyncActiveProjection(activeLeft);
                            }
                        }
                        if (rightTrainerAction == "REST")
                        {
                            var active = ActiveCreature(activeRight);
                            if (active is not null)
                            {
                                active.Health = Math.Min(session.TournamentRules.StartingHealth, active.Health + Math.Max(1, session.TournamentRules.RecoveryAmount / 2));
                                SyncActiveProjection(activeRight);
                            }
                        }

                        var leftDamage = leftMove == "ATTACK" ? StableRange($"{session.Id:N}|{session.TournamentRound}|{session.TournamentMatchIndex}|{session.TournamentExchange}|L|{leftCreature}|{leftTrainer}", session.TournamentRules.MinimumDamage, session.TournamentRules.MaximumDamage) : 0;
                        var rightDamage = rightMove == "ATTACK" ? StableRange($"{session.Id:N}|{session.TournamentRound}|{session.TournamentMatchIndex}|{session.TournamentExchange}|R|{rightCreature}|{rightTrainer}", session.TournamentRules.MinimumDamage, session.TournamentRules.MaximumDamage) : 0;
                        if (leftTrainerAction == "FOCUS" && leftDamage > 0) leftDamage += session.TournamentRules.TrainerFocusBonus;
                        if (rightTrainerAction == "FOCUS" && rightDamage > 0) rightDamage += session.TournamentRules.TrainerFocusBonus;
                        if (rightMove == "GUARD") leftDamage = Math.Max(0, leftDamage - session.TournamentRules.GuardReduction);
                        if (leftMove == "GUARD") rightDamage = Math.Max(0, rightDamage - session.TournamentRules.GuardReduction);
                        if (rightTrainerAction == "BRACE") leftDamage = Math.Max(0, leftDamage - session.TournamentRules.TrainerBraceReduction);
                        if (leftTrainerAction == "BRACE") rightDamage = Math.Max(0, rightDamage - session.TournamentRules.TrainerBraceReduction);

                        var leftActiveCreature = ActiveCreature(activeLeft);
                        var rightActiveCreature = ActiveCreature(activeRight);
                        if (leftMove == "RECOVER" && leftActiveCreature is not null)
                            leftActiveCreature.Health = Math.Min(session.TournamentRules.StartingHealth, leftActiveCreature.Health + session.TournamentRules.RecoveryAmount);
                        if (rightMove == "RECOVER" && rightActiveCreature is not null)
                            rightActiveCreature.Health = Math.Min(session.TournamentRules.StartingHealth, rightActiveCreature.Health + session.TournamentRules.RecoveryAmount);
                        if (rightActiveCreature is not null)
                            rightActiveCreature.Health = Math.Max(0, rightActiveCreature.Health - leftDamage);
                        if (leftActiveCreature is not null)
                            leftActiveCreature.Health = Math.Max(0, leftActiveCreature.Health - rightDamage);
                        SyncActiveProjection(activeLeft);
                        SyncActiveProjection(activeRight);

                        var leftAutoSwitched = activeLeft.Health <= 0 && SwitchCreature(activeLeft, string.Empty, countAgainstLimit: false);
                        var rightAutoSwitched = activeRight.Health <= 0 && SwitchCreature(activeRight, string.Empty, countAgainstLimit: false);
                        if (leftAutoSwitched || rightAutoSwitched)
                            AddTimeline("KO-SWITCH", activeLeft, activeRight, $"Engine forced healthy reserve: left={(leftAutoSwitched ? activeLeft.CreatureName : "none")}; right={(rightAutoSwitched ? activeRight.CreatureName : "none")}.");

                        RestBenchedCreatures(activeLeft);
                        RestBenchedCreatures(activeRight);
                        SyncActiveProjection(activeLeft);
                        SyncActiveProjection(activeRight);

                        AddTimeline("COMMAND", activeLeft, activeRight, $"L {leftTrainerAction}/{leftMove}; R {rightTrainerAction}/{rightMove}.");
                        AddTimeline("IMPACT", activeLeft, activeRight, $"Damage L->{rightDamage}, R->{leftDamage}; team HP {TeamHealth(activeLeft)}:{TeamHealth(activeRight)}.");

                        var forcedDecision = session.TournamentExchange >= session.TournamentRules.MaximumExchangesPerMatch;
                        CouncilKernelTournamentFighterState? winner = null;
                        CouncilKernelTournamentFighterState? loser = null;
                        if (!HasHealthyCreature(activeLeft) || !HasHealthyCreature(activeRight) || forcedDecision)
                        {
                            var leftTeamHealth = TeamHealth(activeLeft);
                            var rightTeamHealth = TeamHealth(activeRight);
                            if (leftTeamHealth == rightTeamHealth)
                            {
                                var chooseLeft = StableRange($"{session.Id:N}|{session.TournamentRound}|{session.TournamentMatchIndex}|decision", 0, 1) == 0;
                                winner = chooseLeft ? activeLeft : activeRight;
                                loser = chooseLeft ? activeRight : activeLeft;
                            }
                            else
                            {
                                winner = leftTeamHealth > rightTeamHealth ? activeLeft : activeRight;
                                loser = ReferenceEquals(winner, activeLeft) ? activeRight : activeLeft;
                            }
                            loser.Eliminated = true;
                            winner.Wins++;
                            winner.SwitchesUsedInCurrentFight = 0;
                            session.TournamentNextRoundFighterIds.Add(winner.FighterId);
                            session.TournamentMatchIndex += 2;
                        }

                        var leftFlavor = ReadTaggedValue(leftCreature, "FLAVOR");
                        var rightFlavor = ReadTaggedValue(rightCreature, "FLAVOR");
                        var leftVoice = ReadTaggedValue(leftCreature, "VOICE");
                        var rightVoice = ReadTaggedValue(rightCreature, "VOICE");
                        var leftPose = leftMove switch { "GUARD" => "guard", "RECOVER" => "recover", "WAIT" => "idle", _ => "attack" };
                        var rightPose = rightMove switch { "GUARD" => "guard", "RECOVER" => "recover", "WAIT" => "idle", _ => "attack" };
                        var leftImpactPose = rightDamage > 0 ? "hit" : leftPose;
                        var rightImpactPose = leftDamage > 0 ? "hit" : rightPose;
                        frames.Add(MakeFrame(
                            $"ROUND {session.TournamentRound} // TRAINER ACTION",
                            activeLeft,
                            activeRight,
                            $"{ShortModel(activeLeft.TrainerModelName)}: {leftTrainerAction}   {ShortModel(activeRight.TrainerModelName)}: {rightTrainerAction}",
                            $"Creature commands: {activeLeft.CreatureName}={leftMove}; {activeRight.CreatureName}={rightMove}.",
                            "command",
                            "command",
                            leftBefore,
                            rightBefore,
                            "..."));
                        frames.Add(MakeFrame(
                            "CREATURES MOVE",
                            activeLeft,
                            activeRight,
                            Compact(string.Join(" / ", new[] { leftFlavor, rightFlavor }.Where(value => !string.IsNullOrWhiteSpace(value))), 140),
                            "Joint-anchored ASCII actor rigs move; authoritative HP changes only in the engine-impact frame.",
                            leftPose,
                            rightPose,
                            leftBefore,
                            rightBefore,
                            leftMove == "ATTACK" || rightMove == "ATTACK" ? ">><<" : "<>"));
                        frames.Add(MakeFrame(
                            "ARENA CLASH",
                            activeLeft,
                            activeRight,
                            $"{leftMove}  <<< *** >>>  {rightMove}",
                            Compact(string.Join(" / ", new[] { leftVoice, rightVoice }.Where(value => !string.IsNullOrWhiteSpace(value))), 140),
                            leftPose,
                            rightPose,
                            leftBefore,
                            rightBefore,
                            "***"));
                        frames.Add(MakeFrame(
                            "ENGINE IMPACT",
                            activeLeft,
                            activeRight,
                            $"Active HP: {activeLeft.CreatureName} {leftBefore}->{activeLeft.Health}; {activeRight.CreatureName} {rightBefore}->{activeRight.Health}.",
                            $"Damage {leftDamage}/{rightDamage}; total team HP {TeamHealth(activeLeft)}/{TeamHealth(activeRight)}; rest recovery applied to benched creatures.",
                            leftImpactPose,
                            rightImpactPose,
                            activeLeft.Health,
                            activeRight.Health,
                            "!!!"));
                        frames.Add(MakeFrame(
                            "RECOVERY / BENCH",
                            activeLeft,
                            activeRight,
                            "Switched-out and reserve creatures remain in the team roster and rest between exchanges.",
                            $"Trainer switch budget: {activeLeft.SwitchesUsedInCurrentFight}/{session.TournamentRules.MaximumCreatureSwitchesPerFight} vs {activeRight.SwitchesUsedInCurrentFight}/{session.TournamentRules.MaximumCreatureSwitchesPerFight}.",
                            activeLeft.Health <= 0 ? "hit" : "idle",
                            activeRight.Health <= 0 ? "hit" : "idle",
                            activeLeft.Health,
                            activeRight.Health,
                            "::"));

                        var consequence = winner is null
                            ? $"Exchange resolved: {activeLeft.CreatureName} {activeLeft.Health} HP ({TeamHealth(activeLeft)} team); {activeRight.CreatureName} {activeRight.Health} HP ({TeamHealth(activeRight)} team)."
                            : $"Match resolved: trainer {ShortModel(winner.TrainerModelName)} and team advance; trainer {ShortModel(loser!.TrainerModelName)} is eliminated from this fictional bracket.";

                        if (winner is not null)
                        {
                            session.TournamentExchange = 0;
                            AddTimeline("MATCH", winner, loser, consequence);
                            PrepareBracket();
                        }
                        frames.Add(MakeFrame(
                            winner is null ? "NEXT EXCHANGE" : "BRACKET ADVANCE",
                            activeLeft,
                            activeRight,
                            consequence,
                            string.IsNullOrWhiteSpace(session.TournamentChampionFighterId) ? "Tournament continues." : "Ceremonial champion decided.",
                            winner is not null && ReferenceEquals(winner, activeLeft) ? "victory" : activeLeft.Health <= 0 ? "hit" : "idle",
                            winner is not null && ReferenceEquals(winner, activeRight) ? "victory" : activeRight.Health <= 0 ? "hit" : "idle",
                            activeLeft.Health,
                            activeRight.Health,
                            winner is null ? "VS" : "WIN"));
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
                    session.FrameText = session.AnimationFrames.LastOrDefault() ?? RenderKernelTournamentWaiting(session);
                    session.FrameStyleRuns = session.AnimationFrameStyleRuns.Count > 0
                        ? CloneAsciiStyleRuns(session.AnimationFrameStyleRuns[^1])
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
                builder.AppendLine($"Rules: `{session.TournamentRuntimeClassKey}` · Round {Math.Max(1, session.TournamentRound)} · Engine-owned HP/bracket/switch state · switch cap {session.TournamentRules.MaximumCreatureSwitchesPerFight}");
                foreach (var fighter in session.TournamentFighters)
                {
                    var status = string.Equals(fighter.FighterId, session.TournamentChampionFighterId, StringComparison.OrdinalIgnoreCase)
                        ? "champion"
                        : fighter.Eliminated ? "eliminated" : "active";
                    var roster = fighter.CreatureRoster.Count == 0
                        ? $"{fighter.CreatureName}:{Math.Max(0, fighter.Health)}"
                        : string.Join(", ", fighter.CreatureRoster.Select(creature => $"{(creature.IsActive ? ">" : creature.IsResting ? "~" : "")}{creature.Name}:{Math.Max(0, creature.Health)}"));
                    builder.AppendLine($"- trainer `{fighter.TrainerModelName}` · active **{fighter.CreatureName}** {Math.Max(0, fighter.Health)} HP · team [{roster}] · switches {fighter.SwitchesUsedInCurrentFight}/{session.TournamentRules.MaximumCreatureSwitchesPerFight} · {fighter.Wins} win(s) · {status} · creature model `{fighter.CreatureModelName}`");
                }
                if (session.TournamentTimeline.Count > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine("Recent engine timeline:");
                    foreach (var entry in session.TournamentTimeline.TakeLast(5))
                        builder.AppendLine($"- T{entry.Sequence:00} R{entry.Round}E{entry.Exchange} {entry.EventKind}: {entry.Detail}");
                }
                builder.AppendLine();
                if (!string.IsNullOrWhiteSpace(session.TournamentChampionFighterId))
                {
                    var champion = session.TournamentFighters.FirstOrDefault(item => string.Equals(item.FighterId, session.TournamentChampionFighterId, StringComparison.OrdinalIgnoreCase));
                    builder.AppendLine($"Champion: **{champion?.CreatureName ?? "Unknown"}** with trainer `{champion?.TrainerModelName ?? "Unknown"}` · imaginary ceremonial prize awarded.");
                    builder.AppendLine("[[TOURNAMENT_COMPLETE]]");
                }
                else
                {
                    if (session.TournamentMatchIndex + 1 < session.TournamentRoundFighterIds.Count)
                    {
                        var left = session.TournamentFighters.First(item => string.Equals(item.FighterId, session.TournamentRoundFighterIds[session.TournamentMatchIndex], StringComparison.OrdinalIgnoreCase));
                        var right = session.TournamentFighters.First(item => string.Equals(item.FighterId, session.TournamentRoundFighterIds[session.TournamentMatchIndex + 1], StringComparison.OrdinalIgnoreCase));
                        builder.AppendLine($"Current legal match: trainer **{ShortSummaryModel(left.TrainerModelName)}** / **{left.CreatureName}** vs trainer **{ShortSummaryModel(right.TrainerModelName)}** / **{right.CreatureName}**.");
                    }
                    builder.AppendLine(initialized ? "The first exchange may begin." : "The next bounded trainer action and creature exchange may begin.");
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

        /// <summary>Returns a short model label for the transcript summary without exposing endpoint noise.</summary>
        private string ShortSummaryModel(string value)
        {
            try
            {
                var model = (value ?? string.Empty).Trim();
                var separator = model.LastIndexOf(" — ", StringComparison.Ordinal);
                if (separator >= 0 && separator + 3 < model.Length)
                    model = model[(separator + 3)..];
                var at = model.IndexOf(" @ ", StringComparison.Ordinal);
                if (at > 0)
                    model = model[..at];
                return model.Length <= 20 ? model : model[..20];
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Formatting a Kernel Creature Tournament model label failed.");
                throw;
            }
        }
    }
}

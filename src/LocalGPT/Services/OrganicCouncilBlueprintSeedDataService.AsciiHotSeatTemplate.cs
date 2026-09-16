using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>
/// Adds the maintained hot-seat multiplayer ASCII showcase team to the default Council seed catalog.
/// </summary>
public sealed partial class OrganicCouncilBlueprintSeedDataService
{
    /// <summary>
    /// Creates a two-human hot-seat arena that deliberately exercises LocalGPT's full ASCII presentation surface.
    /// </summary>
    /// <returns>The seeded hot-seat ASCII showcase team.</returns>
    private OrganicCouncilTeamDefinition CreateAsciiHotSeatShowcaseTeam()
    {
        try
        {
            const string rendererName = "ASCII Hot Seat Display Director";
            return new OrganicCouncilTeamDefinition
            {
                Key = "ascii-hot-seat-showcase",
                DisplayName = "ASCII Hot Seat — Neon Relay Arena",
                Purpose = "A two-player local hot-seat Council game. Player 1 and Player 2 physically share the keyboard, alternate human-only turns, and compete in a deterministic neon arena while one AI referee owns game state and one AI display director showcases full-frame ASCII, incremental cell/text/fill/blit drawing, HUD updates and pregenerated browser-local animation.",
                AllMembersReadinessPreflightMode = CouncilAllMembersReadinessPreflightMode.Disabled,
                Roles =
                [
                    new OrganicCouncilRoleDefinition
                    {
                        Role = "Player 1 Hot Seat",
                        Expertise = "local human game input",
                        Responsibility = "When this seat is requested, pass the keyboard to Player 1, inspect the current ASCII arena, and enter exactly one command: MOVE N|S|E|W, DASH N|S|E|W, EMOTE <short text>, or PASS. DASH is available once per match. Do not decide Player 2's move or referee the result.",
                        HumanParticipationMode = HumanParticipationMode.HumanOnly,
                        PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                        LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                        BoundaryMode = CouncilRoleBoundaryMode.Strict
                    },
                    new OrganicCouncilRoleDefinition
                    {
                        Role = "Player 2 Hot Seat",
                        Expertise = "local human game input",
                        Responsibility = "When this seat is requested, pass the keyboard to Player 2, inspect the current ASCII arena, and enter exactly one command: MOVE N|S|E|W, DASH N|S|E|W, EMOTE <short text>, or PASS. DASH is available once per match. Do not decide Player 1's move or referee the result.",
                        HumanParticipationMode = HumanParticipationMode.HumanOnly,
                        PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                        LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                        BoundaryMode = CouncilRoleBoundaryMode.Strict
                    },
                    new OrganicCouncilRoleDefinition
                    {
                        Role = "Arena Referee",
                        Expertise = "deterministic turn resolution, scorekeeping, collision rules and compact machine-readable state",
                        Responsibility = "Own the only canonical Neon Relay Arena state. Resolve both human commands together, preserve exact positions/scores/dash charges, emit one HOTSEAT_STATE line every round, and never change presentation pixels directly.",
                        AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                        MinimumAiParticipants = 1,
                        MaximumAiParticipants = 1,
                        DistinctAiAssignmentGroup = "ascii-hot-seat-runtime",
                        AllowDistinctAiAssignmentFallback = true,
                        PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                        BoundaryMode = CouncilRoleBoundaryMode.Strict
                    },
                    new OrganicCouncilRoleDefinition
                    {
                        Role = "ASCII Display Director",
                        Expertise = "fixed-cell terminal composition, HUD layout, sprites, incremental drawing and short ASCII animation",
                        Responsibility = $"Own all display mutations using rendererName '{rendererName}'. Treat the referee's HOTSEAT_STATE as authoritative, never change scores or movement, and use the smallest suitable ASCII DXFunction while preserving a readable chat fallback.",
                        AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                        MinimumAiParticipants = 1,
                        MaximumAiParticipants = 1,
                        DistinctAiAssignmentGroup = "ascii-hot-seat-runtime",
                        AllowDistinctAiAssignmentFallback = true,
                        PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                        BoundaryMode = CouncilRoleBoundaryMode.Strict
                    }
                ],
                PreferredCapabilities =
                [
                    "localgpt.ascii.surface.get",
                    "localgpt.game.session.start",
                    "localgpt.game.session.get",
                    "localgpt.game.input-gate.set",
                    "localgpt.game.display.get",
                    "localgpt.game.display.text.write",
                    "localgpt.game.display.cell.set",
                    "localgpt.game.display.region.fill",
                    "localgpt.game.display.region.blit",
                    "localgpt.game.frame.submit",
                    "localgpt.game.animation.submit",
                    "localgpt.regex.list",
                    "localgpt.regex.get",
                    "localgpt.regex.test",
                    "localgpt.knowledge.list"
                ],
                AllowedAutomaticFunctions =
                [
                    "localgpt.ascii.surface.get",
                    "localgpt.game.session.start",
                    "localgpt.game.session.get",
                    "localgpt.game.input-gate.set",
                    "localgpt.game.display.get",
                    "localgpt.game.display.text.write",
                    "localgpt.game.display.cell.set",
                    "localgpt.game.display.region.fill",
                    "localgpt.game.display.region.blit",
                    "localgpt.game.frame.submit",
                    "localgpt.game.animation.submit",
                    "localgpt.regex.list",
                    "localgpt.regex.get",
                    "localgpt.regex.test",
                    "localgpt.knowledge.list"
                ],
                WorkflowSteps =
                [
                    new CouncilWorkflowStepDefinition
                    {
                        Key = "hotseat-boot",
                        DisplayName = "Boot the hot-seat arena",
                        SortOrder = 10,
                        Phase = "Arena boot",
                        Role = "Arena Referee",
                        ExecutionMode = "LeaderSingle",
                        IncludePriorTranscript = false,
                        CanUseOrganicFunctions = true,
                        AutomaticFunctionPolicyMode = CouncilAutomaticFunctionPolicyMode.ExactAllowList,
                        AllowedAutomaticFunctions = ["localgpt.ascii.surface.get", "localgpt.game.session.start", "localgpt.game.session.get", "localgpt.game.input-gate.set", "localgpt.knowledge.list"],
                        PromptTemplate = """
Create a fresh Neon Relay Arena match. First inspect localgpt.ascii.surface.get. Then call localgpt.game.session.start exactly once with gameKey="ascii-doom", teamKey="ascii-hot-seat-showcase", controlMode="Shared", directorMode="Deterministic", frameWidth=100 and frameHeight=32. This underlying game session is a presentation host only: DO NOT call localgpt.game.control in this team. Immediately call localgpt.game.input-gate.set with humanInputRequired=false and reason="Hot-seat input is collected by HumanOnly Council roles" so the single-player game overlay does not compete with the two hot-seat prompts. Copy the returned exact sessionId, turn, width and height into one line:
DISPLAY_HOST sessionId=<guid> turn=<integer> width=<integer> height=<integer>

Initialize the Council-owned multiplayer state exactly as:
HOTSEAT_STATE round=0 p1=(3,6) p2=(27,6) p1Score=0 p2Score=0 p1Dash=1 p2Dash=1 orb=(15,6) winner=none

Rules for every later round:
- Logical arena coordinates are x=1..29 and y=1..11. Border cells outside that range are walls.
- MOVE N/S/E/W moves one cell. DASH N/S/E/W moves up to two legal cells and consumes that player's one dash charge. EMOTE and PASS do not move.
- Resolve both players from the same prior state, then apply both moves simultaneously.
- If both would finish on the same cell, both bounce back to their prior cells.
- Landing on the orb scores +2. If both legally land on the orb in the same round, each scores +1.
- After an orb is collected, advance to the next spawn in this cycle: (15,3), (15,9), (9,3), (21,9), (9,9), (21,3), (15,6), then repeat.
- First player to 6 points wins. Otherwise after round 10, higher score wins; equal score is a draw.
- No randomness, hidden rules, invented bonuses or retroactive corrections.
Do not render yet. End by saying the display director may build the intro and Player 1 takes the first hot seat.
""",
                        UseBuiltInBehavior = false
                    },
                    new CouncilWorkflowStepDefinition
                    {
                        Key = "hotseat-intro-display",
                        DisplayName = "Render the animated arena intro",
                        SortOrder = 20,
                        Phase = "ASCII showcase intro",
                        Role = "ASCII Display Director",
                        ExecutionMode = "LeaderSingle",
                        IncludePriorTranscript = true,
                        CanUseOrganicFunctions = true,
                        AutomaticFunctionPolicyMode = CouncilAutomaticFunctionPolicyMode.ExactAllowList,
                        AllowedAutomaticFunctions = ["localgpt.game.session.get", "localgpt.game.display.get", "localgpt.game.frame.submit", "localgpt.game.animation.submit", "localgpt.regex.list", "localgpt.regex.get", "localgpt.regex.test", "localgpt.knowledge.list"],
                        PromptTemplate = $$"""
Build the opening presentation for the exact DISPLAY_HOST and HOTSEAT_STATE from the prior step. Use rendererName "{{rendererName}}" for every display mutation in this entire match. Read the session first. Consult localgpt.regex.list/get/test or localgpt.knowledge.list only when useful for reusable frame/state parsing; never invent a function name.

Showcase the full-frame path first: submit one complete 100x32 frame with localgpt.game.frame.submit. It should contain a bordered 31x13 logical arena enlarged/positioned cleanly inside the terminal, title art, Player 1 '@', Player 2 '&', orb '*', score/dash HUD, a compact command legend, and a large "PASS TO PLAYER 1" cue. Keep all lines fixed-width and readable.

Then showcase browser-local animation: pregenerate 3-5 complete 100x32 frames that animate a short neon power-up/title effect without changing game state and submit them once through localgpt.game.animation.submit with a brisk delay. The first/stable animation frame must remain a valid arena view. Never call a model per animation frame.

Visible answer: one short welcome line, the controls, and "Player 1: take the keyboard." Do not invent or resolve a player move.
""",
                        UseBuiltInBehavior = false
                    },
                    new CouncilWorkflowStepDefinition
                    {
                        Key = "hotseat-player-1",
                        DisplayName = "Player 1 hot seat",
                        SortOrder = 30,
                        Phase = "Pass keyboard to Player 1",
                        Role = "Player 1 Hot Seat",
                        ExecutionMode = "LeaderSingle",
                        IncludePriorTranscript = true,
                        LoopGroup = "neon-hot-seat-match",
                        MaximumLoopIterations = 10,
                        CanUseOrganicFunctions = false,
                        AutomaticFunctionPolicyMode = CouncilAutomaticFunctionPolicyMode.Disabled,
                        UseBuiltInBehavior = false
                    },
                    new CouncilWorkflowStepDefinition
                    {
                        Key = "hotseat-player-2",
                        DisplayName = "Player 2 hot seat",
                        SortOrder = 40,
                        Phase = "Pass keyboard to Player 2",
                        Role = "Player 2 Hot Seat",
                        ExecutionMode = "LeaderSingle",
                        IncludePriorTranscript = true,
                        LoopGroup = "neon-hot-seat-match",
                        MaximumLoopIterations = 10,
                        CanUseOrganicFunctions = false,
                        AutomaticFunctionPolicyMode = CouncilAutomaticFunctionPolicyMode.Disabled,
                        UseBuiltInBehavior = false
                    },
                    new CouncilWorkflowStepDefinition
                    {
                        Key = "hotseat-referee",
                        DisplayName = "Resolve both hot-seat moves",
                        SortOrder = 50,
                        Phase = "Deterministic arena resolution",
                        Role = "Arena Referee",
                        ExecutionMode = "LeaderSingle",
                        IncludePriorTranscript = true,
                        TranscriptVisibility = CouncilTranscriptVisibilityMode.FullCouncil,
                        LoopGroup = "neon-hot-seat-match",
                        MaximumLoopIterations = 10,
                        LoopCompletionMarker = "[[HOTSEAT_GAME_OVER]]",
                        CanUseOrganicFunctions = false,
                        AutomaticFunctionPolicyMode = CouncilAutomaticFunctionPolicyMode.Disabled,
                        PromptTemplate = """
Resolve exactly one Neon Relay Arena round from the latest prior HOTSEAT_STATE and the two newest human-only contributions for Player 1 Hot Seat and Player 2 Hot Seat. Parse commands conservatively; malformed input becomes PASS. Apply the boot rules exactly and simultaneously. Do not mutate the ASCII display.

Output exactly one canonical state line in this shape:
HOTSEAT_STATE round=<1..10> p1=(x,y) p2=(x,y) p1Score=<n> p2Score=<n> p1Dash=<0|1> p2Dash=<0|1> orb=(x,y) winner=<none|p1|p2|draw>
Then give at most three short referee bullets: normalized actions, scoring/collision result, and next objective.
If a player reached 6 points OR this is round 10, append exactly [[HOTSEAT_GAME_OVER]]. Otherwise do not emit that marker.
""",
                        UseBuiltInBehavior = false
                    },
                    new CouncilWorkflowStepDefinition
                    {
                        Key = "hotseat-display",
                        DisplayName = "Render the resolved multiplayer turn",
                        SortOrder = 60,
                        Phase = "ASCII arena rendering",
                        Role = "ASCII Display Director",
                        ExecutionMode = "LeaderSingle",
                        IncludePriorTranscript = true,
                        TranscriptVisibility = CouncilTranscriptVisibilityMode.FullCouncil,
                        LoopGroup = "neon-hot-seat-match",
                        MaximumLoopIterations = 10,
                        CanUseOrganicFunctions = true,
                        AutomaticFunctionPolicyMode = CouncilAutomaticFunctionPolicyMode.ExactAllowList,
                        AllowedAutomaticFunctions =
                        [
                            "localgpt.game.session.get",
                            "localgpt.game.display.get",
                            "localgpt.game.display.text.write",
                            "localgpt.game.display.cell.set",
                            "localgpt.game.display.region.fill",
                            "localgpt.game.display.region.blit",
                            "localgpt.game.frame.submit",
                            "localgpt.game.animation.submit",
                            "localgpt.regex.list",
                            "localgpt.regex.get",
                            "localgpt.regex.test",
                            "localgpt.knowledge.list"
                        ],
                        ProducesFinalAnswer = true,
                        ProducesAsciiFrame = true,
                        AsciiFrameWidth = 100,
                        AsciiFrameHeight = 32,
                        PromptTemplate = $$"""
Render the newest canonical HOTSEAT_STATE to the exact DISPLAY_HOST. You are the only display owner and MUST use rendererName "{{rendererName}}" for every mutation. Never call localgpt.game.control; that single-player engine is not the multiplayer authority here.

Read localgpt.game.session.get and localgpt.game.display.get before editing so you know the exact session turn and current pixels. Demonstrate efficient differential drawing rather than blindly regenerating a full frame every round:
1. Use localgpt.game.display.region.fill to clear/redraw only changing arena/HUD regions when practical.
2. Use localgpt.game.display.region.blit for medium sprites, borders, banners, impact bursts or the command panel.
3. Use localgpt.game.display.text.write for scoreboard, round, orb coordinates, dash charges, referee caption and the next-seat cue.
4. Use localgpt.game.display.cell.set for the exact '@', '&' and '*' gameplay glyphs after clearing their previous cells.
5. Use localgpt.game.frame.submit only when a genuine whole-screen scene change is cleaner (for example final victory tableau or recovery from bad continuity).
6. On an orb score, bounce collision, final result, or every third round, pregenerate 2-6 complete frames and submit exactly one localgpt.game.animation.submit call. Keep the frames state-consistent; animation is presentation only and browser-local.
7. Database-backed localgpt.regex.list/get/test and localgpt.knowledge.list may be used to reuse parsing/layout facts instead of recreating them. Do not persist new regexes from this game.

Visual design: neon arcade border, readable 31x13 arena, '@' Player 1, '&' Player 2, '*' orb, score bars, remaining DASH indicators, round counter, compact event log and an obvious "PASS TO PLAYER 1" cue whenever the match continues. Use simple ASCII characters that survive monospace rendering.

Visible chat answer must remain compact: one line with score/round, one line describing the resolved event, and either "Pass the keyboard to Player 1." or the final winner/draw statement. If the referee emitted [[HOTSEAT_GAME_OVER]], make the final display a celebratory full-frame/animation showcase and do not ask for another move.
""",
                        UseBuiltInBehavior = false
                    }
                ],
                MainRoundInstructionTemplate = "This is a real local hot-seat loop: Player 1 and Player 2 are separate human-only Council roles and must alternate at the same machine. The Arena Referee owns canonical multiplayer state; the ASCII Display Director owns presentation only. The display session is a fixed-cell presentation host and must never be mistaken for the multiplayer rules engine.",
                ArchitectureContracts =
                [
                    .. DefaultArchitectureContracts(),
                    "Player 1 and Player 2 are HumanOnly roles. LocalGPT must pause for each seat rather than simulate a missing player command.",
                    "The latest HOTSEAT_STATE emitted by the Arena Referee is the only canonical multiplayer state. Display pixels and animation frames are views, never authority.",
                    "The underlying ascii-doom Council game session is used only as the supported fixed-cell display host. This team never advances it through localgpt.game.control.",
                    $"Exactly one ASCII renderer identity ('{rendererName}') owns all display mutations for the presentation-host turn so incremental writes and animations cannot fight over frame ownership.",
                    "Animations are pregenerated bounded 2-12 frame sequences submitted once for browser-local playback; no per-frame model calls are allowed.",
                    "The showcase should exercise full-frame, text, cell, fill, blit, display-read and animation paths while keeping the visible chat concise and playable."
                ]
            };
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Creating the ASCII hot-seat showcase team was canceled.");
            else
                logger.LogError(exception, "Creating the ASCII hot-seat showcase team failed.");
            throw;
        }
    }
}

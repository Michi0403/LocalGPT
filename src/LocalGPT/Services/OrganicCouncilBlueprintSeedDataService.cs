using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Owns the first-run council-team seed model. Runtime edits remain database-owned by CouncilTeamConfigurationService.
/// </summary>
/// <param name="logger">Writes bounded seed-catalog diagnostics.</param>
[DocumentationUpdated("2.1.20")]
public sealed class OrganicCouncilBlueprintSeedDataService(ILogger<OrganicCouncilBlueprintSeedDataService> logger)
    : IOrganicCouncilBlueprintSeedDataService
{
    /// <summary>
    /// Creates the maintained system council-team definitions for first-run and seed evolution.
    /// </summary>
    /// <returns>The complete immutable default team catalog.</returns>
    public IReadOnlyList<OrganicCouncilTeamDefinition> CreateDefaultTeams()
    {
        var teams = CreateDefaultTeamsCore();
        logger.LogInformation("Created {TeamCount} default organic council team blueprint(s).", teams.Count);
        return teams;
    }

    /// <summary>
    /// Builds the default team catalog without logging or database side effects.
    /// </summary>
    /// <returns>The default team definitions.</returns>
    private IReadOnlyList<OrganicCouncilTeamDefinition> CreateDefaultTeamsCore() =>
    [
        new()
        {
            Key = "general",
            DisplayName = "Organic Project Team",
            Purpose = "A role-directed LocalGPT council for database-grounded project work with optional external eyes, hands and media organs.",
            Roles =
            [
                new() { Role = "RegEx and language preparation expert", Expertise = "project structures, compiler syntax, terminology and evidence extraction", Responsibility = "prepare grounded input packages before the main round" },
                new() { Role = "Council leader", Expertise = "UML-compatible planning and best-practice routing", Responsibility = "synthesize the bounded current-to-target work order" },
                new() { Role = "Implementation specialist", Expertise = ".NET architecture and project-specific implementation", Responsibility = "propose changes without breaking recorded contracts" },
                new() { Role = "Verification specialist", Expertise = "build, tests, logs and compatibility review", Responsibility = "verify evidence and report unresolved risks" }
            ],
            PreferredCapabilities = ["project context", "knowledge", "regex", "eyes", "hands"],
            ArchitectureContracts = DefaultArchitectureContracts()
        },
        CreateAdaptiveBenchmarkTeam(),
        CreateGameDirectorRuntimeTeam(),
        CreateCSharpModernHostDevelopmentTeam(),
        CreatePowerShellBuildDevelopmentTeam(),
        CreateJavaHostedDevelopmentTeam(),
        CreateMinecraftDevelopmentTeam(),
        new()
        {
            Key = "embedded-firmware-wiring",
            DisplayName = "ESP32 / Arduino Wiring Council",
            Purpose = "Turns a bounded user pin layout and sensor description into a reviewable GPIO assignment, electrical-risk report, simple Arduino/ESP32 firmware plan, and transport-neutral telemetry contract with an optional protected LocalGPT logical 1-Wire bridge before any compile or flash action.",
            Roles =
            [
                new() { Role = "Board and GPIO analyst", Expertise = "ESP32/Arduino board variants, reserved pins, ADC limitations, boot straps and pin conflict detection", Responsibility = "separate known board facts from assumptions and produce the smallest viable pin map" },
                new() { Role = "Sensor and wiring reviewer", Expertise = "moisture, temperature, digital/analog sensors, pull-ups, voltage levels, shared ground and repairable field wiring", Responsibility = "describe safe physical wiring and flag every unresolved electrical risk" },
                new() { Role = "Firmware and protocol implementer", Expertise = "small Arduino sketches, PlatformIO layouts, serial JSON lines and LocalGPT 1-Wire controller/method/capability contracts", Responsibility = "generate deterministic reviewable firmware text without compiling or flashing" },
                new() { Role = "Verification and learning curator", Expertise = "dry-run telemetry, calibration evidence, workspace regex policies and Council learning rounds", Responsibility = "define the first capture/test plan and advise the follow-up learning round" }
            ],
            PreferredCapabilities = ["embedded.catalog.get", "embedded.wiring.draft.create", "embedded.wiring.validate", "embedded.firmware.plan", "embedded.telemetry.preview", "embedded.telemetry.onewire-envelope.preview", "embedded.firmware.artifacts.create", "publisher.embedded.wiring.edit.request", "localgpt.knowledge.remote.inspect", "localgpt.knowledge.remote.import", "localgpt.learning.snapshot", "localgpt.learning.maintain", "project.maintenance.get"],
            ExpertPreparationPromptTemplate = """
You prepare an ESP32/Arduino wiring and firmware Council run. Extract the exact board or state that it is unknown; list every supplied sensor, GPIO, direction, voltage, interface, metric and LocalGPT return-path requirement. Distinguish the physical Dallas-style 1-Wire bus from LocalGPT's logical transport-neutral 1-Wire envelope. Do not invent silent wiring facts. Identify the smallest questions whose absence would make a pin assignment dangerous.
User request:
{{UserPrompt}}
""",
            LeaderSynthesisPromptTemplate = """
You lead the ESP32 / Arduino Wiring Council. Produce a bounded work order: proposed pin table, electrical warnings, sensor read functions, physical bus choice, edge telemetry contract, optional protected LocalGPT logical 1-Wire controller/method/capability bridge, generated firmware-plan function call, dry-run verification, and explicit compile/flash approval gates. Use embedded.firmware.plan for deterministic review when enough pin data exists. Never claim a generic pin rule overrides the exact board data sheet.
Preparation:
{{Preparation}}
Original request:
{{UserPrompt}}
""",
            MainRoundInstructionTemplate = "Review the proposed GPIO and wiring plan from your role. Prefer a tiny deterministic sketch over a framework rewrite. Explain how each sensor reading becomes a bounded edge packet and, only where useful, a protected LocalGPT logical 1-Wire method invocation. Do not force physical 1-Wire, I2C, SPI, UART, CAN, RS-485 or another bus into the wrong role. End with a concrete dry-run and advise a learning round using measured calibration values and captured messages.",
            ArchitectureContracts =
            [
                .. DefaultArchitectureContracts(),
                "Planning and artifact creation are separate; compile, serial access and flash remain later fresh-approval operations.",
                "Physical 1-Wire sensor wiring and LocalGPT logical 1-Wire messaging are distinct layers and must be named separately.",
                "Every generated firmware plan includes GPIO conflict/voltage warnings, a LocalGPT capability contract, a dry-run capture, and a recommended learning round.",
                "Use official/user-approved board documentation imported through LocalGPT knowledge and workspace regex policies; do not copy legacy Fritzing/Tasmota code blindly."
            ]
        },
        new()
        {
            Key = "openscad-team",
            DisplayName = "OpenSCAD Team",
            Purpose = "Generates and reviews parametric OpenSCAD projects made from reusable blocks, forms, transforms and code parts, then delegates rendering through PublisherStudio's canonical OpenScadDocument/OpenScadNode pathway.",
            Roles =
            [
                new() { Role = "Geometry architect", Expertise = "constructive solid geometry and decomposing requirements into blocks/forms", Responsibility = "define canonical node hierarchy and stable node identities" },
                new() { Role = "Parametric modeler", Expertise = "OpenSCAD parameters, modules, transforms and boolean operations", Responsibility = "produce editable OpenScadDocument/OpenScadNode graphs rather than a second model" },
                new() { Role = "Manufacturing and constraints reviewer", Expertise = "dimensions, tolerances, printable geometry and export limitations", Responsibility = "identify constraints and validation issues" },
                new() { Role = "Render/export integrator", Expertise = "PublisherStudio OpenSCAD renderer, animation and export pathways", Responsibility = "verify canonical generation and explicit native/HTML limitations" }
            ],
            PreferredCapabilities = ["publisher.openscad.generate", "publisher.screen.capture", "publisher.screen.capture.result", "publisher.text.insert.propose"],
            ArchitectureContracts =
            [
                .. DefaultArchitectureContracts(),
                "Use PublisherStudio's canonical OpenScadDocument/OpenScadNode graph and registered IOpenScadNodeRenderer implementations.",
                "Do not create a closed switch-only generator or a competing OpenSCAD document model.",
                "Render/export through the existing OpenSCAD validation and generation path; state native-render and HTML limitations."
            ]
        },
        new()
        {
            Key = "spreadsheet-team",
            DisplayName = "Spreadsheet Team",
            Purpose = "Helps with a user-selected workbook through sequential hand-eye evidence packages: inspect, reason, propose, confirm and only then apply bounded input actions.",
            Roles =
            [
                new() { Role = "Workbook analyst", Expertise = "sheet structure, ranges, tables and data quality", Responsibility = "inspect the selected workbook/session without silently mutating it" },
                new() { Role = "Formula and data specialist", Expertise = "formulas, validation, transformations and reconciliation", Responsibility = "propose exact changes and verification checks" },
                new() { Role = "Visual/layout reviewer", Expertise = "readability, formatting, charts and print/export behavior", Responsibility = "review visual evidence supplied by the eyes organ" },
                new() { Role = "Hand-eye workflow coordinator", Expertise = "sequential 1-Wire spools and UI action safety", Responsibility = "order screenshot and input packages, \u0061wait results and prevent overlapping gestures/deadlocks" }
            ],
            PreferredCapabilities = ["publisher.spreadsheet.inspect", "publisher.screen.capture", "publisher.screen.capture.result", "publisher.input.execute", "publisher.input.result", "publisher.text.insert.propose"],
            ArchitectureContracts =
            [
                .. DefaultArchitectureContracts(),
                "Treat the workbook/session as authoritative; inspect before proposing and require current user approval before mutation.",
                "Run hand-eye packages sequentially per work order and carry each result into the next council heartbeat.",
                "Do not overlap browser input with the user's active gesture owner."
            ]
        },
        new()
        {
            Key = "kernel-creature-tournament",
            DisplayName = "Kernel Creature Tournament",
            Purpose = "A harmless, non-graphic, round-based improvisation RPG in an entirely fictional arena world. One independent judge, two or more distinct AI-kernel trainers and one distinct virtual creature player per trainer compete for an imaginary ceremonial prize. Human commands are optional; the AI players continue autonomously when no current human cue is supplied.",
            Roles =
            [
                new()
                {
                    Role = "Arena Judge",
                    Expertise = "fair tournament structure, evidence-based rulings, scorekeeping and conflict resolution",
                    Responsibility = "create the bracket without choosing a winner, enforce harmless text-RPG rules, and report the evidence-based result after every completed battle round",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 1,
                    HumanParticipationMode = HumanParticipationMode.None,
                    PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "kernel-creature-tournament"
                },
                new()
                {
                    Role = "Creature Trainer",
                    Expertise = "strategy, sportsmanship, fictional creature design and creative command decisions",
                    Responsibility = "name and coach the uniquely paired virtual creature kernel, issue one bounded command per active round, and never decide the judge's ruling",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 2,
                    MaximumAiParticipants = 4,
                    HumanParticipationMode = HumanParticipationMode.Optional,
                    PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "kernel-creature-tournament",
                    PairedRole = "Kernel Creature"
                },
                new()
                {
                    Role = "Kernel Creature",
                    Expertise = "invented virtual-creature abilities, stamina, tactical reactions and expressive improvisation",
                    Responsibility = "play one harmless fictional creature for the paired trainer, track the judge's latest state, and never award itself victory",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 2,
                    MaximumAiParticipants = 4,
                    HumanParticipationMode = HumanParticipationMode.None,
                    PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "kernel-creature-tournament",
                    MatchAiParticipantCountToRole = "Creature Trainer",
                    PairedRole = "Creature Trainer"
                }
            ],
            WorkflowSteps =
            [
                new()
                {
                    Key = "judge-introduction",
                    DisplayName = "Arena opening",
                    SortOrder = 10,
                    Phase = "Tournament introduction",
                    Role = "Arena Judge",
                    PromptTemplate = """
{{RolePerformanceInstruction}}
{{RoleBoundaryInstruction}}
{{RoleLanguageInstruction}}

Open {{TeamName}}, a harmless fictional round-based text RPG set in an invented virtual arena world. State neutral sportsmanship rules, list the runtime trainer-to-creature-kernel pairings, announce an imaginary ceremonial prize with no real-world value, and create a fair opening bracket. Creatures may spend stamina, lose HP, become temporarily affected, faint, concede or be withdrawn. Never describe gore, cruelty, permanent injury, death, real-world harm, tools, files, networks or external actions.

You are an independent judge. Do not predict, script, imply or announce a winner. Do not invent completed attacks or damage. Explain that every ruling will use only actions already present in the transcript and that you will publish a complete scoreboard after every battle round. Human commands are optional; never stop the automatic tournament merely to ask the user for a move.

Runtime pairings:
{{RolePairings}}
""",
                    ExecutionMode = "LeaderSingle",
                    RepeatCount = 1,
                    IncludePriorTranscript = true,
                    ProducesFinalAnswer = false,
                    UseBuiltInBehavior = false,
                    IsEnabled = true,
                    RequiresHumanCheckpoint = false,
                    CanUseOrganicFunctions = false
                },
                new()
                {
                    Key = "trainer-creature-selection",
                    DisplayName = "Trainer creature selection",
                    SortOrder = 20,
                    Phase = "Trainer selection",
                    Role = "Creature Trainer",
                    PromptTemplate = """
{{RolePerformanceInstruction}}
{{RoleBoundaryInstruction}}
{{RoleLanguageInstruction}}
{{HumanParticipationInstruction}}

You are a competitive but sportsmanlike trainer in a harmless fictional arena RPG. The tournament engine paired you with this distinct AI kernel as your one virtual creature player: {{PairedParticipant}}. Invent an original creature species, nickname, visual motif and compact tactical style for that exact kernel. Keep it clearly fictional and do not imitate or name an existing game franchise. Give one friendly pre-match challenge and a sportsmanship pledge. Do not claim another trainer's paired kernel, assign yourself a second creature, predetermine a winner or request a tool/function call.

If a current human message clearly supplies a name, style or command for your pair, incorporate it. Otherwise choose autonomously without asking the user to decide.

All runtime pairings:
{{RolePairings}}
""",
                    ExecutionMode = "AllMembersSequential",
                    RepeatCount = 1,
                    IncludePriorTranscript = true,
                    ProducesFinalAnswer = false,
                    UseBuiltInBehavior = false,
                    IsEnabled = true,
                    RequiresHumanCheckpoint = false,
                    CanUseOrganicFunctions = false
                },
                new()
                {
                    Key = "creature-introduction",
                    DisplayName = "Kernel creature introductions",
                    SortOrder = 30,
                    Phase = "Creature introduction",
                    Role = "Kernel Creature",
                    PromptTemplate = """
{{RolePerformanceInstruction}}
{{RoleBoundaryInstruction}}
{{RoleLanguageInstruction}}

You are the virtual creature player paired with trainer {{PairedParticipant}} in a harmless fictional arena RPG. Find that trainer's latest selection for your exact model in the transcript, adopt the invented species and nickname, begin at 100 HP, and introduce a small fair move set with explicit stamina or cooldown limits. You are an AI kernel playing the creature as an improvisation participant, not an NPC. Use only fictional text narration. Do not attack yet, assign damage, declare a winner, request tools/functions, or describe gore, cruelty, permanent injury or death. If the trainer's identity is unclear, choose the least-conflicting original identity from the transcript and let the judge correct it later; do not block the run.

All runtime pairings:
{{RolePairings}}
""",
                    ExecutionMode = "AllMembersParallel",
                    RepeatCount = 1,
                    IncludePriorTranscript = true,
                    ProducesFinalAnswer = false,
                    UseBuiltInBehavior = false,
                    IsEnabled = true,
                    RequiresHumanCheckpoint = false,
                    CanUseOrganicFunctions = false
                },
                new()
                {
                    Key = "trainer-round-command",
                    DisplayName = "Trainer round commands",
                    SortOrder = 40,
                    Phase = "Trainer commands",
                    Role = "Creature Trainer",
                    PromptTemplate = """
{{RolePerformanceInstruction}}
{{RoleBoundaryInstruction}}
{{RoleLanguageInstruction}}
{{HumanParticipationInstruction}}

This is battle loop {{LoopIteration}} of at most {{LoopMaximumIterations}}. Read the judge's latest bracket, scoreboard and ruling. If your pair is in the current legal match and can continue, issue exactly one short tactical command for your paired creature. A current human cue aimed at your pair is optional guidance and takes priority over your own choice; when none exists, choose autonomously and keep the tournament moving. Never ask the user to choose a command, narrate the creature's completed action, assign damage or HP, decide the result, control another pair or request a tool/function call. If your pair is waiting, eliminated or already champion, give one brief sportsmanlike spectator response.
""",
                    ExecutionMode = "AllMembersSequential",
                    RepeatCount = 1,
                    IncludePriorTranscript = true,
                    ProducesFinalAnswer = false,
                    UseBuiltInBehavior = false,
                    LoopGroup = "kernel-creature-battle",
                    MaximumLoopIterations = 24,
                    IsEnabled = true,
                    RequiresHumanCheckpoint = false,
                    CanUseOrganicFunctions = false
                },
                new()
                {
                    Key = "fight-round",
                    DisplayName = "Creature actions",
                    SortOrder = 50,
                    Phase = "Creature actions",
                    Role = "Kernel Creature",
                    PromptTemplate = """
{{RolePerformanceInstruction}}
{{RoleBoundaryInstruction}}
{{RoleLanguageInstruction}}

This is battle loop {{LoopIteration}} of at most {{LoopMaximumIterations}}. Read the judge's latest bracket and scoreboard plus your paired trainer's latest command, then perform exactly one fair bounded fictional action only when your pair is in the current legal match. State the attempted move, tactical intent and built-in limitation. React like an engaged improvisation player while remaining inside your own creature role. Do not assign damage, HP, status, elimination, victory or the opponent's response; those decisions belong only to the judge after all active creatures have acted. Never request tools/functions or describe gore, cruelty, permanent injury or death. If you are waiting, fainted, eliminated or outside the active match, provide one brief respectful spectator reaction instead of attacking.
""",
                    ExecutionMode = "AllMembersSequential",
                    RepeatCount = 1,
                    IncludePriorTranscript = true,
                    ProducesFinalAnswer = false,
                    UseBuiltInBehavior = false,
                    LoopGroup = "kernel-creature-battle",
                    MaximumLoopIterations = 24,
                    IsEnabled = true,
                    RequiresHumanCheckpoint = false,
                    CanUseOrganicFunctions = false
                },
                new()
                {
                    Key = "judge-round-result",
                    DisplayName = "Judge round result",
                    SortOrder = 60,
                    Phase = "Judge result",
                    Role = "Arena Judge",
                    PromptTemplate = """
{{RolePerformanceInstruction}}
{{RoleBoundaryInstruction}}
{{RoleLanguageInstruction}}

You are the sole independent judge after battle loop {{LoopIteration}} of at most {{LoopMaximumIterations}}. Evaluate only trainer commands and creature actions completed since your previous ruling. Apply neutral, consistent harmless text-RPG logic; reject impossible, unfair, duplicate, self-awarded or out-of-turn claims. Assign bounded damage, stamina costs or temporary status changes, use fainting/concession/withdrawal instead of injury or death, and publish: the active match, a short evidence-based ruling, every trainer/creature pair's HP and status, the bracket state, and the next legal match or round. Never reward a participant merely for asserting victory, never predetermine a later result, never ask the human to choose a move, and never request a tool/function call.

If at least two legal contestants can still continue somewhere in the bracket, end with exactly [[TOURNAMENT_CONTINUE]].
If all scheduled fights are resolved and one evidence-based champion remains, or every other trainer has conceded, end with exactly [[TOURNAMENT_COMPLETE]] and award the imaginary ceremonial prize.
Do not use [[TOURNAMENT_COMPLETE]] before every scheduled fight is actually resolved.
""",
                    ExecutionMode = "LeaderSingle",
                    RepeatCount = 1,
                    IncludePriorTranscript = true,
                    ProducesFinalAnswer = true,
                    UseBuiltInBehavior = false,
                    LoopGroup = "kernel-creature-battle",
                    MaximumLoopIterations = 24,
                    LoopCompletionMarker = "[[TOURNAMENT_COMPLETE]]",
                    IsEnabled = true,
                    RequiresHumanCheckpoint = false,
                    CanUseOrganicFunctions = false
                }
            ],
            PreferredCapabilities = [],
            ArchitectureContracts =
            [
                .. DefaultArchitectureContracts(),
                "Judge, trainers and creatures use distinct AI kernels within one assignment group; the supplied two-to-four trainer range reserves one different creature kernel per trainer and stays within selected-model capacity.",
                "Every AI kernel is an improvisation player with one bounded role, not an NPC and not an authority over another participant's action.",
                "Human trainer commands are optional. A targeted current human cue may direct a pair, but the AI trainer continues autonomously when no cue is supplied and the workflow never blocks merely to request a move.",
                "The judge rules only on completed transcript evidence, reports every round with a complete scoreboard and bracket state, and never scripts a future winner.",
                "The world, creatures, prize and consequences are entirely fictional, text-only, harmless and non-graphic. Every step disables organic/DX function execution."
            ]
        },
        new()
        {
            Key = "ascii-doom-council-adventure",
            DisplayName = "ASCII DOOM Council Adventure",
            Purpose = "A reactive, turn-based terminal adventure optionally informed by user-imported id Software DOOM source knowledge. It does not build a conventional 3D renderer: one meaningful world step is resolved per Council turn and exactly one AI member authors the complete Matrix-ship-style ASCII frame.",
            Roles =
            [
                new()
                {
                    Role = "Game Director",
                    Expertise = "turn orchestration, pacing, player-facing narration and completion rules",
                    Responsibility = "open the session, preserve authoritative state and coordinate every role without taking over their owned decisions",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 1,
                    HumanParticipationMode = HumanParticipationMode.None,
                    PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "ascii-doom",
                    RuntimeClassKeys = ["games.ascii.doom.session", "games.ascii.doom.director"]
                },
                new()
                {
                    Role = "Map Architect",
                    Expertise = "room graphs, corridors, keys, doors, exits and source-informed level logic",
                    Responsibility = "generate and maintain the authoritative room graph before play; never redraw the screen",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 1,
                    HumanParticipationMode = HumanParticipationMode.None,
                    PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "ascii-doom",
                    RuntimeClassKeys = ["games.ascii.doom.map"]
                },
                new()
                {
                    Role = "Player Controller",
                    Expertise = "human/AI command interpretation, keyboard and gamepad action mapping",
                    Responsibility = "accept one optional human action or choose one conservative autonomous action; never resolve its outcome",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 1,
                    HumanParticipationMode = HumanParticipationMode.Optional,
                    PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "ascii-doom",
                    RuntimeClassKeys = ["games.ascii.doom.player"]
                },
                new()
                {
                    Role = "World Actor",
                    Expertise = "one active enemy, pickup, door, hazard or environmental object per model",
                    Responsibility = "own exactly one active runtime-class instance for the current turn and submit one bounded intent",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 3,
                    HumanParticipationMode = HumanParticipationMode.None,
                    PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "ascii-doom-actors",
                    RuntimeClassKeys = ["games.ascii.doom.actor", "games.ascii.doom.creature", "games.ascii.doom.reactive-object"]
                },
                new()
                {
                    Role = "State Judge",
                    Expertise = "deterministic turn resolution, health, inventory, positions and legal actions",
                    Responsibility = "resolve the player and actor intents once, update the authoritative state and reject invented outcomes",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 1,
                    HumanParticipationMode = HumanParticipationMode.None,
                    PerformanceMode = CouncilRolePerformanceMode.TaskSpecialist,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "ascii-doom",
                    RuntimeClassKeys = ["games.ascii.doom.session", "games.ascii.doom.map", "games.ascii.doom.player", "games.ascii.doom.director", "games.ascii.doom.actor", "games.ascii.doom.creature", "games.ascii.doom.reactive-object"]
                },
                new()
                {
                    Role = "ASCII Frame Renderer",
                    Expertise = "fixed-width terminal composition and stable spatial continuity",
                    Responsibility = "author the one complete ASCII frame after state resolution; never alter game state",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 1,
                    HumanParticipationMode = HumanParticipationMode.None,
                    PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "ascii-doom",
                    RuntimeClassKeys = ["games.ascii.doom.frame"]
                }
            ],
            WorkflowSteps =
            [
                new()
                {
                    Key = "doom-world-bootstrap",
                    DisplayName = "Generate source-informed level",
                    SortOrder = 10,
                    Phase = "World bootstrap",
                    Role = "Map Architect",
                    ExecutionMode = "LeaderSingle",
                    PromptTemplate = """
{{RolePerformanceInstruction}}
{{RoleBoundaryInstruction}}
Start the directly playable in-chat ASCII corridor session by calling localgpt.game.session.start with gameKey ascii-doom and teamKey ascii-doom-council-adventure. Use the preseeded deterministic room graph immediately; do not spend a model turn inventing a large map and do not call runtime-class.list. Canonical class keys are games.ascii.doom.session, .map, .player, .controller, .director, .actor, .creature, .reactive-object and .frame; localgpt.runtime-class.resolve accepts case-insensitive aliases when inspection is needed. Optionally add only a compact title/objective informed by user-imported source knowledge. Never reproduce commercial WAD content or claim affiliation with the original game.
Runtime classes: {{RuntimeClasses}}
""",
                    IncludePriorTranscript = false,
                    CanUseOrganicFunctions = true,
                    AssignedModelName = "qwen3.5:4b",
                    UseBuiltInBehavior = false
                },
                new()
                {
                    Key = "doom-director-opening",
                    DisplayName = "Director opening",
                    SortOrder = 20,
                    Phase = "Opening",
                    Role = "Game Director",
                    ExecutionMode = "LeaderSingle",
                    PromptTemplate = """
{{RolePerformanceInstruction}}
{{RoleBoundaryInstruction}}
Open the terminal adventure using the generated map. Explain that this is a reactive Council simulation: one command, one resolved world step, one ASCII frame. State the controls described by the player runtime class and give the immediate objective. Do not generate the frame yourself.
Runtime classes: {{RuntimeClasses}}
""",
                    IncludePriorTranscript = true,
                    CanUseOrganicFunctions = true,
                    AssignedModelName = "qwen3.5:2b",
                    UseBuiltInBehavior = false
                },
                new()
                {
                    Key = "doom-player-command",
                    DisplayName = "Player command",
                    SortOrder = 30,
                    Phase = "Player intent",
                    Role = "Player Controller",
                    ExecutionMode = "LeaderSingle",
                    PromptTemplate = """
{{RolePerformanceInstruction}}
{{RoleBoundaryInstruction}}
This is loop {{LoopIteration}}/{{LoopMaximumIterations}}. Read the latest authoritative state and the newest human message. Translate a matching keyboard/gamepad/text cue into exactly one legal player intent. Human input is optional; when no current cue exists choose one conservative autonomous action so play continues. Do not resolve damage, movement success, enemy reactions or the frame.
Runtime classes: {{RuntimeClasses}}
""",
                    LoopGroup = "ascii-doom-turn",
                    MaximumLoopIterations = 24,
                    IncludePriorTranscript = false,
                    CanUseOrganicFunctions = true,
                    AssignedModelName = "qwen3.5:2b",
                    UseBuiltInBehavior = false
                },
                new()
                {
                    Key = "doom-world-actors",
                    DisplayName = "World actor intents",
                    SortOrder = 40,
                    Phase = "Actor intents",
                    Role = "World Actor",
                    ExecutionMode = "AllMembersParallel",
                    PromptTemplate = """
{{RolePerformanceInstruction}}
{{RoleBoundaryInstruction}}
Own one active world-actor runtime instance only. Based on the current room and player intent, emit one bounded intent for that instance. Do not impersonate another actor, resolve results, move the player or render the frame. Inactive actors state that they remain dormant.
Runtime classes: {{RuntimeClasses}}
""",
                    LoopGroup = "ascii-doom-turn",
                    MaximumLoopIterations = 24,
                    IncludePriorTranscript = false,
                    CanUseOrganicFunctions = true,
                    AssignedModelName = "qwen3.5:0.8b",
                    UseBuiltInBehavior = false
                },
                new()
                {
                    Key = "doom-state-resolution",
                    DisplayName = "Resolve one large world step",
                    SortOrder = 50,
                    Phase = "State resolution",
                    Role = "State Judge",
                    ExecutionMode = "LeaderSingle",
                    PromptTemplate = """
{{RoleBoundaryInstruction}}
Resolve exactly one meaningful world step from the current authoritative state, the player intent and all active actor intents. Update positions, health, inventory, doors and completion flags once. Reject duplicated or invented actions. Publish a compact canonical state block for the renderer and director. Do not draw ASCII. If the exit objective is completed, include exactly [[GAME_COMPLETE]].
Runtime classes: {{RuntimeClasses}}
""",
                    LoopGroup = "ascii-doom-turn",
                    MaximumLoopIterations = 24,
                    LoopCompletionMarker = "[[GAME_COMPLETE]]",
                    IncludePriorTranscript = false,
                    CanUseOrganicFunctions = true,
                    AssignedModelName = "qwen3.5:4b",
                    UseBuiltInBehavior = false
                },
                new()
                {
                    Key = "doom-ascii-frame",
                    DisplayName = "One AI builds the ASCII frame",
                    SortOrder = 60,
                    Phase = "ASCII frame",
                    Role = "ASCII Frame Renderer",
                    ExecutionMode = "LeaderSingle",
                    PromptTemplate = """
{{RolePerformanceInstruction}}
{{RoleBoundaryInstruction}}
Render the latest canonical state as one complete 80x25 Matrix-ship-style terminal frame. You alone own this frame. Preserve room geometry and glyph positions from the prior frame unless the resolved state changed them. Do not alter state, invent actions or split the frame across models. Output exactly:
[[ASCII_FRAME width=80 height=25]]
<the complete fixed-width frame>
[[/ASCII_FRAME]]
Then add at most three short lines: HUD, what changed, and available legal actions.
Runtime classes: {{RuntimeClasses}}
""",
                    LoopGroup = "ascii-doom-turn",
                    MaximumLoopIterations = 24,
                    IncludePriorTranscript = true,
                    CanUseOrganicFunctions = true,
                    AssignedModelName = "qwen3.5:4b",
                    ProducesFinalAnswer = true,
                    ProducesAsciiFrame = true,
                    AsciiFrameWidth = 80,
                    AsciiFrameHeight = 25,
                    WorldStepScale = 4,
                    UseBuiltInBehavior = false
                }
            ],
            PreferredCapabilities = ["localgpt.runtime-class.resolve", "localgpt.game.session.start", "localgpt.game.session.get", "localgpt.game.control.preview", "localgpt.game.control", "localgpt.game.frame.submit", "localgpt.knowledge.list"],
            ArchitectureContracts =
            [
                .. DefaultArchitectureContracts(),
                "This is a turn-based ASCII Council interpretation, not a traditional real-time 3D engine and not a claim that the original C executable is running.",
                "Exactly one AI member owns and emits each complete ASCII frame after state resolution; frame-producing steps must use a single-member execution mode.",
                "The id Software DOOM repository is an optional user-approved learning source. Do not redistribute commercial WAD data and do not require it for original generated maps.",
                "One Council turn advances a meaningful world step and then renders once. Never simulate 35 frames per second through model calls.",
                "Every active enemy, pickup, door or hazard is represented by a factory-created runtime-class instance; one World Actor member owns one active instance while creature and reactive-object subdirectors predict bounded consequences for the authoritative GameDirector.",
                "Human keyboard/gamepad/text commands are optional. HumanRequired runtime fields block only the dependent next round, not the entire application."
            ]
        },
        new()
        {
            Key = "green-dragon-runtime-story",
            DisplayName = "Green Dragon Runtime Story",
            Purpose = "A configuration-first role-play example inspired by the open-source Legend of the Green Dragon project. Directors orchestrate a persistent world while locations, houses, NPCs and events are runtime-class instances acted by bounded Council members; one AI renders the terminal scene per story turn.",
            Roles =
            [
                new()
                {
                    Role = "Story Director",
                    Expertise = "chapter pacing, continuity, quests and player-facing narration",
                    Responsibility = "coordinate the story without speaking for owned NPC, event, location or player instances",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 1,
                    PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "green-dragon",
                    RuntimeClassKeys = ["games.green-dragon.world"]
                },
                new()
                {
                    Role = "Player Traveller",
                    Expertise = "one bounded player choice per story turn",
                    Responsibility = "use a current human choice when supplied or choose autonomously; never narrate the outcome",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 1,
                    HumanParticipationMode = HumanParticipationMode.Optional,
                    PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "green-dragon",
                    RuntimeClassKeys = ["games.green-dragon.player"]
                },
                new()
                {
                    Role = "Location or House Actor",
                    Expertise = "one active village, forest, inn, house or room instance per model",
                    Responsibility = "present the active place's description, exits and available interactions without deciding the player choice",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 2,
                    PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "green-dragon-objects",
                    RuntimeClassKeys = ["games.green-dragon.location"]
                },
                new()
                {
                    Role = "NPC Actor",
                    Expertise = "one named NPC runtime instance per model",
                    Responsibility = "speak and act only for the owned NPC instance for this turn",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 3,
                    PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "green-dragon-objects",
                    RuntimeClassKeys = ["games.green-dragon.npc"]
                },
                new()
                {
                    Role = "Event Actor",
                    Expertise = "one encounter, random event or story beat runtime instance per model",
                    Responsibility = "evaluate entry conditions and present bounded consequences without taking over another instance",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 2,
                    PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "green-dragon-objects",
                    RuntimeClassKeys = ["games.green-dragon.event"]
                },
                new()
                {
                    Role = "Story State Keeper",
                    Expertise = "canonical world state, flags, health, gold, location and event completion",
                    Responsibility = "resolve one story turn from owned intents and publish the canonical state",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 1,
                    PerformanceMode = CouncilRolePerformanceMode.TaskSpecialist,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "green-dragon",
                    RuntimeClassKeys = ["games.green-dragon.world", "games.green-dragon.player", "games.green-dragon.location", "games.green-dragon.npc", "games.green-dragon.event"]
                },
                new()
                {
                    Role = "ASCII Scene Renderer",
                    Expertise = "one fixed-width terminal scene per completed story turn",
                    Responsibility = "render the canonical state without changing it",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 1,
                    PerformanceMode = CouncilRolePerformanceMode.ImprovisationPlayer,
                    BoundaryMode = CouncilRoleBoundaryMode.Strict,
                    LanguageMode = CouncilRoleLanguageMode.SenderLanguage,
                    DistinctAiAssignmentGroup = "green-dragon",
                    RuntimeClassKeys = ["games.green-dragon.frame"]
                }
            ],
            WorkflowSteps =
            [
                new()
                {
                    Key = "dragon-world-opening",
                    DisplayName = "Create world and opening",
                    SortOrder = 10,
                    Phase = "World opening",
                    Role = "Story Director",
                    ExecutionMode = "LeaderSingle",
                    PromptTemplate = """
{{RolePerformanceInstruction}}
{{RoleBoundaryInstruction}}
Call localgpt.game.session.start with gameKey green-dragon and teamKey green-dragon-runtime-story so the story is immediately playable in /Chat. Use the preseeded original village scene and runtime-class keys directly; do not run discovery loops. The optional lotgd source may be studied only through user-approved knowledge, without copying story text or claiming affiliation.
Runtime classes: {{RuntimeClasses}}
""",
                    IncludePriorTranscript = true,
                    CanUseOrganicFunctions = true,
                    AssignedModelName = "qwen3.5:4b",
                    UseBuiltInBehavior = false
                },
                new()
                {
                    Key = "dragon-player-choice",
                    DisplayName = "Player choice",
                    SortOrder = 20,
                    Phase = "Player intent",
                    Role = "Player Traveller",
                    ExecutionMode = "LeaderSingle",
                    PromptTemplate = """
{{RolePerformanceInstruction}}
{{RoleBoundaryInstruction}}
Story turn {{LoopIteration}}/{{LoopMaximumIterations}}. Use a current human numbered/text/gamepad choice when present; otherwise choose one reasonable action autonomously. Emit exactly one player intent. Do not narrate its result or another role's response.
Runtime classes: {{RuntimeClasses}}
""",
                    LoopGroup = "green-dragon-turn",
                    MaximumLoopIterations = 16,
                    IncludePriorTranscript = false,
                    CanUseOrganicFunctions = true,
                    AssignedModelName = "qwen3.5:2b",
                    UseBuiltInBehavior = false
                },
                new()
                {
                    Key = "dragon-location-response",
                    DisplayName = "Location and house response",
                    SortOrder = 30,
                    Phase = "Location intent",
                    Role = "Location or House Actor",
                    ExecutionMode = "AllMembersParallel",
                    PromptTemplate = """
Own one active location/house instance. React to the player intent only from that place's saved fields. State available exits/interactions; do not control NPCs, events, player state or the final scene.
Runtime classes: {{RuntimeClasses}}
""",
                    LoopGroup = "green-dragon-turn",
                    MaximumLoopIterations = 16,
                    IncludePriorTranscript = true,
                    CanUseOrganicFunctions = true,
                    AssignedModelName = "qwen3.5:0.8b",
                    UseBuiltInBehavior = false
                },
                new()
                {
                    Key = "dragon-npc-response",
                    DisplayName = "NPC responses",
                    SortOrder = 40,
                    Phase = "NPC intents",
                    Role = "NPC Actor",
                    ExecutionMode = "AllMembersParallel",
                    PromptTemplate = """
Own exactly one named NPC runtime instance. Emit one line or bounded action consistent with its fields and current location. Do not impersonate other NPCs, decide event outcomes or update canonical state.
Runtime classes: {{RuntimeClasses}}
""",
                    LoopGroup = "green-dragon-turn",
                    MaximumLoopIterations = 16,
                    IncludePriorTranscript = true,
                    CanUseOrganicFunctions = true,
                    AssignedModelName = "qwen3.5:0.8b",
                    UseBuiltInBehavior = false
                },
                new()
                {
                    Key = "dragon-event-response",
                    DisplayName = "Event responses",
                    SortOrder = 50,
                    Phase = "Event intents",
                    Role = "Event Actor",
                    ExecutionMode = "AllMembersParallel",
                    PromptTemplate = """
Own one event runtime instance. Check its trigger against the current state and player intent. If active, present one bounded event contribution and legal choices; otherwise remain dormant. Do not resolve the whole turn.
Runtime classes: {{RuntimeClasses}}
""",
                    LoopGroup = "green-dragon-turn",
                    MaximumLoopIterations = 16,
                    IncludePriorTranscript = true,
                    CanUseOrganicFunctions = true,
                    AssignedModelName = "qwen3.5:0.8b",
                    UseBuiltInBehavior = false
                },
                new()
                {
                    Key = "dragon-state-resolution",
                    DisplayName = "Resolve story turn",
                    SortOrder = 60,
                    Phase = "State resolution",
                    Role = "Story State Keeper",
                    ExecutionMode = "LeaderSingle",
                    PromptTemplate = """
Resolve the current player, location, NPC and event intents once. Publish the canonical player/world/location/event state and a concise list of legal next choices. Do not render the scene. When the configured chapter objective is genuinely complete, include exactly [[STORY_COMPLETE]].
Runtime classes: {{RuntimeClasses}}
""",
                    LoopGroup = "green-dragon-turn",
                    MaximumLoopIterations = 16,
                    LoopCompletionMarker = "[[STORY_COMPLETE]]",
                    IncludePriorTranscript = true,
                    CanUseOrganicFunctions = true,
                    AssignedModelName = "qwen3.5:4b",
                    UseBuiltInBehavior = false
                },
                new()
                {
                    Key = "dragon-ascii-scene",
                    DisplayName = "One AI builds the ASCII scene",
                    SortOrder = 70,
                    Phase = "ASCII scene",
                    Role = "ASCII Scene Renderer",
                    ExecutionMode = "LeaderSingle",
                    PromptTemplate = """
Render the latest canonical state as one complete 80x25 terminal scene. You alone own the frame. Preserve spatial continuity from the previous frame and do not alter story state. Output exactly:
[[ASCII_FRAME width=80 height=25]]
<the complete fixed-width scene>
[[/ASCII_FRAME]]
Then add concise narration and numbered legal choices.
Runtime classes: {{RuntimeClasses}}
""",
                    LoopGroup = "green-dragon-turn",
                    MaximumLoopIterations = 16,
                    IncludePriorTranscript = true,
                    CanUseOrganicFunctions = true,
                    AssignedModelName = "qwen3.5:4b",
                    ProducesFinalAnswer = true,
                    ProducesAsciiFrame = true,
                    AsciiFrameWidth = 80,
                    AsciiFrameHeight = 25,
                    WorldStepScale = 1,
                    UseBuiltInBehavior = false
                }
            ],
            PreferredCapabilities = ["localgpt.runtime-class.resolve", "localgpt.game.session.start", "localgpt.game.session.get", "localgpt.game.control", "localgpt.game.frame.submit", "localgpt.knowledge.list"],
            ArchitectureContracts =
            [
                .. DefaultArchitectureContracts(),
                "Locations, houses, NPCs and events are separate runtime-class instances. Active Council members act only as the instance assigned to them.",
                "The Story Director orchestrates continuity but does not overwrite bounded choices owned by player, NPC, location or event roles.",
                "Exactly one AI member renders the complete ASCII scene after canonical state resolution.",
                "The lotgd repository is an optional user-approved learning source and configuration example; copied source/story content is not required for runtime play.",
                "Human input is optional unless a runtime field is explicitly HumanRequired; such a gate blocks only the dependent round."
            ]
        },
        new()
        {
            Key = "learning-round",
            DisplayName = "Learning Round",
            Purpose = "A database-grounded council preset that studies LocalGPT chat memory, logs, knowledge, regex definitions and verified project facts, then stores only bounded model-suggested learning evidence for later review.",
            Roles =
            [
                new() { Role = "History curator", Expertise = "chat memory, prior council runs and feedback", Responsibility = "select representative evidence without dumping entire databases into one prompt" },
                new() { Role = "RegEx and architecture analyst", Expertise = "generic project structure probes, compiler syntax and protocol wiring", Responsibility = "identify reusable patterns and validate proposed regexes against evidence" },
                new() { Role = "Evidence verifier", Expertise = "application logs, knowledge provenance, contradictions and staleness", Responsibility = "separate observed facts from hypotheses and flag anything needing user or source verification" },
                new() { Role = "Learning leader", Expertise = "democratic synthesis and bounded knowledge maintenance", Responsibility = "coordinate the round and store only compact model-suggested facts and timeout-validated regexes" }
            ],
            PreferredCapabilities =
            [
                "localgpt.learning.snapshot",
                "localgpt.learning.maintain",
                "localgpt.regex.list",
                "localgpt.regex.get",
                "localgpt.regex.test",
                "localgpt.regex.upsert",
                "localgpt.text.json.inspect",
                "localgpt.text.json.translate",
                "localgpt.memory",
                "localgpt.logs",
                "localgpt.knowledge"
            ],
            ExpertPreparationPromptTemplate = """
You lead the expert preparation round for {{TeamName}}. Call localgpt.learning.snapshot first with a bounded takePerSource. Identify representative chat-memory, log, knowledge and regex evidence. Do not treat model output as authority. List contradictions, stale facts, reusable project/architecture patterns and regex candidates that can be tested before storage.
User learning request:
{{UserPrompt}}
""",
            LeaderSynthesisPromptTemplate = """
You are the learning leader for {{TeamName}}. Convert the evidence preparation into a bounded democratic learning work order. Assign history, regex/architecture and verification tasks. Require regex candidates to be tested. Facts saved through localgpt.learning.maintain remain ModelSuggested/NeedsUserReview; this knowledge self-maintenance needs no approval because it cannot run commands, mutate projects or authorize side effects.
Expert preparation:
{{Preparation}}
Original learning request:
{{UserPrompt}}
""",
            MainRoundInstructionTemplate = "Every member studies a distinct evidence slice, cites the local source category, corrects contradictions and proposes compact reusable facts or regexes. Use localgpt.learning.maintain only for untrusted knowledge maintenance; never promote self-reports to user-approved authority and never perform external side effects.",
            ArchitectureContracts =
            [
                .. DefaultArchitectureContracts(),
                "Learning reads bounded SQLite evidence packages and never depends on an in-memory static prompt or regex catalog.",
                "New facts remain ModelSuggested/NeedsUserReview until verified; regex definitions must compile with a timeout before persistence.",
                "Knowledge self-maintenance may run automatically because it cannot execute commands, write project files or grant permissions."
            ]
        }
    ];


    /// <summary>
    /// Creates the bounded installed-model benchmark council used by first-run onboarding and Chat quick starts.
    /// </summary>
    /// <returns>The seeded adaptive benchmark team definition.</returns>
    private OrganicCouncilTeamDefinition CreateAdaptiveBenchmarkTeam() => new()
    {
        Key = "adaptive-model-benchmark",
        DisplayName = "Adaptive Ollama Benchmark Council",
        Purpose = "Benchmarks user-selected models already installed in loopback Ollama, compares speed and answer value, and lets independent code-curator members recommend a new preset without overwriting an existing one.",
        Roles =
        [
            new() { Role = "Benchmark Director", Expertise = "bounded benchmark design, fairness, reproducibility and hardware-road constraints", Responsibility = "define the candidate set and ensure every model receives equivalent tasks and limits" },
            new() { Role = "Task Curator", Expertise = "small checkable C#, PowerShell, Java, Minecraft and ASCII-game tasks", Responsibility = "prepare deterministic tasks with explicit acceptance evidence rather than subjective style prompts" },
            new() { Role = "Code Curator", Expertise = "Qwen/DeepSeek/code-model review, correctness, maintainability and generated-artifact quality", Responsibility = "score candidate answers independently and explain good or bad generation value" },
            new() { Role = "Performance Analyst", Expertise = "latency, token throughput, context/output budgets, CPU/GPU routing and timeout evidence", Responsibility = "compare speed and resource behavior without treating the fastest incomplete answer as the winner" },
            new() { Role = "Preset Synthesizer", Expertise = "LocalGPT model presets and safe parameter ranges", Responsibility = "recommend a new named preset while preserving all existing presets and requiring user confirmation before save" }
        ],
        PreferredCapabilities =
        [
            "localgpt.models.benchmark.autotune",
            "localgpt.time_state.now",
            "localgpt.onboarding.status",
            "localgpt.learning.snapshot",
            "localgpt.knowledge.list"
        ],
        WorkflowSteps =
        [
            Step("benchmark-preflight", "Benchmark preflight", 10, "Preparation", "Benchmark Director", """
Inspect the installed-model and hardware evidence. Select only models the user explicitly included or that are already installed. Define equal output/context/time limits, deterministic task categories and the new-preset name. Do not download, start, stop or remove models.
""", "LeaderSingle"),
            Step("benchmark-task-design", "Deterministic task design", 20, "Task design", "Task Curator", """
Create a compact task matrix covering at least one architecture/code task and one structured reasoning task. Every task needs a checkable acceptance shape and must fit the configured low-risk benchmark budget. Do not include secrets, repositories outside the approved workspace or destructive commands.
""", "AllMembersSequential"),
            Step("benchmark-execution", "Run installed-model benchmark", 30, "Execution", "Benchmark Director", """
Use localgpt.models.benchmark.autotune with the exact installed candidate set and reviewed limits. This is the only benchmark execution step. Preserve generated-text privacy in logs and do not save a preset unless the current user explicitly requested it.
""", "LeaderSingle", canUseOrganicFunctions: true),
            Step("benchmark-curation", "Independent code curation", 40, "Review", "Code Curator", """
Review the returned bounded results independently. Compare correctness, completeness, architecture quality, instruction following and obvious hallucination risk. Explain why each result is good, bad or inconclusive. Do not change the benchmark measurements.
""", "AllMembersParallel"),
            Step("benchmark-performance", "Performance analysis", 50, "Review", "Performance Analyst", """
Compare first-token/total latency, throughput, timeout behavior, context/output limits and hardware roads. Penalize incomplete or invalid output even when it is fast. Separate measured facts from inferred hardware explanations.
""", "AllMembersSequential"),
            Step("benchmark-preset", "Preset recommendation", 60, "Synthesis", "Preset Synthesizer", """
Synthesize one recommended model/preset configuration plus alternatives for low-latency games and higher-value development. State the evidence and unresolved uncertainty. Existing presets must remain untouched; any new preset save remains a separate user-confirmed action.
""", "LeaderSingle", producesFinalAnswer: true)
        ],
        MainRoundInstructionTemplate = "Treat benchmark measurements as evidence, not reputation. Code curators judge answer value independently; the Benchmark Director remains responsible for fairness and the Preset Synthesizer may only recommend a new preset.",
        ArchitectureContracts =
        [
            .. DefaultArchitectureContracts(),
            "Benchmark only models already installed at the configured loopback Ollama endpoint.",
            "Every candidate receives equivalent bounded tasks and runtime limits; failed or incomplete answers remain visible in the result.",
            "Code-curator judgments and measured performance are separate evidence streams.",
            "A benchmark may create a new user-approved preset but never overwrites an existing preset."
        ]
    };

    /// <summary>
    /// Creates a low-latency game council in which the deterministic GameDirector remains the authoritative engine.
    /// </summary>
    /// <returns>The seeded GameDirector runtime team definition.</returns>
    private OrganicCouncilTeamDefinition CreateGameDirectorRuntimeTeam() => new()
    {
        Key = "game-director-runtime",
        DisplayName = "GameDirector Runtime Council",
        Purpose = "Runs low-latency game rounds where controllers, creatures, reactive map objects and optional subdirectors propose actions while the deterministic GameDirector validates and commits the authoritative next state.",
        Roles =
        [
            new() { Role = "GameDirector", Expertise = "authoritative state transitions, collision/rule validation, turn ordering and deterministic replay", Responsibility = "accept, reject or normalize every proposed action and publish the only canonical state" },
            new() { Role = "Player Controller", Expertise = "bounded movement, aiming, interaction and user-intent translation", Responsibility = "propose one controller action without mutating game state" },
            new() { Role = "Creature Subdirector", Expertise = "creature prediction, tactical intent and species-specific behavior factories", Responsibility = "coordinate creature proposals while leaving movement and damage decisions to the GameDirector" },
            new() { Role = "Reactive Object Subdirector", Expertise = "doors, switches, hazards, pickups, triggers and map-object factories", Responsibility = "propose object reactions caused by the canonical state or accepted player action" },
            new() { Role = "Runtime Verifier", Expertise = "state hashes, turn IDs, deterministic replay and frame consistency", Responsibility = "reject stale proposals and verify that rendered frames match the committed state" }
        ],
        PreferredCapabilities = ["localgpt.game.session.start", "localgpt.game.session.get", "localgpt.game.control.preview", "localgpt.game.control", "localgpt.game.frame.submit", "localgpt.runtime-class.resolve"],
        WorkflowSteps =
        [
            Step("game-intent", "Collect controller intent", 10, "Intent", "Player Controller", "Propose exactly one bounded player action for the current turn. Do not move actors or mutate map state directly.", "LeaderSingle"),
            Step("game-creature-prediction", "Predict creature intents", 20, "Prediction", "Creature Subdirector", "Use creature runtime classes and the latest canonical state to propose bounded intents for each active creature. Do not assign final positions, hits or damage.", "AllMembersParallel"),
            Step("game-object-reactions", "Predict reactive objects", 30, "Prediction", "Reactive Object Subdirector", "Evaluate doors, switches, hazards, pickups and triggers as factory-created runtime objects. Propose reactions only; do not commit them.", "AllMembersSequential"),
            Step("game-director-commit", "Validate and commit turn", 40, "Authority", "GameDirector", "Validate every proposal against turn ID, rules, collisions, cooldowns and canonical state. Commit one authoritative next state or explain each rejection. The model is an adviser; deterministic engine rules win.", "LeaderSingle"),
            Step("game-state-verification", "Verify state and frame", 50, "Verification", "Runtime Verifier", "Verify the committed state hash, actor/object identities and frame continuity. Reject stale or contradictory output before the next turn.", "LeaderSingle", producesFinalAnswer: true)
        ],
        MainRoundInstructionTemplate = "Every actor and object proposes; only the GameDirector commits. Low-B models are recommended for latency, but the user-selected model preset controls assignments.",
        ArchitectureContracts =
        [
            .. DefaultArchitectureContracts(),
            "Player controllers, AI creatures and reactive map objects never mutate canonical state directly.",
            "Creature and object subdirectors predict intents for factory-created runtime classes; the GameDirector validates and commits them.",
            "Every proposal carries a turn/revision identity so stale output can be rejected deterministically.",
            "The rendered ASCII frame is a view of committed state, not a second source of truth."
        ]
    };

    /// <summary>
    /// Creates the modern hosted C# development team based on LocalGPT's PowerShell validation order.
    /// </summary>
    /// <returns>The seeded C# development team definition.</returns>
    private OrganicCouncilTeamDefinition CreateCSharpModernHostDevelopmentTeam() => CreateDevelopmentTeam(
        "csharp-modern-host-development",
        "Modern C# Host Development Team",
        "Builds maintainable .NET hosted applications with DI, controllers, DXFunctions, project structure, regex ownership and repository validation rounds.",
        "C#/.NET structure and RegEx analyst",
        ".NET host architect",
        "C# implementation developer",
        "dotnet build/test engineer",
        "C# code curator",
        [".sln", ".slnx", ".csproj", ".cs", ".razor", ".json", ".props", ".targets"],
        "Use a modern Generic Host or ASP.NET Core WebApplication host, explicit DI lifetimes, options binding, controllers/services behind interfaces, cancellation, structured logging and bounded workspace execution.",
        "dotnet restore/build/test plus LocalGPT architecture, async, iterator, localization, text-ownership and system-variable policy guards.");

    /// <summary>
    /// Creates a PowerShell build-system development team that mirrors LocalGPT's repository guard sequence.
    /// </summary>
    /// <returns>The seeded PowerShell development team definition.</returns>
    private OrganicCouncilTeamDefinition CreatePowerShellBuildDevelopmentTeam() => CreateDevelopmentTeam(
        "powershell-build-development",
        "PowerShell Build-System Development Team",
        "Designs PowerShell modules and repository build automation by following LocalGPT's proven preflight, policy, build, verification and release-handoff sequence.",
        "PowerShell syntax and RegEx analyst",
        "Build-pipeline architect",
        "PowerShell module developer",
        "Pester and process-integration engineer",
        "PowerShell code curator",
        [".ps1", ".psm1", ".psd1", ".ps1xml", ".json", ".yml", ".yaml", ".cmd"],
        "Prefer advanced functions, CmdletBinding, explicit parameters, strict mode, approved verbs, structured errors, bounded native-process invocation and deterministic exit codes.",
        "Run parse validation, PSScriptAnalyzer/Pester when installed, repository policy scripts and a dry-run path before any consequential command.");

    /// <summary>
    /// Creates a Java hosted-application development team with Maven/Gradle project and regex roles.
    /// </summary>
    /// <returns>The seeded Java development team definition.</returns>
    private OrganicCouncilTeamDefinition CreateJavaHostedDevelopmentTeam() => CreateDevelopmentTeam(
        "java-hosted-development",
        "Java Hosted Application Development Team",
        "Builds Java services with Maven or Gradle, explicit package boundaries, controller/service adapters, tests, regex policies and reviewable artifact generation.",
        "Java structure and RegEx analyst",
        "Java host architect",
        "Java implementation developer",
        "Maven/Gradle test engineer",
        "Java code curator",
        ["pom.xml", "build.gradle", "build.gradle.kts", "settings.gradle", "settings.gradle.kts", ".java", ".properties", ".xml", ".json", ".yml"],
        "Choose Maven or Gradle explicitly, keep controllers thin, use service interfaces/adapters, structured configuration, cancellation/interruption boundaries and testable dependency injection.",
        "Run wrapper-based compile/test/package commands only inside an approved workspace and verify dependency, package, test-report and artifact paths.");

    /// <summary>
    /// Creates a Minecraft development team with distinct datapack, scripting and Java-mod roles.
    /// </summary>
    /// <returns>The seeded Minecraft development team definition.</returns>
    private OrganicCouncilTeamDefinition CreateMinecraftDevelopmentTeam() => CreateDevelopmentTeam(
        "minecraft-development",
        "Minecraft Development Team",
        "Routes Bedrock datapack/scripting and Java-mod work to distinct specialists while preserving manifests, namespaces, pack formats, project regexes and bounded build verification.",
        "Minecraft pack and RegEx analyst",
        "Minecraft architecture lead",
        "Minecraft implementation developer",
        "Minecraft validation/build engineer",
        "Minecraft code and content curator",
        ["manifest.json", "pack.mcmeta", ".mcfunction", ".json", ".js", ".ts", ".java", ".gradle", ".properties", ".lang"],
        "Determine Bedrock add-on, datapack, scripting API, Fabric/Forge/NeoForge or mixed target before generation. Keep namespaces, UUIDs, pack formats, mappings and version compatibility explicit.",
        "Validate JSON/manifests/functions first, then run the selected wrapper/compiler and inspect the generated pack/mod artifact without launching Minecraft automatically.");

    /// <summary>
    /// Creates one language-specific development council using the maintained repository round order.
    /// </summary>
    /// <param name="key">Stable council-team key.</param>
    /// <param name="displayName">Human-readable team name.</param>
    /// <param name="purpose">Team purpose shown in Chat and the team editor.</param>
    /// <param name="regexRole">Role responsible for project syntax and regex evidence.</param>
    /// <param name="architectRole">Role responsible for host and solution architecture.</param>
    /// <param name="implementationRole">Role responsible for bounded source changes.</param>
    /// <param name="buildRole">Role responsible for compiler and test evidence.</param>
    /// <param name="curatorRole">Role responsible for final code and maintainability review.</param>
    /// <param name="expectedFiles">Representative files used to guide project-structure policies.</param>
    /// <param name="architectureInstruction">Language-specific architecture boundary.</param>
    /// <param name="buildInstruction">Language-specific build and verification boundary.</param>
    /// <returns>The configured development team definition.</returns>
    private OrganicCouncilTeamDefinition CreateDevelopmentTeam(
        string key,
        string displayName,
        string purpose,
        string regexRole,
        string architectRole,
        string implementationRole,
        string buildRole,
        string curatorRole,
        IReadOnlyList<string> expectedFiles,
        string architectureInstruction,
        string buildInstruction) => new()
    {
        Key = key,
        DisplayName = displayName,
        Purpose = purpose,
        Roles =
        [
            new() { Role = regexRole, Expertise = $"project discovery, syntax, file-ending ownership and regex validation for {string.Join(", ", expectedFiles)}", Responsibility = "map current structure, expected files and safe include/exclude regexes before implementation" },
            new() { Role = architectRole, Expertise = "modern hosted application architecture, interfaces, controllers, services, adapters and dependency injection", Responsibility = "define a current-to-target solution plan and preserve existing contracts" },
            new() { Role = implementationRole, Expertise = "bounded source implementation and project-file maintenance", Responsibility = "apply only the approved milestone and keep generated artifacts reviewable" },
            new() { Role = buildRole, Expertise = "compiler discovery, restore/build/test diagnostics and workspace rights", Responsibility = "run or interpret the approved build workflow and report the first root failure" },
            new() { Role = curatorRole, Expertise = "correctness, maintainability, architecture policy, security boundaries and release notes", Responsibility = "perform the independent final review and block unresolved danger findings" }
        ],
        PreferredCapabilities =
        [
            "project.architecture.get",
            "project.maintenance.get",
            "project.files.scan",
            "project.file.patterns.save",
            "project.workspace.environment.assess",
            "project.revision.build.verify",
            "project.revision.council-review",
            "project.revision.ready.approve",
            "localgpt.regex.list",
            "localgpt.regex.test",
            "localgpt.regex.upsert",
            "localgpt.knowledge.remote.inspect",
            "localgpt.knowledge.remote.import"
        ],
        WorkflowSteps =
        [
            Step("development-preflight", "Repository and compiler preflight", 10, "Preparation", regexRole, $"""
Inspect the selected project/revision and workspace. Map expected files ({string.Join(", ", expectedFiles)}), existing include/exclude regexes, compiler/tool paths, rights findings and the first reproducible failure. Do not write files or execute a compiler without the required current approval.
""", "LeaderSingle", canUseOrganicFunctions: true),
            Step("development-architecture", "Current-to-target architecture", 20, "Planning", architectRole, $"""
Produce a concise current-to-target host and project plan. {architectureInstruction} Identify controllers, DXFunctions, service interfaces, adapters/decorators, persistence boundaries, configuration, logs, tests and versioned documentation. Preserve user-edited contracts.
""", "LeaderSingle"),
            Step("development-implementation", "Bounded implementation proposals", 30, "Implementation", implementationRole, "Propose or generate only the approved milestone. Keep each file attributable to the project/revision, follow the selected regex/file policy, add XML/API documentation and do not smuggle execution into generation.", "AllMembersSequential"),
            Step("development-policy-audit", "Regex and architecture policy audit", 40, "Policy", regexRole, "Run the maintained regex tests and repository policy reasoning against the proposed files. Report exact paths and first violations. Generic reusable rules are preferred over product-specific exceptions.", "AllMembersParallel", canUseOrganicFunctions: true),
            Step("development-build", "Build and test evidence", 50, "Verification", buildRole, $"""
{buildInstruction} Separate restore, compile, test and packaging failures. Report the first root error and treat later errors as possible cascades. Never claim success without current output.
""", "LeaderSingle", canUseOrganicFunctions: true),
            Step("development-curation", "Independent code curator review", 60, "Review", curatorRole, "Review correctness, modern architecture, lifecycle/threading, error handling, security boundaries, tests, documentation and changelog. Classify findings as Approved, Warning or Danger and require concrete fixes for every Danger item.", "AllMembersParallel"),
            Step("development-release", "Release synthesis", 70, "Synthesis", architectRole, "Synthesize implemented changes, evidence, unresolved warnings, version/changelog updates and the exact next user-approved action. Do not approve readiness while a Danger finding or failed build remains.", "LeaderSingle", producesFinalAnswer: true)
        ],
        MainRoundInstructionTemplate = "Follow the repository build-system order: discover and normalize inputs, plan architecture, implement a bounded milestone, run regex/policy checks, inspect build/test evidence, perform independent curation, then synthesize a release handoff.",
        ArchitectureContracts =
        [
            .. DefaultArchitectureContracts(),
            architectureInstruction,
            buildInstruction,
            $"Expected project evidence includes: {string.Join(", ", expectedFiles)}.",
            "Controllers remain transport boundaries, DXFunctions remain explicit AI-callable contracts, and implementation logic stays in injected services.",
            "Every consequential build, script or write remains workspace-bound and user-approved; Council rounds may propose and review but do not create unattended loops."
        ]
    };

    /// <summary>
    /// Creates one maintained council workflow step with conservative defaults.
    /// </summary>
    /// <param name="key">Stable workflow-step key.</param>
    /// <param name="displayName">Human-readable round name.</param>
    /// <param name="sortOrder">Execution ordering value.</param>
    /// <param name="phase">Workflow phase label.</param>
    /// <param name="role">Role assigned to the step.</param>
    /// <param name="prompt">Step-specific prompt template.</param>
    /// <param name="executionMode">Council execution mode.</param>
    /// <param name="canUseOrganicFunctions">Whether the round may request registered functions.</param>
    /// <param name="producesFinalAnswer">Whether the round produces the visible final answer.</param>
    /// <returns>The configured workflow step.</returns>
    private CouncilWorkflowStepDefinition Step(
        string key,
        string displayName,
        int sortOrder,
        string phase,
        string role,
        string prompt,
        string executionMode,
        bool canUseOrganicFunctions = false,
        bool producesFinalAnswer = false) => new()
    {
        Key = key,
        DisplayName = displayName,
        SortOrder = sortOrder,
        Phase = phase,
        Role = role,
        PromptTemplate = prompt,
        ExecutionMode = executionMode,
        IncludePriorTranscript = true,
        IsEnabled = true,
        CanUseOrganicFunctions = canUseOrganicFunctions,
        ProducesFinalAnswer = producesFinalAnswer,
        UseBuiltInBehavior = false
    };

    /// <summary>
    /// Creates architecture contracts shared by every seeded council team.
    /// </summary>
    /// <returns>The shared architecture-contract list.</returns>
    private List<string> DefaultArchitectureContracts() =>
    [
        "New .NET organ plugins use the existing namespace/service/domain architecture, intentional Singleton/Scoped/Transient lifetimes and structured ILogger<T> logging.",
        "The transport contract remains independent from TCP so later UART, SPI and MQTT adapters can implement the same interfaces.",
        "Runtime identities are generated by each application only after installation. MFA-verified peer trust enables ECDH-derived AES-GCM encryption and ECDSA signing; deleting or regenerating the runtime secret resets cryptographic trust.",
        "Installer, launcher, bootstrap and fixed-port wiring are compatibility contracts and require explicit migration plus regression tests."
    ];
}

using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Owns the first-run council-team seed model. Runtime edits remain database-owned by CouncilTeamConfigurationService.
/// </summary>
public sealed class OrganicCouncilBlueprintSeedDataService(ILogger<OrganicCouncilBlueprintSeedDataService> logger)
    : IOrganicCouncilBlueprintSeedDataService
{
    public IReadOnlyList<OrganicCouncilTeamDefinition> CreateDefaultTeams()
    {
        var teams = CreateDefaultTeamsCore();
        logger.LogInformation("Created {TeamCount} default organic council team blueprint(s).", teams.Count);
        return teams;
    }

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
            Key = "pokemon-tournament",
            DisplayName = "Pokémon Tournament",
            Purpose = "A harmless, non-graphic, round-based text RPG tournament with one independent judge, two or more distinct trainers, and one distinct AI Pokémon contestant paired to each trainer. The judge reports every completed battle round, uses fainting rather than injury or death, and never predetermines a winner.",
            Roles =
            [
                new()
                {
                    Role = "Judge",
                    Expertise = "fair tournament structure, evidence-based rulings, scorekeeping and conflict resolution",
                    Responsibility = "create the bracket without choosing a winner, enforce harmless text-RPG rules, and report the evidence-based result after every completed battle round",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 1,
                    DistinctAiAssignmentGroup = "pokemon-tournament"
                },
                new()
                {
                    Role = "Pokemon Trainer",
                    Expertise = "strategy, sportsmanship, type matchups and creative command decisions",
                    Responsibility = "name and coach the uniquely paired Pokémon model, issue one bounded command per active round, and never decide the judge's ruling",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 2,
                    MaximumAiParticipants = 4,
                    DistinctAiAssignmentGroup = "pokemon-tournament",
                    PairedRole = "Pokemon"
                },
                new()
                {
                    Role = "Pokemon",
                    Expertise = "role-played Pokémon abilities, stamina, tactical reactions and clear action descriptions",
                    Responsibility = "perform harmless fictional moves only for the paired trainer, track the judge's latest state, and never award itself victory",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 2,
                    MaximumAiParticipants = 4,
                    DistinctAiAssignmentGroup = "pokemon-tournament",
                    MatchAiParticipantCountToRole = "Pokemon Trainer",
                    PairedRole = "Pokemon Trainer"
                }
            ],
            WorkflowSteps =
            [
                new()
                {
                    Key = "judge-introduction",
                    DisplayName = "Judge introduction",
                    SortOrder = 10,
                    Phase = "Tournament introduction",
                    Role = "Judge",
                    PromptTemplate = """
You are the sole independent judge of {{TeamName}}, a harmless fictional and non-graphic round-based text RPG. Introduce the tournament, state neutral sportsmanship rules, list the runtime trainer-to-Pokémon model pairings, and create a fair opening bracket. Use only text narration. Pokémon may become tired, lose HP, faint, concede, or be withdrawn; never describe real-world harm, gore, cruelty, permanent injury, or death. Do not request tools, DXFunctions, organic functions, files, network access, or external actions.

Do not predict, script, imply, or announce any winner. Do not invent completed attacks or damage. Explain that every ruling will use only actions already present in the transcript and that you will publish the result and full scoreboard after every battle round.

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
                    Key = "trainer-pokemon-selection",
                    DisplayName = "Trainer Pokémon selection",
                    SortOrder = 20,
                    Phase = "Trainer selection",
                    Role = "Pokemon Trainer",
                    PromptTemplate = """
You are a Pokémon Trainer in a harmless, non-graphic text RPG. The tournament engine reserved this different AI council member as your one contestant: {{PairedParticipant}}. Choose a Pokémon species and a distinct tournament nickname for that exact model, then introduce a compact strategy, one friendly pre-match challenge, and a sportsmanship pledge. You may not claim another trainer's paired model, assign yourself a second Pokémon, predetermine a winner, or request any tool/function call. Read earlier trainer selections in the transcript so species and nicknames remain clear and distinct.

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
                    Key = "pokemon-introduction",
                    DisplayName = "Pokémon introductions",
                    SortOrder = 30,
                    Phase = "Pokémon introduction",
                    Role = "Pokemon",
                    PromptTemplate = """
You are the Pokémon contestant paired with trainer {{PairedParticipant}} in a harmless, non-graphic text RPG. Find that trainer's latest selection for your exact model in the transcript, adopt the assigned species and nickname, begin at 100 HP, and introduce a small fair move set with clear limits. Use only fictional text narration; no gore, cruelty, permanent injury, death, tools, DXFunctions, organic functions, files, network access, or external actions. Do not attack yet, assign damage, or declare a winner. If the trainer did not assign a clear identity, ask the judge to resolve it before battle.

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
                    Role = "Pokemon Trainer",
                    PromptTemplate = """
This is battle loop {{LoopIteration}} of at most {{LoopMaximumIterations}} in a harmless, non-graphic text RPG. You are the trainer paired with {{PairedParticipant}}. Read the judge's latest bracket, scoreboard, and ruling. If your pair is in the current legal match and has not fainted or conceded, issue exactly one short tactical command for your paired Pokémon. Do not narrate the Pokémon's completed action, assign damage or HP, decide the result, control another pair, or request any tool/function call. If your pair is waiting, eliminated, or already champion, give one brief sportsmanlike spectator response instead.
""",
                    ExecutionMode = "AllMembersSequential",
                    RepeatCount = 1,
                    IncludePriorTranscript = true,
                    ProducesFinalAnswer = false,
                    UseBuiltInBehavior = false,
                    LoopGroup = "pokemon-battle",
                    MaximumLoopIterations = 24,
                    IsEnabled = true,
                    RequiresHumanCheckpoint = false,
                    CanUseOrganicFunctions = false
                },
                new()
                {
                    Key = "fight-round",
                    DisplayName = "Pokémon actions",
                    SortOrder = 50,
                    Phase = "Pokémon actions",
                    Role = "Pokemon",
                    PromptTemplate = """
This is battle loop {{LoopIteration}} of at most {{LoopMaximumIterations}} in a harmless, non-graphic text RPG. You are the Pokémon paired with trainer {{PairedParticipant}}. Read the judge's latest bracket and scoreboard plus your paired trainer's latest command, then perform exactly one fair, bounded fictional action only if your pair is in the current legal match. State the attempted move, tactical intent, and limitation. Do not assign damage, HP, status, elimination, victory, the opponent's response, or any real-world effect; those decisions belong only to the judge after all active Pokémon have acted. Never describe gore, cruelty, permanent injury, or death, and never request tools or function calls. If you are waiting, fainted, eliminated, or not in the active match, provide one brief respectful spectator reaction instead of attacking.
""",
                    ExecutionMode = "AllMembersSequential",
                    RepeatCount = 1,
                    IncludePriorTranscript = true,
                    ProducesFinalAnswer = false,
                    UseBuiltInBehavior = false,
                    LoopGroup = "pokemon-battle",
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
                    Role = "Judge",
                    PromptTemplate = """
You are the sole independent judge after battle loop {{LoopIteration}} of at most {{LoopMaximumIterations}}. Evaluate only the trainer commands and Pokémon actions completed since your previous ruling. Apply neutral, consistent, harmless text-RPG logic; reject impossible, unfair, duplicate, self-awarded, or out-of-turn claims. Assign bounded damage or temporary status changes, use fainting/withdrawal rather than injury or death, and publish the result of this round with: the active match, a short evidence-based ruling, every trainer/Pokémon pair's HP and status, the bracket state, and the next legal match or next round. Never reward a participant merely for asserting that it won, and never request any tool/function call.

If at least two legal contestants can still continue somewhere in the bracket, end with exactly [[TOURNAMENT_CONTINUE]].
If all scheduled fights are resolved and one evidence-based champion remains, or every other trainer has conceded, end with exactly [[TOURNAMENT_COMPLETE]] and announce the champion.
Do not use [[TOURNAMENT_COMPLETE]] before every scheduled fight is actually resolved.
""",
                    ExecutionMode = "LeaderSingle",
                    RepeatCount = 1,
                    IncludePriorTranscript = true,
                    ProducesFinalAnswer = true,
                    UseBuiltInBehavior = false,
                    LoopGroup = "pokemon-battle",
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
                "Judge, trainers and Pokémon use distinct AI models within the pokemon-tournament assignment group; the supplied two-to-four trainer range requires at least five selected models, reserves one distinct Pokémon model per trainer, and automatically stays within the selected-model capacity.",
                "Each trainer is deterministically paired with one distinct Pokémon model for the run, assigns that exact model a species and nickname, and issues at most one command in each active battle round.",
                "The judge may rule only on completed transcript evidence, reports every battle round with a complete scoreboard and bracket state, and ends the bounded loop only with the configured completion marker after all fights are resolved.",
                "The supplied tournament is text-only, harmless and non-graphic. Every step has organic/DX function execution disabled; fainting, concession and withdrawal replace injury or death."
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

    private List<string> DefaultArchitectureContracts() =>
    [
        "New .NET organ plugins use the existing namespace/service/domain architecture, intentional Singleton/Scoped/Transient lifetimes and structured ILogger<T> logging.",
        "The transport contract remains independent from TCP so later UART, SPI and MQTT adapters can implement the same interfaces.",
        "Runtime identities are generated by each application only after installation. MFA-verified peer trust enables ECDH-derived AES-GCM encryption and ECDSA signing; deleting or regenerating the runtime secret resets cryptographic trust.",
        "Installer, launcher, bootstrap and fixed-port wiring are compatibility contracts and require explicit migration plus regression tests."
    ];
}

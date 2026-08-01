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

    private List<string> DefaultArchitectureContracts() =>
    [
        "New .NET organ plugins use the existing namespace/service/domain architecture, intentional Singleton/Scoped/Transient lifetimes and structured ILogger<T> logging.",
        "The transport contract remains independent from TCP so later UART, SPI and MQTT adapters can implement the same interfaces.",
        "Runtime identities are generated by each application only after installation. MFA-verified peer trust enables ECDH-derived AES-GCM encryption and ECDSA signing; deleting or regenerating the runtime secret resets cryptographic trust.",
        "Installer, launcher, bootstrap and fixed-port wiring are compatibility contracts and require explicit migration plus regression tests."
    ];
}

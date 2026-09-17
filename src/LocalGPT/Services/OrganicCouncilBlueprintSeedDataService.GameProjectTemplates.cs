using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Provides maintained Council presets for Game-project discovery, development, engine extension work, and playtesting.</summary>
public sealed partial class OrganicCouncilBlueprintSeedDataService
{
    /// <summary>Creates the requirements-first team used to turn a user's game idea into durable Project requirements and an editable Game-project profile.</summary>
    /// <returns>The seeded Game-project discovery team.</returns>
    private OrganicCouncilTeamDefinition CreateGameProjectDiscoveryTeam()
    {
        try
        {
            return new OrganicCouncilTeamDefinition
            {
                Key = "game-project-discovery",
                DisplayName = "Game Project Discovery & Requirements",
                Purpose = "Works with the user before implementation: clarifies the intended player experience, core loop, controls, rules, content boundaries and acceptance criteria, persists agreed items through the normal Project requirement system, and maintains the project-owned game authoring profile without pretending a runtime build is the design source of truth.",
                Roles =
                [
                    new() { Role = "Requirements Facilitator", Expertise = "structured interviews, ambiguity reduction, acceptance criteria and user-language requirement capture", Responsibility = "ask only the smallest useful set of questions, preserve the user's terminology, and persist only requirements the user has actually confirmed" },
                    new() { Role = "Game Design Analyst", Expertise = "core loops, mechanics, progression, pacing, feedback and player agency", Responsibility = "translate the user's desired experience into testable mechanics without silently expanding scope" },
                    new() { Role = "Runtime Feasibility Analyst", Expertise = "LocalGPT Game-project profiles, runtime profiles, ASCII presentation, controls, Council actors and deterministic state", Responsibility = "map requested mechanics onto existing engine/runtime capabilities and clearly identify engine-extension requirements" },
                    new() { Role = "Acceptance Curator", Expertise = "requirement traceability, playtest criteria, edge cases and release readiness", Responsibility = "turn confirmed goals into observable acceptance checks and separate must-have behavior from later ideas" }
                ],
                PreferredCapabilities =
                [
                    "project.architecture.get",
                    "project.game.profile.get",
                    "project.game.profile.save",
                    "project.requirement.save",
                    "project.artifact.save",
                    "human.collaboration.request"
                ],
                WorkflowSteps =
                [
                    Step("game-discovery-context", "Read project and current design", 10, "Discovery", "Runtime Feasibility Analyst", "Read the selected Project architecture, existing requirements and current game authoring profile. Separate confirmed project facts from missing design decisions. Do not build or start the game in this step.", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["project.architecture.get", "project.game.profile.get"]),
                    Step("game-discovery-interview", "Collect user requirements", 20, "Requirements", "Requirements Facilitator", "Ask the user a compact, prioritized requirement question set covering player goal, core loop, controls, win/fail conditions, desired AI/Council participation, presentation, pacing and explicit exclusions. Use human.collaboration.request when answers are missing. Do not infer unconfirmed requirements from genre conventions.", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["human.collaboration.request"]),
                    Step("game-discovery-design", "Translate intent into game design", 30, "Design", "Game Design Analyst", "Translate only confirmed user intent into a minimal playable design: core loop, mechanics, actor responsibilities, feedback, progression and bounded scenario. Mark unresolved choices explicitly instead of choosing for the user.", "AllMembersParallel"),
                    Step("game-discovery-acceptance", "Define acceptance criteria", 40, "Acceptance", "Acceptance Curator", "Define observable acceptance criteria and playtest checks for each confirmed requirement. Flag requests that require an engine extension rather than hiding them inside the game profile.", "AllMembersSequentialOnEachAIHostParallel"),
                    Step("game-discovery-persist", "Persist confirmed project design", 50, "Persistence", "Requirements Facilitator", "Persist each user-confirmed requirement through project.requirement.save and save the agreed runtime-facing authoring values through project.game.profile.save. Keep requirements first-class; do not collapse them into one scenario prompt. Every mutation remains approval-gated.", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["project.requirement.save", "project.game.profile.save"]),
                    Step("game-discovery-handoff", "Discovery handoff", 60, "Synthesis", "Acceptance Curator", "Summarize confirmed requirements, unresolved questions, engine-extension needs, the saved game-profile boundary and the smallest next development milestone. Do not claim a build exists unless one was actually produced later by the Project system.", "LeaderSingle", producesFinalAnswer: true)
                ],
                MainRoundInstructionTemplate = "Requirements are Project-system data. Ask the user when intent is missing, persist only confirmed requirements, keep the editable Game-project profile separate from the compiled runtime definition, and explicitly route unsupported mechanics to engine-extension work.",
                ArchitectureContracts =
                [
                    .. DefaultArchitectureContracts(),
                    "The Project system owns user requirements, game authoring data, revisions and build artifacts; the Game runtime owns none of them.",
                    "User requirements stay as durable LocalGptProjectRequirement records and are not flattened into the scenario prompt or a Council transcript.",
                    "The editable LocalGptGameProjectProfile is project-owned source material; ProjectGameDefinition is a compiled artifact consumed below the Project layer.",
                    "Missing user intent is collected through the maintained human-collaboration boundary rather than guessed from a game genre or existing sample game."
                ]
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the Game-project discovery Council template failed.");
            throw;
        }
    }

    /// <summary>Creates the implementation team used to turn confirmed Game-project requirements into a built and testable project definition.</summary>
    /// <returns>The seeded Game-project development team.</returns>
    private OrganicCouncilTeamDefinition CreateGameProjectDevelopmentTeam()
    {
        try
        {
            return new OrganicCouncilTeamDefinition
            {
                Key = "game-project-development",
                DisplayName = "Game Project Development Team",
                Purpose = "Develops one LocalGPT Game project from confirmed Project requirements and its persisted authoring profile, builds a traceable runtime definition, launches bounded test sessions, and records gaps without moving project ownership into GameDirector.",
                Roles =
                [
                    new() { Role = "Game Project Architect", Expertise = "Project requirements, revisions, build artifacts and game/runtime layering", Responsibility = "maintain Project -> authoring profile -> build definition -> runtime direction and identify engine-extension boundaries" },
                    new() { Role = "Gameplay Systems Designer", Expertise = "rules, actors, controls, encounters, progression and deterministic game-state behavior", Responsibility = "design the smallest mechanics that satisfy confirmed requirements" },
                    new() { Role = "AI and Council Integration Developer", Expertise = "Council roles, GameDirector, player controllers, subdirectors and operator/ASCII interaction", Responsibility = "connect AI participation through existing runtime contracts rather than inventing a second control path" },
                    new() { Role = "Build and Playtest Verifier", Expertise = "project build artifacts, requirement traceability, runtime sessions and reproducible playtests", Responsibility = "build only approved project state, run bounded sessions and report requirement-level evidence and gaps" }
                ],
                PreferredCapabilities =
                [
                    "project.architecture.get",
                    "project.game.profile.get",
                    "project.game.profile.save",
                    "project.game.build",
                    "project.game.get",
                    "project.game.start",
                    "project.requirement.save",
                    "localgpt.game.session.get",
                    "localgpt.game.control",
                    "human.collaboration.request"
                ],
                WorkflowSteps =
                [
                    Step("game-development-baseline", "Load requirements and authoring baseline", 10, "Planning", "Game Project Architect", "Read the selected project, requirements, current authoring profile and latest build. Identify which confirmed requirements are already represented and which require game-profile changes versus engine work.", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["project.architecture.get", "project.game.profile.get", "project.game.get"]),
                    Step("game-development-mechanics", "Design bounded mechanics", 20, "Implementation", "Gameplay Systems Designer", "Design only the next approved playable milestone against the confirmed requirement baseline. Keep canonical state and runtime-profile constraints explicit. Do not use hard-coded built-in game keys as architecture.", "AllMembersParallel"),
                    Step("game-development-ai", "Integrate AI and controls", 30, "Implementation", "AI and Council Integration Developer", "Map human, Operator, AI player, Council role and GameDirector behavior onto the shared game-control/session contracts. Preserve in-ASCII interaction and never make the renderer a source of canonical game state.", "AllMembersSequentialOnEachAIHostParallel"),
                    Step("game-development-profile", "Update project-owned game design", 40, "Persistence", "Game Project Architect", "When the proposed authoring values are agreed, save them through project.game.profile.save. If implementation discovers a new requirement, ask the user where necessary and persist it through project.requirement.save rather than burying it in code or prompts.", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["human.collaboration.request", "project.requirement.save", "project.game.profile.save"]),
                    Step("game-development-build", "Build project definition", 50, "Build", "Build and Playtest Verifier", "Build the Game project through project.game.build only after the authoring profile and requirement baseline are approved. Treat the resulting ProjectGameDefinition as compiled output, not editable design state.", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["project.game.build", "project.game.get"]),
                    Step("game-development-playtest", "Run bounded playtest", 60, "Verification", "Build and Playtest Verifier", "Start the approved project build and inspect canonical runtime state. Exercise only enough controls to verify the current milestone. Record mismatches by requirement and do not rewrite requirements merely to match current behavior.", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["project.game.start", "localgpt.game.session.get", "localgpt.game.control"]),
                    Step("game-development-handoff", "Development handoff", 70, "Synthesis", "Game Project Architect", "Report implemented requirement coverage, current profile/build identities, playtest evidence, engine-extension blockers and the next user-approved milestone.", "LeaderSingle", producesFinalAnswer: true)
                ],
                MainRoundInstructionTemplate = "Develop from durable Project requirements. Project-owned authoring precedes build; build precedes runtime. Keep AI, controls and ASCII presentation below the build boundary and route missing engine capabilities to the engine-extension team.",
                ArchitectureContracts =
                [
                    .. DefaultArchitectureContracts(),
                    "Project requirements and LocalGptGameProjectProfile are authoring sources; ProjectGameDefinition is compiled output and GameDirector is a consumer.",
                    "A runtime session may prove or disprove a requirement but may not silently redefine the requirement.",
                    "Human, Operator, AI, Council and automated control paths converge on the same runtime control contract.",
                    "ASCII remains a renderer/presentation backend and never becomes the authority for game state."
                ]
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the Game-project development Council template failed.");
            throw;
        }
    }

    /// <summary>Creates the team used when a Game-project requirement needs a reusable extension to the LocalGPT game engine/runtime rather than game-specific authoring.</summary>
    /// <returns>The seeded game-engine extension development team.</returns>
    private OrganicCouncilTeamDefinition CreateGameEngineExtensionDevelopmentTeam()
    {
        try
        {
            return new OrganicCouncilTeamDefinition
            {
                Key = "game-engine-extension-development",
                DisplayName = "Game Engine Extension Development Team",
                Purpose = "Develops reusable LocalGPT game-engine/runtime capabilities when a Game project cannot satisfy a confirmed requirement with existing profiles, while preserving the Project -> compiled definition -> runtime -> renderer/controller layering and the repository's normal architecture/build policies.",
                Roles =
                [
                    new() { Role = "Engine Boundary Architect", Expertise = "Project/runtime layering, reusable contracts, DI services and persistence boundaries", Responsibility = "prove that a requested capability belongs in the engine and place it at the lowest reusable layer" },
                    new() { Role = "Simulation Runtime Developer", Expertise = "authoritative state, turns/ticks, actors, rules, collision, replay and serialization", Responsibility = "implement reusable simulation behavior without coupling it to one game's name, project or renderer" },
                    new() { Role = "Renderer and Input Adapter Developer", Expertise = "ASCII frames, viewport/input adapters, controller contracts and future rendering backends", Responsibility = "extend presentation or input through adapters while preserving canonical runtime state" },
                    new() { Role = "AI Runtime Integration Developer", Expertise = "GameDirector, Council roles, tool contracts and bounded autonomous control", Responsibility = "extend AI participation through shared runtime contracts and deterministic validation boundaries" },
                    new() { Role = "Engine Verification Curator", Expertise = "architecture guards, migrations, regression testing, replay/state verification and compatibility", Responsibility = "verify reusable behavior against existing built-in and project-built games before release" }
                ],
                PreferredCapabilities =
                [
                    "project.architecture.get",
                    "project.maintenance.get",
                    "project.workspace.files.list",
                    "project.workspace.file.read",
                    "project.files.scan",
                    "project.revision.build.verify",
                    "project.revision.council-review",
                    "project.revision.ready.approve",
                    "project.requirement.save",
                    "localgpt.regex.list",
                    "localgpt.regex.test"
                ],
                WorkflowSteps =
                [
                    Step("engine-extension-boundary", "Prove engine ownership", 10, "Architecture", "Engine Boundary Architect", "Read the originating project requirement and current runtime architecture. Prove why this capability is reusable engine behavior rather than game-specific data. Define the lowest contract that can satisfy more than one game without moving project ownership downward.", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["project.architecture.get", "project.maintenance.get"]),
                    Step("engine-extension-simulation", "Design simulation contract", 20, "Design", "Simulation Runtime Developer", "Design authoritative state and transition changes, persistence/replay implications, version compatibility and deterministic validation. Avoid game-name switches and renderer dependencies.", "AllMembersParallel"),
                    Step("engine-extension-adapters", "Design adapters", 30, "Design", "Renderer and Input Adapter Developer", "Define renderer/input adapter changes so ASCII remains supported while future backends can consume the same committed state. Keep controls transport-neutral and runtime-owned.", "AllMembersSequentialOnEachAIHostParallel"),
                    Step("engine-extension-ai", "Design AI integration", 40, "Design", "AI Runtime Integration Developer", "Map AI/Council proposals onto the same typed controller/session contracts used by humans. The model proposes; deterministic engine validation commits.", "AllMembersSequentialOnEachAIHostParallel"),
                    Step("engine-extension-policy", "Repository policy and migration review", 50, "Verification", "Engine Verification Curator", "Check required EF migrations/snapshots, DI lifetimes, async continuation policy, text-service ownership, application-static policy, diagnostics, XML docs and backward compatibility before any release handoff.", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["localgpt.regex.list", "localgpt.regex.test", "project.revision.council-review"]),
                    Step("engine-extension-handoff", "Engine extension handoff", 60, "Synthesis", "Engine Boundary Architect", "Summarize the reusable engine change, affected contracts/migrations, compatibility impact, verification evidence and which originating Game-project requirements can now proceed.", "LeaderSingle", producesFinalAnswer: true)
                ],
                MainRoundInstructionTemplate = "Treat engine work as a reusable lower-layer capability only after proving it cannot remain project data. Preserve Project ownership above compiled game definitions, keep runtime canonical state independent from renderers, and obey the repository's normal architecture guards without exemptions.",
                ArchitectureContracts =
                [
                    .. DefaultArchitectureContracts(),
                    "Engine extensions are reusable lower-layer capabilities; game-specific rules, names, scenarios and requirements remain Project-system data.",
                    "Schema changes require an explicit EF migration plus matching model snapshot and compatibility review; disabling maintenance guards is not an acceptable implementation technique.",
                    "Runtime canonical state is independent from ASCII rendering, chat components and Council transcripts.",
                    "New engine capabilities preserve existing built-in games and project-built games unless an explicit versioned migration is approved."
                ]
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the game-engine extension development Council template failed.");
            throw;
        }
    }

    /// <summary>Creates the independent team used to playtest built Game projects against their durable requirement baseline.</summary>
    /// <returns>The seeded Game-project playtest team.</returns>
    private OrganicCouncilTeamDefinition CreateGameProjectPlaytestTeam()
    {
        try
        {
            return new OrganicCouncilTeamDefinition
            {
                Key = "game-project-playtest",
                DisplayName = "Game Project Playtest & QA",
                Purpose = "Runs bounded project-built game sessions, evaluates controls, pacing, rule consistency and AI behavior against the durable Project requirement baseline, and reports defects without changing requirements to fit the current implementation.",
                Roles =
                [
                    new() { Role = "Player Experience Tester", Expertise = "controls, clarity, feedback, pacing and discoverability", Responsibility = "play from the user's perspective and report concrete friction with turn/frame evidence" },
                    new() { Role = "Rules and Balance Tester", Expertise = "mechanic consistency, difficulty, exploits, progression and fairness", Responsibility = "exercise bounded edge cases and distinguish design choices from rule defects" },
                    new() { Role = "AI Behavior Tester", Expertise = "GameDirector, Council players, subdirectors and autonomous control", Responsibility = "verify AI actions remain within the same control/rule contract as human actions" },
                    new() { Role = "Requirement Verification Lead", Expertise = "acceptance criteria, traceability, reproducibility and defect triage", Responsibility = "map each observation to a durable project requirement and produce the final pass/gap report" }
                ],
                PreferredCapabilities =
                [
                    "project.architecture.get",
                    "project.game.get",
                    "project.game.start",
                    "localgpt.game.session.get",
                    "localgpt.game.control",
                    "project.requirement.save",
                    "human.collaboration.request"
                ],
                WorkflowSteps =
                [
                    Step("game-playtest-baseline", "Load build and requirements", 10, "Preparation", "Requirement Verification Lead", "Read the project requirement baseline and latest compiled game definition. Select a small set of acceptance checks for this run and do not modify the build.", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["project.architecture.get", "project.game.get"]),
                    Step("game-playtest-start", "Start test session", 20, "Execution", "Player Experience Tester", "Start the approved project build and inspect its initial canonical state. Keep the session inside the normal game runtime and ASCII controls.", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["project.game.start", "localgpt.game.session.get"]),
                    Step("game-playtest-player", "Player experience pass", 30, "Execution", "Player Experience Tester", "Exercise a bounded sequence of normal player controls. Record confusing feedback, blocked progress, scroll/input problems or control mismatches with the relevant turn/state evidence.", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["localgpt.game.session.get", "localgpt.game.control"]),
                    Step("game-playtest-rules", "Rules and balance pass", 40, "Execution", "Rules and Balance Tester", "Probe a small set of edge cases relevant to the selected requirements. Do not brute-force the game or keep an unattended loop running.", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["localgpt.game.session.get", "localgpt.game.control"]),
                    Step("game-playtest-ai", "AI control parity pass", 50, "Execution", "AI Behavior Tester", "Verify AI/Council-controlled actions use the same accepted control semantics and cannot bypass deterministic rules. Compare canonical state before and after representative AI actions.", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["localgpt.game.session.get", "localgpt.game.control"]),
                    Step("game-playtest-report", "Requirement-level QA report", 60, "Verification", "Requirement Verification Lead", "Produce a requirement-by-requirement evidence report with Passed, Gap, Regression or Not Tested. Propose new defect requirements when appropriate, but persist them only through project.requirement.save after user approval.", "LeaderSingle", canUseOrganicFunctions: true, producesFinalAnswer: true, allowedAutomaticFunctions: ["project.requirement.save", "human.collaboration.request"])
                ],
                MainRoundInstructionTemplate = "Playtest the built artifact, not the design transcript. Use canonical session state as runtime evidence, map findings to durable Project requirements, and never weaken a requirement simply because the current build fails it.",
                ArchitectureContracts =
                [
                    .. DefaultArchitectureContracts(),
                    "Playtest evidence is runtime evidence; Project requirements remain the acceptance authority.",
                    "A playtest may create a proposed defect requirement through the normal approval path but does not rewrite confirmed requirements automatically.",
                    "Human and AI actions are compared through the same control/session contracts.",
                    "Bounded playtests stop cleanly; they do not create unattended Council/game loops."
                ]
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the Game-project playtest Council template failed.");
            throw;
        }
    }
}

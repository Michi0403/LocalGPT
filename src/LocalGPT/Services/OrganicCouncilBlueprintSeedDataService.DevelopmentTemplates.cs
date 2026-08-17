using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates organic council blueprint seed behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class OrganicCouncilBlueprintSeedDataService
    {
    /// <summary>
    /// Creates a low-latency game council in which the deterministic GameDirector remains the authoritative engine.
    /// </summary>
    /// <returns>The seeded GameDirector runtime team definition.</returns>
    private OrganicCouncilTeamDefinition CreateGameDirectorRuntimeTeam() {
    try
    {
        return new()
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
            Step("game-object-reactions", "Predict reactive objects", 30, "Prediction", "Reactive Object Subdirector", "Evaluate doors, switches, hazards, pickups and triggers as factory-created runtime objects. Propose reactions only; do not commit them.", "AllMembersSequentialOnEachAIHostParallel"),
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateGameDirectorRuntimeTeam)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateGameDirectorRuntimeTeam)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates the modern hosted C# development team based on LocalGPT's PowerShell validation order.
    /// </summary>
    /// <returns>The seeded C# development team definition.</returns>
    private OrganicCouncilTeamDefinition CreateCSharpModernHostDevelopmentTeam() {
    try
    {
        return CreateDevelopmentTeam(
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateCSharpModernHostDevelopmentTeam)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateCSharpModernHostDevelopmentTeam)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates a PowerShell build-system development team that mirrors LocalGPT's repository guard sequence.
    /// </summary>
    /// <returns>The seeded PowerShell development team definition.</returns>
    private OrganicCouncilTeamDefinition CreatePowerShellBuildDevelopmentTeam() {
    try
    {
        return CreateDevelopmentTeam(
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreatePowerShellBuildDevelopmentTeam)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreatePowerShellBuildDevelopmentTeam)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates a Java hosted-application development team with Maven/Gradle project and regex roles.
    /// </summary>
    /// <returns>The seeded Java development team definition.</returns>
    private OrganicCouncilTeamDefinition CreateJavaHostedDevelopmentTeam() {
    try
    {
        return CreateDevelopmentTeam(
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateJavaHostedDevelopmentTeam)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateJavaHostedDevelopmentTeam)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates a Minecraft development team with distinct datapack, scripting and Java-mod roles.
    /// </summary>
    /// <returns>The seeded Minecraft development team definition.</returns>
    private OrganicCouncilTeamDefinition CreateMinecraftDevelopmentTeam() {
    try
    {
        return CreateDevelopmentTeam(
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateMinecraftDevelopmentTeam)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateMinecraftDevelopmentTeam)} failed.");
        throw;
    }
}

    }
}

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
        string buildInstruction) {
    try
    {
        return new()
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
            Step("development-implementation", "Bounded implementation proposals", 30, "Implementation", implementationRole, "Propose or generate only the approved milestone. Keep each file attributable to the project/revision, follow the selected regex/file policy, add XML/API documentation and do not smuggle execution into generation.", "AllMembersSequentialOnEachAIHostParallel"),
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateDevelopmentTeam)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateDevelopmentTeam)} failed.");
        throw;
    }
}

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
    /// <param name="requiresHumanCheckpoint">Whether the round waits at the maintained human collaboration boundary before execution.</param>
    /// <param name="enableRolePeerReview">Whether same-role members run the optional usefulness/vote pass after their primary answers.</param>
    /// <param name="summarizeRoleResults">Whether one role member consolidates the same-role results before downstream workflow steps.</param>
    /// <param name="includePriorTranscript">Whether the prior Council transcript is appended when the prompt template does not explicitly place it.</param>
    /// <param name="allowedAutomaticFunctions">Optional exact registered-function allow-list for automatic provider tools on this step.</param>
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
        bool producesFinalAnswer = false,
        bool requiresHumanCheckpoint = false,
        bool enableRolePeerReview = false,
        bool summarizeRoleResults = false,
        bool includePriorTranscript = true,
        IReadOnlyList<string>? allowedAutomaticFunctions = null) {
    try
    {
        return new()
    {
        Key = key,
        DisplayName = displayName,
        SortOrder = sortOrder,
        Phase = phase,
        Role = role,
        PromptTemplate = prompt,
        ExecutionMode = executionMode,
        IncludePriorTranscript = includePriorTranscript,
        IsEnabled = true,
        CanUseOrganicFunctions = canUseOrganicFunctions,
        AutomaticFunctionPolicyMode = !canUseOrganicFunctions
            ? CouncilAutomaticFunctionPolicyMode.Disabled
            : allowedAutomaticFunctions is { Count: > 0 }
                ? CouncilAutomaticFunctionPolicyMode.ExactAllowList
                : CouncilAutomaticFunctionPolicyMode.AllPolicyApproved,
        AllowedAutomaticFunctions = allowedAutomaticFunctions?.ToList() ?? [],
        ProducesFinalAnswer = producesFinalAnswer,
        RequiresHumanCheckpoint = requiresHumanCheckpoint,
        EnableRolePeerReview = enableRolePeerReview,
        SummarizeRoleResults = summarizeRoleResults,
        UseBuiltInBehavior = false
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(Step)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(Step)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs default architecture contracts as part of the organic council blueprint seed service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The shared architecture-contract list.</returns>
    private List<string> DefaultArchitectureContracts() {
    try
    {
        return [
        "New .NET organ plugins use the existing namespace/service/domain architecture, intentional Singleton/Scoped/Transient lifetimes and structured ILogger<T> logging.",
        "The transport contract remains independent from TCP so later UART, SPI and MQTT adapters can implement the same interfaces.",
        "Runtime identities are generated by each application only after installation. MFA-verified peer trust enables ECDH-derived AES-GCM encryption and ECDSA signing; deleting or regenerating the runtime secret resets cryptographic trust.",
        "Installer, launcher, bootstrap and fixed-port wiring are compatibility contracts and require explicit migration plus regression tests."
    ];
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(DefaultArchitectureContracts)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(DefaultArchitectureContracts)} failed.");
        throw;
    }
}

    }
}

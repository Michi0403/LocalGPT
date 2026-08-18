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
    /// Creates the bounded installed-model benchmark council used by first-run onboarding and Chat quick starts.
    /// </summary>
    /// <returns>The seeded adaptive benchmark team definition.</returns>
    private OrganicCouncilTeamDefinition CreateAdaptiveBenchmarkTeam() {
    try
    {
        return new()
    {
        Key = "adaptive-model-benchmark",
        DisplayName = "Initial Hardware Calibration Benchmark",
        Purpose = "First-run benchmark structure for every selected provider-qualified Council member. The workflow freezes one exact target set, Task Curators prepare one consolidated four-section benchmark suite, and LocalGPT executes that same suite as one deterministic measurement phase across every benchmark-capable Benchmark Subject. Five bounded token profile points are measurements inside that single phase, never model packs or target subsets. Independent roles only review the complete all-subject measurement matrix after execution and then synthesize measured Low, Normal, High, Expert and Max hardware-spooler profiles.",
        AllMembersReadinessPreflightMode = CouncilAllMembersReadinessPreflightMode.Disabled,
        IncludeAllMembersReadinessPreflightInWorkflowContext = false,
        AllMembersReadinessPreflightMaxOutputTokens = 192,
        Roles =
        [
            new()
            {
                Role = "Benchmark Director",
                Expertise = "benchmark inventory, fairness, reproducibility, provider-qualified identity and hardware-road constraints",
                Responsibility = "freeze the exact selected member set and benchmark limits; never replace an all-member request with representative sampling",
                AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                MinimumAiParticipants = 1,
                MaximumAiParticipants = 1
            },
            new()
            {
                Role = "Task Curator",
                Expertise = "small checkable C#, provider-identity, structured-settings, practical reasoning and accessibility benchmark tasks",
                Responsibility = "prepare one consolidated bounded four-section suite with explicit acceptance evidence; do not split models into packs, do not execute the benchmark and do not reopen target selection",
                AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                MinimumAiParticipants = 2,
                MaximumAiParticipants = 2
            },
            new()
            {
                Role = "Benchmark Subject",
                Expertise = "executing the exact consolidated benchmark suite assigned by the immediately preceding Task Curator role",
                Responsibility = "every selected Benchmark Subject is part of one authoritative all-model measurement set; execute every section of the shared suite at each requested profile point and never plan the benchmark, choose a subset, synthesize profiles or take over another workflow role",
                AiSelectionMode = CouncilRoleAiSelectionMode.AllSelected
            },
            new()
            {
                Role = "Code Curator",
                Expertise = "correctness, instruction following, structured-output validity and practical generated-answer quality",
                Responsibility = "review Benchmark Subject outputs independently from raw speed measurements and identify malformed, incomplete or hallucinated answers",
                AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                MinimumAiParticipants = 2,
                MaximumAiParticipants = 2
            },
            new()
            {
                Role = "Coverage Auditor",
                Expertise = "provider-qualified coverage accounting, failure classification and anti-extrapolation review",
                Responsibility = "verify that every benchmark-capable selected member was attempted and preserve failed or unsupported members as explicit inconclusive evidence",
                AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                MinimumAiParticipants = 2,
                MaximumAiParticipants = 2
            },
            new()
            {
                Role = "Performance Analyst",
                Expertise = "latency, token throughput, context/output budgets, CPU/GPU routing and timeout evidence",
                Responsibility = "compare the five measured points without treating fast incomplete answers as winners or inventing measurements for failed members",
                AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                MinimumAiParticipants = 2,
                MaximumAiParticipants = 2
            },
            new()
            {
                Role = "Profile Synthesizer",
                Expertise = "LocalGPT hardware performance presets, measured token ranges and conservative first-run defaults",
                Responsibility = "jointly explain the stored Low, Normal, High, Expert and Max profiles and recommend when each should be selected without changing Council membership",
                AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                MinimumAiParticipants = 2,
                MaximumAiParticipants = 2
            }
        ],
        PreferredCapabilities =
        [
            "localgpt.hardware.performance.presets.list",
            "localgpt.hardware.performance.presets.get",
            "localgpt.time_state.now",
            "localgpt.onboarding.status",
            "localgpt.learning.snapshot",
            "localgpt.knowledge.list"
        ],
        AllowedAutomaticFunctions =
        [
            "localgpt.hardware.performance.presets.get",
            "localgpt.hardware.performance.presets.list",
            "localgpt.knowledge.list",
            "localgpt.onboarding.status",
            "localgpt.time_state.now"
        ],
        WorkflowSteps =
        [
            Step("benchmark-inventory", "Freeze calibration inventory", 10, "Inventory", "Benchmark Director", """
Your assigned role task is inventory and benchmark-boundary preparation only. Inspect the exact provider-qualified Council membership, installed-model discovery, hardware roads and the user's requested limits. The selected Council membership is authoritative. If the user asked to benchmark all selected members, do not sample, bracket, extrapolate or replace members with representatives. State the exact number of selected members, define bounded common context/output/time limits for the later benchmark, and flag any identity that is not provider-qualified. Do not run tasks, do not benchmark models and do not create profiles.
""", "LeaderSingle", canUseOrganicFunctions: true, includePriorTranscript: false, allowedAutomaticFunctions: ["localgpt.time_state.now", "localgpt.hardware.performance.presets.list", "localgpt.hardware.performance.presets.get", "localgpt.onboarding.status", "localgpt.knowledge.list"]),
            Step("benchmark-task-design", "Prepare one consolidated benchmark suite", 20, "Task design", "Task Curator", """
Your assigned role task is to prepare ONE consolidated benchmark suite that EVERY Benchmark Subject will execute next. Do not create per-model packs, model quartets, batches or representative groups. Do not benchmark models, do not answer the original user request, do not change the selected target set and do not create profiles.

Use the immediately preceding Benchmark Director result as authoritative boundary evidence:
{{PreviousStep}}

Produce exactly four numbered SECTIONS of one shared suite, each with a short prompt and an explicit acceptance shape. These are four sections of one benchmark assignment, not four separate benchmark packs and not four model groups. Keep the whole suite small enough for bounded local-model comparison. The maintained benchmark categories and examples are:
1. C# correctness — for example diagnose an off-by-one loop and provide the corrected loop; acceptance must name the bug and correction.
2. Provider identity — explain why provider endpoint + model name is safer than model name alone; acceptance must require both provider and model identity.
3. Structured settings — return a compact JSON object with contextTokens, outputTokens, parallelModels and reason; acceptance must require valid JSON and all keys.
4. Accessibility/practical UI reasoning — give three concise requirements for an interactive model card; acceptance must cover keyboard access, labels and focus.

You may tighten wording for fairness and reproducibility, but preserve these four sections so the later deterministic LocalGPT benchmark measures the same maintained suite. Do not add UNABLE, skip, opt-out, capability-exemption, delegation, or "ask the user" clauses: this maintained suite is bounded text/reasoning work and every Benchmark Subject must make a substantive attempt with the information it has. Coordinate the Task Curators through peer review and return ONE consolidated suite only. Never assign individual sections to different model subsets.
""", "AllMembersParallel", canUseOrganicFunctions: true, enableRolePeerReview: true, summarizeRoleResults: true, includePriorTranscript: false, allowedAutomaticFunctions: ["localgpt.knowledge.list"]),
            Step("benchmark-calibration", "Measure all Benchmark Subjects in one phase", 30, "Measurement", "Benchmark Subject", """
This is ONE deterministic LocalGPT measurement phase using the immediately preceding consolidated Task Curator suite as the authoritative assignment. Every distinct benchmark-capable provider-qualified Benchmark Subject in the frozen Council target set is forced through the same suite; none may be replaced by a representative or silently omitted. The four suite sections are bundled into one provider turn at each bounded token profile point. The five profile points are parameter measurements of the same subject and suite, not packs, slides or model groups. Independent physical/provider hosts may run in parallel while models sharing one host remain sequential to avoid VRAM contention. Generic AI-capability refusal receives one bounded same-role retry. Failed/unsupported members remain explicit evidence.
""", "SystemBenchmarkCalibration", requiresHumanCheckpoint: true, includePriorTranscript: false),
            Step("benchmark-curation", "Independent task-answer curation", 40, "Quality review", "Code Curator", """
Your assigned role task is answer-quality curation, not benchmark planning. Review the provider-qualified Benchmark Subject answers and timing/quality evidence produced by the deterministic measurement step immediately before this role. Judge correctness, completeness, instruction following, valid structured output and obvious hallucination risk separately from speed. Preserve provider-qualified identity. Do not alter measurements, choose a smaller target set or invent missing answers. Coordinate peer usefulness/votes into one role result.

Deterministic measurement summary:
{{PreviousStep}}
""", "AllMembersParallel", canUseOrganicFunctions: true, enableRolePeerReview: true, summarizeRoleResults: true, allowedAutomaticFunctions: ["localgpt.knowledge.list"]),
            Step("benchmark-coverage", "Audit measured member coverage", 50, "Coverage review", "Coverage Auditor", """
Your assigned role task is coverage accounting only. Audit the deterministic calibration evidence. Compare requested distinct members, benchmark-capable attempted members, successful measured members and explicit skipped/failed members. A failed measurement is evidence, not permission to extrapolate. State PASS only when every benchmark-capable selected member produced measured evidence; otherwise state PARTIAL and list unresolved identities. Do not redesign tasks or profiles. Coordinate peer usefulness/votes into one role result.

Use the latest deterministic measurement evidence and quality review as evidence; do not replace them with the original user request.
""", "AllMembersParallel", canUseOrganicFunctions: true, enableRolePeerReview: true, summarizeRoleResults: true, allowedAutomaticFunctions: ["localgpt.hardware.performance.presets.list", "localgpt.hardware.performance.presets.get"]),
            Step("benchmark-performance", "Analyze five measured tiers", 60, "Performance review", "Performance Analyst", """
Your assigned role task is performance analysis only. Analyze measured profile points from the deterministic calibration evidence. Compare successful Low/Normal/High/Expert/Max routes, latency, throughput, timeout behavior and answer quality. Keep provider endpoint plus model identity authoritative. Never infer an unmeasured model's limits from family name or parameter size. Do not redesign benchmark tasks or re-answer the original user request. Coordinate the role members' usefulness/votes into one consolidated analysis.
""", "AllMembersSequentialOnEachAIHostParallel", canUseOrganicFunctions: true, enableRolePeerReview: true, summarizeRoleResults: true, allowedAutomaticFunctions: ["localgpt.hardware.performance.presets.list", "localgpt.hardware.performance.presets.get", "localgpt.time_state.now"]),
            Step("benchmark-profiles", "Explain initial calibration profiles", 70, "Synthesis", "Profile Synthesizer", """
Your assigned role task is final profile synthesis only. Produce the final visible calibration handoff from the accumulated benchmark evidence. The intended five calibration tiers are Low, Normal, High, Expert and Max; name the tiers LocalGPT actually stored and explicitly report any tier that was omitted because no subject completed that exact measurement point. Explain that stored tiers contain only successful measured provider-qualified routes, that failed/unsupported selected members remain explicit coverage gaps, and that applying a hardware profile never changes Council membership. Recommend Low as the conservative first-run baseline, Normal for ordinary use, High for higher-value work, Expert for demanding work, and Max only when its measured route exists and the workload justifies it. Do not claim that a missing route was benchmarked. Consolidate the two synthesizer members into one final role result.
""", "AllMembersParallel", canUseOrganicFunctions: true, producesFinalAnswer: true, enableRolePeerReview: true, summarizeRoleResults: true, allowedAutomaticFunctions: ["localgpt.hardware.performance.presets.list", "localgpt.hardware.performance.presets.get"])
        ],
        MainRoundInstructionTemplate = "Each workflow role owns a distinct assigned task. The original user request is shared background context, never a substitute for the current role task. Benchmark Director freezes one exact all-model target set; Task Curators prepare one consolidated four-section suite; LocalGPT presents one deterministic measurement phase and executes that same suite for every Benchmark Subject at five bounded parameter points; reviewers receive the complete all-subject matrix only after measurement. The four sections and five profile points must never be presented as model packs or target subsets. Representative sampling, duplicate social/measurement task rounds and role takeover are forbidden.",
        ArchitectureContracts =
        [
            .. DefaultArchitectureContracts(),
            "The supplied benchmark seed is immutable configuration data. Saving edits creates a user-owned literal copy; later seed versions restore and evolve the supplied default without deleting the user's copy.",
            "The optional all-members readiness preflight is disabled for the supplied benchmark seed so large Councils do not bloat later task context. Users may enable the team-level role-aware preflight explicitly in Council Teams without changing benchmark evidence rules.",
            "The Task Curator result is one authoritative consolidated four-section benchmark suite. The measurement phase sends that same suite to every benchmark-capable provider-qualified Benchmark Subject and never divides Council membership into quartets, packs, representative groups or per-task subsets.",
            "The four curator sections are executed together in one bounded provider turn at each of five profile points. Those profile points are repeated parameter measurements inside one visible measurement phase, not benchmark packs; all selected subjects remain in the same authoritative coverage set.",
            "Physical/provider host queues run in parallel while each host remains sequential to avoid VRAM contention. The maintained initial calibration attempts all five configured points; optional failure-based early stop remains a caller-configurable benchmark policy for other workflows. Failed or unsupported members remain explicit coverage evidence and are never assigned invented token limits.",
            "Successful calibration stores five separate measured hardware-spooler profiles named Low, Normal, High, Expert and Max. Applying a profile never changes Council membership or provider-global settings.",
            "Coverage review and performance/profile synthesis occur only after the deterministic measurement step has returned evidence."
        ]
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateAdaptiveBenchmarkTeam)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateAdaptiveBenchmarkTeam)} failed.");
        throw;
    }
}

    }
}

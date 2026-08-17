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
        Purpose = "First-run benchmark social structure for all selected provider-qualified Council members. The workflow freezes the exact target set, Task Curators prepare one concrete checkable task pack, and then LocalGPT makes every benchmark-capable provider-qualified Benchmark Subject execute that exact curated pack at four bounded measured profile points. Only after measured execution do independent roles curate quality, audit coverage, analyze performance and synthesize measured Low, Middle, High and Expert hardware-spooler profiles.",
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
                Responsibility = "prepare one concrete bounded task pack with explicit acceptance evidence for the Benchmark Subjects; do not execute the benchmark and do not reopen target selection",
                AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                MinimumAiParticipants = 2,
                MaximumAiParticipants = 2
            },
            new()
            {
                Role = "Benchmark Subject",
                Expertise = "executing the exact benchmark task pack assigned by the immediately preceding Task Curator role",
                Responsibility = "perform every assigned task exactly once and return task answers only; never plan the benchmark, choose models, synthesize profiles or take over another workflow role",
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
                Responsibility = "compare the four measured points without treating fast incomplete answers as winners or inventing measurements for failed members",
                AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                MinimumAiParticipants = 2,
                MaximumAiParticipants = 2
            },
            new()
            {
                Role = "Profile Synthesizer",
                Expertise = "LocalGPT hardware performance presets, measured token ranges and conservative first-run defaults",
                Responsibility = "jointly explain the stored Low, Middle, High and Expert profiles and recommend when each should be selected without changing Council membership",
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
            Step("benchmark-task-design", "Prepare concrete benchmark task pack", 20, "Task design", "Task Curator", """
Your assigned role task is to prepare the concrete task pack that every Benchmark Subject will execute next. Do not benchmark models, do not answer the original user request, do not change the selected target set and do not create profiles.

Use the immediately preceding Benchmark Director result as authoritative boundary evidence:
{{PreviousStep}}

Produce exactly four numbered tasks with a short prompt and an explicit acceptance shape for each. Keep them small enough for bounded local-model comparison. The maintained benchmark categories and examples are:
1. C# correctness — for example diagnose an off-by-one loop and provide the corrected loop; acceptance must name the bug and correction.
2. Provider identity — explain why provider endpoint + model name is safer than model name alone; acceptance must require both provider and model identity.
3. Structured settings — return a compact JSON object with contextTokens, outputTokens, parallelModels and reason; acceptance must require valid JSON and all keys.
4. Accessibility/practical UI reasoning — give three concise requirements for an interactive model card; acceptance must cover keyboard access, labels and focus.

You may tighten wording for fairness and reproducibility, but preserve these four task categories so the later deterministic LocalGPT benchmark measures the same maintained task family. Do not add UNABLE, skip, opt-out, capability-exemption, delegation, or "ask the user" clauses: these four maintained tasks are bounded text/reasoning work and every Benchmark Subject must make a substantive attempt with the information it has. Coordinate the two Task Curators through peer review and return one consolidated task pack only.
""", "AllMembersParallel", canUseOrganicFunctions: true, enableRolePeerReview: true, summarizeRoleResults: true, includePriorTranscript: false, allowedAutomaticFunctions: ["localgpt.knowledge.list"]),
            Step("benchmark-calibration", "Execute and measure every Benchmark Subject", 30, "Measurement", "Benchmark Subject", """
This step is executed by the LocalGPT benchmark calibration service using the immediately preceding consolidated Task Curator result as the authoritative assignment. Every distinct benchmark-capable provider-qualified member performs that exact four-task pack at four bounded token profile points. The four curator tasks are bundled into one provider turn per profile so the system measures the requested work without multiplying it into four duplicate calls. Generic AI-capability refusal receives one bounded same-role retry. Failed/unsupported members remain explicit evidence; no model is sampled away or assigned invented limits.
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
            Step("benchmark-performance", "Analyze four measured tiers", 60, "Performance review", "Performance Analyst", """
Your assigned role task is performance analysis only. Analyze measured profile points from the deterministic calibration evidence. Compare successful Low/Middle/High/Expert routes, latency, throughput, timeout behavior and answer quality. Keep provider endpoint plus model identity authoritative. Never infer an unmeasured model's limits from family name or parameter size. Do not redesign benchmark tasks or re-answer the original user request. Coordinate the role members' usefulness/votes into one consolidated analysis.
""", "AllMembersSequentialOnEachAIHostParallel", canUseOrganicFunctions: true, enableRolePeerReview: true, summarizeRoleResults: true, allowedAutomaticFunctions: ["localgpt.hardware.performance.presets.list", "localgpt.hardware.performance.presets.get", "localgpt.time_state.now"]),
            Step("benchmark-profiles", "Explain initial calibration profiles", 70, "Synthesis", "Profile Synthesizer", """
Your assigned role task is final profile synthesis only. Produce the final visible calibration handoff from the accumulated benchmark evidence. Name the four profiles that LocalGPT actually stored: Low, Middle, High and Expert. Explain that they contain only successful measured provider-qualified routes, that failed/unsupported selected members remain explicit coverage gaps, and that applying a hardware profile never changes Council membership. Recommend Low as the conservative first-run baseline, Middle for normal use, High for higher-value work, and Expert only when its measured route exists and the workload justifies it. Do not claim that a missing route was benchmarked. Consolidate the two synthesizer members into one final role result.
""", "AllMembersParallel", canUseOrganicFunctions: true, producesFinalAnswer: true, enableRolePeerReview: true, summarizeRoleResults: true, allowedAutomaticFunctions: ["localgpt.hardware.performance.presets.list", "localgpt.hardware.performance.presets.get"])
        ],
        MainRoundInstructionTemplate = "Each workflow role owns a distinct assigned task. The original user request is shared background context, never a substitute for the current role task. Benchmark Director freezes targets and limits; Task Curators prepare the concrete task pack; the LocalGPT measurement phase makes every Benchmark Subject execute that exact pack independently at four bounded profiles; reviewers curate quality and coverage; analysts and synthesizers work only from measured evidence. Representative sampling, duplicate social/measurement task rounds and role takeover are forbidden.",
        ArchitectureContracts =
        [
            .. DefaultArchitectureContracts(),
            "The supplied benchmark seed is immutable configuration data. Saving edits creates a user-owned literal copy; later seed versions restore and evolve the supplied default without deleting the user's copy.",
            "The optional all-members readiness preflight is disabled for the supplied benchmark seed so large Councils do not bloat later task context. Users may enable the team-level role-aware preflight explicitly in Council Teams without changing benchmark evidence rules.",
            "The Task Curator result is the authoritative benchmark assignment. The measurement phase sends that same consolidated four-task pack to every benchmark-capable provider-qualified Benchmark Subject; it does not replace curator work with a second unrelated hard-coded suite.",
            "The four curator tasks are executed together in one bounded provider turn at each of four profile points, reducing duplicate calls while preserving identical independent work across targets.",
            "Physical/provider host queues run in parallel while each host remains sequential to avoid VRAM contention. Two consecutive profile failures stop unsafe escalation for that target; failed or unsupported members remain explicit coverage evidence and are never assigned invented token limits.",
            "Successful calibration stores four separate measured hardware-spooler profiles named Low, Middle, High and Expert. Applying a profile never changes Council membership or provider-global settings.",
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

using LocalGPT.BusinessObjects;

namespace LocalGPT.Services
{
    /// <summary>Creates maintained compiler, maintenance and release-orchestration Council presets.</summary>
    public sealed partial class OrganicCouncilBlueprintSeedDataService
    {
        /// <summary>Creates a resilient workflow step whose required role slot may be taken over by another saved member after provider failure.</summary>
        private CouncilWorkflowStepDefinition ResilientStep(
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
            IReadOnlyList<string>? allowedAutomaticFunctions = null)
        {
            try
            {
                var automaticFunctions = canUseOrganicFunctions
                    ? (allowedAutomaticFunctions ?? []).Append("human.collaboration.request").Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                    : allowedAutomaticFunctions;
                var step = Step(
                    key,
                    displayName,
                    sortOrder,
                    phase,
                    role,
                    prompt,
                    executionMode,
                    canUseOrganicFunctions,
                    producesFinalAnswer,
                    requiresHumanCheckpoint,
                    enableRolePeerReview,
                    summarizeRoleResults,
                    includePriorTranscript,
                    automaticFunctions);
                step.MemberFailureRecoveryMode = CouncilMemberFailureRecoveryMode.RetrySameThenEligibleRolePool;
                step.MemberFailureRecoveryAttempts = 2;
                return step;
            }
            catch (Exception __serviceMethodException)
            {
                if (__serviceMethodException is OperationCanceledException)
                    logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(ResilientStep)} was canceled.");
                else
                    logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(ResilientStep)} failed.");
                throw;
            }
        }

        /// <summary>Creates the preseeded repository reconstruction and compiler Council requested for interactive Chat maintenance.</summary>
        private OrganicCouncilTeamDefinition CreateProgramCompilerTeam()
        {
            try
            {
                return new()
                {
                Key = "program-compiler-team",
                DisplayName = "Program Compiler & Repository Curator",
                Purpose = "Turns repository ZIPs, loose source trees or text/source blobs into a reviewable project revision, uses regex-backed structure evidence before changes, asks the human only for unresolved intent, and requests approved compiler/build actions through the normal Chat/ASCII review cards.",
                AllMembersReadinessPreflightMode = CouncilAllMembersReadinessPreflightMode.Disabled,
                Roles =
                [
                    new() { Role = "Repository intake and regex analyst", Expertise = "archive/source layout, file signatures, project metadata and bounded regex validation", Responsibility = "classify the supplied ZIP/text evidence, reconstruct the likely repository tree and maintain project/file regex evidence without inventing missing files" },
                    new() { Role = "Program architect", Expertise = "project structure, build systems, dependency boundaries and low-context implementation planning", Responsibility = "turn the verified structure into the smallest buildable/reviewable revision plan and identify exact unresolved user choices" },
                    new() { Role = "Compiler operator", Expertise = "toolchain discovery, workspace permissions, compiler selection, build/test diagnostics and platform constraints", Responsibility = "select the persisted project toolchain and request only the exact approved build/verification action" },
                    new() { Role = "Source curator", Expertise = "independent code review, deletion/rejection of bad generated material, regression checks and source provenance", Responsibility = "approve only evidence-backed files and send unsafe or structurally wrong work back through another bounded review round" },
                    new() { Role = "Human review coordinator", Expertise = "clear decisions, suggested responses and deferred approval boundaries", Responsibility = "surface the smallest concrete user choice as AI-proposed buttons/text instead of hiding consequential work in model prose", HumanParticipationMode = HumanParticipationMode.Optional }
                ],
                PreferredCapabilities =
                [
                    "human.collaboration.request",
                    "chat.upload_workspace_files",
                    "chat.upload_workspace_context",
                    "chat.upload_workspace_file",
                    "project.maintenance.get",
                    "project.workspace.files.list",
                    "project.workspace.file.read",
                    "project.workspace.environment.assess",
                    "project.files.scan",
                    "project.file.patterns.save",
                    "project.artifact.save",
                    "project.revision.build.verify",
                    "project.revision.council-review",
                    "project.revision.ready.approve",
                    "toolchain.knowledge.list",
                    "toolchain.installation.list",
                    "toolchain.discover",
                    "localgpt.regex.list",
                    "localgpt.regex.get",
                    "localgpt.regex.test",
                    "localgpt.regex.upsert",
                    "council.artifact_workspaces",
                    "council.artifact_workspace_files",
                    "council.artifact_workspace_file.read",
                    "council.artifact_workspace_file.write",
                    "council.artifact_workspace_zip"
                ],
                WorkflowSteps =
                [
                    ResilientStep("compiler-intake", "Classify repository or source blob", 10, "Intake", "Repository intake and regex analyst", """
Inspect the active upload workspace and selected project before proposing changes. If the input is a repository ZIP/tree, identify its root, project/solution/build files, source folders, toolchain declarations and version metadata. If the input is one or more text/source blobs, infer a candidate file tree only from syntax, names, declarations and cross-references that are actually present. Use bounded upload/project reads and existing regex evidence. Return a concrete manifest: source evidence -> proposed relative path -> confidence -> reason. Never write into the source upload and never assume a language/runtime that metadata contradicts.

User request:
{{UserPrompt}}
""", "AllMembersSequentialOnEachAIHostParallel", canUseOrganicFunctions: true, enableRolePeerReview: true, summarizeRoleResults: true, includePriorTranscript: false, allowedAutomaticFunctions: ["chat.upload_workspace_files", "chat.upload_workspace_context", "chat.upload_workspace_file", "project.maintenance.get", "project.workspace.files.list", "project.workspace.file.read", "localgpt.regex.list", "localgpt.regex.get", "localgpt.regex.test"]),
                    ResilientStep("compiler-structure-review", "Regex structure review", 20, "Structure review", "Source curator", """
Review the proposed repository/file manifest independently. Test useful structure/content regexes against the supplied evidence and reject files that are duplicates, generated noise, contradictory, path-unsafe or unsupported by the source. When the manifest is not good enough, state exactly what must change so the configured X/round continuation can repeat the analysis rather than accepting a weak tree. Preserve valid minority findings when evidence supports them.

Candidate manifest:
{{PreviousStep}}
""", "AllMembersParallel", canUseOrganicFunctions: true, enableRolePeerReview: true, summarizeRoleResults: true, allowedAutomaticFunctions: ["chat.upload_workspace_files", "chat.upload_workspace_file", "localgpt.regex.list", "localgpt.regex.get", "localgpt.regex.test", "project.maintenance.get"]),
                    ResilientStep("compiler-plan", "Buildable revision plan", 30, "Planning", "Program architect", """
Using only the curated manifest and current project-maintenance metadata, produce the smallest buildable revision plan. Preserve existing code style and project architecture. Identify exact relative paths to create/update/delete, project/version implications, expected toolchain, and verification command arguments already persisted for the workspace. If one consequential choice is genuinely ambiguous, invoke human.collaboration.request for that single decision with 2-4 short SuggestedResponsesText options and the narrowest honest gate; otherwise continue without asking the user to repeat known scope. The inline Chat/ASCII review card is the authoritative pause, not prose saying that you are waiting.
""", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["project.maintenance.get", "project.workspace.files.list", "project.workspace.file.read", "toolchain.knowledge.list", "toolchain.installation.list", "localgpt.regex.list", "localgpt.regex.test"]),
                    ResilientStep("compiler-curate", "Curate candidate source", 40, "Curation", "Source curator", """
Review the candidate implementation/reconstruction against the curated structure and current project files. Correct or reject material that is structurally wrong, duplicated, unsafe, style-breaking or unsupported. Bad generated files should be omitted/deleted from the candidate workspace rather than rationalized. Require another bounded round when Danger findings remain. Do not claim compilation from source inspection.
""", "AllMembersSequentialOnEachAIHostParallel", canUseOrganicFunctions: true, enableRolePeerReview: true, summarizeRoleResults: true, allowedAutomaticFunctions: ["project.maintenance.get", "project.workspace.files.list", "project.workspace.file.read", "localgpt.regex.list", "localgpt.regex.get", "localgpt.regex.test"]),
                    ResilientStep("compiler-toolchain", "Select compiler and build host", 50, "Toolchain", "Compiler operator", """
Read the project's persisted version/workspace/compiler metadata and current toolchain inventory. Prefer an already validated compiler that matches repository metadata. If discovery or a build is required, request it through the registered consequential function so LocalGPT creates a human-review card with the exact operation instead of silently executing it. Report the first root diagnostic only; cascading diagnostics remain secondary.
""", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["project.maintenance.get", "project.workspace.environment.assess", "toolchain.knowledge.list", "toolchain.installation.list"]),
                    ResilientStep("compiler-verification", "Compile, test and review evidence", 60, "Verification", "Compiler operator", """
Use the exact selected revision and persisted build arguments. Request project.revision.build.verify only through the normal deferred human approval path. After build evidence exists, require source-hash stability and independent Council review before readiness. A timeout/provider failure must be recoverable by another configured role member; never convert cancellation into success.
""", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["project.maintenance.get", "project.revision.build.verify", "project.revision.council-review", "project.revision.ready.approve"]),
                    ResilientStep("compiler-handoff", "Curated compiler handoff", 70, "Handoff", "Human review coordinator", """
Summarize the reconstructed/updated project, exact version/revision, files accepted/rejected, regex evidence, compiler/build status and remaining decisions. If an approval/question is still pending, make that concrete through the collaboration request/function path so the same buttons are usable in normal Chat and the ASCII surface. Do not hide a failed member, failed build or unresolved Danger finding behind a consensus sentence.
""", "LeaderSingle", canUseOrganicFunctions: true, producesFinalAnswer: true, allowedAutomaticFunctions: ["project.maintenance.get"])
                ],
                ArchitectureContracts =
                [
                    .. DefaultArchitectureContracts(),
                    "ZIP/text intake is evidence-first: uploaded archives/blobs are inspected through bounded workspace services; archive traversal, absolute paths and escaping links are never accepted.",
                    "Repository reconstruction is two-stage: regex/source analysts propose a path manifest, independent curators verify it, and only approved candidate workspace changes progress to build verification.",
                    "Low-parameter models receive narrow file slices, explicit tagged outputs and persisted regex/toolchain facts instead of an entire repository dump.",
                    "Consequential writes/builds remain deferred-approval functions. AI-proposed suggested responses are presentation data, never implicit user consent.",
                    "Required role work uses RetrySameThenEligibleRolePool so a healthy configured member can replace a failed provider/model slot while the failed attempt remains visible evidence."
                ]
                };
            }
            catch (Exception __serviceMethodException)
            {
                if (__serviceMethodException is OperationCanceledException)
                    logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateProgramCompilerTeam)} was canceled.");
                else
                    logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateProgramCompilerTeam)} failed.");
                throw;
            }
        }

        /// <summary>Creates the preseeded multi-node project maintenance and cross-platform publishing Council.</summary>
        private OrganicCouncilTeamDefinition CreateProjectReleaseOrchestrationTeam()
        {
            try
            {
                return new()
                {
                Key = "project-release-orchestration",
                DisplayName = "Project Maintenance & Cross-Platform Publisher",
                Purpose = "Maintains LocalGPT/PublisherStudio or another selected project, plans versioned release outputs, and orchestrates approved platform-specific work across trusted LocalGPT 1-Wire peers according to OS, advertised DX capabilities and measured hardware performance.",
                AllMembersReadinessPreflightMode = CouncilAllMembersReadinessPreflightMode.Disabled,
                Roles =
                [
                    new() { Role = "Release planner", Expertise = "project versions, changelogs, release matrices and source-hash invariants", Responsibility = "freeze the exact revision/version and release target matrix before any platform work" },
                    new() { Role = "1-Wire host scheduler", Expertise = "trusted LocalGPT peers, operating systems, hardware roads, DXFunction capabilities and load distribution", Responsibility = "match each target to eligible peers and distribute same-OS work according to measured capacity without weakening transport trust" },
                    new() { Role = "Platform build operator", Expertise = "Windows, Linux, macOS build/package toolchains and project-maintenance evidence", Responsibility = "request the exact platform build/package operation assigned to this peer and preserve logs/artifact hashes" },
                    new() { Role = "Documentation and PDF operator", Expertise = "DocFX/documentation generation, PDF rendering and resource-aware scheduling", Responsibility = "route PDF/documentation rendering to the strongest eligible trusted system and verify the produced documentation artifact" },
                    new() { Role = "Release curator", Expertise = "cross-platform artifact review, version directories, checksums, regression evidence and publish readiness", Responsibility = "cross-check every platform result and block a release with missing/failed/mismatched artifacts" }
                ],
                PreferredCapabilities =
                [
                    "human.collaboration.request",
                    "project.maintenance.get",
                    "project.workspace.files.list",
                    "project.workspace.file.read",
                    "project.workspace.environment.assess",
                    "project.revision.build.verify",
                    "project.revision.council-review",
                    "project.revision.ready.approve",
                    "project.artifact.save",
                    "toolchain.knowledge.list",
                    "toolchain.installation.list",
                    "localgpt.public_service.invoke",
                    "council.artifact_workspaces",
                    "council.artifact_workspace_files",
                    "council.artifact_workspace_zip"
                ],
                WorkflowSteps =
                [
                    ResilientStep("release-baseline", "Freeze project revision and version", 10, "Baseline", "Release planner", """
Read project.maintenance.get first. Freeze the selected project id, current version, exact revision/source hash, workspace policy, stored version rows, prior release artifacts and target platforms. Use the project's persisted version; propose an increment only when requested/required and never overwrite another version directory. The default release output convention is artifacts/<version>/<platform>/<runtime-or-package-kind>/ unless the selected project workspace stores another approved artifacts subdirectory. If a version/output-path choice is genuinely unresolved, invoke human.collaboration.request with concise SuggestedResponsesText options so the decision appears directly in Chat/ASCII.
""", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["project.maintenance.get", "project.workspace.files.list", "project.workspace.file.read"]),
                    ResilientStep("release-host-matrix", "Plan trusted LocalGPT host matrix", 20, "Scheduling", "1-Wire host scheduler", """
Build a host/target assignment from currently trusted/reachable LocalGPT peers and their advertised operating system, hardware/performance evidence and DX/public-service capabilities. A platform target may run only on an eligible OS/toolchain host. When two or more peers can build the same OS family, distribute independent packages proportionally to measured capacity instead of pinning everything to one machine. Assign PDF/documentation rendering to the strongest eligible peer. Do not infer trust from reachability; use the existing secure 1-Wire trust/transport contract and never include credentials in prompts/logs.
""", "AllMembersSequentialOnEachAIHostParallel", canUseOrganicFunctions: true, enableRolePeerReview: true, summarizeRoleResults: true, allowedAutomaticFunctions: ["project.maintenance.get", "toolchain.knowledge.list", "toolchain.installation.list"]),
                    ResilientStep("release-builds", "Request platform build and package work", 30, "Build", "Platform build operator", """
For each assigned platform target, use the selected project's persisted workspace/compiler/build arguments and exact source hash. Local work uses project.revision.build.verify; remote work is invoked only through a user-enabled public service / secure 1-Wire peer path and therefore produces a normal human approval card before execution. Keep Windows/macOS/Linux outputs separate and versioned. Never claim a remote result until the peer returns evidence.
""", "AllMembersSequentialOnEachAIHostParallel", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["project.maintenance.get", "project.revision.build.verify", "localgpt.public_service.invoke"]),
                    ResilientStep("release-docs", "Build documentation and PDF on strongest eligible host", 40, "Documentation", "Documentation and PDF operator", """
Select the strongest eligible trusted host from the host matrix for documentation/PDF work. Reuse the project's documented build/publish entry points and cache policy; do not invent a PDF toolchain that the project does not declare. Request consequential remote/local execution through the approval path, preserve the output hash/path, and return the result into the same versioned project release set.
""", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["project.maintenance.get", "localgpt.public_service.invoke"]),
                    ResilientStep("release-curation", "Cross-platform artifact curation", 50, "Curation", "Release curator", """
Compare every returned artifact against the frozen revision/version/target matrix. Reject stale-version, wrong-OS, wrong-architecture, missing-documentation, failed-build and hash-mismatch results. Persist non-sensitive release-path/checksum metadata as project artifacts only through the normal approval path. If one target fails, keep successful independent targets but send that target back through another bounded recovery/review round rather than rerunning the entire release blindly.
""", "AllMembersParallel", canUseOrganicFunctions: true, enableRolePeerReview: true, summarizeRoleResults: true, allowedAutomaticFunctions: ["project.maintenance.get", "project.artifact.save", "project.revision.council-review"]),
                    ResilientStep("release-handoff", "Release/publish handoff", 60, "Handoff", "Release planner", """
Return the versioned release directory plan and one status per platform/package/PDF artifact with host, evidence/hash and unresolved approvals. Request project.revision.ready.approve only when build/test/Council review evidence is complete and the source hash still matches. Any further publish/notarize/upload operation remains an explicit user-reviewed action surfaced in Chat/ASCII. When a human choice is needed, invoke human.collaboration.request with concise SuggestedResponsesText buttons rather than merely asking in prose.
""", "LeaderSingle", canUseOrganicFunctions: true, producesFinalAnswer: true, allowedAutomaticFunctions: ["project.maintenance.get", "project.revision.ready.approve"])
                ],
                ArchitectureContracts =
                [
                    .. DefaultArchitectureContracts(),
                    "The project database owns version/revision/workspace/artifact metadata. Release outputs are grouped by project version and platform; persisted workspace/artifact settings override the default artifacts/<version>/<platform> convention.",
                    "Secure 1-Wire peer trust and advertised capability identity remain separate from reachability. Remote DX/public-service work is invoked only through the existing protected peer execution path and explicit approval policy.",
                    "OS-specific packages are assigned only to compatible peers. Equivalent peers may split independent targets according to measured capability/performance, while PDF/documentation work prefers the strongest eligible peer.",
                    "A failed peer/member may be replaced by another eligible configured role member/host without erasing the original failure evidence or changing the frozen source hash.",
                    "Chat and ASCII surfaces are first-class review consoles: deferred function requests and AI-suggested decisions remain actionable there instead of requiring a separate hidden maintenance UI.",
                    "When a workflow needs a human choice, members invoke human.collaboration.request with bounded SuggestedResponsesText choices so the same durable request renders as buttons in Chat, ASCII conversation mode and the non-game work surface."
                ]
                };
            }
            catch (Exception __serviceMethodException)
            {
                if (__serviceMethodException is OperationCanceledException)
                    logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateProjectReleaseOrchestrationTeam)} was canceled.");
                else
                    logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(CreateProjectReleaseOrchestrationTeam)} failed.");
                throw;
            }
        }
    }
}

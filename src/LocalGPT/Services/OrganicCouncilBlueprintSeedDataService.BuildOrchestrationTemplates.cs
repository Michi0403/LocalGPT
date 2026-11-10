using LocalGPT.BusinessObjects;

namespace LocalGPT.Services
{
    /// <summary>Creates maintained compiler, maintenance and release-orchestration Council presets.</summary>
    public sealed partial class OrganicCouncilBlueprintSeedDataService
    {
        /// <summary>Creates a resilient workflow step whose required role slot may be taken over by another saved member after provider failure.</summary>
        /// <param name="key">Key value supplied to the organic council blueprint seed operation and used when producing its result.</param>
        /// <param name="displayName">Display name value supplied to the organic council blueprint seed operation and used when producing its result.</param>
        /// <param name="sortOrder">Sort order value supplied to the organic council blueprint seed operation and used when producing its result.</param>
        /// <param name="phase">Phase value supplied to the organic council blueprint seed operation and used when producing its result.</param>
        /// <param name="role">Role value supplied to the organic council blueprint seed operation and used when producing its result.</param>
        /// <param name="prompt">Prompt value supplied to the organic council blueprint seed operation and used when producing its result.</param>
        /// <param name="executionMode">Execution mode value supplied to the organic council blueprint seed operation and used when producing its result.</param>
        /// <param name="canUseOrganicFunctions">Value indicating whether can use organic functions should apply to this operation.</param>
        /// <param name="producesFinalAnswer">Value indicating whether produces final answer should apply to this operation.</param>
        /// <param name="requiresHumanCheckpoint">Value indicating whether requires human checkpoint should apply to this operation.</param>
        /// <param name="enableRolePeerReview">Value indicating whether enable role peer review should apply to this operation.</param>
        /// <param name="summarizeRoleResults">Value indicating whether summarize role results should apply to this operation.</param>
        /// <param name="includePriorTranscript">Value indicating whether include prior transcript should apply to this operation.</param>
        /// <param name="allowedAutomaticFunctions">String dependency used by the organic council blueprint seed workflow to provide the corresponding application capability.</param>
        /// <returns>The council workflow step definition produced by the operation.</returns>
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
                // Keep optional workflow flags name-bound: Step also owns curation and
                // deferred-approval gates that this resilient wrapper leaves disabled.
                var step = Step(
                    key,
                    displayName,
                    sortOrder,
                    phase,
                    role,
                    prompt,
                    executionMode,
                    canUseOrganicFunctions: canUseOrganicFunctions,
                    producesFinalAnswer: producesFinalAnswer,
                    requiresHumanCheckpoint: requiresHumanCheckpoint,
                    enableRolePeerReview: enableRolePeerReview,
                    summarizeRoleResults: summarizeRoleResults,
                    includePriorTranscript: includePriorTranscript,
                    allowedAutomaticFunctions: automaticFunctions);
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

        /// <summary>
        /// Gives a controller step (curator, compiler operator, verifier) the authority to send unfinished work back: X-Round
        /// revisit to the target step, offered as a native tool, bounded by two accepted transitions per run.
        /// </summary>
        /// <param name="step">The controller step to extend.</param>
        /// <param name="targetStepKey">Default revisit target, normally the step that produces the work being checked.</param>
        /// <returns>The same step with X-Round revisit enabled.</returns>
        private CouncilWorkflowStepDefinition WithControllerRevisit(CouncilWorkflowStepDefinition step, string targetStepKey)
        {
            try
            {
                step.XFunctionsEnabled = true;
                step.XCanRevisit = true;
                step.XDefaultTargetStepKey = targetStepKey;
                step.XMaximumTransitions = 2;
                return step;
            }
            catch (Exception __serviceMethodException)
            {
                if (__serviceMethodException is OperationCanceledException)
                    logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(WithControllerRevisit)} was canceled.");
                else
                    logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCouncilBlueprintSeedDataService)}.{nameof(WithControllerRevisit)} failed.");
                throw;
            }
        }

        /// <summary>Creates the preseeded repository reconstruction and compiler Council requested for interactive Chat maintenance.</summary>
        /// <returns>The organic council team definition produced by the operation.</returns>
        private OrganicCouncilTeamDefinition CreateProgramCompilerTeam()
        {
            try
            {
                return new()
                {
                Key = "program-compiler-team",
                // The evidence gate accepts either reviewed generated source or a changed staged repository,
                // but never model prose in place of an actual archive. The verification role owns recovery.
                RequiredCompletedFunctions = ["council.development.artifact.verify"],
                DisplayName = "Program Compiler & Repository Curator",
                Purpose = "Creates new programs from explicit user requirements or curates existing repository ZIPs/source trees into reviewable project revisions. It distinguishes greenfield authoring from repository maintenance before reading evidence, generates concrete files through the code-generation review workflow, and uses configured toolchains/build approvals instead of making the user repeat a clear request.",
                AllMembersReadinessPreflightMode = CouncilAllMembersReadinessPreflightMode.Disabled,
                Roles =
                [
                    new() { Role = "Repository intake and regex analyst", Expertise = "intent classification, archive/source layout, file signatures, project metadata and bounded regex validation", Responsibility = "classify greenfield versus existing-project work first; inspect uploaded/project evidence only when it is relevant to the requested artifact, and never let unrelated large context replace implementation" },
                    new() { Role = "Program architect", Expertise = "C#/.NET and multi-language project authoring, build systems, dependency boundaries and exact source planning", Responsibility = "turn the request and verified evidence into exact buildable files and invoke the review-backed generation workflow instead of returning a prose-only implementation" },
                    new() { Role = "Compiler operator", Expertise = "toolchain discovery, workspace permissions, compiler selection, build/test diagnostics and platform constraints", Responsibility = "select the persisted validated project toolchain and request only the exact approved build/verification action" },
                    new() { Role = "Source curator", Expertise = "independent code review, deletion/rejection of bad generated material, regression checks and source provenance", Responsibility = "approve only request-matching files, preserve good source, and send structurally wrong work back through another bounded review round" },
                    new() { Role = "Human review coordinator", Expertise = "clear decisions, suggested responses, generated artifact handoff and deferred approval boundaries", Responsibility = "surface only real consequential approvals and return the generated/build artifact when available instead of asking the human to perform the AI's coding work", HumanParticipationMode = HumanParticipationMode.Optional }
                ],
                PreferredCapabilities =
                [
                    "human.collaboration.request",
                    "chat.upload_workspace_files",
                    "chat.upload_workspace_context",
                    "chat.upload_workspace_file",
                    "council.repository.workspace.stage",
                    "session.sandbox.runs",
                    "session.sandbox.run",
                    "localgpt.console.history",
                    "localgpt.console.execute",
                    "localgpt.path.browse",
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
                    "codegen.capabilities",
                    "codegen.review.create",
                    "codegen.review.get",
                    "codegen.review.execute",
                    "council.artifact_workspaces",
                    "council.artifact_workspace_files",
                    "council.artifact_workspace_file.read",
                    "council.artifact_workspace_file.write",
                    "council.artifact_workspace.codedom.preview",
                    "council.artifact_workspace.codedom.write",
                    "council.artifact_workspace.sandbox.run",
                    "council.development.artifact.verify",
                    "council.artifact_workspace_zip"
                ],
                WorkflowSteps =
                [
                    ResilientStep("compiler-intake", "Classify coding intent and relevant evidence", 10, "Intake", "Repository intake and regex analyst", """
Classify the user's request BEFORE reading any workspace.

If the user explicitly asks to create/code/build a new program, solution, script, library or addon from scratch, treat this as GREENFIELD AUTHORING. Do not inspect an old selected project or uploaded workspace merely because one exists. Use uploaded/project evidence only when the user identifies it as requirements/input/reference for the new artifact. Extract the requested language/framework/version, application kind, required behavior, requested build/test/package action and any explicit output name. When the user specifies .NET 10/C#, honor net10.0 directly. Choose conventional safe defaults for ordinary details instead of asking the user to repeat a clear request.

If the user asks to modify, reconstruct, diagnose or compile an EXISTING project/repository (including a next version of supplied source), classify ExistingProject, inspect its active upload workspace and selected project using bounded list/context/file reads. Use the repository's true version, changelog and PowerShell validation policies. Identify root, project/solution/build files, source folders, toolchain declarations and version metadata. For text/source blobs, infer paths only from evidence actually present.

Large relevant evidence is normal work, not a blocker: list first, read bounded/chunked slices, prioritize build/project files and files directly implicated by the task, and continue iteratively. Large unrelated uploads must not hijack a greenfield task. Return an intent manifest with Mode=Greenfield or ExistingProject plus exact requested deliverables and only the evidence actually needed.

User request:
{{UserPrompt}}
""", "AllMembersSequentialOnEachAIHostParallel", canUseOrganicFunctions: true, enableRolePeerReview: true, summarizeRoleResults: true, includePriorTranscript: false, allowedAutomaticFunctions: ["chat.upload_workspace_files", "chat.upload_workspace_file", "project.maintenance.get", "project.workspace.files.list", "project.workspace.file.read", "localgpt.path.browse"]),
                    ResilientStep("compiler-structure-review", "Review target structure", 20, "Structure review", "Source curator", """
Review the intent manifest independently.

For Greenfield work, verify that the proposed solution/project/file shape directly satisfies the user's requested artifact without inventing dependencies or forcing repository intake. The absence of an existing workspace is expected and is not a reason to stop.

For ExistingProject work, honor pinned compiler/runtime/package versions from the current project/revision; never propose a compiler downgrade as a repair. Check version-relevant database Knowledge/regexes and source manifests before researching; migration requires a separate parent-linked approved revision. Test useful structure/content regexes against bounded supplied evidence and reject duplicate, generated-noise, contradictory, path-unsafe or unsupported file mappings. When more evidence is genuinely required, name the smallest bounded file/slice needed; never demand that an entire huge context be loaded before useful work can continue.

Return a corrected target manifest suitable for concrete source generation or maintenance planning.

Candidate manifest:
{{PreviousStep}}
""", "AllMembersParallel", canUseOrganicFunctions: true, enableRolePeerReview: true, summarizeRoleResults: true, allowedAutomaticFunctions: ["chat.upload_workspace_files", "chat.upload_workspace_file", "localgpt.regex.list", "localgpt.regex.get", "localgpt.regex.test", "project.maintenance.get"]),
                    ResilientStep("compiler-plan", "Exact buildable source plan", 30, "Planning", "Program architect", """
Turn the reviewed target manifest into an exact implementation plan that is immediately executable by LocalGPT's code-generation workflow.

For Greenfield work, specify the exact relative files and full source/configuration needed for a minimal complete artifact. When .NET 10/C# is requested, include a net10.0 project/solution shape and ordinary SDK conventions. Do not answer with pseudocode, a tutorial, or a request to inspect unrelated workspaces. If the user requested compile/build, mark BuildAfterGeneration=true. If the user requested a ZIP/artifact, generation already produces a downloadable ZIP; do not ask the user to package it manually.

For ExistingProject work, preserve current architecture/style and identify exact create/update/delete paths plus revision/toolchain implications. If the input is an uploaded repository archive, use chat.upload_workspace_files to get the exact archiveRoot and stage a complete copy via council.repository.workspace.stage (human approval); edit that isolated copy with the reviewed artifact workspace read/write functions, never reconstruct only a few files and call it a complete repository. If a local registered project is selected instead of an archive, stage it by projectId; when the user supplies an unregistered absolute source folder, stage it through the explicit sourcePath approval, preserving the original folder. A path written in a prompt is not itself an access grant. Do not write back to the original project without separate explicit approval. Honor x.y.z with single-digit y and z; roll x.y.9 to x.(y+1).0 and x.9.9 to (x+1).0.0. NEVER silently change the current project's target framework, compiler or language runtime to match a different locally installed version. First verify the selected revision's global.json / project files / language manifests / Python environment and exact installed toolchains. If the exact compiler is missing, issue a human resource request; an upgrade or downgrade can only be proposed as a new user-approved parent-linked project revision via project.revision.save, never in-place. Ask through human.collaboration.request only when one consequential choice is genuinely unresolved; otherwise continue without asking for scope the user already supplied.

Return the exact source/output plan, not a prose substitute for implementation.
""", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["chat.upload_workspace_files", "council.repository.workspace.stage", "council.artifact_workspace_files", "council.artifact_workspace_file.read", "project.maintenance.get", "project.workspace.files.list", "project.workspace.file.read", "toolchain.knowledge.list", "toolchain.installation.list", "codegen.capabilities"]),
                    ResilientStep("compiler-generate", "Create reviewed source and request generation", 40, "Generation", "Program architect", """
Materialize source through the configured LocalGPT functions. A role result that only describes changes is not a completed coding task. Preserve Council peer review and actual approval receipts rather than treating any one member's suggestion as the final deliverable.

ExistingProject (existing repository ZIP, registered project or approved absolute path):
1. Work in the complete approved council.repository.workspace.stage copy identified in the plan. If staging is pending, preserve the exact pending action; do not forge workspace paths or approval.
2. Read only the needed files with council.artifact_workspace_file.read and inspect the neighboring source conventions. Make the actual approved updates through council.artifact_workspace_file.write. For supported structured declarations, use the existing CodeDOM service through council.artifact_workspace.codedom.preview and the separately approved council.artifact_workspace.codedom.write. CodeDOM is optional: it must complement, not replace, normal file edits, and cannot be used to invent unsupported C# syntax or overwrite unrelated code.
3. Record exact relative paths and each successful write receipt. When review or write approval is pending, preserve the queued action and continue only after the existing collaboration system reports approval; do not create a second duplicate action or tell the human to perform the AI's work manually.
4. Preserve all other files, include version rollover and a repository-convention changelog in approved changes, and leave the originating repository untouched. The full source ZIP is produced after review by council.artifact_workspace_zip, not by generating a few replacement snippets.
5. A previously failed step's "Why this step runs again" identifies the exact wrong/missing file or diagnostic; patch only that file in the same staged workspace using approved tools. Do not switch silently to a greenfield generation workflow.

Greenfield (an explicitly new program):
1. Call codegen.capabilities only if the output shape needs checking. Call codegen.review.create with all exact relative files, any optional codeDomTypes, and output targets. Preserve the selected language/runtime and concise review summaries.
2. Call codegen.review.execute with the returned reviewId/reviewHash and actual nested request, setting buildAfterGeneration only when requested. Approval fields are provided by the registry. Do not invent human confirmation.
3. Preserve a pending approval without filing duplicate reviews. After approval/execution, retain the actual generated workspace, build status and source ZIP receipt. Repair a failed review through a new reviewed generation, never claim it was built merely because files were planned.

For both modes, the artifact verification step must eventually obtain council.development.artifact.verify success, not a model-only statement. A pending approval is a pending action, not a completed artifact.
""", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["codegen.capabilities", "codegen.review.create", "codegen.review.get", "codegen.review.execute", "council.artifact_workspace_files", "council.artifact_workspace_file.read", "council.artifact_workspace_file.write", "council.artifact_workspace.codedom.preview", "council.artifact_workspace.codedom.write", "council.repository.workspace.stage"]),
                    WithControllerRevisit(ResilientStep("compiler-curate", "Curate generated or candidate source", 50, "Curation", "Source curator", """
Review the actual generation/review result and candidate source against the user's request. For completed greenfield generation, inspect generated workspace files only as needed; correct/reject material that is structurally wrong, duplicated, unsafe, style-breaking or incomplete. For an approval-pending generation, report the exact pending review without pretending files already exist. For existing-project maintenance, independently review the staged changed files against the original architecture and existing source. Check CodeDOM output exactly as you would hand-written source and preserve untouched files; use real code, not model claims. Large source is handled in relevant chunks; size alone is never a reason to stop doing the assigned review.

Do not claim compilation from source inspection. Preserve a successful generation/build result instead of replacing it with speculative rework.

If source is missing, wrong, or fails its checks, call council.x.revisit with mode "reexecute" targeting compiler-generate and name the exact missing or wrong paths and diagnostic lines. For staged repositories this re-runs the approved staged editing route, not a new greenfield review. Accept actual changed files, never a description of the fix.
""", "AllMembersSequentialOnEachAIHostParallel", canUseOrganicFunctions: true, enableRolePeerReview: true, summarizeRoleResults: true, allowedAutomaticFunctions: ["codegen.review.get", "council.artifact_workspaces", "council.artifact_workspace_files", "council.artifact_workspace_file.read", "project.maintenance.get", "project.workspace.files.list", "project.workspace.file.read", "localgpt.regex.list", "localgpt.regex.get", "localgpt.regex.test", "session.sandbox.runs", "localgpt.console.history", "council.artifact_workspace.codedom.preview"]), "compiler-generate"),
                    WithControllerRevisit(ResilientStep("compiler-toolchain", "Select compiler and interpret build evidence", 60, "Toolchain", "Compiler operator", """
Use the generation/build result first. For greenfield codegen.review.execute with BuildAfterGeneration=true, its bounded build status is the authoritative compiler attempt; do not redundantly route the task into an unrelated selected project. If a matching configured compiler/toolchain must be discovered or validated, use the persisted toolchain inventory and request only the exact consequential action through the normal approval path.

For ExistingProject work, read persisted project/workspace/compiler metadata, prefer a validated compiler matching repository metadata, and use project build verification only against the intended tracked revision. For approved isolated C#, Python or PowerShell snippet checks, submit the current staged source directly through council.artifact_workspace.sandbox.run when it is a standalone executable guest source file, or use session.sandbox.run for a genuinely independent snippet, and poll session.sandbox.runs for a real completed receipt. The Hyperlight guest does NOT mount the repository. For repository PowerShell guards, request localgpt.console.execute with the exact staged workspace path and a repository-owned .ps1 through the separate human approval gate; never describe a host PowerShell guard as having run in the guest VM. Report the first root diagnostic; cascading diagnostics are secondary.

If source or independent verification fails, call council.x.revisit targeting compiler-generate with exact changed-file or build diagnostics. For ExistingProject work re-execute the staged edit route instead of discarding the repository; for Greenfield work re-execute reviewed generation. Do not turn pending human approval into repeated speculative changes.
""", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["codegen.review.get", "project.maintenance.get", "project.workspace.environment.assess", "toolchain.knowledge.list", "toolchain.installation.list", "project.revision.build.verify", "session.sandbox.run", "session.sandbox.runs", "council.artifact_workspace.sandbox.run", "council.artifact_workspace_file.read", "localgpt.console.execute", "localgpt.console.history"]), "compiler-generate"),
                    WithControllerRevisit(ResilientStep("compiler-verification", "Verify generated artifact or tracked revision", 70, "Verification", "Compiler operator", """
Verify the artifact that the user actually requested.

For greenfield generation, use codegen.review.get plus generated-workspace listing/reading to confirm required files and use the build evidence returned by codegen.review.execute. For Python, also confirm the database project/revision, selected or discoverable Python toolchain, and tracked-file structure/content regex evidence created by the project scan. The generation service already produces a ZIP; use its DownloadUrl. Only call council.artifact_workspace_zip after later approved edits changed the generated workspace and the ZIP must be refreshed.

For an existing staged repository, read the changed source files, request the repository PowerShell guards with explicit host-command approval, report failures verbatim, and package the complete approved workspace with council.artifact_workspace_zip. Then call council.development.artifact.verify with workspaceName. It checks a genuine source-byte delta since approved staging and verifies every byte in the complete ZIP. If it says NotReady, repair the missing source edit or stale archive, do not substitute role prose. For greenfield, call council.development.artifact.verify with the approved executed reviewId after generation to confirm the ZIP exists and no recorded build failed. A completed Hyperlight snippet is additional independent guest evidence, never equivalent to whole-solution .NET compilation. For an existing tracked revision, request project.revision.build.verify through the normal deferred approval path and require source-hash stability plus independent review before readiness. A timeout/provider/model failure is recoverable evidence, never success. Large source/output remains bounded and chunked rather than becoming a reason to abandon verification.

If source or independent verification fails, call council.x.revisit targeting compiler-generate with exact changed-file or build diagnostics. For ExistingProject work re-execute the staged edit route instead of discarding the repository; for Greenfield work re-execute reviewed generation. Do not turn pending human approval into repeated speculative changes.
""", "LeaderSingle", canUseOrganicFunctions: true, allowedAutomaticFunctions: ["codegen.review.get", "council.artifact_workspaces", "council.artifact_workspace_files", "council.artifact_workspace_file.read", "council.artifact_workspace_zip", "council.development.artifact.verify", "project.maintenance.get", "project.revision.build.verify", "project.revision.council-review", "project.revision.ready.approve", "session.sandbox.runs", "localgpt.console.history", "localgpt.console.execute"]), "compiler-generate"),
                    ResilientStep("compiler-handoff", "Generated program and compiler handoff", 80, "Handoff", "Human review coordinator", """
Return the concrete result of the requested coding task only when the verification step recorded council.development.artifact.verify success (or clearly state that completion is pending and identify the missing approved action). Include generated or modified files, framework/toolchain, honest build status and downloadable ZIP/artifact URL when available. For repository work return the complete staged source ZIP, incremented version and changelog, verified PowerShell guard outcomes, Hyperlight guest receipts where run, and any unresolved compiler-only verification clearly flagged. Never claim source tests passed from a queued receipt. Always include the generated WorkspacePath/project path as a manual filesystem fallback. Treat the origin-relative DownloadUrl as callable from the same HTTP or HTTPS LocalGPT endpoint, so remote callers can resolve it against the endpoint they used. If generation/build approval is still pending, state the exact pending action and let the existing Human Collaboration card handle it; do not ask the user to type the request again or manually call a function. If a model/provider failed but another member completed the artifact, preserve both facts. Never turn a clear greenfield coding request back into a repository-search assignment.
""", "LeaderSingle", canUseOrganicFunctions: true, producesFinalAnswer: true, allowedAutomaticFunctions: ["codegen.review.get", "project.maintenance.get", "council.artifact_workspaces", "council.artifact_workspace_files"])
                ],
                ArchitectureContracts =
                [
                    .. DefaultArchitectureContracts(),
                    "Intent precedes intake: explicit greenfield create/code/build requests do not inspect unrelated selected projects or uploads merely because those contexts exist.",
                    "Greenfield coding is artifact-first: the Council creates exact reviewed files through codegen.review.create and routes codegen.review.execute immediately; prose-only implementation plans are incomplete when the user requested a concrete program.",
                    "ZIP/text intake for existing-project work is evidence-first and bounded: uploaded archives/blobs are listed and read in relevant chunks; large relevant input is normal workload, while unrelated large input must not hijack the requested task.",
                    "Repository reconstruction remains two-stage: analysts propose a path manifest, independent curators verify it, and only approved candidate workspace changes progress to build verification.",
                    "ExistingProject and Greenfield use the same artifact-verification receipt but different approved source production paths; CodeDOM is an optional generator within the staged workspace, not a replacement for source-level edits.",
                    "Guest runs of staged standalone source are source-hash bound and never claim whole-repository compilation; repository PowerShell guards retain their own approved host console boundary.",
                    "Low-parameter models receive narrow file slices, explicit tagged outputs and persisted regex/toolchain facts instead of an entire repository dump.",
                    "Consequential generation/writes/builds remain deferred-approval functions. AI-proposed suggested responses are presentation data, never implicit user consent.",
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
        /// <returns>The organic council team definition produced by the operation.</returns>
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

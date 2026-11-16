using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>
/// A model-neutral helper Council that transfers verified knowledge and task decomposition
/// from one selected participant to smaller coding subagents, then requires real receipts.
/// The operator assigns stronger and weaker provider-qualified models through the existing
/// Council role editor; no model family, parameter count or host is hard-coded.
/// </summary>
public sealed partial class OrganicCouncilBlueprintSeedDataService
{
    private OrganicCouncilTeamDefinition CreateKnowledgeAssistedSubagentCodingTeam()
    {
        var prepare = ResilientStep(
            "subagent-evidence", "Retrieve bounded knowledge and establish evidence", 10,
            "Preparation", "Knowledge guide", """
Read the actual user task. Retrieve only relevant verified LocalGPT Knowledge, regex,
project and toolchain facts through the registered read-only functions. Distinguish a
working .NET 10 project from an invented plan. Report a compact evidence packet:
required files, relevant framework/version, verified API/function names, concrete
constraints and the exact next coding task. Cite tool results, not model memories.
Do not generate code or claim a build yet.
Task: {{UserPrompt}}
""", "LeaderSingle", canUseOrganicFunctions: true,
            includePriorTranscript: false,
            allowedAutomaticFunctions: ["localgpt.knowledge.list", "localgpt.regex.list",
                "localgpt.regex.get", "localgpt.regex.test", "project.maintenance.get",
                "toolchain.installation.list", "project.workspace.files.list",
                "project.workspace.file.read", "chat.upload_workspace_files", "chat.upload_workspace_file"]);
        // The guide retrieves bounded evidence explicitly; automatic full-corpus injection is intentionally off.
        prepare.IncludeLocalResearchEvidence = false;
        prepare.TranscriptVisibility = CouncilTranscriptVisibilityMode.None;
        prepare.MemberFailureRecoveryAttempts = 1;

        var implement = ResilientStep(
            "subagent-implementation", "Small-model coding contributions", 20,
            "Implementation", "Coding subagent", """
You are an independent coding subagent. Use the compact verified evidence and
bounded work order below, not the full chat history. Do not try to rediscover the
entire project or imitate the model that prepared the task. Produce exact, small
file contents or a bounded proposed patch for the user's requested behavior.
Do not claim files were written, compiled, tested or downloaded unless a real
registered function result proves it. You may consult the listed Knowledge or
regex tools to settle one specific uncertainty.
User request: {{UserPrompt}}
Grounded preparation: {{PreviousStep}}
""", "AllMembersParallel", canUseOrganicFunctions: true,
            includePriorTranscript: false,
            allowedAutomaticFunctions: ["localgpt.knowledge.list", "localgpt.regex.get",
                "localgpt.regex.test", "project.workspace.file.read", "chat.upload_workspace_file"]);
        implement.TranscriptVisibility = CouncilTranscriptVisibilityMode.None;
        implement.MemberFailureRecoveryAttempts = 1;

        var integrate = ResilientStep(
            "subagent-integration", "Review and materialize the candidate", 30,
            "Integration", "Integration guide", """
Check the subagents' actual proposed files for correctness, including project
metadata, compiler target and explicit user constraints. Give credit for useful
small-model contributions, but do not assume consensus makes code correct.
For greenfield programs invoke codegen.review.create with COMPLETE relative file
paths and COMPLETE source text. This function raises the necessary deferred
codegen.review.execute approval; do not replace its tool call with prose or an
informal yes/no question. For existing repositories, use the approved staged
workspace pathway and never write to a random temp clone.
Original request: {{UserPrompt}}
Subagent proposals: {{PreviousStep}}
""", "LeaderSingle", canUseOrganicFunctions: true,
            includePriorTranscript: false,
            allowedAutomaticFunctions: ["codegen.capabilities", "codegen.review.create",
                "codegen.review.get", "codegen.review.list", "council.artifact_workspace_files",
                "council.artifact_workspace_file.read", "council.artifact_workspace_file.write",
                "council.repository.workspace.stage"]);
        integrate.TranscriptVisibility = CouncilTranscriptVisibilityMode.None;
        integrate.MemberFailureRecoveryAttempts = 1;
        integrate.WaitForDeferredApprovalsBeforeNextStep = true;

        var verify = ResilientStep(
            "subagent-verification", "Check actual build and source ZIP", 40,
            "Verification", "Artifact verifier", """
Require an actual successful codegen.review.execute approval/build receipt and
call council.development.artifact.verify for its reviewId. Inspect the exact
files and DownloadUrl. If an approval is still pending, report Pending and stop:
no claim of a finished ZIP, compilation or self-test is allowed. If the build
failed, report the first compiler error and return the smallest corrective work
order to the operator. The original model's reasoning is not a test receipt.
User request: {{UserPrompt}}
Integration evidence: {{PreviousStep}}
""", "LeaderSingle", canUseOrganicFunctions: true,
            includePriorTranscript: false,
            allowedAutomaticFunctions: ["codegen.review.get", "codegen.review.list",
                "council.development.artifact.verify", "council.artifact_workspaces",
                "council.artifact_workspace_files", "council.artifact_workspace_zip",
                "toolchain.installation.list", "localgpt.console.history"]);
        verify.TranscriptVisibility = CouncilTranscriptVisibilityMode.None;
        verify.MemberFailureRecoveryAttempts = 1;
        verify.WaitForDeferredApprovalsBeforeNextStep = true;

        var handoff = ResilientStep(
            "subagent-handoff", "Deliver verified files or exact blocker", 50,
            "Handoff", "Artifact verifier", """
Return the generated source files and real DownloadUrl only after the verified
artifact receipt exists. State which subagent contributed, which knowledge was
used and the actual build/self-test result. A pending approval, missing artifact
or failed build must be reported as pending/failed, not disguised as success.
User request: {{UserPrompt}}
Verification result: {{PreviousStep}}
""", "LeaderSingle", producesFinalAnswer: true,
            includePriorTranscript: false);
        handoff.TranscriptVisibility = CouncilTranscriptVisibilityMode.None;

        return new OrganicCouncilTeamDefinition
        {
            Key = "knowledge-assisted-coding-subagents",
            DisplayName = "Knowledge-Assisted Coding Subagents",
            Purpose = "A stronger selected guide prepares verified Knowledge/regex evidence and bounded work so weaker role-assigned models can produce useful code. Generation, approval and compiler verification remain authoritative LocalGPT functions; no model size is hard-coded.",
            RequiredCompletedFunctions = ["council.development.artifact.verify"],
            AllMembersReadinessPreflightMode = CouncilAllMembersReadinessPreflightMode.Disabled,
            Roles =
            [
                new() { Role = "Knowledge guide", Expertise = "knowledge retrieval, regex, provenance and compact task decomposition", Responsibility = "Prepare grounded small-model work without doing the subagent's implementation" },
                new() { Role = "Coding subagent", Expertise = "small, bounded implementations using verified knowledge", Responsibility = "Produce precise buildable files from the compact preparation; do not simulate execution" },
                new() { Role = "Integration guide", Expertise = "source review and approved codegen workflow", Responsibility = "Select and materialize correct subagent work through registered functions" },
                new() { Role = "Artifact verifier", Expertise = "compiler receipts, approval state and download verification", Responsibility = "Report only verified outputs and actual ZIP paths" }
            ],
            PreferredCapabilities = ["localgpt.knowledge.list", "localgpt.regex.get", "localgpt.regex.test",
                "codegen.review.create", "codegen.review.get", "council.development.artifact.verify"],
            WorkflowSteps = [prepare, implement, integrate, verify, handoff],
            ArchitectureContracts =
            [
                .. DefaultArchitectureContracts(),
                "Guide and coding-subagent roles are assigned through persisted provider-qualified role settings. No model is presumed stronger by name or parameter count.",
                "Prior transcript is excluded from narrow subagent steps; each role consumes the preceding verified handoff and the current user request.",
                "Verified retrieval is context assistance, not evidence of an unperformed action.",
                "Generated files, builds, approval and ZIP delivery require the existing registered function receipts; user verbal consent is not substituted for persisted approval."
            ]
        };
    }
}

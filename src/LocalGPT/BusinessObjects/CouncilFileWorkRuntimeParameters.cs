namespace LocalGPT.BusinessObjects;

/// <summary>Resettable operator policy for coordinated source-file authorship in the supplied compiler workflow.</summary>
public sealed class CouncilFileWorkRuntimeParameters
{
    /// <summary>Enables the coordinated file workflow for the configured team and step keys.</summary>
    public bool Enabled { get; set; }
    /// <summary>Team key whose supplied workflow owns source-file scheduling.</summary>
    public string TeamKey { get; set; } = string.Empty;
    /// <summary>Planning step that produces the workspace decision and manifest.</summary>
    public string PlanStepKey { get; set; } = string.Empty;
    /// <summary>Generation step that drains file work and performs targeted correction.</summary>
    public string GenerationStepKey { get; set; } = string.Empty;
    /// <summary>Configured independent source curation step.</summary>
    public string CurationStepKey { get; set; } = string.Empty;
    /// <summary>Instruction to generate a source file using an approved isolated script.</summary>
    public string GuestAuthorPrompt { get; set; } = string.Empty;
    /// <summary>Maximum seconds to wait for a queued isolated file-generation script.</summary>
    public int GuestCompletionWaitSeconds { get; set; }
    /// <summary>Verification step that obtains the registered artifact receipt.</summary>
    public string VerificationStepKey { get; set; } = string.Empty;
    /// <summary>Permits the established reviewed generation workflow only when no coordinated draft was attempted and the ordinary source route was explicitly selected.</summary>
    public bool AllowLegacyReviewedFallback { get; set; } = true;
    /// <summary>Preparation steps with a bounded output allocation.</summary>
    public string[] PreparationStepKeys { get; set; } = ["compiler-intake", "compiler-structure-review"];
    /// <summary>Run-local preparation output ceiling.</summary>
    public int MaximumPreparationOutputTokens { get; set; } = 4096;
    /// <summary>Run-local output ceiling for a bounded architecture or inspection response, independent of maximum benchmark settings.</summary>
    public int MaximumPlanningOutputTokens { get; set; } = 8192;
    /// <summary>Run-local output ceiling for one full file candidate; the selected hardware road remains authoritative.</summary>
    public int MaximumAuthorOutputTokens { get; set; } = 16384;
    /// <summary>Maximum authoring attempts for each distinct path in a draft.</summary>
    public int MaximumFileAttempts { get; set; }
    /// <summary>Independent negative decisions required before removing a stored candidate, bounded by eligible non-author reviewers.</summary>
    public int RequiredNegativeInspections { get; set; } = 2;
    /// <summary>Additional safe source-only recovery passes after a stalled draft, without repeating approvals or builds.</summary>
    public int MaximumDraftRecoveryPasses { get; set; } = 2;
    /// <summary>Additional author attempts per missing path in each explicitly supervised recovery pass.</summary>
    public int DraftRecoveryAdditionalAttempts { get; set; } = 2;
    /// <summary>Prefers eligible architect roads with sufficient configured output and context capacity over speed alone, without increasing their limits.</summary>
    public bool PreferPlanningCapacity { get; set; } = true;
    /// <summary>Maximum attempts to obtain a valid explicit plan before declaring a blocker.</summary>
    public int MaximumPlanAttempts { get; set; }
    /// <summary>Permits one supervised same-run planning correction after ordinary attempts, when no uncertain begin or deferred approval exists.</summary>
    public bool EnableManifestRecovery { get; set; } = true;
    /// <summary>Maximum bounded pre-approval manifest/source amendment attempts per Council run.</summary>
    public int MaximumDraftAmendments { get; set; } = 2;
    /// <summary>Maximum targeted build-repair generations after the initial approved build.</summary>
    public int MaximumRepairGenerations { get; set; }
    /// <summary>Maximum optimistic-concurrency conflicts before the caller reports a persistent draft failure.</summary>
    public int MaximumCommitConflicts { get; set; }
    /// <summary>Optional prior evidence budget in characters for the architect; the actual user request is never shortened.</summary>
    public int MaximumPlanEvidenceCharacters { get; set; }
    /// <summary>Maximum number of file paths in a generated manifest.</summary>
    public int MaximumManifestFiles { get; set; }
    /// <summary>Instruction for planning only; a structured response chooses the one workspace route.</summary>
    public string PlanPrompt { get; set; } = string.Empty;
    /// <summary>Instruction for one exclusively assigned file, with no filesystem or execution tool authority.</summary>
    public string AuthorPrompt { get; set; } = string.Empty;
    /// <summary>Instruction for independent review of an actual candidate file and its dependencies.</summary>
    public string CuratorPrompt { get; set; } = string.Empty;
    /// <summary>Instruction for choosing exact repairs from actual compiler diagnostics.</summary>
    public string RepairPrompt { get; set; } = string.Empty;
}

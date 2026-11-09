namespace LocalGPT.BusinessObjects;

/// <summary>Defines one ordered Council round and its execution, loop, function and ASCII-frame policies.</summary>
[DocumentationUpdated("2.1.20")]
public sealed class CouncilWorkflowStepDefinition
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this council workflow step definition instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the sort order value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sort order value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int SortOrder { get; set; }
    /// <summary>Gets or sets the workflow phase label.</summary>
    /// <value>The phase value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string Phase { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the role value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The role value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string Role { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the prompt template value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt template value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string PromptTemplate { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the execution mode value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The execution mode value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string ExecutionMode { get; set; } = "AllMembersSequentialOnEachAIHostParallel";
    /// <summary>Gets or sets the exact provider-qualified model used by assigned-model execution.</summary>
    /// <value>The assigned model name value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string AssignedModelName { get; set; } = string.Empty;
    /// <summary>Gets or sets the logical one-based Council round. Zero keeps automatic sequential round numbering.</summary>
    /// <value>The logical round number value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int LogicalRoundNumber { get; set; }
    /// <summary>Gets or sets which prior Council output is visible to this step.</summary>
    /// <value>The transcript visibility value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public CouncilTranscriptVisibilityMode TranscriptVisibility { get; set; } = CouncilTranscriptVisibilityMode.FullCouncil;
    /// <summary>Gets or sets how many times the step is expanded outside a loop group.</summary>
    /// <value>The repeat count value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int RepeatCount { get; set; } = 1;
    /// <summary>Gets or sets whether role members review each other's answers and record usefulness plus a role-local vote after the primary step.</summary>
    /// <value><see langword="true"/> to run the optional role-local peer review; otherwise the historical workflow behavior is preserved.</value>
    public bool EnableRolePeerReview { get; set; }
    /// <summary>Gets or sets whether multiple role-member answers are consolidated into one role result before the next workflow step.</summary>
    /// <value><see langword="true"/> to create a role-local synthesis result; otherwise every primary member answer remains the step result as before.</value>
    public bool SummarizeRoleResults { get; set; }
    /// <summary>Gets or sets how the role-result synthesizer is selected when role-result consolidation is enabled.</summary>
    /// <value>The role-result synthesis member mode used for this workflow step.</value>
    public CouncilRoleResultSynthesisMemberMode RoleResultSynthesisMemberMode { get; set; } = CouncilRoleResultSynthesisMemberMode.DeterministicRandomRoleMember;
    /// <summary>Gets or sets the exact provider-qualified role member used when assigned-member role synthesis is enabled.</summary>
    /// <value>The provider-qualified model selection key preferred for role-result synthesis.</value>
    public string RoleResultSynthesisModelName { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional user-owned role-result synthesis prompt template. Blank uses the database-backed global runtime-policy default.</summary>
    /// <value>A persisted workflow-step prompt template supporting role-synthesis placeholders.</value>
    public string RoleResultSynthesisPromptTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional user-owned same-role peer-review prompt template. Blank uses the database-backed global runtime-policy default.</summary>
    /// <value>A persisted workflow-step prompt template supporting role-peer-review placeholders.</value>
    public string RolePeerReviewPromptTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets whether prior step output is included in the prompt.</summary>
    /// <value>The include prior transcript value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool IncludePriorTranscript { get; set; } = true;
    /// <summary>Gets or sets whether the step produces the visible final answer.</summary>
    /// <value>The produces final answer value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool ProducesFinalAnswer { get; set; }
    /// <summary>Gets or sets whether the built-in orchestration behavior is used.</summary>
    /// <value>The use built in behavior value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool UseBuiltInBehavior { get; set; }
    /// <summary>Gets or sets the loop-group key shared by consecutive steps.</summary>
    /// <value>The loop group value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string LoopGroup { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the maximum loop iterations value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum loop iterations value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int MaximumLoopIterations { get; set; } = 1;
    /// <summary>
    /// Gets or sets the loop completion marker value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The loop completion marker value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string LoopCompletionMarker { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the council workflow step definition state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets whether the step pauses for a human checkpoint.</summary>
    /// <value>The requires human checkpoint value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool RequiresHumanCheckpoint { get; set; }
    /// <summary>Gets or sets whether LocalGPT must complete deterministic curation of every upload workspace referenced by the request before this workflow step may start.</summary>
    /// <value><see langword="true"/> to block the step until every referenced original archive and safely extracted file has complete byte-read coverage.</value>
    public bool RequiresCompleteUploadWorkspaceCuration { get; set; }
    /// <summary>Requires a successful model-initiated chat.upload_workspace_file result when the run has attached workspaces; a curated byte inventory alone is not sufficient.</summary>
    public bool RequiresVerifiedWorkspaceFileRead { get; set; }
    /// <summary>Retries this configured read-only workflow step once when source files were curated but no model completed a workspace file read.</summary>
    public bool RetryMissingWorkspaceFileReadOnce { get; set; }
    /// <summary>Additional same-step attempts for missing verified workspace reads; zero retains the legacy one-retry checkbox behavior.</summary>
    /// <value>Additional approved attempts for a missing verified file-read function receipt.</value>
    public int MissingWorkspaceFileReadRecoveryAttempts { get; set; }
    /// <summary>Whether to include bounded actual source excerpts in a retry prompt, without treating excerpts as model tool receipts.</summary>
    /// <value>Whether bounded source content is offered as retry context.</value>
    public bool IncludeWorkspaceSourceExcerptsOnRecovery { get; set; }
    /// <summary>User-configurable instruction for missing verified workspace file reads.</summary>
    /// <value>Custom retry instruction to the assigned reviewer.</value>
    public string WorkspaceFileReadRecoveryPromptTemplate { get; set; } = string.Empty;
    /// <summary>Ordered case-insensitive filename/path terms for choosing representative source excerpts; empty uses stable alphabetical ordering.</summary>
    /// <value>User-selected source filename ranking tokens.</value>
    public List<string> WorkspaceSourcePriorityTerms { get; set; } = [];
    /// <summary>Minimum distinct files whose recorded read intervals cover the complete content before this step may advance; zero uses one when verified reading is required.</summary>
    /// <value>Required number of completely read workspace files, not merely listed or hashed files.</value>
    public int RequiredCompletedWorkspaceFiles { get; set; }
    /// <summary>Requires at least one completely read extracted file within each uploaded archive root for this step.</summary>
    /// <value>Whether the saved step must inspect evidence from every original archive's extracted root.</value>
    public bool RequireEachWorkspaceArchiveRead { get; set; }
    /// <summary>Allows bounded same-step continuations only while the preceding execution completed additional distinct source files.</summary>
    /// <value>Whether measurable new file coverage is required to authorize another configured source-review retry.</value>
    public bool ContinueWorkspaceReviewOnlyOnProgress { get; set; }
    /// <summary>Includes a deterministic local knowledge and regex inventory before this configured step begins.</summary>
    public bool IncludeLocalResearchEvidence { get; set; }
    /// <summary>Gets or sets whether the workflow must wait for approval-gated deferred DXFunctions requested by this step before advancing to the next configured step.</summary>
    /// <value><see langword="true"/> to pause while exact deferred requests await local human approval or decline and to expose approved results as downstream evidence.</value>
    public bool WaitForDeferredApprovalsBeforeNextStep { get; set; }
    /// <summary>Gets or sets whether registered organic/DX functions may be requested.</summary>
    /// <value>The can use organic functions value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool CanUseOrganicFunctions { get; set; } = true;
    /// <summary>Gets or sets the user-editable automatic/native function policy for this workflow step.</summary>
    /// <value>The persisted policy that determines whether no functions, the team list, an exact step list, or the complete policy-approved catalog is exposed.</value>
    public CouncilAutomaticFunctionPolicyMode AutomaticFunctionPolicyMode { get; set; } = CouncilAutomaticFunctionPolicyMode.Legacy;
    /// <summary>Gets or sets step-level provider function guidance, strict only under explicit strict mode.</summary>
    /// <value>Registered DXFunction names preferred in normal modes and exclusively allowed only by explicit StrictStepAllowList.</value>
    public List<string> AllowedAutomaticFunctions { get; set; } = [];
    /// <summary>Gets or sets how LocalGPT returns intermediate text-gateway function results to this workflow step's assigned model.</summary>
    /// <value>The bounded continuation policy used after the step emitted one or more registered DXFunction requests.</value>
    public CouncilToolResultContinuationMode ToolResultContinuationMode { get; set; } = CouncilToolResultContinuationMode.SameMemberBounded;
    /// <summary>Gets or sets the maximum same-member model turns used to consume successive text-gateway function results.</summary>
    /// <value>A bounded continuation count. Zero disables continuation regardless of mode.</value>
    public int ToolResultContinuationRounds { get; set; } = 2;
    /// <summary>Gets or sets how many same-member corrective role retries LocalGPT may run after a generic non-performance/refusal result.</summary>
    /// <value>A user-editable bounded retry count; zero disables role-compliance retry.</value>
    public int RoleComplianceRetryCount { get; set; } = 1;
    /// <summary>Gets or sets how this workflow step recovers a required member slot after provider failure, timeout, or unusable visible output.</summary>
    /// <value>The persisted round recovery policy. Eligible substitutes are always resolved from the role's saved Social Team model-selection policy.</value>
    public CouncilMemberFailureRecoveryMode MemberFailureRecoveryMode { get; set; } = CouncilMemberFailureRecoveryMode.RetrySameThenEligibleRolePool;
    /// <summary>Gets or sets the maximum number of round-level recovery turns attempted after the participant's built-in same-request safe fallback has already failed.</summary>
    /// <value>A bounded user-editable recovery count. Zero disables round-level member recovery without changing the selected recovery mode.</value>
    public int MemberFailureRecoveryAttempts { get; set; } = 3;
    /// <summary>Gets or sets whether LocalGPT may request a final-answer-only continuation when a provider returns thinking without substantive visible output.</summary>
    /// <value><see langword="true"/> to enable the user-configured final-answer recovery pass.</value>
    public bool FinalAnswerRecoveryEnabled { get; set; } = true;
    /// <summary>Gets or sets the maximum output-token budget used by the final-answer recovery pass.</summary>
    /// <value>A positive bounded token budget configured per workflow step.</value>
    public int FinalAnswerRecoveryMaxOutputTokens { get; set; } = 8192;
    /// <summary>Gets or sets whether this step may emit first-class X-Round control requests.</summary>
    /// <value>The x functions enabled value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool XFunctionsEnabled { get; set; }
    /// <summary>Gets or sets whether X-Rounds may reconsider or deliberately re-execute another workflow step.</summary>
    /// <value>The x can revisit value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool XCanRevisit { get; set; }
    /// <summary>Gets or sets whether an X-Function may return an explicit text result and finish the parent workflow.</summary>
    /// <value>The x can return text value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool XCanReturnText { get; set; }
    /// <summary>Gets or sets whether an X-Function may run one bounded single-model derived subtask.</summary>
    /// <value>The x can start single model value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool XCanStartSingleModel { get; set; }
    /// <summary>Gets or sets whether an X-Function may start another configured Council as a derived subtask.</summary>
    /// <value>The x can start council value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool XCanStartCouncil { get; set; }
    /// <summary>Gets or sets the maximum number of X-Round transitions accepted from this source step in one run.</summary>
    /// <value>The x maximum transitions value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int XMaximumTransitions { get; set; } = 3;
    /// <summary>Gets or sets whether every X-Round request from this step must be approved by a local human before it changes control flow.</summary>
    /// <value>The x requires human approval value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool XRequiresHumanApproval { get; set; }
    /// <summary>Gets or sets the default target workflow-step key for revisit actions.</summary>
    /// <value>The x default target step key value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string XDefaultTargetStepKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the default Council team key used by the start-council X-Function.</summary>
    /// <value>The x child council team key value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string XChildCouncilTeamKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the maximum nested child-Council depth allowed when this step emits start-council X-Functions.</summary>
    /// <value>The x maximum child council depth value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int XMaximumChildCouncilDepth { get; set; } = 1;
    /// <summary>Gets or sets the default provider-qualified model identity used by the start-single-model X-Function.</summary>
    /// <value>The x child model name value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string XChildModelName { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the step owns a complete ASCII frame.</summary>
    /// <value>The produces ascii frame value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool ProducesAsciiFrame { get; set; }
    /// <summary>
    /// Gets or sets the ascii frame width value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ascii frame width value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int AsciiFrameWidth { get; set; } = 80;
    /// <summary>
    /// Gets or sets the ascii frame height value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ascii frame height value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int AsciiFrameHeight { get; set; } = 25;
    /// <summary>Gets or sets the logical world-step scale represented by one frame.</summary>
    /// <value>The world step scale value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int WorldStepScale { get; set; } = 1;
    /// <summary>
    /// Gets or sets whether a <c>SystemGameEngineResolution</c> step initializes the Council-driven game (lineup, asset check
    /// and presentation) instead of resolving one turn. A game that is not initialized yet is always initialized first.
    /// </summary>
    /// <value><c>true</c> for the lineup/initialization step of a Council-driven game.</value>
    public bool GameEngineInitializes { get; set; }
    /// <summary>
    /// Gets or sets whether the members of this step are narrowed to the models of the entrants the running Council-driven
    /// game currently pairs; the full role pool runs when the game reports no complete active pair.
    /// </summary>
    /// <value><c>true</c> to run only the active entrants' members.</value>
    public bool LimitToActiveGameEntrants { get; set; }
    /// <summary>
    /// Gets or sets the workflow-step keys whose answers form this step's transcript context. Empty keeps the configured
    /// transcript visibility unchanged.
    /// </summary>
    /// <value>The workflow-step keys of the bounded context.</value>
    public List<string> ContextStepKeys { get; set; } = [];
    /// <summary>Gets or sets the context step keys whose earliest answers (identities, introductions) are always kept.</summary>
    /// <value>The identity step keys.</value>
    public List<string> ContextIdentityStepKeys { get; set; } = [];
    /// <summary>Gets or sets how many identity answers are kept; zero or less keeps all.</summary>
    /// <value>The identity answer limit.</value>
    public int ContextIdentityStepLimit { get; set; }
    /// <summary>Gets or sets how many of the latest non-identity context answers are kept; zero or less keeps all.</summary>
    /// <value>The recent answer limit.</value>
    public int ContextRecentStepLimit { get; set; }
}

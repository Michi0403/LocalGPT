$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$required = @(
    # Technical truth is collected by the coordinator, not voted into existence by models.
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.FileWorkAuthority.cs'; Tokens = @('GetCompilerInstallationsAsync', 'LastValidationSucceeded', 'ApprovedLocalKnowledgeReferences', 'researchPolicy.IsStrict', 'localgpt.knowledge.remote.inspect', 'outcome.HandlerDispatched != true', 'outcome.DescriptorReadOnly != true', 'channel-version', 'MatchingReleaseMetadataRetrieved', 'SHA256.HashData(bytes)', 'FileWorkReceiptJson = receipt', 'PreserveFileWorkTechnicalEvidenceAsync', 'overwrite: false') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.ConfiguredWorkflowExecution.cs'; Tokens = @('BuildFileWorkAuthorityAsync', 'ExistingSourceAvailable = existingSourceAvailable', 'selectedProject!.TrackedFiles.Any', 'CouncilFileWorkPlanPrerequisiteNotice') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.FileWork.cs'; Tokens = @('TechnicalAuthority = technicalAuthority', 'plan.DraftSharedContract = JsonSerializer.Serialize', 'workspaceMode.GetString() == "ExistingRepository" && !existingSourceAvailable', 'existingSourceAvailable && policy.AllowLegacyReviewedFallback', 'BoundProjectId = result.ProjectId') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.RunOrchestration.cs'; Tokens = @('!compactSourcePreparation', '!result.CoordinatedSourceWork && result.ChangeReview is null', 'councilSpooler.Complete(result, interrupted: true)', 'enableAutomaticTools: !result.CoordinatedSourceWork') }
    @{ Path = 'src/LocalGPT/Services/DxAiFunctionRegistry.cs'; Tokens = @('registered allowed values: {enumElement.GetRawText()}', 'Registered fields:', 'candidate.GetString(), value.GetString()', 'For an active draft, amend with its exact draftAmendmentDraftId') }
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.Draft.cs'; Tokens = @('draftOperation=begin forbids files', 'draftOperation=begin forbids codeDomTypes', 'draftOperation=begin requires a nonempty goal', 'draftOperation=begin requires a nonempty draftManifest') }
    @{ Path = 'src/LocalGPT/Services/Council/CouncilSpoolerService.cs'; Tokens = @('UpdateIdentity(MultiModelCouncilResult result)', 'interrupted ? runtimePolicy.GetString("CouncilSpoolerInterrupted")', 'FunctionActivity = ReadGatewayActivity(step)') }
    @{ Path = 'src/LocalGPT/Services/GoalExecutionService.DurableExecution.cs'; Tokens = @('var interruptedSnapshot = councilSpooler.GetSnapshot(councilRunId)', 'receipt.ProjectId = interruptedSnapshot?.ProjectId', 'receipt.ProjectRevisionId = interruptedSnapshot?.ProjectRevisionId') }
    @{ Path = 'src/LocalGPT/Services/CouncilLiveSessionService.cs'; Tokens = @('activity.AdmittedAtUtc ??=', 'activity.CompletedAtUtc ??=', 'AdmittedAtUtc = activity.AdmittedAtUtc, CompletedAtUtc = activity.CompletedAtUtc', 'Guid? executionRunId = null', 'executionPrefix is null || activity.ActivityKey.StartsWith(executionPrefix, StringComparison.Ordinal)') }
    @{ Path = 'src/LocalGPT/Components/Shared/LiveCouncilLaneBoard.razor'; Tokens = @('CouncilLiveSessions.GetParticipantActivitiesForDisplay(runId, executionRunId)') }
    @{ Path = 'src/LocalGPT/Components/Layout/CouncilSourceProgressPanel.razor'; Tokens = @('Spooler.Changed += OnSpoolerChanged', 'Spooler.Changed -= OnSpoolerChanged', 'LiveCouncilLaneBoard', 'IconCssClass="IconCssClass"', 'CouncilRunStepSummary.StartedAtUtc', 'CouncilRunStepSummary.CompletedAtUtc') }
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.DraftTargetValidation.cs'; Tokens = @('IsContradictoryFrameworkInspection', 'runtime.codegen.review-target-replacement', 'DtdProcessing.Prohibit', 'PackageVersion', 'declared = declared.Where') }
    # 7.6.4: the real 20 Nov Twitch review lost consumed SQLite/name decisions, omitted the host/data graph,
    # accepted a malformed solution, and labeled disabled compilation as an actual build failure.
    @{ Path = 'src/LocalGPT/Services/HumanCollaborationService.SourceDecisions.cs'; Tokens = @('item.CouncilRunId == councilRunId', 'item.Status == answered || item.Status == consumed', 'item.RequestKind != approvalKind', 'item.UserResponse', 'DecidedAtUtc') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.FileWork.cs'; Tokens = @('GetSourcePlanningDecisionsAsync(result.RunId', 'HumanDecisions = sourceDecisions', 'CouncilFileWorkHumanDecisionInstruction') }
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.SourceClosure.cs'; Tokens = @('ValidateSourceManifestCoverage', 'GetSelectedSolutionName', 'IsSourceSolutionPath', 'runtime.project.solution-file-extension', 'IsSourceProjectPath', 'IsSourceBuildDeclarationPath', 'no specific file basename', 'ValidateDraftSourceReferences', 'Solution is missing the Visual Studio solution-format header', 'Guid.TryParse(fields[7]', 'Solution references', 'ProjectReference', 'ValidateCompletedSourceCoverage', 'Compiler verification') }
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.Draft.cs'; Tokens = @('ValidateSourceManifestCoverage(request.Goal, manifest, decisions)', 'ValidateCompletedSourceCoverage(plan, humanDecisions)', 'ValidateDraftSourceReferences(file.RelativePath, file.Content, manifest)') }
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.DraftAssignments.cs'; Tokens = @('ValidateDraftSourceReferences(current.RelativePath, source', 'plan.DraftManifest') }
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.Execution.cs'; Tokens = @('CodeGenerationReviewStatuses.BuildDisabled', 'CodeGenerationReviewStatuses.BuildNotAttempted', 'BuildCompletedSuccessfully(entity.BuildStatus)') }
    @{ Path = 'src/LocalGPT/Services/CouncilDevelopmentDxAiFunctions.cs'; Tokens = @('CodeGenerationReviewStatuses.BuildDisabled', 'CodeGenerationReviewStatuses.BuildNotAttempted', 'Status = "SourceOnly"') }
    @{ Path = 'src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs'; Tokens = @('CouncilFileWorkHumanDecisionInstruction', 'Human answers are requirements, not permission') }

    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.DraftInspection.cs'; Tokens = @('IsContradictoryFrameworkInspection(plan.Goal, decision.Diagnostic)', 'CouncilFileWorkConflictingFrameworkReviewNotice') }
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.Draft.cs'; Tokens = @('ValidateDraftProjectTarget(plan.Goal, candidate.RelativePath, candidate.Content)', 'ValidateDraftProjectTarget(plan.Goal, file.RelativePath, file.Content)') }
    @{ Path = 'src/LocalGPT/Services/ProjectMaintenanceDxAiFunctions.cs'; Tokens = @('parameters.SummaryOnly ?? (request.AutomaticInvocation && unmaterialized)', 'TrackedFileCount = files.Count', 'summaryOnly = false') }
    @{ Path = 'src/LocalGPT/Services/GoalExecutionService.DurableExecution.cs'; Tokens = @('receipt.ProjectId = councilResult.ProjectId', 'receipt.ProjectRevisionId = councilResult.ProjectRevisionId') }
    @{ Path = 'src/LocalGPT/Services/Council/CouncilSpoolerService.cs'; Tokens = @('snapshot.ProjectId = result.ProjectId', 'snapshot.ProjectRevisionId = result.ProjectRevisionId', 'snapshot.DevelopmentArtifactVerified = result.DevelopmentArtifactVerified') }
    # Keep proposals separate from verified file dependencies; restore the approved source-only build route.
    # Correlate native calls to the run-owned project; never accept participant markup as coordinated delivery.
    @{ Path = 'src/LocalGPT/Services/OllamaThinkingChatClient.ToolExecution.cs'; Tokens = @('councilExecutionIdentity.ProjectId', 'councilExecutionIdentity.ProjectRevisionId', 'result.OperationId', 'result.HandlerDispatched', 'result.SchemaHash') }
    @{ Path = 'src/LocalGPT/Services/ProviderModelRuntimeService.cs'; Tokens = @('ambientContext.Current.ExecutionIdentity') }
    @{ Path = 'src/LocalGPT/Services/ProjectMaintenanceDxAiFunctions.cs'; Tokens = @('parameters.ProjectId ?? identity?.ProjectId', 'identity?.ProjectRevisionId') }
    @{ Path = 'src/LocalGPT/Services/DxAiFunctionRegistry.cs'; Tokens = @('result.HandlerDispatched ??= false', 'result.HandlerDispatched = true', 'SchemaHash = schemaHash', 'CancelledUnknown', 'FailedUnknown') }
    @{ Path = 'src/LocalGPT/Services/DxAiFunctionInvocationAuditService.cs'; Tokens = @('context.ExecutionIdentity?.ChatRunId', 'HandlerDispatched = result.HandlerDispatched', 'SchemaHash = result.SchemaHash') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.FileWork.cs'; Tokens = @('EnableManifestRecovery', 'alternativeRouteObserved || uncertainBegin', 'created.Status != "InvalidParameters"', 'SourceManifestMissing', 'sourceReceipt.Status == "SourceOnly"', 'MissingRequirements = ["Successful build verification"]', 'FileWorkReceiptJson = receiptJson') }
    # 7.7.5: approval, SHA-256 source integrity and source-only status apply to every review, not only coordinated drafts.
    @{ Path = 'src/LocalGPT/Services/CouncilDevelopmentDxAiFunctions.cs'; Tokens = @('!review.ApprovalConsumed', 'Status = "SourceOnly"', 'actualHash != file.ContentHash') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.WorkspaceAndResearchGates.cs'; Tokens = @('result.CoordinatedSourceWork', 'return result.DevelopmentArtifactVerified;') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.FileWorkManifest.cs'; Tokens = @('SeparateManifestPackageProposals(plan)', 'UnverifiedExternalReferencesByProject', 'paths.Contains', 'sourceExtensions.Contains', 'runtime.codegen.package-proposal-id') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.FileWork.cs'; Tokens = @('PreviousCandidate = previousCandidate', 'CouncilFileWorkPlanShapeInstruction', 'attemptedArchitects.Add(architect)', 'var rejectedProposal = JsonSerializer.Serialize(explicitPlan)', 'attemptedManifests.Add(candidate)', 'RequiredDotNetTargets = requiredTargets', 'AttachedWorkspaceNames = request.AttachedWorkspaceNames, Evidence = evidence', 'hasAttachedEvidence || item.ModelName is', 'previousCandidate = string.Empty;', 'policy.PreferPlanningCapacity') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.RunOrchestration.cs'; Tokens = @('CouncilFileWorkProjectIdentityInstruction', 'ProjectId = result.ProjectId', 'ProjectRevisionId = result.ProjectRevisionId') }
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.Execution.cs'; Tokens = @('var explicitSolutions = reviewedSources.Where', 'buildTargets.AddRange(explicitTargets.Select(source => ResolveInsideRoot(workspaceRoot, source.RelativePath)).Where(File.Exists))', 'request.UserConfirmedBuild') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.FileWorkManifest.cs'; Tokens = @('draftDependencies', 'JsonValueKind.String', 'runtime.codegen.explicit-dotnet-target', 'ReadSourceManifest(string answer, string goal)') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.LiveInputAndHealth.cs'; Tokens = @('MinimumModelHealthFailureRounds', 'independentFailures', 'measured.Any') }
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.DraftAssignments.cs'; Tokens = @('GetCouncilDraftRunIdsAsync', 'BuildDraftProgressTree') }
    @{ Path = 'src/LocalGPT/Services/Council/CouncilSpoolerService.cs'; Tokens = @('GetRunStepSummaries', 'RecordedStepCount') }
    @{ Path = 'src/LocalGPT/Components/Layout/CouncilSourceProgressPanel.razor'; Tokens = @('draftRuns', 'otherRuns', 'DxTreeView', 'DxFlyout', 'GetRunStepSummaries', 'DetailRowTemplate') }
    # Reject incompatible or repeated source plans before beginning a draft. Build and package remain separate outcomes.
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.FileWorkManifest.cs'; Tokens = @('GetExplicitFileWorkTargets(goal)', 'runtime.codegen.explicit-project-output', 'Requested target {requested[0]}') }
    @{ Path = 'src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs'; Tokens = @('CouncilFileWorkPlanPacketInstruction', 'CouncilFileWorkRepeatedManifestNotice', '"preferPlanningCapacity": true', 'runtime.codegen.explicit-project-output') }
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.Draft.cs'; Tokens = @('output.Kind = NormalizeOutputKind(output.Kind)', 'manifest.ContainsKey(parent)', 'EnsureCouncilRunProjectAsync') }
    @{ Path = 'src/LocalGPT/Services/ArtifactBuildExecutor.cs'; Tokens = @('runtime.project.solution-file-extension', 'GetCompilerInstallationsAsync', 'GetWorkspaceRootsAsync', 'CompilerNotValidated', 'FileName = compiler.ExecutablePath', 'if (!userConfirmed)', 'IsInsideRoot(target, root)') }
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.Execution.cs'; Tokens = @('ArtifactBuildExecutionResult buildResult;', 'exception is not OperationCanceledException', ':BuildCheckError', 'ZipFile.CreateFromDirectory', 'BuildCompletedSuccessfully(entity.BuildStatus)') }
    # Preserve exact candidate evidence and bounded same-draft recovery.
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.DraftInspection.cs'; Tokens = @('requiredNegativeInspections', 'rejectedBy', 'PreviousDecisions') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.FileWork.cs'; Tokens = @('MaximumDraftRecoveryPasses', 'DraftRecoveryAdditionalAttempts', 'RecoveryGuidance') }
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.DraftAssignments.cs'; Tokens = @('includePathHistory', 'ValidateDraftProjectTarget', 'DraftRepairSources.ContainsKey') }
    @{ Path = 'src/LocalGPT/Services/CouncilRuntimeService.PromptAndTextRuntime.cs'; Tokens = @('CouncilChatAssistantHistoryBudgetCharacters') }
    @{ Path = 'src/LocalGPT/Services/GuidUtilityDxAiFunction.cs'; Tokens = @('identity.guid', 'RequiresHumanConfirmation: false') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.FileWork.cs'; Tokens = @('ReadFileWorkAnswer(step)', 'step.VisibleContentHtmlEncoded ? System.Net.WebUtility.HtmlDecode(step.VisibleContent)', 'step.FileWorkReceiptJson', 'plan.Goal = goal') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.PhaseExecution.cs'; Tokens = @('step.FileWorkInstruction = participantPrompt', 'WebUtility.HtmlEncode(participantPrompt)') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.ParticipantExecution.cs'; Tokens = @('VisibleContentHtmlEncoded = providerModel.ProviderKind.Equals(ProviderModelKinds.Ollama') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.FileWorkGuest.cs'; Tokens = @('ReadFileWorkAnswer(authored).Trim()') }
    @{ Path = 'src/LocalGPT/Services/Council/CouncilSpoolerService.cs'; Tokens = @('FileWorkInstruction = source.FileWorkInstruction', 'FileWorkReceiptJson = source.FileWorkReceiptJson', 'WorkflowStepKey = source.WorkflowStepKey') }
    # Artifact-first source recovery: normalize architecture metadata, keep exact file bytes, publish only verified ZIPs.
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.FileWorkManifest.cs'; Tokens = @('contract.GetRawText()', 'JsonValueKind.Object or JsonValueKind.Array', 'GetFileWorkPreparationOutputCeiling') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.FileWork.cs'; Tokens = @('PreviousValidationError = planDiagnostic', '!beginAttempted', '!alternativeRouteObserved', 'attemptedInspections', 'result.Artifacts.Add(new CouncilArtifact', 'result.DevelopmentArtifactVerified = true', 'verified.Succeeded', 'File.Exists(zipPath)') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.ParticipantExecution.cs'; Tokens = @('requestOutputCeiling is int workCeiling', 'Math.Min(executionPlan.EffectiveMaxOutputTokens') }
    @{ Path = 'src/LocalGPT/Services/GoalExecutionService.DurableExecution.cs'; Tokens = @('councilResult.CoordinatedSourceWork', 'councilResult.DevelopmentArtifactVerified', 'safeManifestRetry', 'checkpoint.KeyaccountContinuationsScheduled++', 'verifiedDevelopmentArtifact ? GoalExecutionState.Completed') }
    # Coordinated source authoring: one route, exclusive assignment, exact receipts, immutable approvals.
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.ConfiguredWorkflowExecution.cs'; Tokens = @('RunCoordinatedFileWorkAsync', 'CouncilFileWorkRuntimeParametersJson') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.FileWork.cs'; Tokens = @('ReserveDraftFilesAsync', 'CompleteDraftFileAsync', 'GetDraftInspectionsAsync', 'RecordDraftInspectionAsync', 'GetCodeGenerationBuildEvidenceAsync', 'draftRepairReviewHash', 'draftWorkspaceMode', 'exactAssignments: true') }
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.DraftAssignments.cs'; Tokens = @('DraftAssignments', 'DependencyHashes', 'SaveDraftPlanAsync', 'PayloadJson ==', 'SHA256.HashData') }
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.DraftInspection.cs'; Tokens = @('decision.Accepted == true', 'plan.Files.Remove(file)', 'packet.DependencyHashes') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.FileWorkGuest.cs'; Tokens = @('session.sandbox.run', 'run.SourceSha256 == sourceHash', 'guest.OutputTruncated', 'CompleteDraftFileAsync') }
    @{ Path = 'src/LocalGPT/Services/CodeGenerationWorkflowService.DraftGuest.cs'; Tokens = @('actualId != assignment.Id', 'assignment.RelativePath', 'Convert.FromBase64String') }
    # Reject missing/ambiguous entries and changed bytes independently of the review's draft origin.
    @{ Path = 'src/LocalGPT/Services/CouncilDevelopmentDxAiFunctions.cs'; Tokens = @('foreach (var file in review.Files)', 'if (matches.Count != 1)', 'actualHash != file.ContentHash') }
    # Startup delay is route infrastructure, not model competence. Keep host handoff scoped to the configured role and preserve Stop evidence.
    @{ Path = 'src/LocalGPT/BusinessObjects/MultiModelCouncilModels.cs'; Tokens = @('IsProviderStartupTimeout =>', 'IsContextBudgetFailure || IsProviderStartupTimeout || HostUnreachable') }
    @{ Path = 'src/LocalGPT/BusinessObjects/ServiceRuntimeParameterModels.cs'; Tokens = @('MaximumProviderStartupWaitSeconds') }
    @{ Path = 'src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs'; Tokens = @('"maximumProviderStartupWaitSeconds"', 'CouncilProviderStartupTimeoutNotice', 'CouncilProviderStartupTakeoverNotice', 'CouncilPhaseCancellationPreservedNotice') }
    @{ Path = 'src/LocalGPT/Services/OllamaThinkingChatClient.cs'; Tokens = @('Waiting for the local runtime to accept the stream...', 'Ollama accepted the request.') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.ParticipantExecution.cs'; Tokens = @('MaximumProviderStartupWaitSeconds', 'awaitingInitialProviderResponse && !initialProviderResponseObserved', 'allowRecovery && !startupTimeout', 'CouncilProviderStartupTimeout;') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.PhaseExecution.cs'; Tokens = @('completedPrimarySteps.Enqueue(step)', 'completedTakeoverSteps.Enqueue(replacement)', 'replacementGate.WaitAsync(0, cancellationToken)', 'replacement.RecoveryTargetModelName = modelName', 'allowRecovery: false, fallbackPlan: replacementPlan', 'candidateReference.ModelName, failedReference.ModelName', 'MultiModelCouncilServiceAddOrderedStep(result, step, logger)', 'CouncilPhaseCancellationPreservedNotice') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.WorkflowDefinitionExecution.cs'; Tokens = @('definition.MemberFailureRecoveryAttempts > 0', 'allowStartupHostTakeover: allowStartupHostTakeover') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.RoundRecovery.cs'; Tokens = @('GoalMemberFailureKind.ProviderStartupTimeout', 'previousTakeover is not null && !previousTakeover.IsProviderStartupTimeout', 'allowParticipantRecovery: failureKind != GoalMemberFailureKind.ProviderStartupTimeout') }
    @{ Path = 'src/LocalGPT/Services/GoalExecution/GoalRecoveryHelperService.cs'; Tokens = @('IsHostUsable(state, failedHost) && !startupTimeout', 'candidateReference.ModelName, failedReference.ModelName') }
    @{ Path = 'src/LocalGPT/Services/AiContextBootstrapService.cs'; Tokens = @( 'AiBootstrapReferenceContextPercent', 'AiBootstrapFunctionDiscoveryInstruction', 'includeReferenceContext') }
    @{ Path = 'src/LocalGPT/Services/GoalExecutionService.SemanticPlanning.cs'; Tokens = @( 'hardwareRoadPlanner.BuildPlans', 'road.EffectiveMaxContextTokens', 'fitContextToRequest: true') }
    @{ Path = 'src/LocalGPT/Services/GoalExecutionService.KeyaccountManager.cs'; Tokens = @( 'hardwareRoadPlanner.BuildPlans', 'road.EffectiveMaxContextTokens', 'fitContextToRequest: true') }
    @{ Path = 'src/LocalGPT/Services/ConfigurationSteward/ConfigurationStewardService.cs'; Tokens = @( 'hardwareRoadPlanner.BuildPlans', 'road.EffectiveMaxContextTokens', 'fitContextToRequest: true') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.LiveInputAndHealth.cs'; Tokens = @( '!step.IsInfrastructureFailure', 'var failures = measured.Where(step => !string.IsNullOrWhiteSpace(step.Error)).ToList();', 'var independentFailures = failures.Select(step => step.Round).Distinct().Count();', 'var minimumFailures = Math.Max(2, runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>("CouncilExecutionRuntimeParametersJson").MinimumModelHealthFailureRounds);', 'if (independentFailures < minimumFailures || measured.Any(step => string.IsNullOrWhiteSpace(step.Error) && !string.IsNullOrWhiteSpace(step.VisibleContent)))') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.RoundRecovery.cs'; Tokens = @( 'failedStep?.IsContextBudgetFailure == true') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.PhaseExecution.cs'; Tokens = @('var publishCompletedFirst = hostCount > 1 ||', 'if (presentationOrder is not null && !publishCompletedFirst)', 'await foreach (var modelName in presentationOrder.ReadAllAsync().ConfigureAwait(false))', 'liveCouncilSessions.AppendParticipantActivity((result.ChatRunId ?? result.RunId), activityKey, text)', 'await Task.WhenAll(hostTasks)', 'await Task.WhenAll(tasks)', 'foreach (var step in steps.OrderBy(step => participantOrder.TryGetValue') }
    @{ Path = 'src/LocalGPT/Services/OllamaThinkingChatClient.Transport.cs'; Tokens = @( 'request.ContextCeiling', 'FitOllamaRequestContext(budget, model, logger)') }
    @{ Path = 'src/LocalGPT/Services/GoalExecutionService.ContextRecovery.cs'; Tokens = @( 'checkpoint.Steps[failedIndex] = new GoalExecutionPlanStep', 'checkpoint.Prompt') }
    @{ Path = 'src/LocalGPT/Services/CouncilChatClient.cs'; Tokens = @('request.ChatRunId = chatRunId', 'liveSessions.Touch(chatRunId)') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.ConfiguredWorkflowExecution.cs'; Tokens = @('ChatRunId = request.ChatRunId ?? result.RunId', 'StepCompleted = request.StepCompleted') }
    @{ Path = 'src/LocalGPT/Services/Council/CouncilSpoolerService.cs'; Tokens = @('GetChatSnapshots(Guid chatRunId)', 'ChatRunId = result.ChatRunId') }
    @{ Path = 'src/LocalGPT/Services/Council/Scheduling/CouncilHardwareRoadConfigurationService.cs'; Tokens = @('ResolvePresetRoutes', 'identity.ModelNamesEquivalent(route.ModelName, candidate.ModelName)', 'runtimePolicy.GetInt("DefaultCouncilContextTokens")') }
    @{ Path = 'src/LocalGPT/Services/Persistence/DatabaseInitializationService.SeedPresets.cs'; Tokens = @('CreateDefaultCouncilModelPresets(bounded: true)', 'row.ModelRoutesJson != previous.ModelRoutesJson', 'preset.MaxParallelModels = 1') }
    @{ Path = 'src/LocalGPT/Services/OllamaThinkingChatClient.Conversation.cs'; Tokens = @('ToOllamaMessages(message, toolNames)', 'MeasureRequestCharacters', 'GetDiscoveryBrokerTools(tools)', 'ContextCeiling = budget.NumCtx') }
    @{ Path = 'src/LocalGPT/Services/OllamaThinkingChatClient.MessageMapping.cs'; Tokens = @('OfType<FunctionCallContent>()', 'OfType<FunctionResultContent>()', 'toolNames.TryGetValue(result.CallId', 'Images = message.Images?.ToList()') }
    @{ Path = 'src/LocalGPT/Services/CouncilRuntimeService.OllamaRequestBudget.cs'; Tokens = @('numPredict, configuredContext, promptTokens, required, false, exceedsEstimate', 'Math.Min(ceiling, rounded)') }
    @{ Path = 'src/LocalGPT/Services/OllamaThinkingChatClient.Transport.cs'; Tokens = @('MeasureRequestCharacters(request.Messages, request.Tools)', 'requestOptions.NumCtx', 'budget.PromptExceedsMaximum') }
    @{ Path = 'src/LocalGPT/Services/GoalExecutionService.ContextRecovery.cs'; Tokens = @('priorToolActivity=true', 'TryReplanRemainingAsync', 'The selected hardware-road limits remain unchanged.') }
    @{ Path = 'src/LocalGPT/Services/GoalExecutionService.DurableExecution.cs'; Tokens = @('remainingSteps = checkpoint.Steps.Skip(failedIndex)', 'proposal.Steps.Count > availableSlots', 'contextFailure && replacements.Count != proposal!.Steps.Count') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.RunOrchestration.cs'; Tokens = @('LoadContinuationConversationAsync','ContinuedFromConversationId') }
    @{ Path = 'src/LocalGPT/Services/CouncilRuntimeService.PromptAndTextRuntime.cs'; Tokens = @('CreateUpdate') }
    @{ Path = 'src/LocalGPT/Services/CouncilRuntimeService.cs'; Tokens = @('NotRunLane') }
    @{ Path = 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'; Tokens = @('RunUiActionAsync') }
    # Source-read readiness requires the original unrestricted native-function contract,
    # preferred tools plus broker fallback, and exclusive strict lists.
    @{ Path = 'src/LocalGPT/Services/CouncilAutomaticFunctionPolicyService.cs'; Tokens = @('var exposed = preferred', '"dxai.tools.discover", "dxai.tools.preview", "dxai.tools.invoke"', 'strictTeam || strictStep', 'IsStrict: true', '!IsBrokerTool(name)', 'exposed,') }
    @{ Path = 'src/LocalGPT/Services/OllamaThinkingChatClient.Tools.cs'; Tokens = @('automaticFunctionAllowList is null || automaticFunctionAllowList.Contains(function.Name)', 'function.SupportsAutomaticInvocation', 'function.AvailableToAi') }
    @{ Path = 'src/LocalGPT/Services/OllamaThinkingChatClient.ToolExecution.cs'; Tokens = @('var directMatch = functions.FirstOrDefault', 'automaticFunctionAllowList.Contains("dxai.tools.invoke")', 'var transportMatches = eligible', 'return transportMatches.Count == 1') }
    @{ Path = 'src/LocalGPT/Services/ChatUploadWorkspaceDxAiFunctions.cs'; Tokens = @('"chat.upload_workspace_files"', '"chat.upload_workspace_file"', 'Source: "DIHandler"', 'SupportsAutomaticInvocation: true') }
    @{ Path = 'src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.GameProjectTemplates.cs'; Tokens = @('"game-development-baseline"', '"chat.upload_workspace_files"', '"chat.upload_workspace_file"', '"game-development-mechanics"', '"game-development-ai"') }
    @{ Path = 'src/LocalGPT/BusinessObjects/OrganicCouncilModels.cs'; Tokens = @('StrictTeamAllowList = 5', 'StrictStepAllowList = 6', 'bool IsStrict = false') }
    @{ Path = 'src/LocalGPT/Components/Pages/CouncilTeams.razor.cs'; Tokens = @('CouncilAutomaticFunctionPolicyMode.StrictTeamAllowList', 'CouncilAutomaticFunctionPolicyMode.StrictStepAllowList') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.WorkflowPrompting.cs'; Tokens = @('CouncilWorkflowToolInitiative', 'CouncilWorkflowRepairBoundary', 'automaticFunctionPolicy.IsStrict') }
    @{ Path = 'src/LocalGPT/Services/Persistence/InitialDataCatalog.cs'; Tokens = @('new("CouncilWorkflowToolInitiative", "en", "- Tool initiative:', 'new("CouncilWorkflowRepairBoundary", "en", "- Repair boundary:') }
    @{ Path = 'src/LocalGPT/Services/CouncilRoleAssignmentService.cs'; Tokens = @('AssignAsync', 'UserConfirmed', 'teams.SaveAsync', 'RestrictSelectedRolesToAssignedPool', 'item.IsInstalled') }
    @{ Path = 'src/LocalGPT/Services/CouncilRoleAssignmentIndexService.cs'; Tokens = @('SynchronizeAsync', 'CouncilRoleIndexes', 'CouncilRoleModelBindings') }
    @{ Path = 'src/LocalGPT/Components/Shared/ProviderModelRoleEditor.razor'; Tokens = @('ShowSearchBox="true"', 'Save role assignments', 'ListRenderMode.Virtual', 'DxComboBox TData="TeamOption" TValue="string"', 'ValueChanged="@(async (string key) => await TeamChangedAsync(key))"', 'ValuesChanged="@(async (IEnumerable<string> values) => await SelectedChangedAsync(values))"') }
    @{ Path = 'src/LocalGPT/Migrations/20261029123000_AddCouncilRoleAssignments.cs'; Tokens = @('CouncilRoleIndexes', 'CouncilRoleModelBindings') }
    @{ Path = 'src/LocalGPT/Services/Council/CouncilSpoolerService.cs'; Tokens = @('GetRecentSummaries(int take = 50)', 'CouncilSpoolerRunSummary') }
    @{ Path = 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'; Tokens = @('OpenSelectedCouncilHistoryAsync', 'SelectCouncilSessionAsync', 'CouncilSpooler.GetChatSnapshots(runId)') }
    @{ Path = 'src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs'; Tokens = @('openedCouncilArchive = CouncilSpooler.GetSnapshot(rejoinRunId)', 'localGptReconnect.clearCouncilRun') }
    @{ Path = 'src/LocalGPT/Services/UploadProcessingRecommendationService.cs'; Tokens = @('imageOnly', 'RequiresVerifiedWorkspaceFileRead', 'localgpt.vision.ocr.capability') }
    @{ Path = 'src/LocalGPT/Components/Shared/UploadProcessingAdvisor.razor'; Tokens = @('Check local OCR availability', 'Run OCR on quarantined image', 'RecognizeWorkspaceImageAsync') }
    @{ Path = 'src/LocalGPT/Services/WorkspaceVisionOcrService.cs'; Tokens = @('normalizedRelative = "original/" + normalizedRelative;', 'segment is "." or ".."', 'FileAttributes.ReparsePoint', 'ResolveWorkspaceFile(workspaceRoot, normalizedRelative, logger)') }
    @{ Path = 'src/LocalGPT/Services/UploadProcessingRecommendationService.cs'; Tokens = @('if (imageOnly)', 'return await PersistAsync(fallback, cancellationToken).ConfigureAwait(false);', 'capability.StartsWith("localgpt.vision."', 'capability.EndsWith(".ocr"') }
    # 6.8.5: source-level negative controls for missing Council transcript, ASCII lanes,
    # game popup visibility, browser rejoin identity and council-driven stage styling.
    @{ Path = 'src/LocalGPT/Services/AsciiChatTextService.cs'; Tokens = @('messages.Count == 0 && participantActivities.Count == 0', 'AppendCouncilParticipantActivities(builder, participantActivities)') }
    @{ Path = 'src/LocalGPT/Components/Pages/Chat.razor'; Tokens = @('<div class="chat-council-live-replay" role="region"', 'data-testid="chat-council-live-replay"', 'CouncilLiveSessions.GetTranscriptForDisplay(currentLiveCouncil.RunId)', 'LiveCouncilParticipantActivities(currentLiveCouncil.RunId)') }
    @{ Path = 'src/LocalGPT/Components/Pages/Chat.razor.cs'; Tokens = @('During provider streaming the server-owned Council session is authoritative.', 'CouncilLiveSessions.GetSummary(activeRunId)?.IsRunning == true') }
    @{ Path = 'src/LocalGPT/Components/Pages/Chat.ProviderRuntime.razor.cs'; Tokens = @('OpenRequiredAsciiCouncilSurface', 'InvokeAsync(() =>', 'EnsureRequiredAsciiSurfaceState();') }
    @{ Path = 'src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs'; Tokens = @('openedCouncilArchive = CouncilSpooler.GetSnapshot(rejoinRunId)') }
    @{ Path = 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'; Tokens = @('openedCouncilArchive = CouncilSpooler.GetSnapshot(selectedRunId)') }
    @{ Path = 'src/LocalGPT/Components/Shared/ChatGameConsole.razor'; Tokens = @('IsCouncilDrivenStage ? "chat-game-console-council-stage"') }
    @{ Path = 'src/LocalGPT/Components/Shared/ChatGameConsole.razor.css'; Tokens = @('.chat-game-console-council-stage .chat-game-screen') }
    @{ Path = 'src/LocalGPT/wwwroot/js/localgpt-reconnect.js'; Tokens = @('new MutationObserver(observeModal).observe(document.body, { childList: true });', 'sessionStorage.getItem(storageKey)') }


)
$findings = [Collections.Generic.List[string]]::new()
$phaseSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/MultiModelCouncilService.PhaseExecution.cs'))
# Previous errors may affect ordering, but cannot silently remove selected routes from future phases.
if ($phaseSource -match 'participants\.Where\(model\s*=>\s*exactAssignments\s*\|\|\s*!failedModels\.Contains') {
    $findings.Add('MultiModelCouncilService.PhaseExecution.cs: retain selected participants after a transient error; the host admission and explicit run settings own exclusion.')
}
# A completed remote lane must be queued only after its stream is closed. The
# single reader can then drain it without waiting for another provider's stream.
if ($phaseSource -notmatch 'participantStream\?\.Writer\.TryComplete\(\);\s*if \(publishCompletedFirst\)\s*presentationOrder\?\.Writer\.TryWrite\(modelName\);') {
    $findings.Add('MultiModelCouncilService.PhaseExecution.cs: queue the closed provider stream on participant completion, not in selection order for parallel phases.')
}
if ($phaseSource -match 'foreach\s*\(var modelName in participantOrder\)') {
    $findings.Add('MultiModelCouncilService.PhaseExecution.cs: a fixed-order presentation pump blocks completed remote streams behind slow participants.')
}
$budgetSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/CouncilRuntimeService.OllamaRequestBudget.cs'))
if ($budgetSource -match 'Math\.Max\(current,\s*policy\.MaximumFittedContextTokens\)') {
    $findings.Add('CouncilRuntimeService.OllamaRequestBudget.cs: selected hardware context is a ceiling; split work instead of silently raising allocation.')
}
$recoverySource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/GoalExecutionService.ContextRecovery.cs'))
if ($recoverySource -match 'request\.CouncilRequest\.(?:MaxContextTokens|ResourceLoadPercent)\s*=') {
    $findings.Add('GoalExecutionService.ContextRecovery.cs: automatic context recovery must not change selected hardware limits.')
}
foreach ($entry in $required) {
    $file = Join-Path $root $entry.Path
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { $findings.Add('Missing source: ' + $entry.Path); continue }
    $content = [IO.File]::ReadAllText($file)
    foreach ($token in $entry.Tokens) {
        if ($content.IndexOf($token, [StringComparison]::Ordinal) -lt 0) { $findings.Add($entry.Path + ': missing contract ' + $token) }
    }
}

# Keep the Chat terminal worker from racing a disposed circuit and from
# monopolizing a busy multi-model renderer. This is a source boundary check,
# not a substitute for host-side verification of Blazor stream timeouts.
$chatLifetimeSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.razor.cs'))
foreach ($contract in @(
    'if (isDisposed || componentLifetimeToken.IsCancellationRequested || !showGameConsole)',
    'componentLifetimeToken = componentLifetimeCts.Token;',
    'var lifetimeToken = componentLifetimeToken;',
    'catch (ObjectDisposedException) when (isDisposed)',
    'Interlocked.Exchange(ref asciiMirrorLoopRunning, 0);',
    'if (!isDisposed && !cancellationToken.IsCancellationRequested && Volatile.Read(ref showGameConsole))'
)) {
    if ($chatLifetimeSource.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
        $findings.Add('Chat.razor.cs: missing disposal-safe ASCII mirror contract: ' + $contract)
    }
}
# The Council/game lifetime token must be captured once while its source is alive.
# This rejects a seemingly guarded .Token access that can still throw after circuit disposal.
$chatPartials = @(Get-ChildItem -LiteralPath (Join-Path $root 'src/LocalGPT/Components/Pages') -File -Filter 'Chat*.cs')
foreach ($partial in $chatPartials) {
    $text = [IO.File]::ReadAllText($partial.FullName)
    $withoutConstructor = $text.Replace('componentLifetimeToken = componentLifetimeCts.Token;', '')
    if ($withoutConstructor.IndexOf('componentLifetimeCts.Token', [StringComparison]::Ordinal) -ge 0) {
        $findings.Add($partial.Name + ': cancellation source Token is read after construction; use the cached componentLifetimeToken.')
    }
}
$consoleStateSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Shared/ChatGameConsole.StateAndConversation.razor.cs'))
$consoleWorkerSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Shared/ChatGameConsole.GameInteractionAndRender.razor.cs'))
foreach ($contract in @(
    'consoleRenderToken = consoleRenderCancellation.Token;',
    'snapshot = await ResolveActiveGameAsync().ConfigureAwait(true);',
    'if (snapshot is not null && !operatorMode)',
    'return candidate.CouncilRunId == councilRunId;',
    'return councilGame;'
)) {
    if ($consoleStateSource.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
        $findings.Add('ChatGameConsole.StateAndConversation.razor.cs: missing game/circuit recovery contract: ' + $contract)
    }
}
foreach ($contract in @(
    'if (disposed)',
    'consoleRenderToken.IsCancellationRequested',
    'consoleRenderToken)'
)) {
    if ($consoleWorkerSource.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
        $findings.Add('ChatGameConsole.GameInteractionAndRender.razor.cs: missing disposed console worker contract: ' + $contract)
    }
}
# Both initial mount and later run changes must reveal a prebootstrapped game.
$gamePresentationCount = ([regex]::Matches($consoleStateSource,
    'if \(snapshot is not null && !operatorMode\)\s*conversationMode = false;')).Count
if ($gamePresentationCount -lt 2) {
    $findings.Add('ChatGameConsole.StateAndConversation.razor.cs: both initial mount and later attachment must enter Game mode for a recovered game.')
}
if ($consoleWorkerSource.IndexOf('consoleRenderCancellation.Token', [StringComparison]::Ordinal) -ge 0) {
    $findings.Add('ChatGameConsole.GameInteractionAndRender.razor.cs: disposed console source Token must not be read by callbacks.')
}
$providerRuntimeSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.ProviderRuntime.razor.cs'))
if ($providerRuntimeSource.IndexOf('throw;', [StringComparison]::Ordinal) -lt 0 -or
    $providerRuntimeSource.IndexOf('A fabricated empty request', [StringComparison]::Ordinal) -lt 0) {
    $findings.Add('Chat.ProviderRuntime.razor.cs: broken Council request creation must fail closed, never create a default Council run.')
}

$chatSessionRenderingSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'))
# Inspect the session-list refresh owner, not an arbitrary Task.Delay in the file.
# The current 1500 ms interval deliberately reduces background UI work; require a
# bounded cancellable debounce instead of pinning one particular millisecond value.
$sessionRefreshMethod = [regex]::Match($chatSessionRenderingSource,
    '(?s)\bprivate\s+void\s+ScheduleLiveCouncilListRefresh\s*\(\s*\)\s*\{(?<body>.*?)\r?\n    \}')
if (-not $sessionRefreshMethod.Success) {
    $findings.Add('Chat.LiveCouncil.razor.cs: session list refresh owner could not be located.')
}
else {
    $sessionRefreshBody = $sessionRefreshMethod.Groups['body'].Value
    $sessionDebounce = [regex]::Match($sessionRefreshBody,
        'await\s+Task\.Delay\(\s*(?<milliseconds>\d+)\s*,\s*cancellationToken\s*\)\.ConfigureAwait\(false\);')
    $debounceMilliseconds = 0
    if (-not $sessionDebounce.Success -or
        -not [int]::TryParse($sessionDebounce.Groups['milliseconds'].Value, [ref]$debounceMilliseconds) -or
        $debounceMilliseconds -lt 500 -or $debounceMilliseconds -gt 5000) {
        $findings.Add('Chat.LiveCouncil.razor.cs: session list refresh requires a cancellable 500-5000 ms debounce during Council streaming.')
    }
    foreach ($contract in @(
        'Interlocked.Exchange(ref liveCouncilListRefreshScheduled, 1)',
        'Interlocked.Exchange(ref liveCouncilListRefreshScheduled, 0)',
        'cancellationToken.IsCancellationRequested',
        'componentLifetimeToken.IsCancellationRequested',
        '!interactiveAttached'
    )) {
        if ($sessionRefreshBody.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
            $findings.Add('Chat.LiveCouncil.razor.cs: session refresh missing scheduling/disposal contract: ' + $contract)
        }
    }
}

# Server-owned Council runs must recover automatically when Blazor rejects a
# dead circuit. Keep the existing manual recovery path and prevent reload loops.
$reconnectSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/wwwroot/js/localgpt-reconnect.js'))
foreach ($contract in @(
    'scheduleAutomaticRecovery(600)',
    'scheduleAutomaticRecovery(1200)',
    'scheduleAutomaticRecovery(15000)',
    'claimAutomaticReload()',
    'reloadAndRejoin();',
    'confirmedRunId = String(runId)',
    'attempts >= 2',
    'components-reconnect-rejected',
    'components-reconnect-hide'
)) {
    if ($reconnectSource.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
        $findings.Add('localgpt-reconnect.js: missing bounded automatic Council recovery: ' + $contract)
    }
}

# A dead Blazor circuit must restore both the Chat conversation and the running
# Council. Recovering a Council marker alone must never split one chat in two.
foreach ($contract in @(
    'conversationStorageKey',
    'readConversationId()',
    "url.searchParams.set('rejoinConversationId', conversationId)",
    'setConversation(conversationId)',
    'clearConversation()',
    "url.searchParams.set('rejoinCouncilRunId', String(runId))"
)) {
    if ($reconnectSource.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
        $findings.Add('localgpt-reconnect.js: exact Chat/Council recovery identity contract missing: ' + $contract)
    }
}
$chatRazorSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.razor.cs'))
$chatPersistSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.PersistenceAndMemory.razor.cs'))
$chatLifecycleSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs'))
$chatLiveSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'))
foreach ($entry in @(
    @{ Name = 'Chat.razor.cs'; Content = $chatRazorSource; Token = 'RequestedRejoinConversationId' },
    @{ Name = 'Chat.razor.cs'; Content = $chatRazorSource; Token = 'LoadSavedConversationAsync(matching, saveCurrentFirst: false, loadIntoDxChat: false)' },
    @{ Name = 'Chat.Lifecycle.razor.cs'; Content = $chatLifecycleSource; Token = 'localGptReconnect.setConversation' },
    @{ Name = 'Chat.LiveCouncil.razor.cs'; Content = $chatLiveSource; Token = 'firstAttachmentToRun && ActiveConversationId is null' },
    @{ Name = 'Chat.PersistenceAndMemory.razor.cs'; Content = $chatPersistSource; Token = 'chatConversationSaveGate.WaitAsync()' },
    @{ Name = 'Chat.PersistenceAndMemory.razor.cs'; Content = $chatPersistSource; Token = 'chatConversationSaveGate.Release()' }
)) {
    if ($entry.Content.IndexOf($entry.Token, [StringComparison]::Ordinal) -lt 0) {
        $findings.Add($entry.Name + ': recovery/persistence contract missing: ' + $entry.Token)
    }
}

# Recovery must preserve the visible DevExpress Council session, not only the DB id.
# The current implementation merges an ordered overlap so reconnecting does not
# duplicate already-visible messages or discard Council messages. Check the whole
# preservation contract, not one older implementation-specific AddRange expression.
foreach ($contract in @(
    'var restoredMessages = restoredChatSession.Messages.ToList();',
    'var visibleMessages = councilSession.Messages.ToList();',
    'var overlap = 0;',
    'councilSession.Messages.AddRange(restoredMessages);',
    'councilSession.Messages.AddRange(visibleMessages.Skip(overlap));',
    'canonicalConversationMessages.AddRange(councilSession.Messages);',
    'DxAiChat.LoadMessages(councilSession.Messages);'
)) {
    if ($chatLiveSource.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
        $findings.Add('Chat.LiveCouncil.razor.cs: restored Chat/Council overlap and visible-session synchronization missing: ' + $contract)
    }
}

# Optional image exclusions must not block a tool call. Validate the published
# JSON schemas in addition to the invocation and runtime defaults.
$imageFunctionSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/LocalAiRuntimeDxAiFunctions.cs'))
$imageSchemas = [regex]::Matches($imageFunctionSource, 'ParameterSchemaJson:\s*"""(?<json>\{[^\r\n]+\})"""')
$checkedNegativePrompts = 0
foreach ($match in $imageSchemas) {
    # An optional JSON property must be looked up through PSObject.Properties.
    # Under Set-StrictMode -Version Latest, accessing a missing property directly
    # throws PropertyNotFoundStrict *before* a null check can evaluate it.
    # Most DX function schemas legitimately have no negativePrompt property.
    try {
        $schema = $match.Groups['json'].Value | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        $findings.Add('LocalAiRuntimeDxAiFunctions.cs: malformed JSON function schema: ' + $_.Exception.Message)
        continue
    }
    if ($null -eq $schema) {
        $findings.Add('LocalAiRuntimeDxAiFunctions.cs: unexpectedly empty JSON function schema.')
        continue
    }
    $propertiesMember = $schema.PSObject.Properties['properties']
    if ($null -eq $propertiesMember -or $null -eq $propertiesMember.Value) { continue }
    $negativePromptMember = $propertiesMember.Value.PSObject.Properties['negativePrompt']
    if ($null -eq $negativePromptMember -or $null -eq $negativePromptMember.Value) { continue }

    $checkedNegativePrompts++
    $requiredMember = $schema.PSObject.Properties['required']
    if ($null -ne $requiredMember -and @($requiredMember.Value) -contains 'negativePrompt') {
        $findings.Add('LocalAiRuntimeDxAiFunctions.cs: optional negativePrompt must not be schema-required.')
    }
    $defaultMember = $negativePromptMember.Value.PSObject.Properties['default']
    if ($null -eq $defaultMember -or $defaultMember.Value -cne '') {
        $findings.Add('LocalAiRuntimeDxAiFunctions.cs: negativePrompt must default to an empty exclusion set.')
    }
}
if ($checkedNegativePrompts -ne 2) {
    $findings.Add('LocalAiRuntimeDxAiFunctions.cs: both image generation and edit schemas require optional negativePrompt contracts.')
}
if ($imageFunctionSource.IndexOf('binding.Value.NegativePrompt ??= string.Empty;', [StringComparison]::Ordinal) -lt 0) {
    $findings.Add('LocalAiRuntimeDxAiFunctions.cs: null optional prompt must be normalized before runtime dispatch.')
}
$registrySource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/DxAiFunctionRegistry.cs'))
if ($registrySource.IndexOf('propertyValue.ValueKind == JsonValueKind.Null', [StringComparison]::Ordinal) -lt 0 -or
    $registrySource.IndexOf('string.Equals(property.Name, "negativePrompt"', [StringComparison]::Ordinal) -lt 0) {
    $findings.Add('DxAiFunctionRegistry.cs: null optional negativePrompt must not fail the required parameter validator.')
}
$keyaccountSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/GoalExecutionService.KeyaccountManager.cs'))
if ($keyaccountSource.IndexOf('ReadKeyaccountReviewObject(text)', [StringComparison]::Ordinal) -lt 0 -or
    $keyaccountSource.IndexOf('JsonDocument.Parse(text.Substring(start, end - start + 1))', [StringComparison]::Ordinal) -ge 0) {
    $findings.Add('GoalExecutionService.KeyaccountManager.cs: parse a bounded single JSON review, not first-to-last brace prose.')
}

# The Council run is server-owned; UI streaming is only an expendable observer.
# Keep the contract in this maintained, cross-platform PowerShell guard rather
# than adding release-specific Python source tests to tests/static.
$clientPath = Join-Path $root 'src/LocalGPT/Services/CouncilChatClient.cs'
$clientSource = [IO.File]::ReadAllText($clientPath)
$streamSignature = 'public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync'
$workerSignature = 'private async Task<MultiModelCouncilResult?> RunCouncilInBackgroundAsync'
$streamStart = $clientSource.IndexOf($streamSignature, [StringComparison]::Ordinal)
if ($streamStart -lt 0) {
    $findings.Add('CouncilChatClient.cs: missing streaming Council observer entry point.')
}
else {
    $streamEnd = $clientSource.IndexOf($workerSignature, $streamStart, [StringComparison]::Ordinal)
    if ($streamEnd -lt 0) {
        $findings.Add('CouncilChatClient.cs: missing independently owned Council worker.')
    }
    else {
        $stream = $clientSource.Substring($streamStart, $streamEnd - $streamStart)
        $launchIndex = $stream.IndexOf('var runTask = RunCouncilInBackgroundAsync(', [StringComparison]::Ordinal)
        $firstYieldIndex = $stream.IndexOf('yield return councilRuntime.CreateUpdate(liveMessageMarker', [StringComparison]::Ordinal)
        if ($launchIndex -lt 0 -or $firstYieldIndex -lt 0 -or $launchIndex -ge $firstYieldIndex) {
            $findings.Add('CouncilChatClient.cs: start the backend Council task before yielding the first browser update.')
        }
        foreach ($contract in @(
            'using var observerCancellationRegistration = cancellationToken.Register(',
            'Interlocked.Exchange(ref attached, 0)',
            'if (Volatile.Read(ref attached) == 1)',
            'RunCouncilInBackgroundAsync(request, liveCancellation,',
            'yield break;',
            'var chatRunId = request.RunId;',
            'request.ChatRunId = chatRunId;',
            'liveSessions.Begin(',
            'liveSessions.Append(chatRunId, text)',
            'liveSessions.SetStatus(chatRunId, message)',
            'liveSessions.Touch(chatRunId)',
            'Channel.CreateBounded<string>(new BoundedChannelOptions(512)',
            'FullMode = BoundedChannelFullMode.DropOldest',
            'request.StreamUpdate = Publish'
        )) {
            if ($stream.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
                $findings.Add('CouncilChatClient.cs: observer isolation contract missing: ' + $contract)
            }
        }
        if ($stream -match '(?m)^\s*liveSessions\.Cancel\s*\(') {
            $findings.Add('CouncilChatClient.cs: disconnecting a browser observer must never cancel the backend Council run.')
        }
    }
}
# A durable goal may change request.RunId after the observer has attached.
# The background owner must complete the same parent session that received updates.
$workerStart = $clientSource.IndexOf($workerSignature, [StringComparison]::Ordinal)
if ($workerStart -ge 0) {
    $workerEnd = $clientSource.IndexOf('public object? GetService(', $workerStart, [StringComparison]::Ordinal)
    if ($workerEnd -lt 0) {
        $findings.Add('CouncilChatClient.cs: background owner boundary missing.')
    }
    else {
        $worker = $clientSource.Substring($workerStart, $workerEnd - $workerStart)
        foreach ($contract in @('var chatRunId = request.ChatRunId ?? request.RunId;', 'serviceScopeFactory.CreateAsyncScope()', 'liveSessions.Complete(chatRunId)', 'writer.TryComplete()')) {
            if ($worker.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
                $findings.Add('CouncilChatClient.cs: background owner contract missing: ' + $contract)
            }
        }
    }
}
if ($clientSource -match 'liveSessions\s*\.\s*(?:Begin|Append|SetStatus|Touch|Complete)\s*\(\s*request\.RunId\b') {
    $findings.Add('CouncilChatClient.cs: live session ownership must use the stable parent Chat identity, not the mutable GoalExecution run ID.')
}
# 6.8.7: a disposed Chat circuit must not turn a request-factory cancellation
# into ArgumentNullException or launch a second backend Council run.
$clientRequestStart = $clientSource.IndexOf('var requestMessages = messages.ToList();', [StringComparison]::Ordinal)
$clientRequestGuard = if ($clientRequestStart -ge 0) { $clientSource.IndexOf('if (request is null)', $clientRequestStart, [StringComparison]::Ordinal) } else { -1 }
$clientPlanStart = if ($clientRequestStart -ge 0) { $clientSource.IndexOf('if (request.ModelNames.Count == 0', $clientRequestStart, [StringComparison]::Ordinal) } else { -1 }
if ($clientRequestStart -lt 0 -or $clientRequestGuard -lt 0 -or $clientPlanStart -lt 0 -or $clientRequestGuard -ge $clientPlanStart) {
    $findings.Add('CouncilChatClient.cs: reject a failed or disposed-circuit request before accessing model lists.')
}
$clientNonStreamingStart = $clientSource.IndexOf('private async Task<string> RunCouncilAsync', [StringComparison]::Ordinal)
$clientNonStreamingGuard = if ($clientNonStreamingStart -ge 0) { $clientSource.IndexOf('if (request is null)', $clientNonStreamingStart, [StringComparison]::Ordinal) } else { -1 }
$clientNonStreamingModels = if ($clientNonStreamingStart -ge 0) { $clientSource.IndexOf('request.UnavailableModelSelections.Count', $clientNonStreamingStart, [StringComparison]::Ordinal) } else { -1 }
if ($clientNonStreamingStart -lt 0 -or $clientNonStreamingGuard -lt 0 -or $clientNonStreamingModels -lt 0 -or $clientNonStreamingGuard -ge $clientNonStreamingModels) {
    $findings.Add('CouncilChatClient.cs: non-streaming Council must not dereference a missing request.')
}
$liveCouncilUi = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'))
$selectStart = $liveCouncilUi.IndexOf('private async Task SelectCouncilSessionAsync(', [StringComparison]::Ordinal)
$selectEnd = if ($selectStart -ge 0) { $liveCouncilUi.IndexOf('private async Task OpenSelectedCouncilHistoryAsync(', $selectStart, [StringComparison]::Ordinal) } else { -1 }
if ($selectStart -lt 0 -or $selectEnd -lt 0 -or $liveCouncilUi.Substring($selectStart, $selectEnd - $selectStart).IndexOf('await OpenSelectedCouncilHistoryAsync()', [StringComparison]::Ordinal) -lt 0) {
    $findings.Add('Chat.LiveCouncil.razor.cs: selecting a Council run must automatically restore its transcript.')
}
$historyStart = $liveCouncilUi.IndexOf('private async Task OpenSelectedCouncilHistoryAsync(', [StringComparison]::Ordinal)
$historyEnd = if ($historyStart -ge 0) { $liveCouncilUi.IndexOf('private void ToggleCouncilEvidenceStep(', $historyStart, [StringComparison]::Ordinal) } else { -1 }
if ($historyStart -lt 0 -or $historyEnd -lt 0) {
    $findings.Add('Chat.LiveCouncil.razor.cs: Council history attachment boundary is missing.')
}
else {
    $history = $liveCouncilUi.Substring($historyStart, $historyEnd - $historyStart)
    foreach ($contract in @('receipt is null && liveSnapshot is null', 'if (liveSnapshot is not null)', 'AttachToLiveCouncilSessionAsync(runId, reloadChatControl: true)')) {
        if ($history.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
            $findings.Add('Chat.LiveCouncil.razor.cs: live Council rejoin must not depend on an existing spool checkpoint: ' + $contract)
        }
    }
}

$chatJsPath = Join-Path $root 'src/LocalGPT/wwwroot/js/localgpt-chat-ui.js'
$chatJs = [IO.File]::ReadAllText($chatJsPath)
$nativeStopStart = $chatJs.IndexOf("nativeStop.addEventListener('click'", [StringComparison]::Ordinal)
$nativeStopEnd = if ($nativeStopStart -ge 0) {
    $chatJs.IndexOf('const nativeSend = actionButtons.find(', $nativeStopStart, [StringComparison]::Ordinal)
} else { -1 }
if ($nativeStopStart -lt 0 -or $nativeStopEnd -lt 0) {
    $findings.Add('localgpt-chat-ui.js: missing native Stop click handler or its boundary.')
}
else {
    $nativeStopHandler = $chatJs.Substring($nativeStopStart, $nativeStopEnd - $nativeStopStart)
    foreach ($contract in @(
        'if (!councilComposerActive || !councilComposerDotNet) return;',
        "invokeMethodAsync('StopActiveCouncilRunAsync')"
    )) {
        if ($nativeStopHandler.IndexOf($contract, [StringComparison]::Ordinal) -lt 0) {
            $findings.Add('localgpt-chat-ui.js: explicit native Stop click contract missing: ' + $contract)
        }
    }
}
if ($chatJs.IndexOf("nativeStop.hasAttribute('data-localgpt-council-stop-bridge')", [StringComparison]::Ordinal) -lt 0) {
    $findings.Add('localgpt-chat-ui.js: native Stop click bridge must prevent duplicate registration.')
}
$liveChatPath = Join-Path $root 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'
$liveChat = [IO.File]::ReadAllText($liveChatPath)
if ($liveChat -notmatch '(?s)\[JSInvokable\]\s*public\s+async\s+Task<bool>\s+StopActiveCouncilRunAsync\s*\(') {
    $findings.Add('Chat.LiveCouncil.razor.cs: missing JS-callable explicit Stop handler.')
}
if ($liveChat.IndexOf('CouncilLiveSessions.Cancel(runId)', [StringComparison]::Ordinal) -lt 0) {
    $findings.Add('Chat.LiveCouncil.razor.cs: explicit Stop must cancel the authoritative live Council session.')
}

# These runtime targets remain supported; version strings are read from source
# by the existing repository boundary audit, never hardcoded into this guard.
$projectFile = Join-Path $root 'src/LocalGPT/LocalGPT.csproj'
$projectXml = [xml]([IO.File]::ReadAllText($projectFile))
$runtimeNode = $projectXml.SelectSingleNode('/Project/PropertyGroup/RuntimeIdentifiers')
if ($null -eq $runtimeNode) {
    $findings.Add('LocalGPT.csproj: RuntimeIdentifiers must preserve the existing Windows, Linux and macOS targets.')
}
else {
    $rids = @($runtimeNode.InnerText.Split(';') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    foreach ($rid in @('win-x64', 'win-x86', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')) {
        if ($rid -notin $rids) {
            $findings.Add('LocalGPT.csproj: missing previously supported runtime identifier: ' + $rid)
        }
    }
}
foreach ($relative in @(
    'src/LocalGPT/Services/MultiModelCouncilService.GameEngineResolution.cs',
    'src/LocalGPT/Services/GameSceneEngine.cs',
    'src/LocalGPT/Services/GameStudioService.cs',
    'src/LocalGPT/Services/CouncilGameSessionService.cs'
)) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
        $findings.Add('Council legacy/declarative game implementation missing: ' + $relative)
    }
}

$nativeToolsSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/OllamaThinkingChatClient.Tools.cs'))
if ($nativeToolsSource -match 'automaticFunctionAllowList\s+is\s+null\s*\?\s*function\.Source\s*==\s*"DxFunctionBroker"') {
    $findings.Add('Ollama automatic-tool default regressed to broker-only; 5.5.0 exposed all eligible DXFunctions.')
}
# Prevent misattached XML docs when adding Council spooler methods. A stale <param>
# on the next method generates CS1572, and an omitted tag can generate CS1573.
# Validate both contract and implementation, including exactly one summary block.
foreach ($relative in @(
    'src/LocalGPT/Services/Council/CouncilSpoolerService.cs',
    'src/LocalGPT/Interfaces/ICouncilSpoolerService.cs'
)) {
    $spoolerText = [IO.File]::ReadAllText((Join-Path $root $relative))
    foreach ($contract in @(
        @{ Name = 'GetRecentSummaries'; Parameter = 'take' },
        @{ Name = 'GetSnapshot'; Parameter = 'runId' }
    )) {
        $methodPattern = '(?m)(?<documentation>(?:^[ \t]*///[^\r\n]*\r?\n)+)[ \t]*(?:public[ \t]+)?[^\r\n;{}]*?\b' +
            [regex]::Escape($contract.Name) + '\s*\((?<parameters>[^)]*)\)'
        $methodMatch = [regex]::Match($spoolerText, $methodPattern)
        if (-not $methodMatch.Success) {
            $findings.Add($relative + ': missing adjacent XML documentation for ' + $contract.Name + '.')
            continue
        }
        $xmlDoc = $methodMatch.Groups['documentation'].Value
        $docParameters = @([regex]::Matches($xmlDoc, '<param\s+name="(?<name>[^"]+)"') |
            ForEach-Object { $_.Groups['name'].Value })
        $methodParameters = $methodMatch.Groups['parameters'].Value
        if ($docParameters.Count -ne 1 -or $docParameters[0] -cne $contract.Parameter -or
            $methodParameters -notmatch ('\b' + [regex]::Escape($contract.Parameter) + '\b')) {
            $findings.Add($relative + ': XML parameters do not match ' + $contract.Name + '(' + $contract.Parameter + ') (CS1572/CS1573).')
        }
        if ([regex]::Matches($xmlDoc, '<summary(?:\s|>)').Count -ne 1 -or
            [regex]::Matches($xmlDoc, '<returns>').Count -ne 1) {
            $findings.Add($relative + ': duplicate/missing XML summary or returns for ' + $contract.Name + '.')
        }
    }
}

# Keep new Chat selection properties from collecting XML comments belonging to
# the property below. Misattached documentation can be misleading even if it
# does not itself raise the same compiler diagnostic as an invalid <param>.
$chatComponentSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Components/Pages/Chat.razor.cs'))
foreach ($propertyName in @('CouncilSessionHistoryOptions', 'FeedbackTargetOptions', 'AsciiFunButtonCssClass')) {
    $propertyPattern = '(?m)(?<documentation>(?:^[ \t]*///[^\r\n]*\r?\n)+)[ \t]*private[ \t]+[^\r\n]*\b' +
        [regex]::Escape($propertyName) + '[ \t]*=>'
    $propertyMatch = [regex]::Match($chatComponentSource, $propertyPattern)
    if (-not $propertyMatch.Success -or
        [regex]::Matches($propertyMatch.Groups['documentation'].Value, '<summary(?:\s|>)').Count -ne 1) {
        $findings.Add('Chat.razor.cs: misattached/duplicate XML documentation for ' + $propertyName + '.')
    }
}

# Whenever constructor injection changes, its XML documentation must change with it.
# CS1572/CS1573 are build errors; catch this Council-specific regression in the
# existing mandatory PowerShell source gate, before invoking the C# compiler.
$teamConfigurationSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/CouncilTeamConfigurationService.cs'))
$constructorDocumentationPattern = '(?m)(?<documentation>(?:^[ \t]*///[^\r\n]*(?:\r?\n))+)[ \t]*public[ \t]+CouncilTeamConfigurationService[ \t]*\((?<parameters>[\s\S]*?)\)[ \t]*(?:\r?\n)[ \t]*\{'
$constructorDocumentationMatch = [regex]::Match($teamConfigurationSource, $constructorDocumentationPattern)
if (-not $constructorDocumentationMatch.Success) {
    $findings.Add('CouncilTeamConfigurationService.cs: constructor and its XML documentation could not be located.')
}
else {
    $documented = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($parameterTag in [regex]::Matches($constructorDocumentationMatch.Groups['documentation'].Value, '<param\s+name="(?<name>[^"]+)"')) {
        [void]$documented.Add($parameterTag.Groups['name'].Value)
    }
    $declared = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($argument in $constructorDocumentationMatch.Groups['parameters'].Value.Split(',')) {
        if ($argument -notmatch '([A-Za-z_][A-Za-z0-9_]*)\s*$') {
            $findings.Add('CouncilTeamConfigurationService.cs: could not read constructor parameter: ' + $argument.Trim())
            continue
        }
        [void]$declared.Add($Matches[1])
    }
    foreach ($name in $declared) {
        if (-not $documented.Contains($name)) {
            $findings.Add('CouncilTeamConfigurationService.cs: constructor XML documentation is missing <param name="' + $name + '"> (CS1573).')
        }
    }
    foreach ($name in $documented) {
        if (-not $declared.Contains($name)) {
            $findings.Add('CouncilTeamConfigurationService.cs: XML documentation references an absent constructor parameter: ' + $name + ' (CS1572).')
        }
    }
}

# Exercise the actual persisted reviewer-replacement pattern against the supplied failure shape.
$seedSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs'))
$replacementSeed = [regex]::Match($seedSource, 'new\("runtime\.codegen\.review-target-replacement",\s*@"(?<pattern>[^"]+)"')
if (-not $replacementSeed.Success) {
    $findings.Add('Technical evidence: reviewer replacement regex seed missing.')
}
else {
    $replacement = [regex]::new($replacementSeed.Groups['pattern'].Value, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    foreach ($case in @(
        @{ Text = "Change TargetFramework to 'net8.0'"; Major = '8' },
        @{ Text = 'Switch to .NET 6'; Major = '6' },
        @{ Text = 'Set TargetFramework = net10.0'; Major = '10' }
    )) {
        $match = $replacement.Match($case.Text)
        if (-not $match.Success -or $match.Groups['major'].Value -ne $case.Major) {
            $findings.Add('Technical evidence: unexpected replacement classification: ' + $case.Text)
        }
    }
    if ($replacement.IsMatch('A dependency formerly supported net8.0; verify its current API.')) {
        $findings.Add('Technical evidence: a reference to an older framework is not a downgrade proposal.')
    }
}

# A newly allocated project ID must never select the legacy route by itself.
$fileWorkSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/MultiModelCouncilService.FileWork.cs'))
$routeReject = $fileWorkSource.IndexOf('workspaceMode.GetString() == "ExistingRepository" && !existingSourceAvailable')
$legacyExit = $fileWorkSource.IndexOf('if (plan.DraftWorkspaceMode == "ExistingRepository")')
if ($routeReject -lt 0 -or $legacyExit -lt 0 -or $routeReject -ge $legacyExit -or $fileWorkSource.Contains('ExistingProjectId = result.ProjectId')) {
    $findings.Add('Coordinated source routing: reject a contradictory greenfield manifest before legacy routing; provisioned IDs are not source evidence.')
}
$configuredSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/MultiModelCouncilService.ConfiguredWorkflowExecution.cs'))
$authorityIndex = $configuredSource.IndexOf('await BuildFileWorkAuthorityAsync')
$readinessIndex = $configuredSource.IndexOf('await RunConfiguredAllMembersReadinessPreflightAsync')
if ($authorityIndex -lt 0 -or $readinessIndex -lt 0 -or $authorityIndex -ge $readinessIndex) {
    $findings.Add('Technical evidence must precede configured readiness and Intake.')
}

# Pin the registered draft operation contract, not model-invented choices.
$registrySource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/DxAiFunctionRegistry.cs'))
$draftEnum = [regex]::Match($registrySource, '"draftOperation"\s*:\s*\{\s*"type"\s*:\s*"string"\s*,\s*"enum"\s*:\s*(?<values>\[[^\]]+\])')
if (-not $draftEnum.Success) {
    $findings.Add('Draft operation: registered enum missing.')
}
else {
    # Windows PowerShell 5.1 can return a JSON array as one pipeline object.
    # Assign first, then explicitly enumerate the parsed values on both runtimes.
    $parsedOperations = ConvertFrom-Json -InputObject $draftEnum.Groups['values'].Value
    $operations = @(foreach ($operation in $parsedOperations) { $operation })
    if (($operations -join ',') -cne 'begin,next,complete,amend,submit,repair') {
        $findings.Add('Draft operation: exact enum changed without updating the operation contract.')
    }
    foreach ($invalidOperation in @('Create','Update','Draft','New')) {
        if ($operations -ccontains $invalidOperation) { $findings.Add('Draft operation: invalid alias admitted: ' + $invalidOperation) }
    }
}


# The source-only check must be inside both the pre-authoring begin and final submit path;
# approval and exporter paths must not be the only places where closure is tested.
$draftService = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/CodeGenerationWorkflowService.Draft.cs'))
$beginIndex = $draftService.IndexOf('ValidateSourceManifestCoverage(request.Goal, manifest, decisions)')
$persistIndex = $draftService.IndexOf('db.CodeGenerationChangeReviews.Add(draft)')
$submitIndex = $draftService.IndexOf('ValidateCompletedSourceCoverage(plan, humanDecisions)')
$reviewIndex = $draftService.IndexOf('CreateReviewCoreAsync(plan, draft, cancellationToken)')
if ($beginIndex -lt 0 -or $persistIndex -lt 0 -or $beginIndex -ge $persistIndex) { $findings.Add('Council manifest coverage must block draft persistence before authoring.') }
if ($submitIndex -lt 0 -or $reviewIndex -lt 0 -or $submitIndex -ge $reviewIndex) { $findings.Add('Source-closure validation must precede immutable review creation.') }

# 7.6.6: an immutable review payload does not have Goal; the persisted review entity does.
# The pre-approval execution gate must validate the same recorded goal used by source closure.
$reviewExecutionSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/CodeGenerationWorkflowService.Execution.cs'))
if (-not $reviewExecutionSource.Contains('ValidateCompletedSourceCoverage(reviewedPayload, entity.Goal, decisions)') -or
    -not $reviewExecutionSource.Contains('ValidateDraftProjectTarget(entity.Goal, source.RelativePath, source.Content)')) {
    $findings.Add('Reviewed-source execution must validate the persisted review goal, not a property absent from CodeGenerationReviewPayload.')
}
if ($reviewExecutionSource.Contains('reviewedPayload.Goal')) {
    $findings.Add('CodeGenerationReviewPayload has no Goal property; use the authoritative persisted review entity.')
}

# The source file is rejected before its hash is accepted as a candidate; never accept malformed bytes
# merely because a model reviewer voted yes for that hash.
$assignmentSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/CodeGenerationWorkflowService.DraftAssignments.cs'))
$referencesIndex = $assignmentSource.IndexOf('ValidateDraftSourceReferences(current.RelativePath, source')
$hashIndex = $assignmentSource.IndexOf('var candidateHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)))')
if ($referencesIndex -lt 0 -or $hashIndex -lt 0 -or $referencesIndex -ge $hashIndex) { $findings.Add('Deterministic reference validation must precede candidate hashing and inspection.') }

# 7.6.5: a persisted source graph may be amended only by an exact-id/hash-fenced transaction,
# and an approved review may never be silently replaced with source from an old draft.
$amendment = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/CodeGenerationWorkflowService.DraftRepair.cs'))
$amendmentCoordinator = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/MultiModelCouncilService.FileWorkAmendment.cs'))
$sourceClosure = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/CodeGenerationWorkflowService.SourceClosure.cs'))
foreach ($requiredText in @('DraftAmendmentDraftId', 'DraftAmendmentManifestHash', 'ComputeDraftManifestHash(original)',
    'DraftAmendmentReopenHashes', 'DraftAmendmentCount', 'SaveDraftPlanAsync',
    'NormalizeDraftDependencies(expanded, edges)', 'ValidateSourceManifestCoverage(plan.Goal, expanded, humanDecisions)',
    '!head.ApprovalConsumed', 'DraftRepairRemoveHashes', 'DraftRepairReviewHash')) {
    if (-not $amendment.Contains($requiredText)) { $findings.Add('Source amendment/repair authority missing: ' + $requiredText) }
}
foreach ($requiredText in @('GetSourcePlanningDecisionsAsync(result.RunId', 'GetCouncilDraftProgressAsync(result.RunId',
    'StoredSourceHashes = current.CompletedFiles', 'draftOperation = "amend"', 'draftAmendmentManifestHash = current.ManifestSha256',
    'submitted.Status != "InvalidParameters"')) {
    if (-not $amendmentCoordinator.Contains($requiredText)) { $findings.Add('Source coordinator amendment boundary missing: ' + $requiredText) }
}
foreach ($requiredText in @('GetSourceDraftClosureDiagnosticAsync(result.RunId',
    'TryAmendIncompleteSourceDraftAsync(', 'generation--; // This is the same pre-approval draft')) {
    if (-not $fileWorkSource.Contains($requiredText)) { $findings.Add('Pre-review source closure retry missing: ' + $requiredText) }
}
foreach ($requiredText in @('sourcePaths.Distinct(StringComparer.OrdinalIgnoreCase)',
    'sourcePaths.Count != manifestPaths.Count', 'ValidateDraftSourceReferences(file.RelativePath, file.Content, manifest)',
    'BuildSourceRequirementGraph')) {
    if (-not $sourceClosure.Contains($requiredText)) { $findings.Add('Immutable source graph validation missing: ' + $requiredText) }
}
$registered = $registrySource.Contains('"amend","submit","repair"')
if (-not $registered) { $findings.Add('Registered draft enum does not include the exact amendment operation.') }
$contract = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Interfaces/ICodeGenerationWorkflowService.cs'))
if (-not $contract.Contains('GetSourceDraftClosureDiagnosticAsync(')) { $findings.Add('Missing read-only source closure service contract.') }

# 7.6.7: keep XML parameter documentation adjacent to the declaration it documents.
# The application treats CS1572/CS1573 as errors; orphaned tags prevent compilation.
$humanInterface = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Interfaces/IHumanCollaborationService.cs'))
$sourceDecisionIndex = $humanInterface.IndexOf('Task<IReadOnlyList<CouncilSourceHumanDecision>> GetSourcePlanningDecisionsAsync(')
$snapshotIndex = $humanInterface.IndexOf('Task<HumanCollaborationSnapshot> GetSnapshotAsync(')
if ($sourceDecisionIndex -lt 0 -or $snapshotIndex -le $sourceDecisionIndex) {
    $findings.Add('Human Collaboration source-planning and snapshot declarations are missing or reordered.')
} else {
    $sourceDecisionDocStart = $humanInterface.LastIndexOf('/// <summary>', $sourceDecisionIndex)
    $snapshotDocStart = $humanInterface.LastIndexOf('/// <summary>', $snapshotIndex)
    if ($sourceDecisionDocStart -lt 0 -or $snapshotDocStart -le $sourceDecisionIndex) {
        $findings.Add('Human Collaboration XML documentation must precede both declared methods.')
    } else {
        $sourceDecisionDoc = $humanInterface.Substring($sourceDecisionDocStart, $sourceDecisionIndex - $sourceDecisionDocStart)
        $snapshotDoc = $humanInterface.Substring($snapshotDocStart, $snapshotIndex - $snapshotDocStart)
        if (-not $sourceDecisionDoc.Contains('<param name="councilRunId">') -or
            -not $sourceDecisionDoc.Contains('<param name="cancellationToken">') -or
            $sourceDecisionDoc.Contains('<param name="includeResolved">') -or
            $sourceDecisionDoc.Contains('<param name="take">') -or
            [regex]::Matches($sourceDecisionDoc, '///\s*<summary>').Count -ne 1) {
            $findings.Add('Source-planning XML documentation must match councilRunId and cancellationToken only.')
        }
        foreach ($parameter in @('includeResolved', 'take', 'cancellationToken')) {
            if (-not $snapshotDoc.Contains('<param name="' + $parameter + '">')) {
                $findings.Add('Human Collaboration GetSnapshotAsync XML parameter documentation missing: ' + $parameter)
            }
        }
        if ($snapshotDoc.Contains('<param name="councilRunId">')) {
            $findings.Add('Human Collaboration snapshot XML documentation contains an unrelated Council run parameter.')
        }
    }
}
$workflowConstructorSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/CodeGenerationWorkflowService.cs'))
$constructorIndex = $workflowConstructorSource.IndexOf('public CodeGenerationWorkflowService(')
if ($constructorIndex -lt 0) {
    $findings.Add('CodeGenerationWorkflowService DI constructor is missing.')
} else {
    $constructorDocStart = $workflowConstructorSource.LastIndexOf('/// <summary>', $constructorIndex)
    if ($constructorDocStart -lt 0) {
        $findings.Add('CodeGenerationWorkflowService constructor XML documentation is missing.')
    } else {
        $constructorDoc = $workflowConstructorSource.Substring($constructorDocStart, $constructorIndex - $constructorDocStart)
        if (-not $constructorDoc.Contains('<param name="humanCollaboration">') -or
            [regex]::Matches($constructorDoc, '<param name="').Count -ne 12) {
            $findings.Add('CodeGenerationWorkflowService constructor XML documentation must match all 12 injected collaborators.')
        }
    }
}

# The 20 November fixture: no Program.cs, App.razor, persistence or service source;
# a self-referencing solution and absent additional projects cannot be promoted.
$fixturePaths = @('src/TwitchManager.csproj', 'src/TwitchManager.sln', 'src/TwitchManager/Pages/Index.razor', 'src/TwitchManager/Pages/Timelines.razor')
if ($fixturePaths -contains 'src/Program.cs' -or $fixturePaths -contains 'src/App.razor' -or
    @($fixturePaths | Where-Object { $_ -match '(?i)(database|sqlite|context|service)\.cs$' }).Count -gt 0) {
    $findings.Add('Negative four-file Twitch source manifest fixture is no longer representative.')
}

# 7.6.8: source layouts belong to the caller-bound project and compiler conventions,
# never a universal App.razor, _Imports.razor, Program.cs or wwwroot placement rule.
$closureSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/CodeGenerationWorkflowService.SourceClosure.cs'))
$sourceFormats = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/CodeGenerationWorkflowService.DraftTargetValidation.cs'))
$artifactExecutorSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/ArtifactBuildExecutor.cs'))
$projectTextSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/ProjectMaintenanceService.TextAndValidation.cs'))
$manifestNormalizer = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/MultiModelCouncilService.FileWorkManifest.cs'))
if ($draftService.Contains('ValidateManifestLayout(') -or $sourceFormats.Contains('ValidateManifestLayout(')) {
    $findings.Add('Project authorship must not be blocked by hardcoded Razor/wwwroot layout checks.')
}
if ($closureSource.Contains('Path.GetFileName(path), "Program.cs"') -or
    $closureSource.Contains('Path.GetFileName(path), "App.razor"') -or
    $closureSource.Contains('Path.GetFileName(item.Path), "Program.cs"')) {
    $findings.Add('Source closure must not infer a compiler template layout from fixed basename strings.')
}
if (-not $artifactExecutorSource.Contains('GetRequiredService<IProjectMaintenanceService>()') -or
    -not $artifactExecutorSource.Contains('FileName = compiler.ExecutablePath') -or
    $artifactExecutorSource.Contains('FileName = "dotnet"')) {
    $findings.Add('Approved artifact builds must use the registered, validated compiler executable, never PATH.')
}
if (-not $projectTextSource.Contains('runtimePolicy.GetCollection("TextExtensions")') -or
    -not $manifestNormalizer.Contains('runtimePolicy.GetCollection("SourceExtensions")')) {
    $findings.Add('Maintained source-extension catalogs must remain database-backed.')
}
if (-not $reviewExecutionSource.Contains('IsSourceSolutionPath(source.RelativePath)') -or
    -not $reviewExecutionSource.Contains('IsSourceProjectPath(source.RelativePath)')) {
    $findings.Add('Reviewed source build-target selection must use registered solution and project classifiers.')
}

if ($findings.Count -gt 0) { foreach ($finding in $findings) { Write-Error $finding -ErrorAction Continue }; throw ('Source contract audit failed: ' + $findings.Count + ' finding(s).') }
& (Join-Path $PSScriptRoot 'Assert-CompilerAgnosticArtifactBuild.ps1')
& (Join-Path $PSScriptRoot 'Assert-ReviewedArtifactReceipts.ps1')
& (Join-Path $PSScriptRoot 'Assert-CouncilRoadRuntimePolicy.ps1')
Write-Host 'Maintained PowerShell source contract audit passed.'

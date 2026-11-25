using System.Text.Json;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Coordinates independently authored source files through the existing Council scheduler and reviewed-generation gateway.</summary>
public sealed partial class MultiModelCouncilService
{
    /// <summary>Executes the configured file-authoring route; returns null only for explicitly classified existing repository work.</summary>
    /// <param name="result">Owning Council result and execution identity.</param>
    /// <param name="request">Original Council request and presentation callbacks.</param>
    /// <param name="team">Configured team that owns the workflow.</param>
    /// <param name="steps">Enabled definitions, including approval and verification stages.</param>
    /// <param name="policy">Persisted file-work policy and prompt templates.</param>
    /// <param name="baseUri">Default provider address.</param>
    /// <param name="participants">Selected provider-qualified eligible routes.</param>
    /// <param name="modelRoutes">Configured per-provider hardware limits.</param>
    /// <param name="maxParallelModels">Configured per-host concurrency.</param>
    /// <param name="keepAlive">Configured provider residency policy.</param>
    /// <param name="ollamaNumGpu">Configured fallback acceleration.</param>
    /// <param name="maxContextTokens">Fallback context allowance.</param>
    /// <param name="modelTimeoutSeconds">Configured request deadline.</param>
    /// <param name="firstRound">Next logical Council round number.</param>
    /// <param name="roleAssignments">Existing user-configured role assignments.</param>
    /// <param name="existingSourceAvailable">Coordinator-established evidence of caller-bound existing source.</param>
    /// <param name="technicalAuthority">Run-owned technical snapshot captured before Intake.</param>
    /// <param name="cancellationToken">Cancels the workflow without discarding committed receipts.</param>
    /// <returns>Concrete delivery or incomplete-work report, or null for the existing-repository route.</returns>
    private async Task<string?> RunCoordinatedFileWorkAsync(MultiModelCouncilResult result,
        MultiModelCouncilRequest request, OrganicCouncilTeamDefinition team,
        IReadOnlyList<CouncilWorkflowStepDefinition> steps, CouncilFileWorkRuntimeParameters policy,
        string baseUri, IReadOnlyList<string> participants, IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
        int maxParallelModels, string keepAlive, int? ollamaNumGpu, int maxContextTokens, int modelTimeoutSeconds,
        int firstRound, IReadOnlyDictionary<string, CouncilRoleRuntimeAssignment> roleAssignments, bool existingSourceAvailable, object technicalAuthority, CancellationToken cancellationToken)
    {
        try
        {
            var planStep = steps.Single(item => item.Key == policy.PlanStepKey);
            var generateStep = steps.Single(item => item.Key == policy.GenerationStepKey);
            var verifyStep = steps.Single(item => item.Key == policy.VerificationStepKey);
            var curateStep = steps.SingleOrDefault(item => item.Key == policy.CurationStepKey) ?? verifyStep;
            var round = firstRound;
            var requirementsInstruction = runtimePolicy.GetString("CouncilFileWorkRequirementsInstruction");
            // Do not bypass a user-configured required human or human-only role.
            if (steps.Any(step => roleAssignments.TryGetValue(step.Role, out var assigned) &&
                assigned.HumanParticipationMode is HumanParticipationMode.Required or HumanParticipationMode.HumanOnly))
                return existingSourceAvailable ? null : runtimePolicy.GetString("CouncilFileWorkHumanRouteBlockedNotice");
            // A listed development team authorizes the coordinator's calls through one configured step
            // (its build step); the Program Compiler team keeps using its generation and verification steps.
            var permissionStep = string.IsNullOrWhiteSpace(policy.FunctionPermissionStepKey) ? null
                : steps.SingleOrDefault(item => item.Key == policy.FunctionPermissionStepKey);
            var generationPolicy = councilAutomaticFunctionPolicy.Resolve(team, permissionStep ?? generateStep, false, request.RequestedOrganicCapabilities);
            var verificationPolicy = councilAutomaticFunctionPolicy.Resolve(team, permissionStep ?? verifyStep, false, request.RequestedOrganicCapabilities);
            if (!generationPolicy.Enabled || !verificationPolicy.Enabled ||
                (generationPolicy.IsStrict && generationPolicy.AutomaticFunctionAllowList is { } generatedAllowed &&
                    (!generatedAllowed.Contains("codegen.review.create") || !generatedAllowed.Contains("codegen.review.execute"))) ||
                (verificationPolicy.IsStrict && verificationPolicy.AutomaticFunctionAllowList is { } verifiedAllowed &&
                    !verifiedAllowed.Contains("council.development.artifact.verify")))
                return existingSourceAvailable ? null : runtimePolicy.GetString("CouncilFileWorkPermissionRouteBlockedNotice"); // Preserve the configured permission boundary.

            var routes = OrderParticipantsByObservedHealth(result, participants).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var architectRoutes = routes.Where(model => !roleAssignments.TryGetValue(planStep.Role, out var assigned) ||
                assigned.AiParticipants.Contains(model, StringComparer.OrdinalIgnoreCase)).ToList();
            // A fast classification is not evidence that the same route can author a complete manifest.
            // Prefer available planning token capacity, retaining the observed-health order for equal roads.
            if (policy.PreferPlanningCapacity)
                architectRoutes = architectRoutes.OrderByDescending(model => Math.Min(Math.Max(64, policy.MaximumPlanningOutputTokens),
                        modelRoutes.TryGetValue(model, out var road) ? road.EffectiveMaxOutputTokens : request.MaxOutputTokens))
                    .ThenByDescending(model => modelRoutes.TryGetValue(model, out var road) ? road.EffectiveMaxContextTokens : maxContextTokens)
                    .ToList();
            var authorRoutes = routes.Where(model => !roleAssignments.TryGetValue(generateStep.Role, out var assigned) ||
                assigned.AiParticipants.Contains(model, StringComparer.OrdinalIgnoreCase)).ToList();
            var curatorRoutes = routes.Where(model => !roleAssignments.TryGetValue(curateStep.Role, out var assigned) ||
                assigned.AiParticipants.Contains(model, StringComparer.OrdinalIgnoreCase)).ToList();
            if (architectRoutes.Count == 0 || authorRoutes.Count == 0 || curatorRoutes.Count == 0)
                return runtimePolicy.GetString("CouncilFileWorkNoRoutesNotice");
            // The prior transcript remains in the Council archive. Only bounded visible evidence goes to the architect.
            var evidenceBudget = Math.Max(0, policy.MaximumPlanEvidenceCharacters);
            var evidence = new List<object>();
            var hasAttachedEvidence = request.AttachedWorkspaceNames.Count > 0;
            foreach (var step in result.Steps.Where(item => string.IsNullOrWhiteSpace(item.Error) &&
                !string.IsNullOrWhiteSpace(item.VisibleContent) &&
                (hasAttachedEvidence || item.ModelName is "LocalGPT: workspace curator gate" or "LocalGPT: continuation workspace curator gate"))
                .Reverse().OrderByDescending(item => item.ModelName is "LocalGPT: workspace curator gate" or "LocalGPT: continuation workspace curator gate"))
            {
                if (evidenceBudget <= 0) break;
                var length = Math.Min(evidenceBudget, step.VisibleContent.Length);
                evidence.Add(new { step.Role, step.ModelName, Text = step.VisibleContent[..length] });
                evidenceBudget -= length;
            }
            var goal = string.IsNullOrWhiteSpace(request.GoalText) ? request.Prompt : request.GoalText;
            // A heartbeat may already have marked these answered questions as Consumed.
            // The run-scoped decisions remain authoritative for source planning regardless.
            var sourceDecisions = await humanCollaboration.GetSourcePlanningDecisionsAsync(result.RunId, cancellationToken).ConfigureAwait(false);
            var requiredTargets = GetExplicitFileWorkTargets(goal);
            var excludeDevExpress = runtimePolicy.GetPattern("runtime.codegen.explicit-no-devexpress").IsMatch(goal);
            var progress = await codeGenerationWorkflow.GetCouncilDraftProgressAsync(result.RunId, cancellationToken).ConfigureAwait(false);
            if (progress is null)
            {
                var planDiagnostic = string.Empty;
                var previousCandidate = string.Empty;
                var attemptedArchitects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var attemptedManifests = new HashSet<string>(StringComparer.Ordinal);
                var beginAttempted = false;
                var ordinaryRoute = !existingSourceAvailable;
                var alternativeRouteObserved = false;
                var planAttempts = Math.Max(1, policy.MaximumPlanAttempts);
                var manifestRecoveryGuidance = string.Empty;
                var uncertainBegin = false;
                for (var attempt = 0; attempt < planAttempts + (policy.EnableManifestRecovery ? 1 : 0); attempt++)
                {
                    if (attempt == planAttempts)
                    {
                        // This is another pure planning call in this run, not a replay of earlier tools or approvals.
                        if (!ordinaryRoute || alternativeRouteObserved || uncertainBegin ||
                            (await deferredDxAiInvocations.GetForRunAsync(result.RunId, cancellationToken).ConfigureAwait(false)).Count != 0)
                            break;
                        var decision = await aiHelpEscalator.ReportAttemptAsync(new AiHelpAttemptReport
                        {
                            TaskId = $"council:{result.RunId:N}:source-manifest", AttemptId = Guid.NewGuid(),
                            CouncilRunId = result.RunId, Task = goal[..Math.Min(goal.Length, 2000)],
                            Action = "source manifest recovery", Status = "failed",
                            Error = new AiHelpFailureEvidence { Code = "SourceManifestMissing", Source = "validator", Message = planDiagnostic }
                        }, cancellationToken).ConfigureAwait(false);
                        var notice = runtimePolicy.GetString("CouncilFileWorkManifestRecoveryNotice") + "\n" + JsonSerializer.Serialize(decision);
                        var recoveryStep = new MultiModelCouncilStep { ModelName = "LocalGPT Nurse", Role = "Source recovery", Round = round++,
                            Phase = planStep.Phase, Content = notice, VisibleContent = System.Net.WebUtility.HtmlEncode(notice),
                            StartedAtUtc = DateTime.UtcNow, CompletedAtUtc = DateTime.UtcNow };
                        MultiModelCouncilServiceAddOrderedStep(result, recoveryStep, logger);
                        request.StepCompleted?.Invoke(recoveryStep);
                        request.StreamUpdate?.Invoke("\n\n<pre>" + System.Net.WebUtility.HtmlEncode(notice) + "</pre>\n\n");
                        if (decision.Type is not ("try_alternative" or "retry_with_guidance")) break;
                        manifestRecoveryGuidance = decision.Instruction;
                        previousCandidate = string.Empty;
                    }
                    // Human answers may arrive after an earlier planner attempt. Rebind before each new proposal.
                    sourceDecisions = await humanCollaboration.GetSourcePlanningDecisionsAsync(result.RunId, cancellationToken).ConfigureAwait(false);
                    var architect = architectRoutes.FirstOrDefault(model => !attemptedArchitects.Contains(model))
                        ?? architectRoutes[attempt % architectRoutes.Count];
                    attemptedArchitects.Add(architect);
                    var prompt = policy.PlanPrompt + "\n" + requirementsInstruction + "\n" +
                        runtimePolicy.GetString("CouncilFileWorkHumanDecisionInstruction") + "\n" + runtimePolicy.GetString("CouncilFileWorkPlanShapeInstruction") + "\n" +
                        runtimePolicy.GetString("CouncilFileWorkPlanPacketInstruction") + "\n" + JsonSerializer.Serialize(new
                        {
                            Goal = goal, RequiredDotNetTargets = requiredTargets, ExcludeDevExpress = excludeDevExpress,
                            TechnicalAuthority = technicalAuthority, HumanDecisions = sourceDecisions,
                            BoundProjectId = result.ProjectId, BoundProjectRevisionId = result.ProjectRevisionId,
                            ExistingSourceAvailable = existingSourceAvailable,
                            SourceRouteInstruction = runtimePolicy.GetString("CouncilFileWorkSourceRouteInstruction"),
                            AttachedWorkspaceNames = request.AttachedWorkspaceNames, Evidence = evidence,
                            PreviousValidationError = planDiagnostic, PreviousCandidate = previousCandidate, RecoveryGuidance = manifestRecoveryGuidance,
                            GuestLanguages = fileWorkSandboxes.Languages.Where(language => fileWorkSandboxes.RuntimeReadiness.Any(runtime => runtime.Runtime == language.Runtime && runtime.Provisioned)).Select(language => language.Key).ToList()
                        });
                    // Never carry malformed or rejected locally validated plans into a fresh architect call.
                    // A useful structurally valid candidate is retained only for a known gateway validation rejection.
                    previousCandidate = string.Empty;
                    var replies = await RunFileWorkPhaseAsync(result, request, planStep, baseUri, routes, [architect],
                        round++, planStep.Phase, _ => prompt, modelRoutes, maxParallelModels, keepAlive, ollamaNumGpu,
                        maxContextTokens, modelTimeoutSeconds, null, cancellationToken).ConfigureAwait(false);
                    var reply = replies.LastOrDefault(item => string.IsNullOrWhiteSpace(item.Error));
                    if (reply is null)
                    {
                        planDiagnostic = replies.LastOrDefault()?.Error ?? runtimePolicy.GetString("CouncilFileWorkNoPlanNotice");
                        continue;
                    }
                    try
                    {
                        var explicitPlan = new Dictionary<string, JsonElement>(ReadFileWorkJson<Dictionary<string, JsonElement>>(ReadFileWorkAnswer(reply)), StringComparer.OrdinalIgnoreCase);
                        if (!explicitPlan.TryGetValue("draftWorkspaceMode", out var workspaceMode) || workspaceMode.ValueKind != JsonValueKind.String)
                            throw new ArgumentException("Choose one explicit workspace mode.");
                        if (workspaceMode.GetString() == "ExistingRepository" && !existingSourceAvailable)
                            throw new ArgumentException(runtimePolicy.GetString("CouncilFileWorkExistingSourceRequiredNotice"));
                        ordinaryRoute |= workspaceMode.GetString() == "ReviewedSourceDraft";
                        alternativeRouteObserved |= workspaceMode.GetString() != "ReviewedSourceDraft";
                        var plan = ReadSourceManifest(ReadFileWorkAnswer(reply), goal);
                        if (plan.DraftWorkspaceMode == "ExistingRepository")
                            return null; // Existing staged-repository approval workflow remains authoritative.
                        if (plan.DraftWorkspaceMode == "HyperlightProject")
                            return runtimePolicy.GetString("CouncilFileWorkGuestProjectUnsupportedNotice");
                        if (plan.DraftWorkspaceMode == "HyperlightSourceDraft" && generationPolicy.IsStrict &&
                            generationPolicy.AutomaticFunctionAllowList is { } guestAllowed && !guestAllowed.Contains("session.sandbox.run"))
                            throw new ArgumentException("The configured strict generation role does not permit isolated script execution.");
                        if (plan.DraftWorkspaceMode == "HyperlightSourceDraft" && !fileWorkSandboxes.Languages.Any(language => language.Key == plan.DraftGuestLanguageKey &&
                            fileWorkSandboxes.RuntimeReadiness.Any(runtime => runtime.Runtime == language.Runtime && runtime.Provisioned)))
                            throw new ArgumentException("The selected isolated guest runtime is not provisioned.");
                        if (plan.DraftWorkspaceMode is not ("ReviewedSourceDraft" or "HyperlightSourceDraft"))
                            throw new ArgumentException("Unknown workspace mode.");
                        plan.Goal = goal;
                        // The coordinator, not model JSON, supplies the persisted technical evidence.
                        plan.DraftSharedContract = JsonSerializer.Serialize(new { TechnicalAuthority = technicalAuthority, HumanDecisions = sourceDecisions, Architecture = plan.DraftSharedContract });
                        plan.CouncilRunId = result.RunId;
                        plan.DraftOperation = "begin";
                        // Never accept IDs, raw files or approval state from the planning model.
                        if (plan.Files is not { Count: 0 } || plan.CodeDomTypes is not { Count: 0 } || plan.Outputs is not { Count: > 0 })
                            throw new ArgumentException("The plan requires a file manifest and output targets, not candidate source.");
                        if (plan.DraftManifest is not { Count: > 0 } || plan.DraftManifest.Count > Math.Max(1, policy.MaximumManifestFiles) ||
                            plan.DraftManifest.Values.Any(string.IsNullOrWhiteSpace))
                            throw new ArgumentException("Provide distinct relative paths with nonempty string work orders in draftManifest.");
                        var beginParameters = new { draftOperation = "begin", goal = plan.Goal,
                            title = plan.Title, draftWorkspaceMode = plan.DraftWorkspaceMode,
                            draftGuestLanguageKey = plan.DraftGuestLanguageKey, draftManifest = plan.DraftManifest,
                            draftSharedContract = plan.DraftSharedContract, draftDependencies = plan.DraftDependencies ?? new Dictionary<string, List<string>>(),
                            outputs = plan.Outputs };
                        var candidate = JsonSerializer.Serialize(beginParameters);
                        if (!attemptedManifests.Add(candidate))
                            throw new ArgumentException(runtimePolicy.GetString("CouncilFileWorkRepeatedManifestNotice"));
                        beginAttempted = true;
                        uncertainBegin = true;
                        var created = await InvokeFileWorkFunctionAsync(result, request, round - 1, planStep.Phase,
                            "codegen.review.create", beginParameters, cancellationToken).ConfigureAwait(false);
                        if (!created.Succeeded)
                        {
                            planDiagnostic = created.Error ?? created.Status;
                            // A rejected or interrupted gateway may already have persisted a draft.
                            progress = await codeGenerationWorkflow.GetCouncilDraftProgressAsync(result.RunId, cancellationToken).ConfigureAwait(false);
                            if (progress is not null) break;
                            var retryNotice = runtimePolicy.GetString("CouncilFileWorkManifestRetryNotice") + " " + planDiagnostic;
                            result.Warnings.Add(retryNotice);
                            request.ProgressMessage?.Invoke(retryNotice);
                            request.StreamUpdate?.Invoke("\n\n> " + System.Net.WebUtility.HtmlEncode(retryNotice) + "\n\n");
                            if (created.Status != "InvalidParameters")
                            {
                                uncertainBegin = true;
                                break;
                            }
                            uncertainBegin = false; // Known validation rejection, with no persisted draft found.
                            var rejectedProposal = JsonSerializer.Serialize(explicitPlan);
                            if (rejectedProposal.Length <= Math.Max(0, policy.MaximumPlanEvidenceCharacters))
                                previousCandidate = rejectedProposal;
                            continue;
                        }
                        progress = await codeGenerationWorkflow.GetCouncilDraftProgressAsync(result.RunId, cancellationToken).ConfigureAwait(false);
                        if (progress is not null) break;
                        uncertainBegin = true; // Success without persisted state is not a safe replay signal.
                        break;
                    }
                    catch (Exception exception) when (exception is JsonException or ArgumentException)
                    {
                        planDiagnostic = exception is JsonException jsonError
                            ? $"Invalid JSON field: {jsonError.Path ?? "root"}." : exception.Message;
                        logger.LogInformation("Council {RunId} architect returned an invalid source manifest; diagnostic category {Category}.", result.RunId, exception.GetType().Name);
                        var notice = runtimePolicy.GetString("CouncilFileWorkManifestRetryNotice") + " " + planDiagnostic;
                        result.Warnings.Add(notice);
                        request.ProgressMessage?.Invoke(notice);
                        request.StreamUpdate?.Invoke("\n\n> " + System.Net.WebUtility.HtmlEncode(notice) + "\n\n");
                        if (uncertainBegin) break;
                    }
                }
                // Never route a missing greenfield manifest into generic Generation/Curation.
                // Only a source-bearing caller context can opt into the legacy reviewed route.
                if (progress is null && !beginAttempted && ordinaryRoute && !alternativeRouteObserved && result.Steps.All(item =>
                        !item.Content.Contains("Function call ·", StringComparison.OrdinalIgnoreCase) &&
                        !item.Content.Contains("Function result ·", StringComparison.OrdinalIgnoreCase) &&
                        !item.Content.Contains("<localgpt-dx-call>", StringComparison.OrdinalIgnoreCase) &&
                        !(item.Error?.Contains("priorToolActivity=true", StringComparison.OrdinalIgnoreCase) ?? false)) &&
                    (await deferredDxAiInvocations.GetForRunAsync(result.RunId, cancellationToken).ConfigureAwait(false)).Count == 0)
                {
                    if (existingSourceAvailable && policy.AllowLegacyReviewedFallback)
                    {
                        result.Warnings.Add(runtimePolicy.GetString("CouncilFileWorkLegacyFallbackNotice"));
                        return null;
                    }
                    result.SourceManifestRetrySafe = true;
                }
            }
            if (progress is null)
                return runtimePolicy.GetString("CouncilFileWorkNoPlanNotice");
            // Propagate authoritative identity, not identifiers reconstructed from model text.
            result.ProjectId = progress.ProjectId;
            result.ProjectRevisionId = progress.ProjectRevisionId;
            result.CoordinatedSourceWork = true;
            councilSpooler.UpdateIdentity(result);
            // A failed review transport is not a negative inspection of the source bytes.
            var attemptedInspections = new HashSet<(Guid DraftId, string Path, string Hash, string DependencyRevision, string Reviewer)>();
            var draftRecoveryPass = 0;
            var recoveryGuidance = string.Empty;
            for (var generation = 0; generation <= Math.Max(0, policy.MaximumRepairGenerations); generation++)
            {
                if (progress.IsDraft)
                {
                    var attemptAllowance = Math.Max(1, policy.MaximumFileAttempts) +
                        draftRecoveryPass * Math.Max(0, policy.DraftRecoveryAdditionalAttempts);
                    var maximumBatches = Math.Max(1, policy.MaximumManifestFiles) * attemptAllowance;
                    for (var batch = 0; batch < maximumBatches; batch++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var packets = await codeGenerationWorkflow.ReserveDraftFilesAsync(result.RunId, authorRoutes,
                            attemptAllowance, cancellationToken).ConfigureAwait(false);
                        if (packets.Count > 0)
                        {
                            var byModel = packets.ToDictionary(packet => packet.Assignment.ModelName, StringComparer.OrdinalIgnoreCase);
                            var phase = $"{generateStep.Phase} · X-Round {generation + 1}.{batch + 1}";
                            await RunFileWorkPhaseAsync(result, request, generateStep, baseUri, routes, byModel.Keys.ToList(),
                                round++, phase, model =>
                                {
                                    var packet = byModel[model];
                                    var guest = progress.WorkspaceMode == "HyperlightSourceDraft";
                                    var evidence = guest
                                        ? JsonSerializer.Serialize(new { progress.WorkspaceMode, progress.GuestLanguageKey, RecoveryGuidance = recoveryGuidance, Packet = packet })
                                        : JsonSerializer.Serialize(new { packet.Assignment.RelativePath, packet.Goal, packet.SharedContract,
                                            packet.Dependencies, packet.PreviousSource, RecoveryGuidance = recoveryGuidance, packet.WorkOrder });
                                    return (guest ? policy.GuestAuthorPrompt : policy.AuthorPrompt) + "\n" + requirementsInstruction + "\n" + evidence;
                                }, modelRoutes, maxParallelModels, keepAlive,
                                ollamaNumGpu, maxContextTokens, modelTimeoutSeconds, async step =>
                                {
                                    if (progress.WorkspaceMode == "HyperlightSourceDraft") return; // Import only real, approved guest output after the batch.
                                    var packet = byModel[step.ModelName];
                                    var receipt = await codeGenerationWorkflow.CompleteDraftFileAsync(result.RunId, packet.Assignment,
                                        step.ModelName, ReadFileWorkAnswer(step), step.Error, cancellationToken).ConfigureAwait(false);
                                    step.FileWorkReceiptJson = JsonSerializer.Serialize(new { receipt.RelativePath, receipt.Id, receipt.Sha256, receipt.Diagnostic });
                                    step.VisibleContent += "\n\n" + System.Net.WebUtility.HtmlEncode(step.FileWorkReceiptJson);
                                    if (string.IsNullOrWhiteSpace(receipt.Sha256) && string.IsNullOrWhiteSpace(step.Error))
                                        step.Error = receipt.Diagnostic;
                                }, cancellationToken).ConfigureAwait(false);
                            if (progress.WorkspaceMode == "HyperlightSourceDraft")
                            {
                                var guestSteps = result.Steps.Where(item => item.Round == round - 1 && item.Phase == phase).ToList();
                                foreach (var packet in packets)
                                {
                                    var authored = guestSteps.LastOrDefault(item => item.ModelName == packet.Assignment.ModelName);
                                    await ImportFileWorkGuestOutputAsync(result, request, generateStep, progress, packet,
                                        authored, round - 1, phase, cancellationToken).ConfigureAwait(false);
                                }
                            }
                        }
                        progress = await codeGenerationWorkflow.GetCouncilDraftProgressAsync(result.RunId, cancellationToken).ConfigureAwait(false)
                            ?? throw new InvalidOperationException("The source draft disappeared during file generation.");
                        // Review actual committed bytes, not the prose transcript. A changed dependency invalidates its inspection.
                        var inspections = await codeGenerationWorkflow.GetDraftInspectionsAsync(result.RunId, cancellationToken).ConfigureAwait(false);
                        if (inspections.Count > 0 && routes.Count > 1)
                        {
                            var reviewPackets = new Dictionary<string, CodeGenerationDraftInspection>(StringComparer.OrdinalIgnoreCase);
                            foreach (var inspection in inspections)
                            {
                                var reviewer = curatorRoutes.Where(model => !reviewPackets.ContainsKey(model) &&
                                    !string.Equals(model, inspection.Author, StringComparison.OrdinalIgnoreCase) && !inspection.PreviousReviewers.Contains(model, StringComparer.OrdinalIgnoreCase) &&
                                    !attemptedInspections.Contains((inspection.DraftId, inspection.Path, inspection.Sha256, JsonSerializer.Serialize(inspection.DependencyHashes.OrderBy(pair => pair.Key)), model)))
                                    .OrderByDescending(model => modelRoutes.TryGetValue(model, out var road) ? road.EffectiveMaxContextTokens : maxContextTokens)
                                    .FirstOrDefault();
                                if (reviewer is not null)
                                {
                                    reviewPackets.Add(reviewer, inspection);
                                    attemptedInspections.Add((inspection.DraftId, inspection.Path, inspection.Sha256, JsonSerializer.Serialize(inspection.DependencyHashes.OrderBy(pair => pair.Key)), reviewer));
                                }
                            }
                            if (reviewPackets.Count > 0)
                            {
                                await RunFileWorkPhaseAsync(result, request, curateStep, baseUri, routes, reviewPackets.Keys.ToList(),
                                    round++, $"{curateStep.Phase} · X-Round {generation + 1}.{batch + 1}", model => policy.CuratorPrompt + "\n" + requirementsInstruction + "\n" + JsonSerializer.Serialize(reviewPackets[model]),
                                    modelRoutes, maxParallelModels, keepAlive, ollamaNumGpu, maxContextTokens, modelTimeoutSeconds,
                                    async step =>
                                    {
                                        var inspection = reviewPackets[step.ModelName];
                                        try
                                        {
                                            if (!string.IsNullOrWhiteSpace(step.Error)) return;
                                            var fields = new Dictionary<string, JsonElement>(ReadFileWorkJson<Dictionary<string, JsonElement>>(ReadFileWorkAnswer(step)), StringComparer.OrdinalIgnoreCase);
                                            if (!fields.TryGetValue("accepted", out var accepted) || accepted.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                                                throw new ArgumentException("The inspection requires an explicit accepted boolean.");
                                            if (fields.TryGetValue("diagnostic", out var diagnostic) && diagnostic.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                                                fields["diagnostic"] = JsonSerializer.SerializeToElement(diagnostic.GetRawText());
                                            var decision = JsonSerializer.Deserialize<CodeGenerationDraftInspectionDecision>(JsonSerializer.Serialize(fields), new JsonSerializerOptions(JsonSerializerDefaults.Web))
                                                ?? throw new JsonException("Missing inspection decision.");
                                            if (decision.Sha256 != inspection.Sha256 || (decision.Accepted == false && string.IsNullOrWhiteSpace(decision.Diagnostic)))
                                                throw new ArgumentException("The inspection did not bind a valid decision to the exact candidate.");
                                            decision.Reviewer = step.ModelName;
                                            var independentCount = curatorRoutes.Count(model => !string.Equals(model, inspection.Author, StringComparison.OrdinalIgnoreCase));
                                            await codeGenerationWorkflow.RecordDraftInspectionAsync(result.RunId, reviewPackets[step.ModelName],
                                                decision, Math.Max(1, Math.Min(policy.RequiredNegativeInspections, independentCount)), cancellationToken).ConfigureAwait(false);
                                            step.FileWorkReceiptJson = JsonSerializer.Serialize(new { inspection.Path, inspection.DraftId, decision.Sha256, decision.Accepted, decision.Diagnostic, decision.Reviewer });
                                        }
                                        catch (Exception exception) when (exception is ArgumentException or JsonException or InvalidOperationException)
                                        {
                                            logger.LogWarning("Council inspection of {Path} returned no valid decision ({Category}); the source candidate remains unchanged.", inspection.Path, exception.GetType().Name);
                                            step.FileWorkReceiptJson = JsonSerializer.Serialize(new { inspection.Path, inspection.DraftId, inspection.Sha256, Accepted = (bool?)null, Error = exception.GetType().Name, Diagnostic = exception.Message });
                                        }
                                    }, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        progress = await codeGenerationWorkflow.GetCouncilDraftProgressAsync(result.RunId, cancellationToken).ConfigureAwait(false)
                            ?? throw new InvalidOperationException("The source draft disappeared during inspection.");
                        var outstandingInspections = await codeGenerationWorkflow.GetDraftInspectionsAsync(result.RunId, cancellationToken).ConfigureAwait(false);
                        if (progress.MissingPaths.Count == 0 && (routes.Count == 1 || outstandingInspections.Count == 0)) break;
                        if (packets.Count == 0 && (inspections.Count == 0 || inspections.All(inspection => curatorRoutes.All(model =>
                            string.Equals(model, inspection.Author, StringComparison.OrdinalIgnoreCase) || inspection.PreviousReviewers.Contains(model, StringComparer.OrdinalIgnoreCase) ||
                            attemptedInspections.Contains((inspection.DraftId, inspection.Path, inspection.Sha256, JsonSerializer.Serialize(inspection.DependencyHashes.OrderBy(pair => pair.Key)), model)))))) break;
                    }
                    var unresolved = await codeGenerationWorkflow.GetDraftInspectionsAsync(result.RunId, cancellationToken).ConfigureAwait(false);
                    if (progress.MissingPaths.Count > 0 || (routes.Count > 1 && unresolved.Count > 0))
                    {
                        // Only read/author/inspect the existing unsubmitted draft. No approval or host operation may be replayed.
                        if (draftRecoveryPass < Math.Max(0, policy.MaximumDraftRecoveryPasses) && progress.IsDraft &&
                            progress.WorkspaceMode == "ReviewedSourceDraft" && progress.MissingPaths.Count > 0)
                        {
                            var recovery = await aiHelpEscalator.ReportAttemptAsync(new AiHelpAttemptReport
                            {
                                TaskId = $"council:{result.RunId:N}:draft:{progress.ReviewId:N}", AttemptId = Guid.NewGuid(),
                                CouncilRunId = result.RunId, Task = string.Join("; ", progress.MissingPaths).Length > 2000
                                    ? string.Join("; ", progress.MissingPaths)[..2000] : string.Join("; ", progress.MissingPaths),
                                Action = "source draft recovery", Status = draftRecoveryPass == 0 ? "failed" : "no_progress",
                                Error = new AiHelpFailureEvidence { Code = "SourceDraftIncomplete", Source = "validator",
                                    Message = runtimePolicy.GetString("CouncilFileWorkIncompleteNotice") }
                            }, cancellationToken).ConfigureAwait(false);
                            var notice = runtimePolicy.GetString("CouncilFileWorkRecoveryNotice") + "\n" + JsonSerializer.Serialize(recovery);
                            var recoveryStep = new MultiModelCouncilStep { ModelName = "LocalGPT Nurse", Role = "Source recovery", Round = round++,
                                Phase = generateStep.Phase, Content = notice, VisibleContent = System.Net.WebUtility.HtmlEncode(notice),
                                StartedAtUtc = DateTime.UtcNow, CompletedAtUtc = DateTime.UtcNow };
                            MultiModelCouncilServiceAddOrderedStep(result, recoveryStep, logger);
                            request.StepCompleted?.Invoke(recoveryStep);
                            request.StreamUpdate?.Invoke("\n\n<pre>" + System.Net.WebUtility.HtmlEncode(notice) + "</pre>\n\n");
                            if (recovery.Type is "try_alternative" or "retry_with_guidance")
                            {
                                draftRecoveryPass++;
                                recoveryGuidance = recovery.Instruction;
                                generation--; // This is not a new build generation; the same draft and receipts remain authoritative.
                                continue;
                            }
                        }
                        return FormatIncompleteFileWork(result, progress);
                    }
                    draftRecoveryPass = 0;
                    recoveryGuidance = string.Empty;
                    // Prevent an incomplete four-file source graph from becoming a review. When
                    // the bytes are locally complete but source contracts fail, request only an
                    // additive or explicitly hash-fenced correction, not another initial begin.
                    var closureDiagnostic = await codeGenerationWorkflow.GetSourceDraftClosureDiagnosticAsync(result.RunId,
                        cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(closureDiagnostic))
                    {
                        if (progress.AmendmentCount >= Math.Max(0, policy.MaximumDraftAmendments))
                            return runtimePolicy.GetString("CouncilFileWorkIncompleteNotice") + "\n" +
                                "The persistent source amendment limit was reached: " + closureDiagnostic;
                        var amendmentRound = round;
                        round += Math.Max(1, Math.Min(Math.Max(0, policy.MaximumDraftAmendments), architectRoutes.Count));
                        if (await TryAmendIncompleteSourceDraftAsync(result, request, policy, planStep,
                            baseUri, routes, architectRoutes, modelRoutes, maxParallelModels, keepAlive, ollamaNumGpu,
                            maxContextTokens, modelTimeoutSeconds, amendmentRound, progress, closureDiagnostic,
                            cancellationToken).ConfigureAwait(false))
                        {
                            progress = await codeGenerationWorkflow.GetCouncilDraftProgressAsync(result.RunId, cancellationToken).ConfigureAwait(false)
                                ?? throw new InvalidOperationException("The amended source draft disappeared.");
                            generation--; // This is the same pre-approval draft, not another build generation.
                            continue;
                        }
                        return runtimePolicy.GetString("CouncilFileWorkIncompleteNotice") + "\n" + closureDiagnostic;
                    }
                    // Submitting through the existing handler creates the immutable review and its exact deferred approval.
                    var submission = await InvokeFileWorkFunctionAsync(result, request, round, generateStep.Phase,
                        "codegen.review.create", new { draftOperation = "submit" }, cancellationToken).ConfigureAwait(false);
                    if (!submission.Succeeded)
                        return runtimePolicy.GetString("CouncilFileWorkIncompleteNotice") + "\n" + submission.Error;
                }
                // The existing approval waiter executes only exact approved gateway calls.
                var approvalStep = new CouncilWorkflowStepDefinition { DisplayName = generateStep.DisplayName, WaitForDeferredApprovalsBeforeNextStep = true };
                await WaitForDeferredApprovalsBeforeNextStepAsync(result, request, approvalStep,
                    round++, generateStep.Phase, false, cancellationToken).ConfigureAwait(false);
                progress = await codeGenerationWorkflow.GetCouncilDraftProgressAsync(result.RunId, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("The submitted review disappeared.");
                // Approval may be consumed by the existing inbox worker before this heartbeat sees its result.
                using (var buildDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    buildDeadline.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, modelTimeoutSeconds)));
                    while (progress.Status == CodeGenerationReviewStatuses.Generating ||
                        (await deferredDxAiInvocations.GetForRunAsync(result.RunId, cancellationToken).ConfigureAwait(false)).Any(item =>
                            item.FunctionName == "codegen.review.execute" && item.Status == vocabulary.Get().DeferredExecuting))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), buildDeadline.Token).ConfigureAwait(false);
                        progress = await codeGenerationWorkflow.GetCouncilDraftProgressAsync(result.RunId, cancellationToken).ConfigureAwait(false)
                            ?? throw new InvalidOperationException("The submitted review disappeared during execution.");
                    }
                }
                result.ChangeReview = await codeGenerationWorkflow.GetReviewAsync(progress.ReviewId, cancellationToken).ConfigureAwait(false);
                var buildEvidence = await deferredDxAiInvocations.GetCodeGenerationBuildEvidenceAsync(result.RunId, progress.ReviewId, cancellationToken).ConfigureAwait(false);
                if (progress.Status == CodeGenerationReviewStatuses.BuildPassed)
                {
                    var verified = await InvokeFileWorkFunctionAsync(result, request, round, verifyStep.Phase,
                        "council.development.artifact.verify", new { reviewId = progress.ReviewId }, cancellationToken).ConfigureAwait(false);
                    if (verified.Succeeded && result.ChangeReview is { } review &&
                        !string.IsNullOrWhiteSpace(review.ZipFileName) && Path.GetFileName(review.ZipFileName) == review.ZipFileName)
                    {
                        var zipPath = Path.Combine(artifactService.ArtifactRoot, review.ZipFileName);
                        if (File.Exists(zipPath))
                        {
                            var existingArtifact = result.Artifacts.FirstOrDefault(artifact => string.Equals(artifact.FilePath, zipPath, StringComparison.OrdinalIgnoreCase));
                            if (existingArtifact is not null)
                            {
                                existingArtifact.QualityStatus = CodeGenerationReviewStatuses.BuildPassed;
                                existingArtifact.ContractStatus = "Verified";
                                existingArtifact.ContractChecks = ["council.development.artifact.verify"];
                                existingArtifact.MissingRequirements = [];
                            }
                            else
                                result.Artifacts.Add(new CouncilArtifact
                                {
                                    Name = review.ZipFileName, Kind = "SourceZip", FilePath = zipPath,
                                    DownloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(review.ZipFileName)}",
                                    QualityStatus = CodeGenerationReviewStatuses.BuildPassed,
                                    ContractStatus = "Verified", ContractChecks = ["council.development.artifact.verify"]
                                });
                            result.DevelopmentArtifactVerified = true;
                            return runtimePolicy.GetString("CouncilFileWorkVerifiedNotice") + "\n" + JsonSerializer.Serialize(verified.Value);
                        }
                    }
                    return runtimePolicy.GetString("CouncilFileWorkIncompleteNotice") + "\n" + verified.Error;
                }
                // A real approved source archive remains useful even if the compiler is unavailable or reports defects.
                // Only the registered verifier may attest that the archive contains the approved bytes; SourceOnly never satisfies delivery.
                if ((progress.Status is CodeGenerationReviewStatuses.BuildFailed or CodeGenerationReviewStatuses.Generated or
                    CodeGenerationReviewStatuses.BuildDisabled or CodeGenerationReviewStatuses.BuildNotAttempted) &&
                    result.ChangeReview is { ApprovalConsumed: true, IsSourceDraftReview: true } sourceReview &&
                    sourceReview.CouncilRunId == result.RunId && !string.IsNullOrWhiteSpace(sourceReview.ZipFileName) &&
                    Path.GetFileName(sourceReview.ZipFileName) == sourceReview.ZipFileName)
                {
                    var sourceReceipt = await InvokeFileWorkFunctionAsync(result, request, round, verifyStep.Phase,
                        "council.development.artifact.verify", new { reviewId = sourceReview.Id }, cancellationToken).ConfigureAwait(false);
                    var sourceZip = Path.Combine(artifactService.ArtifactRoot, sourceReview.ZipFileName);
                    if (!sourceReceipt.Succeeded && sourceReceipt.Status == "SourceOnly" && File.Exists(sourceZip) &&
                        !result.Artifacts.Any(artifact => string.Equals(artifact.FilePath, sourceZip, StringComparison.OrdinalIgnoreCase)))
                    {
                        var sourceArtifact = new CouncilArtifact
                        {
                            Name = sourceReview.ZipFileName, Kind = "SourceZip", FilePath = sourceZip,
                            DownloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(sourceReview.ZipFileName)}",
                            QualityStatus = sourceReview.Status, ContractStatus = "SourceOnly",
                            ContractChecks = ["Approved source hashes verified"], MissingRequirements = ["Successful build verification"]
                        };
                        result.Artifacts.Add(sourceArtifact);
                        var notice = runtimePolicy.GetString("CouncilFileWorkSourceOnlyNotice") + " " + sourceArtifact.DownloadUrl;
                        result.Warnings.Add(notice);
                        request.ProgressMessage?.Invoke(notice);
                        request.StreamUpdate?.Invoke("\n\n" + System.Net.WebUtility.HtmlEncode(notice) + "\n\n");
                    }
                }
                if (progress.Status != CodeGenerationReviewStatuses.BuildFailed || generation == policy.MaximumRepairGenerations ||
                    string.IsNullOrWhiteSpace(buildEvidence))
                    return FormatIncompleteFileWork(result, progress);
                // Corrective work can name only actual manifest paths and actual compiler diagnostics.
                var repairer = curatorRoutes.OrderByDescending(model => modelRoutes.TryGetValue(model, out var road) ? road.EffectiveMaxContextTokens : maxContextTokens).First();
                var repairPrompt = policy.RepairPrompt + "\n" + JsonSerializer.Serialize(new { progress.CompletedFiles, BuildEvidence = buildEvidence });
                var repairReplies = await RunFileWorkPhaseAsync(result, request, curateStep, baseUri, routes, [repairer], round++,
                    $"{curateStep.Phase} · build repair {generation + 1}", _ => repairPrompt, modelRoutes, maxParallelModels,
                    keepAlive, ollamaNumGpu, maxContextTokens, modelTimeoutSeconds, null, cancellationToken).ConfigureAwait(false);
                var repairReply = repairReplies.LastOrDefault(step => string.IsNullOrWhiteSpace(step.Error));
                if (repairReply is null) break;
                Dictionary<string, string> repair;
                try
                {
                    repair = ReadFileWorkJson<Dictionary<string, string>>(ReadFileWorkAnswer(repairReply));
                }
                catch (JsonException exception)
                {
                    // An unusable repair answer ends the repair attempts like an empty one; the persisted draft,
                    // approvals and build evidence stay authoritative instead of aborting the whole file work.
                    logger.LogWarning(exception, "Council {RunId} build repair answer was not a path-to-work-order JSON object.", result.RunId);
                    var notice = runtimePolicy.GetString("CouncilFileWorkIncompleteNotice") + "\n" + exception.Message;
                    result.Warnings.Add(notice);
                    request.ProgressMessage?.Invoke(notice);
                    request.StreamUpdate?.Invoke("\n\n> " + System.Net.WebUtility.HtmlEncode(notice) + "\n\n");
                    break;
                }
                if (repair.Count == 0 || repair.Values.Any(string.IsNullOrWhiteSpace) ||
                    progress.CompletedFiles.Count + repair.Keys.Count(path => !progress.CompletedFiles.ContainsKey(path)) > Math.Max(1, policy.MaximumManifestFiles)) break;
                var reopened = await InvokeFileWorkFunctionAsync(result, request, round, generateStep.Phase, "codegen.review.create",
                    new { draftOperation = "repair", draftRepairReviewId = progress.ReviewId,
                        draftRepairReviewHash = progress.ReviewHash, draftManifest = repair }, cancellationToken).ConfigureAwait(false);
                if (!reopened.Succeeded) break;
                progress = await codeGenerationWorkflow.GetCouncilDraftProgressAsync(result.RunId, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("The corrective draft disappeared.");
            }
            return FormatIncompleteFileWork(result, progress);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Council {RunId} coordinated file work failed; persisted source remains available.", result.RunId);
            return runtimePolicy.GetString("CouncilFileWorkIncompleteNotice") + "\n" + exception.Message;
        }
    }

    /// <summary>Runs a bounded independent file-work phase through the normal host scheduler and live-lane renderer.</summary>
    /// <param name="result">Owning Council result.</param>
    /// <param name="request">Owning request and callbacks.</param>
    /// <param name="definition">Configured role and workflow step identity.</param>
    /// <param name="baseUri">Default provider address.</param>
    /// <param name="allRoutes">Selected Council members.</param>
    /// <param name="workers">Distinct assigned provider routes for this phase.</param>
    /// <param name="round">Logical round number.</param>
    /// <param name="phase">Displayed phase identity.</param>
    /// <param name="promptFactory">Exact bounded work packet for each route.</param>
    /// <param name="modelRoutes">Configured hardware roads.</param>
    /// <param name="maxParallelModels">Per-host concurrency limit.</param>
    /// <param name="keepAlive">Provider residency policy.</param>
    /// <param name="ollamaNumGpu">Fallback GPU policy.</param>
    /// <param name="maxContextTokens">Fallback context limit.</param>
    /// <param name="modelTimeoutSeconds">Configured model deadline.</param>
    /// <param name="onCompleted">Optional durable completion callback.</param>
    /// <param name="cancellationToken">Cancels assigned work.</param>
    /// <returns>Actual completed participant results for this phase.</returns>
    private async Task<IReadOnlyList<MultiModelCouncilStep>> RunFileWorkPhaseAsync(MultiModelCouncilResult result,
        MultiModelCouncilRequest request, CouncilWorkflowStepDefinition definition, string baseUri,
        IReadOnlyList<string> allRoutes, IReadOnlyList<string> workers, int round, string phase,
        Func<string, string> promptFactory, IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
        int maxParallelModels, string keepAlive, int? ollamaNumGpu, int maxContextTokens, int modelTimeoutSeconds,
        Func<MultiModelCouncilStep, Task>? onCompleted, CancellationToken cancellationToken)
    {
        try
        {
            var filePolicy = ResolveFileWorkPolicy(runtimePolicy.GetJson<CouncilFileWorkRuntimeParameters>("CouncilFileWorkRuntimeParametersJson"),
                request.CouncilTeamKey, out _);
            var workBudget = definition.Key == filePolicy.GenerationStepKey ? filePolicy.MaximumAuthorOutputTokens : filePolicy.MaximumPlanningOutputTokens;
            var fittedRoads = modelRoutes.ToDictionary(item => item.Key, item => item.Value with
            {
                EffectiveMaxOutputTokens = Math.Min(item.Value.EffectiveMaxOutputTokens, Math.Max(64, workBudget))
            }, StringComparer.OrdinalIgnoreCase);
            await RunPhaseAsync(result, baseUri, workers, round, phase, definition.Role, promptFactory, string.Empty,
                Math.Min(request.MaxOutputTokens, Math.Max(64, workBudget)), maxParallelModels, keepAlive, ollamaNumGpu, maxContextTokens, modelTimeoutSeconds,
                request.ProgressMessage, request.StreamUpdate, request.StepCompleted, fittedRoads, request.AllowParallelHardwareRoads,
                cancellationToken, allowDxFunctions: false, councilMembers: allRoutes, roleComplianceRetryCount: 0,
                finalAnswerRecoveryEnabled: false, exactAssignments: true, requestOutputCeiling: Math.Max(64, workBudget), onParticipantCompletedAsync: async step =>
                {
                    step.WorkflowStepKey = definition.Key;
                    step.WorkflowRevision = 1;
                    if (onCompleted is not null) await onCompleted(step).ConfigureAwait(false);
                }).ConfigureAwait(false);
            return result.Steps.Where(step => step.Round == round && step.Phase == phase && workers.Contains(step.ModelName, StringComparer.OrdinalIgnoreCase)).ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Council file work phase {Phase} failed.", phase); throw; }
    }

    /// <summary>Reverses the actual provider presentation encoding at the machine work boundary, without changing the displayed answer.</summary>
    /// <param name="step">Completed provider step with server-owned presentation provenance.</param>
    /// <returns>Plain source or JSON response; literal entities from unformatted providers remain unchanged.</returns>
    private string ReadFileWorkAnswer(MultiModelCouncilStep step)
    {
        try
        {
            return step.VisibleContentHtmlEncoded ? System.Net.WebUtility.HtmlDecode(step.VisibleContent) : step.VisibleContent;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the provider response for coordinated file work failed.");
            throw;
        }
    }

    /// <summary>Parses an exact JSON object from a model, optionally enclosed in one Markdown fence.</summary>
    /// <typeparam name="T">Expected serialized work contract.</typeparam>
    /// <param name="answer">Visible model response, never the reasoning stream.</param>
    /// <returns>Validated deserialized contract; callers validate its domain fields.</returns>
    private T ReadFileWorkJson<T>(string answer) where T : class
    {
        try
        {
            var value = answer.Trim();
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            if (value.StartsWith('{') && value.EndsWith('}'))
                return JsonSerializer.Deserialize<T>(value, options) ?? throw new JsonException("Missing structured work response.");
            var lines = value.Split('\n');
            var fences = Enumerable.Range(0, lines.Length).Where(index => lines[index].TrimStart().StartsWith("```", StringComparison.Ordinal)).ToArray();
            if (fences.Length != 2 || fences[1] <= fences[0] + 1 ||
                !string.Equals(lines[fences[1]].Trim(), "```", StringComparison.Ordinal) ||
                lines[fences[0]].Trim() is not ("```" or "```json" or "```JSON"))
                throw new JsonException("Expected one complete JSON object or one unambiguous JSON fence.");
            value = string.Join("\n", lines[(fences[0] + 1)..fences[1]]).Trim();
            return JsonSerializer.Deserialize<T>(value, options) ?? throw new JsonException("Missing structured work response.");
        }
        catch (Exception exception) { logger.LogDebug(exception, "Council file contract JSON was invalid."); throw; }
    }

    /// <summary>Invokes the registered gateway under the current Council identity and records the actual result.</summary>
    /// <param name="result">Owning Council state.</param>
    /// <param name="request">Original request and presentation callbacks.</param>
    /// <param name="round">Current logical round.</param>
    /// <param name="phase">Current logical phase.</param>
    /// <param name="functionName">Exact registered function identifier.</param>
    /// <param name="parameters">Server-constructed parameter contract.</param>
    /// <param name="cancellationToken">Cancels the invocation.</param>
    /// <returns>Actual gateway result, never model-provided confirmation.</returns>
    private async Task<DxAiFunctionInvocationResult> InvokeFileWorkFunctionAsync(MultiModelCouncilResult result,
        MultiModelCouncilRequest request, int round, string phase, string functionName, object parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = ambientContext.PushCouncil(result.RunId, round, phase, executionIdentity: new CouncilExecutionIdentity(result.ChatRunId ?? result.RunId, result.ProjectId, result.ProjectRevisionId));
            var startedAt = DateTime.UtcNow;
            var outcome = await fileWorkFunctions.InvokeAsync(functionName, new DxAiFunctionInvocationRequest
            {
                Parameters = JsonSerializer.SerializeToElement(parameters, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                AutomaticInvocation = true, ConversationId = result.ChatRunId ?? result.RunId,
                ProjectId = result.ProjectId, ProjectVersionId = result.ProjectRevisionId
            }, cancellationToken).ConfigureAwait(false);
            var receiptJson = JsonSerializer.Serialize(new
            {
                outcome.OperationId, outcome.HandlerDispatched, outcome.DescriptorReadOnly, outcome.SchemaHash,
                outcome.FunctionName, outcome.Status, outcome.Succeeded, outcome.Value, outcome.Error
            });
            var visible = functionName + " -> " + outcome.Status + "\n" + receiptJson;
            var step = new MultiModelCouncilStep { ModelName = "LocalGPT DXFunction gateway", Round = round, Phase = phase,
                Role = "Source workspace", CouncilMembers = result.ModelNames.ToList(), Content = visible, VisibleContent = visible,
                FileWorkReceiptJson = receiptJson,
                Error = outcome.Succeeded ? string.Empty : outcome.Error ?? outcome.Status,
                StartedAtUtc = startedAt, CompletedAtUtc = DateTime.UtcNow };
            MultiModelCouncilServiceAddOrderedStep(result, step, logger);
            request.StepCompleted?.Invoke(step);
            request.ProgressMessage?.Invoke(functionName + " -> " + outcome.Status);
            request.StreamUpdate?.Invoke("\n\n" + System.Net.WebUtility.HtmlEncode(functionName + " -> " + outcome.Status + " · " + outcome.OperationId.ToString("D")) + "\n\n");
            return outcome;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Council file-work function {Function} failed.", functionName); throw; }
    }

    /// <summary>Reports retained source and any independently hash-checked but uncompiled downloads without claiming successful delivery.</summary>
    /// <param name="result">Owning Council result.</param>
    /// <param name="progress">Latest persisted draft or review progress.</param>
    /// <returns>Incomplete delivery report with links to available source-only archives.</returns>
    private string FormatIncompleteFileWork(MultiModelCouncilResult result, object progress)
    {
        try
        {
            var downloads = result.Artifacts.Where(artifact => artifact.ContractStatus == "SourceOnly")
                .Select(artifact => $"{runtimePolicy.GetString("CouncilFileWorkSourceOnlyNotice")} [{artifact.Name}]({artifact.DownloadUrl})");
            return runtimePolicy.GetString("CouncilFileWorkIncompleteNotice") + "\n" + JsonSerializer.Serialize(progress) + "\n" + string.Join("\n", downloads);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not format incomplete Council file-work evidence for {RunId}.", result.RunId);
            throw;
        }
    }

}

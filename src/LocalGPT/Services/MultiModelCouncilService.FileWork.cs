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
    /// <param name="cancellationToken">Cancels the workflow without discarding committed receipts.</param>
    /// <returns>Concrete delivery or incomplete-work report, or null for the existing-repository route.</returns>
    private async Task<string?> RunCoordinatedFileWorkAsync(MultiModelCouncilResult result,
        MultiModelCouncilRequest request, OrganicCouncilTeamDefinition team,
        IReadOnlyList<CouncilWorkflowStepDefinition> steps, CouncilFileWorkRuntimeParameters policy,
        string baseUri, IReadOnlyList<string> participants, IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
        int maxParallelModels, string keepAlive, int? ollamaNumGpu, int maxContextTokens, int modelTimeoutSeconds,
        int firstRound, IReadOnlyDictionary<string, CouncilRoleRuntimeAssignment> roleAssignments, CancellationToken cancellationToken)
    {
        try
        {
            var planStep = steps.Single(item => item.Key == policy.PlanStepKey);
            var generateStep = steps.Single(item => item.Key == policy.GenerationStepKey);
            var verifyStep = steps.Single(item => item.Key == policy.VerificationStepKey);
            var curateStep = steps.SingleOrDefault(item => item.Key == policy.CurationStepKey) ?? verifyStep;
            var round = firstRound;
            // Do not bypass a user-configured required human or human-only role.
            if (steps.Any(step => roleAssignments.TryGetValue(step.Role, out var assigned) &&
                assigned.HumanParticipationMode is HumanParticipationMode.Required or HumanParticipationMode.HumanOnly))
                return null;
            var generationPolicy = councilAutomaticFunctionPolicy.Resolve(team, generateStep, false, request.RequestedOrganicCapabilities);
            var verificationPolicy = councilAutomaticFunctionPolicy.Resolve(team, verifyStep, false, request.RequestedOrganicCapabilities);
            if (!generationPolicy.Enabled || !verificationPolicy.Enabled ||
                (generationPolicy.IsStrict && generationPolicy.AutomaticFunctionAllowList is { } generatedAllowed &&
                    (!generatedAllowed.Contains("codegen.review.create") || !generatedAllowed.Contains("codegen.review.execute"))) ||
                (verificationPolicy.IsStrict && verificationPolicy.AutomaticFunctionAllowList is { } verifiedAllowed &&
                    !verifiedAllowed.Contains("council.development.artifact.verify")))
                return null; // Preserve the existing configured permission route.

            var routes = OrderParticipantsByObservedHealth(result, participants).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var architectRoutes = routes.Where(model => !roleAssignments.TryGetValue(planStep.Role, out var assigned) ||
                assigned.AiParticipants.Contains(model, StringComparer.OrdinalIgnoreCase)).ToList();
            var authorRoutes = routes.Where(model => !roleAssignments.TryGetValue(generateStep.Role, out var assigned) ||
                assigned.AiParticipants.Contains(model, StringComparer.OrdinalIgnoreCase)).ToList();
            var curatorRoutes = routes.Where(model => !roleAssignments.TryGetValue(curateStep.Role, out var assigned) ||
                assigned.AiParticipants.Contains(model, StringComparer.OrdinalIgnoreCase)).ToList();
            if (architectRoutes.Count == 0 || authorRoutes.Count == 0 || curatorRoutes.Count == 0)
                return runtimePolicy.GetString("CouncilFileWorkNoRoutesNotice");
            // The prior transcript remains in the Council archive. Only bounded visible evidence goes to the architect.
            var evidenceBudget = Math.Max(0, policy.MaximumPlanEvidenceCharacters);
            var evidence = new List<object>();
            foreach (var step in result.Steps.Where(item => string.IsNullOrWhiteSpace(item.Error) &&
                !string.IsNullOrWhiteSpace(item.VisibleContent)).Reverse())
            {
                if (evidenceBudget <= 0) break;
                var length = Math.Min(evidenceBudget, step.VisibleContent.Length);
                evidence.Add(new { step.Role, step.ModelName, Text = step.VisibleContent[..length] });
                evidenceBudget -= length;
            }
            var progress = await codeGenerationWorkflow.GetCouncilDraftProgressAsync(result.RunId, cancellationToken).ConfigureAwait(false);
            if (progress is null)
            {
                var planDiagnostic = string.Empty;
                var beginAttempted = false;
                var ordinaryRoute = false;
                var alternativeRouteObserved = false;
                for (var attempt = 0; attempt < Math.Max(1, policy.MaximumPlanAttempts); attempt++)
                {
                    var architect = architectRoutes[attempt % architectRoutes.Count];
                    var prompt = policy.PlanPrompt + "\n" + JsonSerializer.Serialize(new { Goal = request.Prompt, Evidence = evidence, PreviousValidationError = planDiagnostic,
                        GuestLanguages = fileWorkSandboxes.Languages.Where(language => fileWorkSandboxes.RuntimeReadiness.Any(runtime => runtime.Runtime == language.Runtime && runtime.Provisioned)).Select(language => language.Key).ToList() });
                    var replies = await RunFileWorkPhaseAsync(result, request, planStep, baseUri, routes, [architect],
                        round++, planStep.Phase, _ => prompt, modelRoutes, maxParallelModels, keepAlive, ollamaNumGpu,
                        maxContextTokens, modelTimeoutSeconds, null, cancellationToken).ConfigureAwait(false);
                    var reply = replies.LastOrDefault(item => string.IsNullOrWhiteSpace(item.Error));
                    if (reply is null) continue;
                    try
                    {
                        var explicitPlan = ReadFileWorkJson<Dictionary<string, JsonElement>>(reply.VisibleContent);
                        if (!explicitPlan.TryGetValue("draftWorkspaceMode", out var workspaceMode) || workspaceMode.ValueKind != JsonValueKind.String)
                            throw new ArgumentException("Choose one explicit workspace mode.");
                        ordinaryRoute = workspaceMode.GetString() == "ReviewedSourceDraft";
                        alternativeRouteObserved |= !ordinaryRoute;
                        var plan = ReadSourceManifest(reply.VisibleContent);
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
                        plan.Goal = request.Prompt;
                        plan.CouncilRunId = result.RunId;
                        plan.DraftOperation = "begin";
                        // Never accept IDs, raw files or approval state from the planning model.
                        if (plan.Files is not { Count: 0 } || plan.CodeDomTypes is not { Count: 0 } || plan.Outputs is not { Count: > 0 })
                            throw new ArgumentException("The plan requires a file manifest and output targets, not candidate source.");
                        if (plan.DraftManifest is not { Count: > 0 } || plan.DraftManifest.Count > Math.Max(1, policy.MaximumManifestFiles) ||
                            plan.DraftManifest.Values.Any(string.IsNullOrWhiteSpace))
                            throw new ArgumentException("Provide distinct relative paths with nonempty string work orders in draftManifest.");
                        beginAttempted = true;
                        var created = await InvokeFileWorkFunctionAsync(result, request, round - 1, planStep.Phase,
                            "codegen.review.create", new { draftOperation = "begin", goal = plan.Goal,
                                title = plan.Title, draftWorkspaceMode = plan.DraftWorkspaceMode,
                                draftGuestLanguageKey = plan.DraftGuestLanguageKey, draftManifest = plan.DraftManifest,
                                draftSharedContract = plan.DraftSharedContract, draftDependencies = plan.DraftDependencies ?? new Dictionary<string, List<string>>(),
                                outputs = plan.Outputs }, cancellationToken).ConfigureAwait(false);
                        if (!created.Succeeded)
                        {
                            planDiagnostic = created.Error ?? created.Status;
                            // A rejected or interrupted gateway may already have persisted a draft.
                            progress = await codeGenerationWorkflow.GetCouncilDraftProgressAsync(result.RunId, cancellationToken).ConfigureAwait(false);
                            if (progress is not null) break;
                            continue;
                        }
                        progress = await codeGenerationWorkflow.GetCouncilDraftProgressAsync(result.RunId, cancellationToken).ConfigureAwait(false);
                        if (progress is not null) break;
                    }
                    catch (Exception exception) when (exception is JsonException or ArgumentException)
                    {
                        planDiagnostic = exception is JsonException jsonError
                            ? $"Invalid JSON field: {jsonError.Path ?? "root"}." : exception.Message;
                        logger.LogInformation("Council {RunId} architect returned an invalid source manifest; diagnostic category {Category}.", result.RunId, exception.GetType().Name);
                        request.ProgressMessage?.Invoke(runtimePolicy.GetString("CouncilFileWorkManifestRetryNotice") + " " + planDiagnostic);
                    }
                }
                // The old reviewed generation path remains available only before any draft
                // creation was attempted, with an explicit ordinary workspace decision and no prior tool effects.
                if (progress is null && !beginAttempted && ordinaryRoute && !alternativeRouteObserved && result.Steps.All(item =>
                        !item.Content.Contains("Function call ·", StringComparison.OrdinalIgnoreCase) &&
                        !item.Content.Contains("Function result ·", StringComparison.OrdinalIgnoreCase) &&
                        !item.Content.Contains("<localgpt-dx-call>", StringComparison.OrdinalIgnoreCase) &&
                        !(item.Error?.Contains("priorToolActivity=true", StringComparison.OrdinalIgnoreCase) ?? false)) &&
                    (await deferredDxAiInvocations.GetForRunAsync(result.RunId, cancellationToken).ConfigureAwait(false)).Count == 0)
                {
                    if (policy.AllowLegacyReviewedFallback)
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
            // A failed review transport is not a negative inspection of the source bytes.
            var attemptedInspections = new HashSet<(Guid DraftId, string Path, string Hash, string DependencyRevision, string Reviewer)>();
            for (var generation = 0; generation <= Math.Max(0, policy.MaximumRepairGenerations); generation++)
            {
                if (progress.IsDraft)
                {
                    var maximumBatches = Math.Max(1, policy.MaximumManifestFiles) * Math.Max(1, policy.MaximumFileAttempts);
                    for (var batch = 0; batch < maximumBatches; batch++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var packets = await codeGenerationWorkflow.ReserveDraftFilesAsync(result.RunId, authorRoutes,
                            Math.Max(1, policy.MaximumFileAttempts), cancellationToken).ConfigureAwait(false);
                        if (packets.Count > 0)
                        {
                            var byModel = packets.ToDictionary(packet => packet.Assignment.ModelName, StringComparer.OrdinalIgnoreCase);
                            var phase = $"{generateStep.Phase} · X-Round {generation + 1}.{batch + 1}";
                            await RunFileWorkPhaseAsync(result, request, generateStep, baseUri, routes, byModel.Keys.ToList(),
                                round++, phase, model => (progress.WorkspaceMode == "HyperlightSourceDraft" ? policy.GuestAuthorPrompt : policy.AuthorPrompt)
                                    + "\n" + JsonSerializer.Serialize(new { progress.WorkspaceMode, progress.GuestLanguageKey, Packet = byModel[model] }), modelRoutes, maxParallelModels, keepAlive,
                                ollamaNumGpu, maxContextTokens, modelTimeoutSeconds, async step =>
                                {
                                    if (progress.WorkspaceMode == "HyperlightSourceDraft") return; // Import only real, approved guest output after the batch.
                                    var packet = byModel[step.ModelName];
                                    var receipt = await codeGenerationWorkflow.CompleteDraftFileAsync(result.RunId, packet.Assignment,
                                        step.ModelName, step.VisibleContent, step.Error, cancellationToken).ConfigureAwait(false);
                                    step.VisibleContent += "\n\n" + JsonSerializer.Serialize(new { receipt.RelativePath, receipt.Id, receipt.Sha256, receipt.Diagnostic });
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
                                    round++, $"{curateStep.Phase} · X-Round {generation + 1}.{batch + 1}", model => policy.CuratorPrompt + "\n" + JsonSerializer.Serialize(reviewPackets[model]),
                                    modelRoutes, maxParallelModels, keepAlive, ollamaNumGpu, maxContextTokens, modelTimeoutSeconds,
                                    async step =>
                                    {
                                        var inspection = reviewPackets[step.ModelName];
                                        try
                                        {
                                            if (!string.IsNullOrWhiteSpace(step.Error)) return;
                                            var fields = ReadFileWorkJson<Dictionary<string, JsonElement>>(step.VisibleContent);
                                            if (!fields.TryGetValue("accepted", out var accepted) || accepted.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                                                throw new ArgumentException("The inspection requires an explicit accepted boolean.");
                                            var decision = ReadFileWorkJson<CodeGenerationDraftInspectionDecision>(step.VisibleContent);
                                            if (decision.Sha256 != inspection.Sha256 || (decision.Accepted == false && string.IsNullOrWhiteSpace(decision.Diagnostic)))
                                                throw new ArgumentException("The inspection did not bind a valid decision to the exact candidate.");
                                            decision.Reviewer = step.ModelName;
                                            await codeGenerationWorkflow.RecordDraftInspectionAsync(result.RunId, reviewPackets[step.ModelName],
                                                decision, cancellationToken).ConfigureAwait(false);
                                        }
                                        catch (Exception exception) when (exception is ArgumentException or JsonException or InvalidOperationException)
                                        {
                                            logger.LogWarning("Council inspection of {Path} returned no valid decision ({Category}); the source candidate remains unchanged.", inspection.Path, exception.GetType().Name);
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
                        return runtimePolicy.GetString("CouncilFileWorkIncompleteNotice") + "\n" + JsonSerializer.Serialize(progress);
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
                            if (!result.Artifacts.Any(artifact => string.Equals(artifact.FilePath, zipPath, StringComparison.OrdinalIgnoreCase)))
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
                if (progress.Status != CodeGenerationReviewStatuses.BuildFailed || generation == policy.MaximumRepairGenerations ||
                    string.IsNullOrWhiteSpace(buildEvidence))
                    return runtimePolicy.GetString("CouncilFileWorkIncompleteNotice") + "\n" + JsonSerializer.Serialize(progress);
                // Corrective work can name only actual manifest paths and actual compiler diagnostics.
                var repairer = curatorRoutes.OrderByDescending(model => modelRoutes.TryGetValue(model, out var road) ? road.EffectiveMaxContextTokens : maxContextTokens).First();
                var repairPrompt = policy.RepairPrompt + "\n" + JsonSerializer.Serialize(new { progress.CompletedFiles, BuildEvidence = buildEvidence });
                var repairReplies = await RunFileWorkPhaseAsync(result, request, curateStep, baseUri, routes, [repairer], round++,
                    $"{curateStep.Phase} · build repair {generation + 1}", _ => repairPrompt, modelRoutes, maxParallelModels,
                    keepAlive, ollamaNumGpu, maxContextTokens, modelTimeoutSeconds, null, cancellationToken).ConfigureAwait(false);
                var repairReply = repairReplies.LastOrDefault(step => string.IsNullOrWhiteSpace(step.Error));
                if (repairReply is null) break;
                var repair = ReadFileWorkJson<Dictionary<string, string>>(repairReply.VisibleContent);
                if (repair.Count == 0 || repair.Values.Any(string.IsNullOrWhiteSpace) ||
                    progress.CompletedFiles.Count + repair.Keys.Count(path => !progress.CompletedFiles.ContainsKey(path)) > Math.Max(1, policy.MaximumManifestFiles)) break;
                var reopened = await InvokeFileWorkFunctionAsync(result, request, round, generateStep.Phase, "codegen.review.create",
                    new { draftOperation = "repair", draftRepairReviewId = progress.ReviewId,
                        draftRepairReviewHash = progress.ReviewHash, draftManifest = repair }, cancellationToken).ConfigureAwait(false);
                if (!reopened.Succeeded) break;
                progress = await codeGenerationWorkflow.GetCouncilDraftProgressAsync(result.RunId, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("The corrective draft disappeared.");
            }
            return runtimePolicy.GetString("CouncilFileWorkIncompleteNotice") + "\n" + JsonSerializer.Serialize(progress);
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
            var filePolicy = runtimePolicy.GetJson<CouncilFileWorkRuntimeParameters>("CouncilFileWorkRuntimeParametersJson");
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

    /// <summary>Parses an exact JSON object from a model, optionally enclosed in one Markdown fence.</summary>
    /// <typeparam name="T">Expected serialized work contract.</typeparam>
    /// <param name="answer">Visible model response, never the reasoning stream.</param>
    /// <returns>Validated deserialized contract; callers validate its domain fields.</returns>
    private T ReadFileWorkJson<T>(string answer) where T : class
    {
        try
        {
            var value = answer.Trim();
            if (value.StartsWith("```", StringComparison.Ordinal) && value.EndsWith("```", StringComparison.Ordinal))
            {
                var begin = value.IndexOf('\n');
                if (begin < 0) throw new JsonException("Incomplete JSON fence.");
                value = value[(begin + 1)..^3].Trim();
            }
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            try
            {
                return JsonSerializer.Deserialize<T>(value, options)
                    ?? throw new JsonException("Missing structured work response.");
            }
            catch (JsonException) when (value.Contains("&quot;", StringComparison.Ordinal))
            {
                // Some display adapters encode JSON delimiters. Decode only if raw JSON fails,
                // never mutate already valid JSON or the bytes of an authored source file.
                return JsonSerializer.Deserialize<T>(System.Net.WebUtility.HtmlDecode(value), options)
                    ?? throw new JsonException("Missing structured work response.");
            }
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
            using var scope = ambientContext.PushCouncil(result.RunId, round, phase);
            var outcome = await fileWorkFunctions.InvokeAsync(functionName, new DxAiFunctionInvocationRequest
            {
                Parameters = JsonSerializer.SerializeToElement(parameters, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                AutomaticInvocation = true, ConversationId = result.ChatRunId ?? result.RunId,
                ProjectId = result.ProjectId, ProjectVersionId = result.ProjectRevisionId
            }, cancellationToken).ConfigureAwait(false);
            var visible = functionName + " -> " + outcome.Status + "\n" + JsonSerializer.Serialize(outcome.Value);
            var step = new MultiModelCouncilStep { ModelName = "LocalGPT DXFunction gateway", Round = round, Phase = phase,
                Role = "Source workspace", CouncilMembers = result.ModelNames.ToList(), Content = visible, VisibleContent = visible,
                Error = outcome.Succeeded ? string.Empty : outcome.Error ?? outcome.Status,
                StartedAtUtc = DateTime.UtcNow, CompletedAtUtc = DateTime.UtcNow };
            MultiModelCouncilServiceAddOrderedStep(result, step, logger);
            request.StepCompleted?.Invoke(step);
            request.ProgressMessage?.Invoke(functionName + " -> " + outcome.Status);
            request.StreamUpdate?.Invoke("\n\n" + functionName + " -> " + outcome.Status + "\n\n");
            return outcome;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Council file-work function {Function} failed.", functionName); throw; }
    }
}

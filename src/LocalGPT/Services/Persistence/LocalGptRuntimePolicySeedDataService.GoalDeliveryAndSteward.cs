using LocalGPT.BusinessObjects;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Runtime values introduced with LocalGPT 7.0.4: Ollama request sizing, compact native tools, final-answer recovery,
/// goal delivery for approval-gated 1-Wire capabilities, goal team selection and the configuration steward.
/// Missing rows are added on startup; existing user-edited rows are never overwritten.
/// </summary>
public sealed partial class LocalGptRuntimePolicySeedDataService
{
    /// <summary>Creates the 7.0.4 runtime value seeds.</summary>
    /// <returns>Seed values.</returns>
    private IReadOnlyList<LocalGptRuntimeSystemVariableSeed> CreateGoalDeliveryAndStewardValues()
    {
        try
        {
            return
            [
                new("OllamaRequestBudgetPolicyJson", """
{
  "charactersPerToken": 3.2,
  "thinkingReserveTokens": 2048,
  "contextSafetyMarginTokens": 512,
  "contextGrowthStepTokens": 1024,
  "useEstimatedContextFitting": false,
  "maximumFittedContextTokens": 32768,
  "assumedProviderDefaultContextTokens": 4096,
  "maximumOutputTokens": 262144,
  "minimumFittedOutputTokens": 1024
}
""", "System.String"),
                new("CouncilCommonRolesJson", "[]", "System.String"),
                new("CouncilUnlistedStepNativeToolMode", "CompactWithBroker", "System.String"),
                new("CouncilCapabilityNativeFunctionsJson", """
{
  "publisher.text.insert.propose": ["publisher.text.proposal.request"],
  "publisher.text.edit.request": ["publisher.text.feedback.request"],
  "publisher.spreadsheet.inspect": ["publisher.spreadsheet.inspect"],
  "publisher.document.create": ["publisher.document.create.request"],
  "publisher.sessions.list": ["publisher.sessions.list"],
  "publisher.screen.capture": ["publisher.screen.capture.request"],
  "publisher.screen.record": ["publisher.screen.record.request"],
  "publisher.website.content.request": ["publisher.website.content.request"],
  "publisher.embedded.wiring.edit.request": ["publisher.embedded.wiring.edit.request"]
}
""", "System.String"),
                new("CouncilChatHistoryBudgetCharacters", "48000", "System.Int32"),
                new("CouncilChatAssistantHistoryBudgetCharacters", "6000", "System.Int32"),
                new("GoalExecutionResourceMode", "Automatic", "System.String"),
                new("GoalAutoResourceProbeMinimumHostGiB", "32", "System.Int32"),
                new("GoalAutoResourceProbeMinimumGpuGiB", "16", "System.Int32"),
                new("GoalStewardAiEnabled", "true", "System.Boolean"),
                new("GoalStewardAiTimeoutSeconds", "25", "System.Int32"),
                new("GoalStewardAiSystemPrompt", """
You are the read-only Configuration Steward AI subagent working WITH GoalExecutionService.
The deterministic hardware survey and the registered model route are authoritative.
Never invent device memory, benchmark results, files, completed tool calls or permissions.
The previous request failed before inference due to context sizing. Advise how to DELIVER
its original goal using smaller independently executable work packets and relevant
registered knowledge/source tools. Do not propose an unsupported larger context.
Return exactly one JSON object: {"strategy":"decompose","reason":"one concise actionable step"}.
You have no permission to change settings or bypass user approvals.
""", "System.String"),
                new("GoalContextRecoveryPlannerInstruction", """
You are the GoalExecutionService planning delegate, not the final-answer author.
The prior Council step could not fit the selected model context; this is an execution
obstacle, not a completed or cancelled user goal. Use the original goal and the
previousFailure evidence included in this planner input. Produce valid schema-conforming
steps using real team keys from the supplied catalog. When team selection is permitted,
prefer an available knowledge-assisted subagent team that decomposes evidence retrieval,
code generation and verification into separately executable work packets. Otherwise
retain the requested team and split its objective into smaller independent steps.
Respect explicitly selected provider-qualified model routes, team preservation and all
human approvals; do not invent a new model, team, tool, generated file, build or ZIP.
Each proposed step must reduce the previous prompt/tool scope and name the next concrete
action; do not propose repeating the failed Council transcript or claiming success.
""", "System.String"),
                new("GoalContextRecoverySubstepInstruction", """
GoalExecutionService is continuing the original goal after a previous Council context
preflight failure. Complete ONLY the assigned current step below; use the configured
team's existing approved DX functions and concise relevant knowledge, and hand off
verified evidence to subsequent goal steps. Do not paste failed Council transcripts.
The original user deliverable and approvals remain mandatory. A build, test, codegen
review, execution, receipt, or downloadable ZIP may be reported successful ONLY when
its actual registered tool result proves success. If approval is pending, report that
state and stop consequential actions until the user grants it.
""", "System.String"),
                new("CouncilFinalRecoveryTaskExcerptCharacters", "6000", "System.Int32"),
                new("GoalDeliveryObjectiveTemplate", "Deliver the finished result of the original goal now: invoke {{Functions}} exactly as the original request specifies (target, format and the complete final text). Reuse the completed step outputs below; do not redo earlier work and do not answer with a description instead of the delivery.", "System.String"),
                new("GoalCatalogFallbackMinimumTeamScore", "6", "System.Int32"),
                new("GoalFallbackMinimumModelBillionParameters", "0", "System.Int32"),
                new("ConfigurationStewardPolicyJson", """
{
  "minimumRecommendedCouncilLoadPercent": 60,
  "recommendedCouncilLoadPercent": 100,
  "alternativeCouncilLoadPercent": 60,
  "referenceAutoRoadMinimumOutputTokens": 256,
  "referenceAutoRoadMaximumOutputTokens": 4096,
  "referenceAutoRoadMinimumContextTokens": 2048,
  "referenceAutoRoadMaximumContextTokens": 32768,
  "maximumNativeToolsWithoutPreference": 64,
  "maximumFindings": 25,
  "approvalRiskLevel": "High"
}
""", "System.String")
            ];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the 7.0.4 runtime value seeds failed.");
            throw;
        }
    }

    /// <summary>Creates the 7.0.4 runtime collection seeds.</summary>
    /// <returns>Seed collections.</returns>
    private IReadOnlyList<LocalGptRuntimeCollectionSeed> CreateGoalDeliveryAndStewardCollections()
    {
        try
        {
            return
            [
                new("CouncilBaseNativeFunctions", []),
                new("GoalApprovalGatedCapabilityPrefixes", ["publisher."]),
                new("GoalAutoRoutingExcludedTeamKeys", ["configuration-steward"])
            ];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the 7.0.4 runtime collection seeds failed.");
            throw;
        }
    }
}

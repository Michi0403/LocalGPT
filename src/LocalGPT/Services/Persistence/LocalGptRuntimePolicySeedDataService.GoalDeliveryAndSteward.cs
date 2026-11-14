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
  "maximumFittedContextTokens": 32768,
  "assumedProviderDefaultContextTokens": 4096,
  "maximumOutputTokens": 262144,
  "minimumFittedOutputTokens": 1024
}
""", "System.String"),
                new("CouncilUnlistedStepNativeToolMode", "CompactWithBroker", "System.String"),
                new("CouncilCapabilityNativeFunctionsJson", """
{
  "publisher.text.insert.propose": ["publisher.text.proposal.request"],
  "publisher.text.edit.request": ["publisher.text.feedback.request"],
  "publisher.spreadsheet.inspect": ["publisher.spreadsheet.inspect"],
  "publisher.screen.capture": ["publisher.screen.capture.request"],
  "publisher.screen.record": ["publisher.screen.record.request"],
  "publisher.website.content.request": ["publisher.website.content.request"],
  "publisher.embedded.wiring.edit.request": ["publisher.embedded.wiring.edit.request"]
}
""", "System.String"),
                new("CouncilFinalRecoveryTaskExcerptCharacters", "6000", "System.Int32"),
                new("GoalDeliveryObjectiveTemplate", "Deliver the finished result of the original goal now: invoke {{Functions}} exactly as the original request specifies (target, format and the complete final text). Reuse the completed step outputs below; do not redo earlier work and do not answer with a description instead of the delivery.", "System.String"),
                new("GoalCatalogFallbackMinimumTeamScore", "6", "System.Int32"),
                new("GoalFallbackMinimumModelBillionParameters", "7", "System.Int32"),
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

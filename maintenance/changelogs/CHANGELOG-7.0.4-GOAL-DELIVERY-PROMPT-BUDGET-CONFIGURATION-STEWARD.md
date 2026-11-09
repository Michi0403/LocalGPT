# LocalGPT 7.0.4: goal delivery, prompt budget and Configuration Steward

7.0.4 is 7.0.3 plus fixes for the failures in the 9 November 2026 logs (19:43–19:54 runs) and a new Configuration Steward. It pairs with PublisherStudio 4.7.5, and older PublisherStudio versions still work.

**Not compiled.** No .NET SDK is available here. Checks performed are listed under Verification.

## What the logs showed

- Every Council step ran with 1408 output and 11264 context tokens: default Council power 30% on the automatic road.
- 223 native tool schemas were attached because the steps had no tool preference. The prompt outgrew the context, Ollama cut the oldest content, and the tool list ended at `embedded.wiring…`. `publisher.text.proposal.request` was no longer visible.
- gpt-oss thinking used the output budget. Stories ended mid-sentence ("Echo‑Eclipse had") or were empty.
- The final-answer recovery only said "your previous response…". The model answered a different question with the generic "council is ready" table.
- The story goal stopped after its first step. Two rules caused this:
  - every step of a goal with a required capability ended as *NeedsVerification* and broke the loop;
  - the Keyaccount Manager required a local file artifact for `publisher.*` work and demanded human review.
- The help question fell back to keyword routing and picked `ascii-doom-council-adventure`. With one model that team cannot fill its distinct-model roles (`InvalidOperationException`).

## Fixed

### Ollama request budget

`OllamaThinkingChatClient.CreateRequestAsync` measures messages and native tool schemas and calls the new `CouncilRuntimeService.OllamaThinkingChatClientResolveRequestBudget`:

- `num_ctx` is raised (never lowered) when prompt + output + margin would not fit, up to `maximumFittedContextTokens`. It is logged at Information with the numbers, or at Warning when even the maximum is too small.
- With explicit thinking, `num_predict` = visible answer budget + `thinkingReserveTokens`.
- The policy is in the runtime value `OllamaRequestBudgetPolicyJson`.

### Compact native tools

`CouncilAutomaticFunctionPolicyService.Resolve` takes the run's requested capabilities.

- Steps without a saved tool preference attach a compact set (`CouncilUnlistedStepNativeToolMode` = `CompactWithBroker`):
  - the base collection `CouncilBaseNativeFunctions`;
  - the functions mapped from requested capabilities (`CouncilCapabilityNativeFunctionsJson`);
  - attachments, AI help and the tool broker.
- Steps with a preference also get the capability functions. Strict allow-lists are unchanged.
- `AllPolicyApproved` restores the old behavior.

### Final-answer recovery keeps the task

The recovery turn restates the head and tail of the original request (`CouncilFinalRecoveryTaskExcerptCharacters`) and continues a partial visible answer instead of restarting.

### Goal delivery for approval-gated capabilities

These are goals whose required capabilities all match `GoalApprovalGatedCapabilityPrefixes` (default `publisher.`):

- Intermediate *NeedsVerification* steps continue to the next planned step. Nothing is replayed, and the goal still ends as *NeedsVerification*.
- New evidence kind `ApprovalGatedRequest` (from `IDeferredDxAiInvocationService.GetForRunAsync`) records the delivery requests of each step.
- `ApplyApprovalGatedDeliveryRule` runs after every Keyaccount review:
  - With no recorded delivery, the next objective is the delivery (`GoalDeliveryObjectiveTemplate`) within the continuation limit. The step key is `delivery-N`.
  - With a recorded delivery, the pending approval replaces a second human-review stop.
- The local-artifact rule no longer applies to these goals.
- `MultiModelCouncilResult.DeliverableText` carries the last content step's output without the Keyaccount report.

### Goal team selection

- New `ICouncilTeamStaffingEstimator`. It mirrors the hard role-assignment rule: distinct groups without fallback, paired partners.
- Without a semantic plan, the requested team is kept. Another team is chosen only when required capabilities need it or its keyword score reaches `GoalCatalogFallbackMinimumTeamScore` (6).
- Unstaffable teams are never auto-selected. Semantic steps for unstaffable teams run with the selected team.
- An explicitly selected unstaffable team is blocked with `SelectedCouncilTeamNeedsMoreDistinctModels`.
- Auto-discovered models now cover the team's distinct-model requirement.

## Added: Configuration Steward

You asked for this: configuration and data problems should be investigated by a role with functions, the data must not be trusted blindly, and every change is decided by you in the common approval system with options.

- **Checks** (`IConfigurationHealthCheck`, read-only, one class per file in `Services/ConfigurationSteward`):

  | Check | What it reports | Repair options |
  |---|---|---|
  | `council-load-default` | Default Council power below `minimumRecommendedCouncilLoadPercent` (60), with the automatic-road token budget at that value and a note when it equals the legacy 30% default | set 100%, set 60%, or keep |
  | `runtime-value-integrity` | SystemVariables rows that do not parse as their declared type: numbers, booleans, GUIDs, JSON documents and collections. JSON and secret-like values are not shown. | reset to the shipped value, or keep |
  | `team-staffing` | Teams that need more distinct models than are installed | report only; the steward does not edit teams |
  | `native-tool-mode` | Unlisted steps attach all tools while more than 64 are registered | compact, or keep |

- **`localgpt.configuration.diagnose`** (read-only, automatic) returns the findings. Each has evidence, a verification note on what the data cannot prove, and a fingerprint.
- **`localgpt.configuration.repair.request`** is a self-gated coordination function, like `human.collaboration.request`:
  - It recomputes the finding from live data and queues a Human Collaboration approval: operation `localgpt.configuration.repair`, risk High, exact fingerprint.
  - The finding's options appear as selectable answers. The recommended option is preselected, and no free text is accepted.
  - The same request is queued as a deferred invocation. Approving an option in Approvals & team runs it again, and the second call finds the approved option.
  - `ConfigurationRepairApplier` applies the option in one transaction. Each change compares the stored value with the observed one; one difference rolls everything back (`StateChanged`). After a commit, the runtime policy is reloaded.
  - Declining or choosing "keep" changes nothing. Changed data produces a new fingerprint and a new decision.
- **Configuration Steward team** (seed `configuration-steward`): investigator, data skeptic and repair coordinator. All roles accept every selected model.
- Thresholds are in `ConfigurationStewardPolicyJson`.

## New runtime values

All are added to existing databases on startup; existing rows are not overwritten.

- `OllamaRequestBudgetPolicyJson`
- `CouncilUnlistedStepNativeToolMode`
- `CouncilCapabilityNativeFunctionsJson`
- `CouncilFinalRecoveryTaskExcerptCharacters`
- `GoalDeliveryObjectiveTemplate`
- `GoalCatalogFallbackMinimumTeamScore`
- `ConfigurationStewardPolicyJson`
- collections `CouncilBaseNativeFunctions` (empty) and `GoalApprovalGatedCapabilityPrefixes` (`publisher.`)

## Changed files

- Budget:
  - `OllamaThinkingChatClient.Conversation.cs`
  - `CouncilRuntimeService.OllamaRequestBudget.cs` (new)
  - `OllamaRequestBudget.cs`, `OllamaRequestBudgetPolicy.cs` (new)
- Tools: `CouncilAutomaticFunctionPolicyService.cs`, `ICouncilAutomaticFunctionPolicyService.cs`, `MultiModelCouncilService.WorkflowDefinitionExecution.cs`
- Recovery: `MultiModelCouncilService.RecoveryAndPersistence.cs`, `MultiModelCouncilService.ParticipantExecution.cs`, `MultiModelCouncilService.RecoveryTaskExcerpt.cs` (new)
- Goal:
  - `GoalExecutionService.cs`, `.DurableExecution.cs`, `.KeyaccountManager.cs`
  - `.Delivery.cs` and `.TeamSelection.cs` (new)
  - `MultiModelCouncilModels.cs` (`DeliverableText`)
- Deferred evidence: `DeferredDxAiInvocationService.cs` (partial), `DeferredDxAiInvocationService.RunEvidence.cs` (new), `IDeferredDxAiInvocationService.cs`, `DeferredDxAiInvocationSummary.cs` (new)
- Staffing: `ICouncilTeamStaffingEstimator.cs`, `Services/Council/CouncilTeamStaffingEstimator.cs`, `CouncilTeamStaffingEstimate.cs` (all new)
- Steward:
  - `Services/ConfigurationSteward/*` (8 new files)
  - `IConfigurationHealthCheck.cs`, `IConfigurationRepairApplier.cs`, `IConfigurationStewardService.cs`
  - `Configuration*.cs` business objects (8 new files)
  - `OrganicCouncilBlueprintSeedDataService.ConfigurationStewardTemplate.cs`
- Seeds: `LocalGptRuntimePolicySeedDataService.cs` (partial), `LocalGptRuntimePolicySeedDataService.GoalDeliveryAndSteward.cs` (new)
- `Program.ServiceRegistration.cs`, `docs/guide/chat-and-council.md`, `README.md`, `LocalGPT.csproj` (7.0.4)

## Verification

- Brace and parenthesis balance of every changed C# file.
- XML documentation heuristic: no new missing `<param>` tags; the remaining hits are pre-existing or false positives on property initializers.
- No `static` members and no new `Regex` construction in new code.
- No file over 600 lines. `GoalExecutionService.DurableExecution.cs` has 584.
- Tokens required by `Assert-AiHelpEscalator.ps1`, `Assert-CouncilXRoundWiring.ps1` and `Assert-HumanCollaboration.ps1` are still present.

## Runtime checks

1. **Story via PublisherStudio AI Assist.** The log shows `Raised the Ollama context` (or no raise when it fits) and about 10–15 attached tools instead of 223, including `publisher.text.proposal.request`.
   - The goal runs every planned step.
   - Either a delivery request appears in Approvals & team, or a `delivery-1` step runs.
   - PublisherStudio inserts the text once.
2. **Help question in Chat with one model.** No `ascii-doom-council-adventure` selection; the requested or general team answers.
3. **Configuration Steward.** Ask "diagnose my configuration" with the Configuration Steward team, or call `localgpt.configuration.diagnose`. With `DefaultCouncilResourceLoadPercent` = 30:
   - The finding shows three options.
   - Approve "Set default Council power to 100%" and check that the row is 100.
   - Change the row by hand before approving and check that you get `StateChanged` and a new request.

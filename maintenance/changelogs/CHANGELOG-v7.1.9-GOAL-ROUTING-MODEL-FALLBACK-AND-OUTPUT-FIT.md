# LocalGPT 7.1.9 - Goal routing, model fallback, output fitting, compact session tools, honest recovery

**Observed (live test log):** a PublisherStudio Excel/Word goal was routed from "general" to configuration-steward by keyword score; the fallback picked codegemma:2b although much larger models were installed; the plain chat session attached 232 DXFunctions; Ollama requests wanted 375931 context tokens (output 262144) against a 65536 maximum; every council step answered empty in under a second, each followed by 3 identical recovery turns, and the run reported "100% successful across 12 step(s)".

**Changed**
- Goal team routing never auto-selects a team listed in `GoalAutoRoutingExcludedTeamKeys`; a preserved requested team is still honoured. Keyword routing now logs the top three candidates and scores.
- Goal installed-model fallback orders by preferred-key index, then models at or above `GoalFallbackMinimumModelBillionParameters`, loaded state, size descending. Size is parsed from the Ollama parameter size in the candidate details, else the model tag (existing `runtime.model.size-token` pattern); no network call. Small models are used only when too few larger ones exist. The selection log gives the reason and the skipped small-model count. The Chat page fallback is unchanged (it already prefers gpt-oss:20b and has no shared helper).
- Ollama request budget: configured output stays the upper bound. When prompt + output + margin exceeds the context ceiling, this request's output is reduced to max(`minimumFittedOutputTokens`, ceiling - prompt - margin) and logged at Information. Nothing is written to configuration. The existing Warning remains when even the minimum does not fit.
- Plain chat sessions attach the compact base tool set plus broker via the new `ICouncilAutomaticFunctionPolicyService.ResolveSessionFunctions`; `CouncilUnlistedStepNativeToolMode = AllPolicyApproved` still attaches everything.
- Runtime benchmark counts a step as successful only with no error and usable visible content. Round recovery stops after one identical retry (same model, tools, budget) following an empty answer and logs a Warning.
- Version 7.1.8 -> 7.1.9.

**New runtime values (added on startup, existing rows untouched):** `GoalAutoRoutingExcludedTeamKeys` (collection, default configuration-steward); `GoalFallbackMinimumModelBillionParameters` (System.Int32, 7); `minimumFittedOutputTokens` (1024) inside `OllamaRequestBudgetPolicyJson` (class default 1024 covers existing rows).

**Files:** GoalExecutionService.cs, GoalExecutionService.TeamSelection.cs, GoalExecutionService.FallbackModels.cs (new), CouncilRuntimeService.OllamaRequestBudget.cs, OllamaRequestBudgetPolicy.cs, CouncilAutomaticFunctionPolicyService.cs, ICouncilAutomaticFunctionPolicyService.cs, ProviderModelRuntimeService.cs, MultiModelCouncilService.LiveInputAndHealth.cs, MultiModelCouncilService.RoundRecovery.cs, LocalGptRuntimePolicySeedDataService.GoalDeliveryAndSteward.cs, LocalGPT.csproj.

**Not compiled here:** no .NET SDK or PowerShell; no build, guard or runtime check was run.

**Runtime checks:** repeat the PublisherStudio Excel/Word goal (expect no configuration-steward routing, a 7B+ model, compact tool count in the plain session log, a fitted-output Information line instead of the 375931 warning, no benchmark 100% with empty steps, recovery stopping after one identical retry).

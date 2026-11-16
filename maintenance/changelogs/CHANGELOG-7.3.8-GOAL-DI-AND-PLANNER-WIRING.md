# LocalGPT 7.3.8 — DI-owned GoalExecution continuation and selected-model planner repair

- Fix the GoalExecutionService.ContextRecovery partial: all helpers are *instance* methods on the existing scoped GoalExecutionService, not static wrappers or a new layer. Uses the injected ILogger<GoalExecutionService>, Council/planner, runtime policy and ledger through the existing calling chain. Methods log and handle exceptions explicitly.
- Fix model planning: an installed, explicitly selected model may plan even when it was not separately flagged configured or preferred. ProviderModelIdentity resolves against the **full candidate inventory** and refuses ambiguous bare keys. User-selected providers/teams remain authoritative.
- Give goal-level recovery actual user-editable instructions (GoalContextRecoveryPlannerInstruction, GoalContextRecoverySubstepInstruction) via the existing database-owned runtime policy; no separate coordinator or hard-coded model role.
- Preflight context overflows produce a structured signal through existing Ollama→Council→GoalExecution wiring. GoalExecution attempts validated semantic subgoals (including existing knowledge-assisted team if permitted), then a bounded compact continuation with a fresh Council run. Original output preference, durable receipts and user approvals remain intact.
- This overlay preserves the 7.3.7 partial/file layout and corrects its architecture-policy violation. No source file deletions, Git actions or changes to DI registrations.

## Review/acceptance

1. Run build/Assert-ApplicationStaticPolicy.ps1: no static fallback declarations in GoalExecutionService.ContextRecovery.cs.
2. On a selected installed Ollama model not marked configured/preferred: semantic planner must select exactly that provider-qualified candidate (or fail safely if ambiguous/unavailable).
3. Inject an over-budget Council request: error travels from client to Council failed-step evidence, then GoalExecution replans into smaller configured subgoals or a compact attempt. No identical retry, no changed saved token setting.
4. Verify codegen review creation -> human approval -> actual .NET 10 build and self-test -> verified downloadable ZIP; never report a ZIP without a real receipt.
5. Simulate provider HTTP 500 after an uncertain function call: do not replay side effects automatically.
6. Test with PreserveSelectedTeam enabled and disabled. Disabled allows the configured knowledge-assisted subagent team; enabled preserves the user's selected team.

This assistant environment lacks .NET SDK 10, PowerShell and DevExpress packages: no full compilation or live application testing is claimed.

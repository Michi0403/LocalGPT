# LocalGPT 6.7.9 — Offline source validation

## Verification scope

- Code changes were based on the complete LocalGPT 6.7.8 source tree.
- Controller, services, DTO/contract, runtime-policy seeds, all required DI registrations, auto-discovered DXAiFunction descriptors, globally injected prompts, and build-wired PowerShell guard were inspected offline.
- The PowerShell guard includes source-level checks for waiting-state protection, curator's disabled automatic tool access, stable user request handling, schema optionality, and role/global prompt wiring.
- These source checks are NOT equivalent to executing the Windows PowerShell 5.1 guard, the C# compiler, a real LLM, or ASP.NET Core end-to-end tests.

## Required acceptance cases on a machine with .NET 10 and PowerShell

1. Build and rebuild LocalGPT in Debug and Release (confirm `AssertLocalGptAiHelpEscalation` executes).
2. Confirm `ai.help.report_attempt` and `ai.help.status` appear in the saved DX function catalog with AI exposure enabled subject to user policy; new team/role choices must respect strict allowlists.
3. Report first failure -> `try_alternative`; report a distinct second failure -> one curator request -> `retry_with_guidance` or pending user; curator-guided failure -> `await_user`.
4. Identical `attemptId` submitted twice -> same result, not another curator or duplicated human question.
5. Report failed/success while awaiting a human -> still `await_user`; refresh the page/restart LocalGPT -> same saved task and request ID.
6. Resolve the exact existing Human Collaboration inbox question and use `ai.help.status` with `checkHumanResponse=true`; any accepted guidance is a proposal, not a tool authorization.
7. Disable/unconfigure the selected curator model -> bounded escalation without fabricated reviewer success.
8. Verify normal image tools still accept absent/null `negativePrompt`, while genuine required parameters and model-install Human Collaboration approvals still apply.
9. Verify Council run and Chat resume flows stay separate and retain the same original conversation ID.

## Not certified here

No .NET SDK or Windows PowerShell / pwsh binary is available for a real build or running tests here. The underlying Blazor `TryClaimPendingStream` error and real UI recovery have NOT been reproduced or confirmed resolved. Existing-user saved runtime-policy overrides remain authoritative; new defaults are reconciled through existing seed policy infrastructure.

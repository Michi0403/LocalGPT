# LocalGPT 7.3.7 — goal-owned context recovery and subagent wiring

Source-review overlay against `main` at commit `73d1e8234e677c6300a4616050bf298e3fcf1404` (7.3.6).

- Non-streaming Ollama requests now propagate the structured context failure instead of silently converting it into a generic null response.
- Ollama context preflight produces an explicit recoverable failure for the Council rather than sending a request known to overflow. The client does **not** decide whether the user goal is finished.
- Preserve the role-selected native DX functions rather than silently pruning those functions to fit. Generic unscoped chat can still prune optional tool schemas.
- `GoalExecutionService.DurableExecution` now distinguishes **function availability** from **executed side effects** for a narrow, proven preflight-only failure. If any tool invocation, review, verified read or artifact may have occurred, existing approval and verification behavior remains intact.
- For an eligible preflight-only failure, invoke the existing semantic goal replanner (and its registered team/subagent selection) before declaring failure. Respect `PreserveSelectedTeam`, configured providers, replan limits, and original user goals.
- If semantic replanning yields no valid plan, GoalExecutionService schedules one focused new attempt in the original team using the existing durable checkpoint/receipt mechanism. No replay of completed steps, no manual approval bypass, no separate coordinator service.
- New goal-context retries use the existing runtime-configured substep template and omit recursive chat history while retaining the verbatim original goal and verified prior work. Output reserve is adapted from the context shortfall diagnostic using the existing `OllamaRequestBudgetPolicyJson` settings, and caller settings are restored afterwards.
- An invalid semantic replan does not consume the successful-replan allowance; the configured maximum still bounds every actual retry.

## Not claimed

The overlay is uncompiled in the assistant environment (no .NET 10/DevExpress feed). A provider HTTP 500 with potentially uncertain external effects is intentionally **not** automatically replayed. A real HelloMath approval/build/ZIP test and an oversized-context Council test remain required.

## Safety and inspection

No Git operation, automatic commit, deletions, migrations or application-data changes. Inspect `review/7.3.6-to-7.3.7.patch` and compare the complete changed files before manually applying anything.

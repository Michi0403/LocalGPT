# Validation notes — LocalGPT 7.7.2

## Source contracts

`build/Assert-CouncilRoadRuntimePolicy.ps1` is a PowerShell 5.1-compatible, version-neutral guard, already called from the repository's mandatory architecture gates. It now detects a return to blocking estimated request sizes or replacing configured roads with advertised model metadata.

## Runtime reproduction matrix (for the operator's Ollama/.NET environment)

1. Council model `deepseek-coder:6.7b`: operator-selected `num_ctx=67072`, advertised context `16384`, estimated prompt ~24063. Confirm `/api/chat` is issued with `options.num_ctx=67072`, no LocalGPT `OllamaContextBudgetExceeded`, then record provider HTTP status and `prompt_eval_count` if available.
2. Repeat a known Council run through streaming and non-streaming clients. Confirm actual provider errors include `Ollama returned HTTP status + error body`, and a provider context rejection does not undergo native tool or thinking fallback repeats.
3. Council with a smaller road than request estimate: confirm diagnostics show the estimate but the provider still receives the request; inspect whether Ollama accepts, truncates, or rejects before drawing a conclusion.
4. Enable `OllamaRequestBudgetPolicyJson.useEstimatedContextFitting=true` explicitly for memory-constrained hosts. Confirm allocation rounds up using `contextGrowthStepTokens` and never grows above the configured route. Disable it to restore full configured context.
5. Compare console/CLI runs using exact request messages, tool schemas and options, not only the task text. Record actual provider `prompt_eval_count` and stream behavior for each run.
6. Test a multi-member Council with heterogeneous route sizes, native tools, and several rounds. Verify user edits, persisted roads, cancellation, hardware leases and existing memory guards remain intact.

These steps are not marked as completed. No executable .NET or PowerShell environment is provided here.

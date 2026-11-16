# LocalGPT 7.3.9 — nonthrowing provider preflight and goal-owned continuation

## Scope

- Restore the user requirement: NO `throw new InvalidOperationException` for a routine Ollama context-fit preflight in `CreateRequestAsync`.
- Instead, provider request creation returns a tuple containing either a prepared request or a machine-readable failure. An oversized request is never sent to Ollama.
- Streaming returns that failure as a normal observable response update; nonstreaming uses `CreateFailureResponse`.
- `MultiModelCouncilService.ParticipantExecution` explicitly classifies the response as a failed member step and stores the diagnostic in `Error` for the existing recovery/journal pipeline.
- The existing 7.3.8 `GoalExecutionService.DurableExecution` and `GoalExecutionService.ContextRecovery` consume the observed result, replan through the injected provider planner and Council, and preserve original goal receipts and human approval boundaries.
- Existing selected-provider/DI-planner fix from 7.3.8 remains unchanged. No new service, extra DI registration, static helper, or tool registry was added.

## Limitations and acceptance

- GoalExecution can act only once an owning Council subrun returns; this patch does not implement a mid-round Council interruption/handoff.
- User-initiated cancellation remains authoritative; no automatic resume after Stop.
- The preflight must produce `OllamaContextBudgetExceeded` in the saved member `Error`, not a provider exception or a fake successful consensus.
- Confirm that the UI preserves the streamed failure and prior chat transcript, and the goal checkpoint records a distinct replanned attempt without losing verified tool receipts.
- Not compiled or integration-tested in this environment (requires .NET 10 and project dependencies). Inspect and test before use.

# LocalGPT 3.1.3 — Council round recovery, cancellation and live UI stability

This release is a forward-only enhancement of 3.1.2. It preserves the durable benchmark evidence, benchmark coverage truth guard, Social Team workflow configuration, provider adapters, DX functions, live Council lanes and persistence behavior already present in 3.1.2.

## Why this release exists

A long-running Council exposed three separate failure modes:

1. a model could exceed the Council timeout, exhaust the existing same-member safe fallback, and leave a required configured round without a usable member result;
2. stopping a Council intentionally cancels the active `HttpClient` request, which correctly surfaces as `OperationCanceledException`/`TaskCanceledException` internally but was still able to flow through the generic run-failure boundary and look like an application failure;
3. the JavaScript-only `.localgpt-live-user-message-row` was inserted inside a Blazor/DevExpress-owned message subtree, so normal Council re-renders could remove the row and the mutation observer would immediately add it again, producing visible flicker.

## Configured round/member recovery

`CouncilWorkflowStepDefinition` now persists two explicit Social Team settings:

- `MemberFailureRecoveryMode`
  - `Disabled`
  - `RetrySameMember`
  - `RetrySameThenEligibleRolePool`
- `MemberFailureRecoveryAttempts`, bounded from 0 through 8 and defaulting to 3 for newly created/default-deserialized workflow steps.

The existing participant-level safe fallback remains the first recovery boundary. It retries the same provider-qualified model with bounded fallback settings. If that still returns an error/timeout/unusable result, configured round recovery now repairs the required member slot rather than silently allowing it to disappear.

`RetrySameThenEligibleRolePool` treats that existing participant-level fallback as the same-member attempt, then chooses a different eligible provider-qualified member. The replacement pool is derived from the role's persisted Social Team selection policy:

- `AllSelected` / `RandomRange` use the selected Council participant pool;
- `AssignedModels` / `AssignedModelsRandomRange` use only the role's saved `AssignedModelKeys` that are present in this Council run;
- distinct-role assignment groups remain respected so recovery does not steal a member reserved for another role in the same distinct group;
- `AssignedModelSingle` never substitutes another identity and therefore retries only the exact assigned model.

When an alternate is permitted, LocalGPT prefers an eligible member on a different AI-host road from the failed member, then uses the existing observed-health ordering. This allows a failed local Ollama road to hand the required work to another eligible provider/host when the saved role pool permits it.

Every failed original step remains in the ordered Council evidence. Every recovery turn gets its own `automatic member recovery` phase and inherits the same workflow key/revision/X-cause metadata. Recovery is bounded; if all configured turns fail, LocalGPT records an explicit unresolved warning rather than fabricating success or silently dropping the round.

## Phase isolation

Unexpected per-participant infrastructure exceptions inside a parallel/host-queue phase are now converted to explicit failed `MultiModelCouncilStep` evidence. The rest of that host queue and other independent host queues can continue, and configured round recovery can repair the failed slot.

The outer phase boundary no longer logs and silently swallows unexpected orchestration exceptions. After logging, an infrastructure-level phase exception is rethrown so it reaches a real recovery/failure boundary instead of making an entire round disappear from control flow.

## Explicit Council stop is expected cancellation

Stopping the active Council still cancels the provider request. `HttpClient` uses `OperationCanceledException`/`TaskCanceledException` to unwind an in-flight request; that mechanism is intentionally preserved because replacing it with a fake response would break .NET cancellation semantics.

The surrounding LocalGPT boundaries now classify caller cancellation correctly:

- `OllamaThinkingChatClient.SendRequestOnceAsync` logs caller cancellation at Debug without an exception stack;
- `MultiModelCouncilService.RunAsync` handles caller cancellation before its generic failure boundary, writes/preserves the partial run log, closes the spool snapshot as a non-failure terminal run, and rethrows cancellation to the owning live-session adapter;
- `CouncilChatClient` already converts that cancellation to the visible `AI Council run was stopped by an explicit user action` status;
- `ComponentActivityService` no longer emits full exception stacks for expected component-lifetime cancellation.

A debugger configured to break on first-chance `TaskCanceledException` can still stop on the `HttpClient.SendAsync` line before LocalGPT catches it. That is debugger behavior, not an unhandled application failure.

## Live user-message flicker

Direct user messages sent into a running Council now remain owned by the normal Blazor/DevExpress chat session. The .NET callback inserts the accepted message into the authoritative `SelectedSession.Messages` collection and performs one renderer-affine `LoadMessages` refresh for that explicit user send.

JavaScript no longer mirrors the same message as a `.localgpt-live-user-message-row` inside the DevExpress message list, and the normal enhancement heartbeat no longer tries to recreate those shadow rows. Existing CSS selectors are retained for compatibility, but the conflicting synthetic DOM path is no longer used.

This removes the render/remove/reinsert loop that caused the live user row to flash on every Council UI update while preserving the same visible user-message feature through the authoritative component state.

## Compatibility

- No existing 3.1.2 feature was reverted.
- No EF Core migration or SQLite schema change is introduced.
- Existing saved Social Teams remain readable. Missing new recovery properties receive the model defaults and become user-editable when saved again.
- BenchmarkEvidence archive schema remains unchanged.
- 1-Wire protocol remains 2.1.1.
- .NET 10 and DevExpress 25.2 package lanes remain unchanged.

## Validation boundary

This source archive was prepared without a .NET SDK/DevExpress build toolchain in the execution environment. Repository static audits were run, but compilation and runtime smoke testing must be performed on the developer machine before release.

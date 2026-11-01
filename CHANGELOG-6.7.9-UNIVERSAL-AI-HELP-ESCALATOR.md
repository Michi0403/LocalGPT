# LocalGPT 6.7.9 — Universal AI Help Escalator

## Architecture

Native ASP.NET Core Controller, domain BusinessObjects and DI-owned services; no TypeScript helper or separate retry framework.

- `AiHelpEscalationController`: `POST /api/ai/help/attempt`, `GET /api/ai/help/status`, `POST /api/ai/help/human-response`. No route executes a user's requested tool.
- `AiHelpEscalatorService`: task-scoped serialized, durable state in the canonical LocalGPT `runtime/ai-help-escalations.json` file. Atomic replacement; includes identical-failure fingerprint, attempt-ID replay detection, curated-once and pending-user guards. Task storage survives browser circuit loss and process restart. Existing Human Collaboration request tables remain authoritative for human answers.
- `AiHelpCuratorService`: bounded independent provider-qualified model request (automatic tool access disabled) with schema-constrained suggestion. An explicit curator model key selects only the configured model; otherwise first configured available model is used. Model text is not authority.
- `Human Collaboration`: one idempotent queued request through existing `IHumanCollaborationService`; no parallel inbox, no implicit authorization, and no automatic restart of a pending task. A NextPhase gate is created only for its real Council run/ambient context; unrelated operations are not granted any new authority.
- `ReportAiHelpAttemptFunction`: automatically DI-scanned DXAiFunction `ai.help.report_attempt`; tool schema requires only taskId/task/action/status. Error/attemptId/userQuestion are optional. `negativePrompt` does not exist in this schema.
- `InspectAiHelpAttemptFunction`: `ai.help.status` reads a pending decision or checks whether the human answered. This never executes failed work.
- Global AI guidance is a distinct, editable runtime-policy seed `HelpEscalatorGlobalInstruction`, included in normal chat bootstrap, Council participants, goal planner, Keyaccount reviewer and upload advisor. Role-specific tool boundaries are preserved.
- Additional editable seed values control curator model, curator prompt and deadline, unguided-failure limit, state retention and first alternative instruction. No database schema change or EF migration is necessary; existing runtime-policy seed/DB reconciliation owns these entries.
- `build/Assert-AiHelpEscalator.ps1`: source-contract test built into normal Debug and Release targets with file-located failures. No release-version whitelist or Python/Node test suite.

## Behavioral contract

A single reported failure returns a different approach (by default). A stall, repeated matching failure, or threshold crossing consults the curator once. A genuinely different curator instruction permits one further attempt. Further failure queues an answerable human question. Repeated reports while awaiting the human return the same await_user decision; even an unverified model claim of success cannot clear it. User answers require actual persisted inbox status, and the next action does not waive any tool approval. A completed task is terminal; the caller must use a different stable task ID for a new goal.

This is a recovery facility exposed to AI agents, not a claimed elimination of the underlying Blazor `TryClaimPendingStream` framework error. The operator may still need to verify the already-implemented automatic UI reconnect path.

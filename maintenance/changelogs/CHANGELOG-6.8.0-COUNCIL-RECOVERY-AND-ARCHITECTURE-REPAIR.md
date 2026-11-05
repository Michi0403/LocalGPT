# LocalGPT 6.8.0 — Council recovery and AI-help maintenance repair

## Confirmed regressions fixed

- **Debug build MSB3073 (`Assert-CouncilXRoundWiring.ps1`)**: the guard demanded the obsolete literal `councilSession.Messages.AddRange(restoredChatSession.Messages);` even though 6.7.9 had already replaced it with an ordered-overlap merge. The validator now checks the real recovery contract: restored messages, visible messages after the overlap, the canonical cache, and DevExpress reload. It does not weaken or remove the mandatory build target, replace the merge with a duplicate-prone copy, or waive persistence checks.
- **Human Collaboration replies were not recognized by AI help**: `HumanCollaborationService.ResolveRequestAsync` records normal guidance as `HumanStatusAnswered`, but `AiHelpEscalatorService.CheckHumanResponseAsync` previously accepted only `HumanStatusApproved`. The recovery path now recognizes the persisted `Answered` status, while still refusing absent/empty guidance. A missing persisted inbox request is reported as a terminal inconsistency instead of looking like an indefinitely pending question.
- **Out-of-order request replay**: tracking only `LastAttemptId` meant that an old HTTP/DX invocation could be counted again after a different attempt. The ledger now retains the latest 32 distinct attempt IDs, preserves its existing last-ID field for compatibility, and suppresses recent duplicate attempts without altering the failure count or curator budget.

## Architectural integration

- When an enabled Council workflow's existing configured member recovery budget is exhausted, `MultiModelCouncilService.RoundRecovery.cs` records a bounded, stable, idempotent `no_progress` report through the shared `IAiHelpEscalatorService`. Original failed evidence, the saved recovery budget, and existing user approval/role policies remain authoritative. This report does not replay a failed tool or automatically rerun the Council.
- When Goal Execution cannot safely replan a failed step, it records an idempotent report through the same service. The goal checkpoint is saved **before** the curator call, including the `NeedsApproval` state if external effects may have occurred. The escalator does not resume steps or turn a human's guidance into tool authorization.
- Non-strict Council native-function preference lists now include `ai.help.report_attempt` and `ai.help.status` alongside the existing attachments/broker. **Strict team/step allow-lists and disabled workflow steps are left intact**; the operator must explicitly allow these functions there.
- Curator diagnostics can receive an optional exact `functionName`; the curator reads the corresponding **registered DXAiFunction parameter schema** and separately identifies required and optional fields. Caller-supplied claims are not treated as verified schema requirements. An optional `failedModelSelectionKey` prevents curator auto-selection from choosing that same provider-qualified model; a user-specified unavailable or same-model curator does not silently switch hosts.
- AI-help PowerShell guard assertions cover the new Goal/Council connections, answer-state acceptance, schema verification, and bounded replay protection.

## Version and scope

- Version rolls from `6.7.9` to **`6.8.0`**, never `6.7.10`.
- **Only LocalGPT source changed.** PublisherStudio 4.7.1 remains untouched; no revised PublisherStudio archive is issued.
- No database migration, tool execution authority change, external package upgrade, C# style rewrite, or GitHub operation.

## Important remaining limits

- This is **not a certified .NET 10 build**. Neither the Windows PowerShell 5.1 build guards nor the application/runtime have been executed in this environment; see `VALIDATION-6.8.0-OFFLINE.md`.
- AI-help decisions are reported to the calling owner, but there is **no new universal automatic resume scheduler for arbitrary Chat tasks**. A resolved human question provides guidance and still requires its owning orchestrator to resume through ordinary policies. The existing Blazor `TryClaimPendingStream` problem is not claimed to be solved by this change.

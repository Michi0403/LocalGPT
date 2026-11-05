# LocalGPT 5.4.6 — Unified Human Collaboration review surfaces

## Motivation

The inline Chat AI requests and the Human Collaboration Inbox were independently rendered.
They shared a database service but differed in request filtering, free-text limits, reuse-scope
controls, feedback presentation and decision execution. That divergence made legitimate prompts
appear in only one UI and made decisions seem to disappear.

## Changes

- Both review surfaces now render `HumanCollaborationRequestCard` for every pending
  `HumanCollaborationRequest` in the same database queue. The Chat orders its current Council
  first but does not suppress requests from other runs or approved request categories.
- Common request editor exposes a consistent description, response suggestions, human answer,
  team note, explicit approve/decline, approval reuse-scope and single-use checkbox. Selecting
  a suggestion never implicitly approves a consequential operation.
- All decisions go through the existing `IHumanCollaborationService.ResolveRequestAsync` policy
  and are visible in the Inbox's persisted decision history. Chat also renders recently saved
  decision receipts beneath the shared approval cards. Both subscribe to service change events.
- Pending request forms use an expected `DecisionVersion` plus pending-status check under the
  service's existing database synchronization. A stale second tab cannot silently overwrite an
  approval saved in the first tab; deliberate saved-decision edits also check the version.
- Deferred DXFunction calls continue to run only after an explicit saved approval, under the
  existing supervised-task executor. Previous Chat-only duplicate submission methods were removed.
- No new permission database, bypass, separate request type, or automatic approval was added.
- The source version and web asset query versions are 5.4.6.

## Expected local checks

Build on the maintained .NET 10 SDK. Open two browser tabs with Chat and Human Collaboration
visible; create an approval, feedback/suggestion, and a blocked Council request. Confirm both
show the exact same request identity; approve or answer from one and observe the other refresh;
verify a stale second form cannot reverse the first decision; check approved deferred calls
execute once and declined calls never execute. Confirm that existing saved approvals and user
preferences were not reset.

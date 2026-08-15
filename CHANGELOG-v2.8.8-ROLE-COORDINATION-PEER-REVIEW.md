# LocalGPT 2.8.8 — optional role coordination and peer review

## Added

- Added opt-in same-role peer review to each saved Council workflow step. When two or more distinct AI members complete the same role, the role members can review the other role-member answers, report a 0–100% usefulness assessment, record corrections, and cast one role-local vote.
- Added opt-in same-role result synthesis. One role member can consolidate the parallel role answers into a single downstream role result while the original answers remain visible and durable in `/chat` and restored sessions.
- Added synthesizer selection modes: a stable pseudo-random role member for the current run/round, or one exact provider-qualified role member selected in Council Teams.
- Added `{{ExecutingRoleMember}}` and `{{RolePeerMembers}}` workflow prompt placeholders.

## Compatibility and behavior

- Both coordination switches default to **off**. Existing saved teams therefore retain their previous execution behavior unless a user explicitly enables either feature.
- A one-member role keeps the existing behavior even if coordination was enabled; LocalGPT does not manufacture a redundant review/synthesis turn.
- Peer review and role synthesis run with DX/organic function execution disabled so completed functions and side effects are not repeated during coordination.
- If an explicitly selected synthesizer is not part of the actual role subset for that run, or failed earlier, LocalGPT falls back to a healthy deterministic role member for that consolidation and records a warning without changing the saved team.

## Role/member identity repair

- Every configured workflow prompt now states the exact executing provider-qualified member, the full assigned role-member list, and the other members of the current role.
- Model names found in benchmark targets, user text, tool arguments, previous role output, or transcript evidence are explicitly identified as task data unless they also match the current role-member list. This prevents benchmark subject models such as `gpt-oss:20b`/`gemma3:27b` from being mistaken for the Task Curator members that are actually executing the role.

## Validation boundary

- Source-only/static validation was used. No `dotnet`, MSBuild, Visual Studio build, package restore, or GitHub access was invoked.
- The Windows build/runtime test remains authoritative.

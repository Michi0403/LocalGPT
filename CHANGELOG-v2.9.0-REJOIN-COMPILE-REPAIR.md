# LocalGPT 2.9.0 — Council rejoin compile repair

## Fixed

- Repaired the 2.8.9 Council rejoin composer-draft capture in `Components/Pages/Chat.razor`.
- `ComponentBase.InvokeAsync(Func<Task>)` no longer has its `void` completion result assigned to a `string`.
- The composer draft is now captured inside the renderer-dispatched callback and copied back only after that callback completes.
- Preserves the 2.8.9 single-bind Council transcript rejoin/circuit-recovery design; no rollback of the rejoin fix.

## Preserved

- Optional 2.8.8 role-member peer review, voting and role synthesis.
- Council membership/routing, DXFunction policy, reasoning/function trace visibility and session persistence.
- All existing LocalGPT `@rendermode` boundaries.
- LocalGPT Wire Protocol 2.1.1.

## Version

Version rolls from 2.8.9 to 2.9.0 according to the repository release rule that minor and patch slots never reach two digits.

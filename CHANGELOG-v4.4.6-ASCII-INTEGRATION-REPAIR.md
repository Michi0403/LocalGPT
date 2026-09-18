# LocalGPT 4.4.6 — ASCII integration repair

## Summary

LocalGPT 4.4.6 closes the remaining gap between direct ASCII-terminal launches and Council/preset launches. The game session is now correlated to the Council run before a chat conversation identifier necessarily exists, required ASCII-game teams can deterministically prebootstrap their configured game runtime, and later DXFunctions/UI lookups resolve the same authoritative session by explicit session, conversation, or Council-run ownership.

## Changes

- Added Council-run game lookup to `ICouncilGameSessionService` and routed game/session/display DXFunction resolution through explicit-session, conversation, then Council-run ownership.
- Added `CouncilGameWorkflowBootstrapService`. It runs before Council model workflow execution, only for persisted teams that explicitly require the ASCII surface and declare a supported game runtime-class family. Difficulty, map size, enemy density and campaign behavior remain owned by the team's assigned runtime classes; the bootstrap only establishes the runtime session.
- Updated supplied ASCII DOOM and Green Dragon workflow prompts to recover the prebootstrapped session first and call `localgpt.game.session.start` only as recovery when no active session exists.
- Refreshed the system Council-team seed version while preserving user-modified/copied team ownership.
- Narrowed supplied ASCII-game automatic-function exposure to explicit workflow allow-lists so small models receive the functions needed for each phase instead of an unrelated tool catalog.
- Fixed the ASCII terminal browser input router so pointer/keyboard focus is not stolen from DevExpress comboboxes, listboxes, options, buttons and other interactive descendants.
- Fixed terminal session attachment so unrelated game-change events cannot replace the game owned by the current conversation/Council run.
- Tightened the DevExpress popup/body/console sizing contract and supplied an explicit bounded popup height for non-fullscreen use.
- Kept continuation intent explicit: renderer-affine component paths use `ConfigureAwait(true)` where required by the Blazor renderer; background/service paths remain `ConfigureAwait(false)`.
- Preserved the 4.4.5 configurable ten-level DOOM campaign, optional-human/runtime-control separation, documentation meteors, macOS package title repair, Mermaid layout repair and the restored 4.4.2 navigation shell.

## Compatibility

No destructive database migration or seed reset is introduced. Existing copied/user-modified teams and runtime classes remain authoritative. Maintained system seed rows may refresh through the normal seed-version path when they are still system-owned/unmodified.

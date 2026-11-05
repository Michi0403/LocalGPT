# LocalGPT 6.8.9 — Council Rejoin / Chat Hydration Repair

## Problem reproduced in source 6.8.8

- Selecting or reloading a completed Council restored the evidence panel only; `DxAIChat.LoadMessages` was not reached when `GetAttachmentSnapshot(runId)` was absent.
- A failed live attachment could leave the chat empty even if a retained Council spool checkpoint was available.
- `IsSelectedCouncilSessionAttached` treated a requested rejoin ID as proof that a live UI attachment actually succeeded.
- The reconnect bridge only published actively running IDs; selected, completed runs could lose their exact browser recovery target on full reload.

## Targeted source changes

- Rejoin now uses exact run marker matching to reuse a saved SQLite Chat conversation. Only an assistant message with the matching run marker qualifies, preventing an unrelated saved chat from being attached.
- When no exact saved Chat exists, a bounded, *display-only* projection of the selected Council spool is loaded into DevExpress Chat. Full original prompt, steps, thinking, errors and final answer remain available in the independent evidence panel. This path does **not** request a new Council run or perform SQLite recovery.
- Initial full-page rejoin and failed live attachment now take the same spool recovery fallback.
- Completed selected run IDs are retained by the browser reconnect bridge for subsequent reloads. Explicit clearing removes the browser recovery target without canceling the backend run.
- Live rejoin availability is based on a real attachment, not merely on a pending/rerequested ID.
- A temporarily unavailable composer-draft JS call does not by itself abort server-owned transcript attachment.

## Preservation boundary

- Tournament/game rules, Council participant scheduling/provider watchdog, installer and SQLite corruption recovery are unchanged from 6.8.8.
- No new database tables, migrations, services, DI registrations or cross-circuit static stores.
- Rejoin code is still part of the Chat page partial. A broader component split is intentionally deferred until functional recovery can be tested and locked down.

## Known limitations

- No Windows .NET build or Blazor/DevExpress runtime test was performed. This is a source repair candidate, not a verified production release.
- The spool fallback shows bounded previews (64 steps, 2,048 characters per step); full evidence remains accessible separately. DevExpress function-call metadata cannot be reconstructed from plain spool text when the saved Chat record is missing.
- The Blazor `TryClaimPendingStream` timeout and broader WebSocket stability are not proven fixed by these changes.

# LocalGPT 4.3.8 — architecture compliance and ASCII control hardening

## Scope

This release keeps the 4.3.7 chat, Council, game and Operator feature work, but moves the new behavior back behind LocalGPT's maintained architecture boundaries instead of weakening or bypassing them. The repository guard scripts and existing `InteractiveServer` model remain authoritative.

## Architecture corrections

- Restored the `MainLayout` diagnostics boundary to its maintained `@key="NavigationManager.Uri"` contract. Sidebar opening/closing now changes renderer-owned drawer state directly, so `/chat` no longer needs a URI mutation to toggle the drawer and the active Chat/game circuit is not recreated by that action.
- The drawer receives its open state as a normal component parameter from `MainLayout`; query-string state is still accepted by the layout for navigation/deep-link compatibility, but same-page drawer toggles do not navigate.
- Removed the new application `static` helper methods from the collaboration/game UI path. Hot-seat classification is service-owned and tied to the Council role-response operation contract plus the active Council-owned game run.
- Moved Operator command parsing, saved-session text formatting and saved-session selector matching into `IConsoleOperatorService`/`ConsoleOperatorService`. Razor components dispatch typed operator actions instead of performing direct string parsing/search operations.
- Reworked the new Chat Operator continuations to follow the repository async policy: renderer transitions use `InvokeAsync`, ordinary asynchronous work uses `ConfigureAwait(false)`, and no new renderer-affine `ConfigureAwait(true)` exemptions were added.
- Refreshed the reviewed JavaScript diagnostics manifest for the intentional `localgpt-game-console.js` resize/input-guard change rather than disabling the diagnostics guard.
- Added the missing XML parameter documentation introduced by the game/Council ownership changes.

## Preserved and enhanced behavior

- Hardware/configuration sliders still commit on `change`, avoiding continuous server-side feedback while dragging.
- `/chat` remains vertically reachable, and opening/closing the navigation/history drawer keeps the active Chat/ASCII game state alive.
- AI/Council requests associated with the active game remain interactively answerable inside the ASCII screen; game play no longer requires leaving ASCII mode for Human Collaboration UI.
- Human Council-role turns are projected directly into the game controls/text input rather than hardcoding Player 1/Player 2 names in the collaboration drawer.
- Operator mode still controls chat prompts, Council prompt/status/stop/skip, saved sessions, game lifecycle, ASCII modes and fullscreen from the terminal surface.
- Chat, Game and Operator ASCII views retain Fit, Width and Native fullscreen scaling.
- Council completion/cancellation still closes stale participant lanes and stops only game sessions owned by that Council run.
- Existing `InteractiveServer` page/island declarations are unchanged.

## Source validation

The source tree was checked with the repository's Python architecture and async-continuation audits, a direct equivalent of the text-service ownership guard, the JavaScript diagnostics manifest contract, and the 4.3.8 release audit. No `dotnet` build/restore/publish and no GitHub/online repository access were used for this handoff.

# LocalGPT 4.3.7 — chat, ASCII and Council interaction repair

## Scope

This release repairs the reported `/chat`, AI Council configuration, ASCII-game interaction and Council lifecycle defects while preserving the existing LocalGPT architecture, selected provider/model behavior, database ownership, and `InteractiveServer` rendering model. It does not replace the current Council/game services or introduce a second runtime path.

## Fixed

- Changed the chat configuration and Council hardware range controls from continuous server-side `input` commits to committed `change` updates. The browser thumb remains locally draggable, while LocalGPT persists/re-renders the value after the user finishes the adjustment instead of feeding every intermediate drag position back through Blazor Server.
- Restored vertical reachability on `/chat` so session controls at the bottom are not trapped below an internal minimum-height child.
- Prevented a sidebar/history query-string toggle from disposing and recreating the active `/chat` page. The main error-boundary identity now follows the route path rather than the complete URI, and same-route drawer navigation stays enhanced/client-side.
- Added element-level resize observation to the ASCII console so it recalculates its frame scale when the navigation drawer or another parent layout change alters the available width.
- On Council completion or cancellation, participant lanes that are still marked running are closed immediately instead of remaining as stale “Completed model lanes”. Council-owned ASCII game sessions are also ended without touching unrelated game sessions.

## ASCII game and Operator workflow

- `Player 1 Hot Seat` and `Player 2 Hot Seat` no longer appear as ordinary collaboration-drawer work while the Neon Relay game is running. Their pending human turn is projected directly onto the ASCII game surface as MOVE, DASH, PASS and short EMOTE controls, with keyboard/game-control mapping available on the same surface.
- Other pending AI/Council human-interaction requests associated with the active Council run can be answered inside the ASCII game surface, including approve/decline, suggested choices and free-text responses. The request content remains AI/Council-owned; the ASCII surface is the interaction projection.
- The persistent ASCII wall now supports replayable Chat, Game and Operator modes without requiring the user to leave the terminal surface during play.
- Operator mode can submit normal chat prompts, start Council prompts, show/stop/skip the attached Council run, list/create/open chat sessions, start/status/end ASCII games, switch ASCII presentation mode, and enter fullscreen.
- Fullscreen is available for Chat, Game and Operator presentations with Fit, Width and Native scaling modes. Input fields are excluded from game-keyboard shortcuts so typing in Operator/human-response controls does not accidentally move the game.

## Council/game ownership

- Council-started game sessions now retain their owning Council run identifier.
- Council completion/cancellation ends only running game sessions owned by that Council run.
- Existing standalone chat/game sessions remain independent and are not mass-cancelled.

## Regression protection

- Added `build/audit_release_4_3_7.py` to verify the slider commit contract, chat-route preservation, ASCII interaction/operator controls, Council lifecycle cleanup, routable `InteractiveServer` pages, version metadata and tracked documentation snapshot.
- Static service-resilience and Council X-Round/heartbeat audits continue to pass after the changes.
- No .NET build, restore or publish was performed for this source-only handoff, per the supplied environment constraint.

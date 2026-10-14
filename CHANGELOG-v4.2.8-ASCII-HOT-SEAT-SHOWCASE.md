# LocalGPT 4.2.8 — ASCII hot-seat showcase team

LocalGPT 4.2.8 adds a maintained preseeded Council team named **ASCII Hot Seat — Neon Relay Arena** (`ascii-hot-seat-showcase`).

## Multiplayer hot-seat Council workflow

- Adds separate `Player 1 Hot Seat` and `Player 2 Hot Seat` roles with `HumanOnly` participation so the workflow pauses for the person currently holding the keyboard instead of asking an AI to impersonate that seat.
- Alternates both human seats inside a bounded ten-round Council loop.
- Adds one AI `Arena Referee` that owns the deterministic multiplayer state, simultaneous movement resolution, collision handling, orb scoring, dash charges and the end-of-match marker.
- Keeps the latest `HOTSEAT_STATE` line authoritative. Display pixels and animations are presentation only.

## ASCII feature showcase

- Starts one bounded 100×32 Council game session as a fixed-cell display host, then hides its single-player input overlay so it does not compete with the two Council hot-seat prompts.
- Uses one stable `ASCII Hot Seat Display Director` renderer identity for the whole match.
- Preseeds explicit use of display readback, text writes, single-cell writes, region fills, region blits, full-frame submission and 2–12 frame pregenerated animation submission.
- The intro deliberately exercises full-frame plus local browser animation; normal rounds prefer incremental display mutations; scoring, collision and finale events can trigger short state-consistent ASCII movies.
- Keeps visible chat compact and playable while the richer presentation remains on the ASCII surface.
- Gives the display role bounded access to database-backed regex and knowledge lookup functions so reusable parsing/layout evidence can be reused instead of recreated.

## Release identity

- Advances LocalGPT application, installer-console and webview-wrapper release identity from 4.2.7 to 4.2.8.
- Historical 4.2.7 release and validation records remain unchanged.

No .NET build, restore, publish, signing/notarization or GitHub operation was performed for this source handoff.

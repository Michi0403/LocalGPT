# LocalGPT 2.8.9 — Council rejoin and circuit recovery

## Fixed

- Rejoining a long-running Council no longer rebinds the entire DevExpress `DxAIChat` message collection on every live heartbeat. The control is bound once on the initial join/rejoin; subsequent progress is rendered from the server-owned live-session snapshot.
- Live transcript and participant-lane presentation now use the cached authoritative Council snapshot, allowing a fresh Blazor circuit to reconstruct the visible Council state after a WebSocket/circuit loss.
- Chat autosave merges the authoritative live-Council marker message before persistence so a non-rebound DevExpress control cannot overwrite the newer server transcript with stale client content.
- Rejoin operations are serialized so a user-triggered join cannot race a scheduled live-session refresh into two concurrent DevExpress message binds.
- The running-session summary now distinguishes an available-but-not-yet-joined live Council from the absence of a Council heartbeat.
- SignalR gives temporary browser/main-thread stalls more tolerance while retaining server-side rejoin as the authoritative recovery mechanism.

## Preserved

- Council execution remains server-owned and does not restart when the browser reconnects.
- Full user-visible reasoning, tool calls/results, ordered Council transcript, member lanes, human contributions, role peer review and optional role synthesis from 2.8.8 remain intact.
- Existing `@rendermode InteractiveServer` boundaries are unchanged.
- No protocol version was changed.

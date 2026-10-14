# LocalGPT 2.2.10

## Chat ASCII-console removal

- Added an always-visible **Close** action inside the `/chat` ASCII game console.
- The action is rendered for the empty launcher and active game states.
- The action remains available in Fit, Width and Native fullscreen scale modes and in responsive narrow layouts.
- Closing the panel exits browser fullscreen before the Blazor component is removed, avoiding a stale fullscreen element or inaccessible Chat viewport.
- Closing only hides the console surface; the authoritative game session stays available and can be rejoined by showing ASCII games again.
- The existing top-level Show/Hide ASCII games action remains available as a second route.

## Versioning

- LocalGPT application version: `2.2.10`.
- `LocalGPT.WireProtocolVersion`: `2.1.1` (unchanged; no wire-contract change).

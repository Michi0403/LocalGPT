# LocalGPT 4.6.8 - Tournament pixel and rejoin resilience

## Kernel Creature Tournament runtime repair

- Correlates game-backed Council workflow steps with the exact `kernel-creature-tournament` session instead of accepting whichever game session was most recently active for the same Council run.
- Uses the same exact game-family lookup in the workflow bootstrap and Chat game surface so a side game cannot displace the tournament renderer or deterministic engine lookup.
- Raises the Kernel Creature Tournament deterministic frame from the ordinary 80x25 game surface to a 144x40 bootstrap target and preserves a bounded 96-160 by 32-48 renderer range.
- Keeps the existing deterministic game state, HP, bracket, control and animation mechanics unchanged.

## ASCII and pixel presentation

- Adds an ASCII/Pixel presentation selector to the active game plane without creating a second game state or control path.
- The pixel surface rasterizes the same deterministic frame, semantic style runs and animation sequence used by the ASCII renderer.
- Existing games remain ASCII-first; the pixel view is an optional presentation mode.
- Fullscreen Fit, Width and Native scaling now apply to the pixel canvas as well as the ASCII surface.

## Rejoin and operator recovery

- Same-run Council rejoin no longer clears the already attached transcript identity and forces a complete `DxAIChat` projection rebind.
- Live-session attachment waits are bounded to three seconds so a busy circuit does not leave the rejoin action waiting indefinitely.
- Opening Operator from the Game plane now remembers that origin and returns directly to the active game when Operator is closed.
- Browser-side attachment caches remain retryable after transient circuit or JS interop loss.

## Non-fullscreen terminal layout

- The popup owns one bounded viewport instead of competing nested scroll surfaces.
- Header actions are height-bounded and independently scrollable when a narrow viewport forces several rows of controls.
- Conversation/operator follow-tail now stops when the user scrolls away from the bottom and resumes only when the user returns near the tail.
- Kernel Tournament presentation uses the additional frame resolution without forcing the normal Doom/corridor presentation to the same geometry.

## Regression guards

- Extended the maintained Kernel Creature Tournament source audit to cover exact game-family correlation, high-resolution frames, pixel presentation, Operator return, bounded rejoin attachment and manual-scroll preservation.
- Active LocalGPT identities moved from 4.6.7 to 4.6.8 while preserving the version-slot policy.
- PublisherStudio source is unchanged for this release.

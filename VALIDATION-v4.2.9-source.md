# LocalGPT 4.2.9 source validation

Validation for this handoff is source-only. No `dotnet`, MSBuild, restore, publish, application launch, release build, signing/notarization, GitHub access or GitHub API operation was performed.

## Runtime/game source review

- New ASCII DOOM state carries authoritative `WorldMap`, `MapSeed`, extraction coordinates, enemy snapshots and `CombatMessage`.
- Procedural maps remain connected and now include a guaranteed starter arena plus one opening hostile in direct line of sight.
- Player movement rejects walls, map edges and living-enemy cells.
- Enemy movement uses bounded map pathfinding and cannot occupy another live enemy or the player tile.
- AI hunter navigation reads the authoritative map, targets visible hostiles, avoids occupied hostile cells and routes to extraction after combat.
- AI-origin `localgpt.game.control` is rejected by the service while `ControlMode == Human`; ownership switching is not automatic-safe.

## Council function/tool review

- `localgpt.game.session.start`, game control/frame/input-gate mutations, and ASCII display mutations that only coordinate in-memory LocalGPT state are marked `IsCoordinationOnly` where automatic invocation is intended.
- `localgpt.game.control-mode.set` remains direct-only for automatic-tool purposes so a model cannot silently enable autoplay.
- `localgpt.game.session.get` and `localgpt.game.display.get` allow an omitted session ID and resolve the newest active game session.
- Embedded JSON schemas for the modified game/display DXFunctions parse successfully.
- The ASCII DOOM seed explicitly forbids guessed session IDs and human-collaboration requests for session identity.
- The Doom Council observes deterministic game state and no longer runs a 24-iteration autonomous control loop from one chat request.

## UI/browser review

- Embedded Fit scaling derives natural frame dimensions from terminal text columns/rows and the computed mono font instead of a stretched PRE scroll box.
- Fit mode has no internal scrolling; Width/Native modes follow the newest frame while the user remains at the bottom and stop auto-following after deliberate upward scrolling.
- The reviewed JavaScript diagnostics manifest contains the normalized SHA-256 for the changed game-console script.
- `node --check` accepts `src/LocalGPT/wwwroot/js/localgpt-game-console.js`.
- Existing Blazor `@rendermode` directives are unchanged relative to 4.2.8.

## Static audits

Passed in this source tree:

- `audit_application_architecture.py --mode all`
- `audit_service_resilience.py`
- `audit_configurable_behavior_policy.py`
- `audit_chat_ascii_console.py`
- `audit_provider_qualified_council.py`
- `audit_async_continuations.py`

The historical `audit_ascii_experience_4_2_5.py` remains unchanged and also fails on the unmodified 4.2.8 baseline with three obsolete 4.2.5 string-contract checks; it is therefore recorded rather than treated as a 4.2.9 release gate.

LocalGPT, installer-console and webview-wrapper application versions are 4.2.9 and retain single-digit semantic-version slots. Project XML, DocFX JSON and embedded modified DXFunction schemas are checked separately during packaging.

Compiler/runtime validation remains authoritative in the user's development environment.

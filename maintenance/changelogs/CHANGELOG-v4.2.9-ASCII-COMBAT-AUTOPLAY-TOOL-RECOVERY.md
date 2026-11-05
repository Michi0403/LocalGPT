# LocalGPT 4.2.9 — ASCII combat, autoplay and Council-tool recovery

## Playable ASCII combat world

- Replaces the old static/implicit corridor assumptions with one connected per-session procedural `WorldMap`.
- Adds persistent enemy records with stable keys, names, glyphs, positions, health, maximum health and contact damage.
- Every fresh map opens in a guaranteed starter arena with one hostile directly in the initial sightline, so the first frame immediately demonstrates targeting and shooting instead of showing only a wall.
- Adds deterministic line-of-sight shooting, damage, enemy death, enemy pursuit/pathfinding, contact attacks, defeat, and an extraction objective after all hostiles are eliminated.
- Adds a compact live radar containing walls, player, extraction and enemy glyphs.
- Keeps the deterministic GameDirector/service authoritative; model narration cannot replace collision/combat state.

## Map-aware AI hunter

- Replaces the fixed/autonomous action cycle with state-based target selection and breadth-first pathfinding over the authoritative map.
- Visible targets are aligned and shot; unseen targets are approached through traversable cells; after combat the hunter routes to extraction.
- Living hostile cells are excluded from player pathfinding, preventing routes that repeatedly propose movement through an occupied tile.
- Blocked edges/walls produce visible replan feedback.
- AI-origin controls are structurally rejected while the game is in `Human` control mode. Autonomous play happens only after the user explicitly selects AI/shared ownership.

## Council/DXFunction repair

- Marks safe in-memory game-session start/control/frame/input-gate and ASCII display mutations as coordination-only automatic functions where appropriate, allowing Ollama native-tool metadata to include them.
- Keeps control-mode ownership switching out of automatic invocation so an AI cannot silently turn Human mode into autoplay.
- Makes `localgpt.game.session.get` accept an omitted `sessionId` and return the newest active session.
- Makes `localgpt.game.display.get` use the same optional-session behavior.
- Updates the ASCII DOOM Council seed to never invent, guess or ask the human for a game-session GUID and to never fall back to nonexistent public-service aliases.
- The Council Player Controller observes canonical Human/AI state instead of issuing duplicate movement; the State Judge reports service-owned resolution rather than applying a second world step.
- The ASCII Frame Renderer mirrors the deterministic game frame rather than hallucinating a replacement collision map.
- Reduces the ASCII DOOM Council loop from 24 autonomous iterations to one Council observation/presentation pass per Council request. Direct Game-tab controls and explicit AI hunter autoplay continue independently.

## Embedded ASCII viewport

- Fit scaling now measures actual fixed-width text columns, line count, mono glyph width, line height and padding rather than using a stretched PRE element's scroll box as the natural frame dimensions.
- Embedded Fit mode uses the complete available game viewport without forcing the PRE itself to fake the viewport height.
- Width/native scrolling keeps follow-tail behavior while the user remains near the bottom, but respects deliberate manual scrolling.
- Keeps Fullscreen, Width and Native presentation modes available.

## Release identity

- Advances LocalGPT, installer console and webview wrapper from 4.2.8 to 4.2.9.
- Refreshes the reviewed JavaScript diagnostics hash for `localgpt-game-console.js`.
- Preserves historical 4.2.8 release and validation records.

No .NET build, restore, publish, signing/notarization or GitHub operation was performed for this source handoff.

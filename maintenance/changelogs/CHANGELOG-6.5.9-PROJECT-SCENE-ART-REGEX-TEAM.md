# LocalGPT 6.5.9 — Project-authored ASCII/ANSI scenes and catalog-regex display state

## New / changed

- Added a serialized `GameSceneArtworkMode` choice (`ClassicGlyph`, `AuthoredAscii`, `AuthoredAnsi`, `AuthoredPixel`) in the existing Game Studio scene/revision model. No new EF table or game-key switch; existing scenes default to `ClassicGlyph`.
- Added project-owned `GameSceneTextCanvas` with literal, whitespace-preserving rows, bounded viewport placement, world-time windows and optional scene-variable visibility conditions.
- Added `AsciiRows` to project-owned `GameSceneSprite`. Both the authoritative C# ASCII projection and the browser pixel/character preview can display authored multi-line sprites and canvas compositions; the existing pixel-palette artwork path is retained.
- Added `GameSceneRegexTransition` declarations referencing **persisted names** in the existing LocalGPT regex catalog, with optional named/numbered capture group or configured literal value. Per-session `GameSceneClock.SceneVariables` can drive conditional canvases and `{variable}` HUD/sprite text, without granting regex rules game-mechanics authority.
- Scene transitions resolve regex names through the existing bounded `IRegexPatternService` and require `IRegexCuratorService` independent review and explicit user approval at preview, build, launch and live game control; missing or pending patterns fail with a named error. Approval-gated GameDirector input is matched before committing a scene control, and presentation updates are staged until canonical scene controls succeed. Game-project build/approved revision launch verifies catalog references.
- Game-session snapshots now carry the session-scoped scene-variable values to browser renderers so regex-driven transitions remain visible to clients.
- Extended small, independent Game Studio scene patches so configurable teams can propose canvas, regex-transition and artwork-mode changes alongside actors, tiles, pixels, palette, animation and HUD.
- Added a total 96,000-character scene art budget, a Game Studio style picker, and a nonpersistent preview tester for catalog-driven changes. Expanded the JS canvas preview consistently with authored text screens, sprites and dynamic display values.
- Added `game-scene-art-curation`, an **optional editable seeded Council team example** with function-selected Scene Director, Artist, Knowledge/Regex Author and independent Curator workflows. The team can propose new catalog regexes with `localgpt.regex.upsert`, but cannot auto-approve them. These roles are not special runtime keys: any user-configured Council team can invoke the registered Game Studio scene functions subject to its permission/approval policy.
- Project-game compiled frame dimensions now follow the approved scene's view size so detailed authored screens are not silently reduced to the separate authoring-profile defaults.
- Added `docs/examples/game-studio/SCENE-AUTHORING.md` demonstrating an isolated 1990s-style ASCII scene patch and its regex catalog linkage.

## Preserved / not claimed

- The Kernel Creature Tournament legacy procedural **skeleton/rig renderer** remains completely unchanged and available for 1970s-style games. This release does not magically give a built-in tournament session custom Game Studio artwork: the new path is for projects using approved scene revisions.
- Deterministic simulation, GameDirector turn approval and human/AI/Operator shared controls are unchanged by the selected artwork mode.
- Authored project and team data retains explicit independent review and human approval; no AI output is automatically accepted as scene design.
- Rich per-cell ANSI foreground/background effects beyond existing palette behavior are not claimed. `AuthoredAnsi` selects authored terminal-character art with current palette capability, not full VT/ANSI escape control.

## Validation limits

- Source-level checks, JavaScript syntax/execution smoke checks and C# project XML checks performed without .NET or PowerShell. No claim of .NET 10 compilation, PowerShell architecture-guard pass or a macOS end-to-end Council/game browser validation.

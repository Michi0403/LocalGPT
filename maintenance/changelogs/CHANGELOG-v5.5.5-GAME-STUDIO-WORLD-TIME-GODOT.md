# LocalGPT 5.5.5 — AI Game Studio, world time and revisioned Godot C# authoring

## New Game Studio workspace

- Added `/game-studio` to the maintained DevExpress/InteractiveServer navigation, with a hierarchical world/actor/effects/curation/Godot/revision editor, split view, bounded source editor, deterministic ASCII projection, pixel canvas, and authored-time stepping controls.
- Introduced durable, project-owned scene documents for **open worlds**, streamed-style tile coordinates, reusable AI-authored pixel sprites, semantic geometry/exclusion masks, moving actors, frame animations, per-row/per-plane horizontal and vertical parallax, keyed palette changes, configurable HUD, and timeline data.
- Added **fixed-step world time** (`FixedTicksPerSecond`, `WorldSecondsPerRealSecond`, `SecondsPerTurn`), independent of FPS and rendering frequency. Pending simulation time stays in the accumulator under bounded catch-up; motion is measured in world cells per world second.
- Added semantic scene curation that rejects out-of-bounds geometry, decorative overlap with solid/exclusion cells, missing palette tokens and sprite references, invalid clock speeds, malformed animation timing and excessive scene sizes. An artist's accidental object on a wall now receives a specific failure rather than becoming authoritative game state.

## AI creativity with independently reviewed proposals

- Added five discoverable DI-backed DXFunctions: `project.game.scene.get`, `project.game.scene.propose`, `project.game.scene.patch.propose`, `project.game.scene.proposal.get`, and `project.game.scene.review`.
- A Council artist can submit an isolated, unique just-in-time scene candidate using the project's own creative configuration. A second, differently identified curator model can review it. The service independently recomputes geometry and other structural findings; proposals require a positive independent review **and** a human UI approval to be accepted.
- Small local models may now submit only a targeted scene patch (one actor, sprite, HUD element, palette or parallax band). Untouched content is copied forward, while a separate read-only proposal lookup lets the curator fetch the exact candidate before reviewing.
- Preserved the one-use human gate for DXFunction writes. No prompt bodies, game-specific art, game-specific renderer classes, or direct regex patterns were added as hardcoded runtime policy.

## Godot C# as the editable end state

- Embedded a generic Godot C# 2D renderer/simulator source template and generator for a Godot project (`project.godot`, Godot .NET SDK `.csproj`, `.tscn`, C# script, `scene.json`). Users can revise all generated text sources in the DevExpress editor or in Godot and re-import the changes.
- **Saving scene data or a Godot file only updates the open worktree**. Explicit **Close worktree and create revision** creates the Project revision and immutable per-file artifact snapshots, after which the draft is sealed. The next open worktree inherits the completed scene and file snapshots without mutating the completed revision.
- Godot export uses the Game project's configured existing root, explicit UI approval, canonical-path/reparse-point checks, per-file export receipts and conflict detection. The export will not overwrite external edits; import accepts non-conflicting Godot edits and discovers new supported source-text files.
- Project-built `ProjectGameDefinition` now carries a curated `Scene` only when it comes from the **completed current revision**; an open draft does not silently enter a build.
- Added an independently editable parallax/pixel demo at `docs/examples/game-studio-parallax-scene.json` and workflow reference `docs/reference/game-studio-revisioned-godot.md`.

## Maintained architecture and compatibility

- Existing ASCII Doom, Green Dragon, Kernel Creature Tournament, Chat display functions, approved project maintenance, SQL data editors, exclusion recovery, Install/CanIRunAI behaviors, prompts/regex ownership, and historical revision snapshots were retained. No EF entity migration was needed: drafts, proposals, reviews, and file snapshots reuse the existing Project Architecture artifact store.
- New Razor route is registered with explicit non-prerendered InteractiveServer mode; service DI and scoped lifetimes are maintained. Renderer-affine UI method names were explicitly registered in the existing async continuation policy; no audit guard was relaxed.
- Advanced version and script cache identifiers from **5.5.4 → 5.5.5**. Patch and minor numbers remain one digit. PublisherStudio source was not changed.

## Known boundaries and testing instructions

- Source-only delivery: **No dotnet build, NuGet restore, Godot build, runtime smoke test or GitHub access was used**. Run the normal Visual Studio build and Godot .NET import on the target environment; report any compiler/device-specific failure for a targeted 5.5.6 correction.
- The new Game Studio world/pixel simulation and Godot authoring UI are separate from the maintained classic GameDirector session rules. A completed scene is included in new project build artifacts, but classic Chat game sessions have **not** been redirected to use the new simulator. Future migration can add a generic GameDirector session profile without changing established games.
- This revision edits source-text Godot assets (60,000 characters per file, 128 files per worktree). Binary art/audio/font files remain in the external Godot project and the existing Project Maintenance file tracking flow. No binary asset ingestion is claimed.
- For repeated edits, keep the draft **open**. Export, edit in Godot, and press **Import Godot edits** before closing. Resolve reported conflicts explicitly. Closing is a human decision, not a side effect of Save.

## Verification

Added `build/audit_game_studio_5_5_5.py`. The maintained source-only audits for Razor architecture, renderer-affine async rules, async boundaries, resilience/logging, transient UI state, configurable prompt/regex ownership, DevExpress control usage and previous Data Management/learning contracts are run without .NET. See `RELEASE-CHECKLIST-v5.5.5.md` for the per-file audit record.

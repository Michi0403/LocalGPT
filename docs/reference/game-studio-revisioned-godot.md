# Game Studio: AI-authored ASCII, pixel 2D and revisioned Godot C# projects

Game Studio is a new `/game-studio` route under LocalGPT Projects. Create a **Game** project first and configure an existing, writable **project root**. This workbench deliberately uses database-first Project artifacts, the existing human approval gate, InteractiveServer and DevExpress Blazor components.

## Try a scene without building the application from this repository

1. In Projects, create or choose a Game project and provide its Godot project root. In Game Studio, select it and click **Open editable worktree**.
2. Select **Tiles**, **Actors** or **Planes** in the left DevExpress tree. Paste the contents of `docs/examples/game-studio-parallax-scene.json` into the scene JSON editor and **Validate and save world JSON**.
3. The ASCII preview and pixel canvas use the same scene. Use **+1 real second** or **+1 world turn** and observe creature position and two independent line-scroll speeds. The second palette applies after twelve game-world seconds.
4. Open **Godot project files** and **Generate Godot project**. This creates only missing text artifacts (project.godot, `.csproj`, `scenes/Main.tscn`, `src/LocalGptSceneRuntime.cs`, `scene.json`). **Publish scene.json** applies the current approved scene to the source worktree.
5. **Export to project root** to write the Godot files. Open the project in the C#/.NET edition of Godot, edit the `.cs`, scene, shader or JSON files, and use **Import Godot edits** before completing the revision. The export refuses to replace externally edited files; imports detect conflicting simultaneous edits and discover new text source files under `src`, `scenes`, and `assets`.
6. Continue editing the scene and Godot source as often as needed. **Close worktree and create revision** is the *only* operation that creates a completed Project revision and immutable per-file source snapshots. Open a new worktree to resume editing on a child revision. It copies the previous committed source files; editing never mutates a completed revision.

The generated Godot `.csproj` targets the maintained .NET-capable Godot 4.4.1 SDK starter. The user can edit this file to match their locally installed Godot/.NET SDK. Nothing invokes a build or Godot automatically.

## Project-owned scene structure

- `worldWidth`, `worldHeight`, `chunkSize`: open-world coordinates and tile chunk size; actors and tiles are not tied to a hardcoded level or game family.
- `fixedTicksPerSecond`, `worldSecondsPerRealSecond`, `secondsPerTurn`: world-time semantics independent of client FPS; unfinished fixed-step work is retained in the simulator accumulator.
- `tiles`: positions, glyphs/sprite identities, `solid` collision and `blocksDecoration` semantic exclusion masks. Curator diagnostics block a creature/artifact placed in a forbidden tile.
- `actors`: ID, kind, world position, speed in world cells/second, palette, collision and artist/curator provenance.
- `sprites`: arbitrary AI-authored pixel rows plus color-token lookup; blank pixels are transparent.
- `animations`: per-object sprite frame lists and durations in **world seconds**, so the same creature can receive unique motion without a game-specific C# class.
- `layers`: depth, first/last scanline bands, horizontal/vertical speeds, camera parallax factors, glyph patterns and wrapping; suitable for Frogger water, platformer hills and pseudo-3D tunnel effects.
- `paletteTimeline`: synchronized color changes in world seconds. `timeline`: future/scene-owned cues may be preserved in authored data.
- `hud`: independent, optional, AI-authored text and position. `{tick}`, `{worldSeconds}`, `{turn}` are data placeholders, not fixed game layout.

## Council collaboration for original artwork

The existing DXFunction registry discovers these five new service-backed functions automatically:

1. `project.game.scene.get` — read scene and existing curation findings, including proposed candidate identities.
2. `project.game.scene.propose` — an artist model supplies a complete original scene candidate; it stays isolated from the active world.
3. `project.game.scene.patch.propose` — small 0.8B–2B models can upsert just one or a few sprites, actors, cells, HUD elements, palettes or scrolling layers; the untouched project scene is copied into the candidate.
4. `project.game.scene.proposal.get` — read the **exact** candidate scene by proposal ID without forcing every unrelated candidate into a small curator's context.
5. `project.game.scene.review` — **another** Council model sends an explicit review/critique of that candidate. The service independently checks geometry, sprite integrity, blocked-cell placements, bounds and clocks. A blocked proposal cannot receive an approved review.

Both writes retain the existing one-use **human approval**, and accepting into the worktree requires a stored independent positive review *plus* human action. Configure your own project prompts and Council team/models; no creative prompts or story/game-specific visual assets are added as hardcoded runtime policy. Small local models may work with partial scene proposals and receive specific findings for another iteration.

## Revision behavior and limits

- A file **save** is an update to an artifact scoped by the open worktree's ID. No Project revision is created.
- **Close** snapshots the scene and all file contents into Project revision-linked artifacts, then seals the worktree. It does not overwrite history or make an external Git commit.
- Export/import only touches a configured existing Game project root and only after explicit user input; path traversal and symbolic link targets are rejected. Conflicting external edits stop export.
- The artifact store limits a working source file to 60,000 text characters and a worktree to 128 files. Binary Godot imports (PNG, audio, font files) remain in the user's own Godot workspace and must be managed by existing project file-maintenance workflows; this version's built-in editor stores only text source assets.
- A compiled `ProjectGameDefinition` can include the **closed** scene from its current revision. The legacy ASCII Doom/Green Dragon/Tournament GameDirector sessions are intentionally preserved; the new general-purpose scene simulation, pixel projection and Godot authoring path operate through Game Studio for now. Legacy game controls have not been silently redirected.
- The repository's existing per-file Project Maintenance learning checklist remains the review owner for tracked files; scan/synchronize a Godot project there to create checklist entries for new source files.

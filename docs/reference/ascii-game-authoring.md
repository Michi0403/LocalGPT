# ASCII game authoring and color contract

LocalGPT keeps **game state**, **plain ASCII text**, and **presentation styling** separate. This is intentional: a game frame remains copyable/searchable plain text while the shared terminal may render safe color metadata on top of it.

## Evidence order for local models

For game work, especially with 0.8B, 1B and 2B local models, use this narrow order instead of guessing from broad repository context:

1. Read the selected **Project** requirements, Game profile and compiled build facts.
2. Read `localgpt.ascii.surface.get` for the current terminal capability and `localgpt.game.display.palette.get` for exact palette rules.
3. Retrieve only the narrow LocalGPT **Knowledge** entry needed for the task.
4. Use database-backed `localgpt.regex.list`, `localgpt.regex.get` and `localgpt.regex.test` only when a concrete parse/extraction task exists. Prefer an existing tested expression over inventing one.
5. Mutate the smallest sufficient display area; do not regenerate a complete frame when one text, fill or blit operation is enough.

This keeps small models supplied with LocalGPT-specific facts while avoiding irrelevant context that can pull them toward unrelated project conventions.

## Color modes

`TerminalDefault` preserves LocalGPT's established black/green terminal for unstyled cells. Explicit style runs may still use indexed colors. `Ansi16` uses indexes 0-15 and defaults to bright green 10 on black 0. `Indexed256` uses xterm-compatible indexes 0-255 and defaults to green 46 on black 0.

Never place raw ANSI escape sequences or HTML in canonical frame text. Use `CouncilAsciiTextStyle` metadata or the matching DXFunction JSON style object with optional foreground/background indexes plus bold, dim and invert flags.

A complete frame may carry bounded horizontal `styleRuns` with `y`, `x`, `length` and `style`. Animations may carry matching per-frame runs and a subtitle style. LocalGPT clips/normalizes runs to the live session dimensions and browser rendering creates only text nodes and safe spans.

## Choosing display operations

Use `localgpt.game.display.get` for current dimensions/content and `localgpt.game.display.palette.get` for palette indexes. Prefer:

- `localgpt.game.display.cell.set` for one cell.
- `localgpt.game.display.text.write` for a short styled line or label.
- `localgpt.game.display.region.fill` for panels, clears and repeated textures.
- `localgpt.game.display.region.blit` for a multiline sprite or ASCII art block; this is usually the most efficient choice for small models.
- `localgpt.game.frame.submit` only for a complete still frame.
- `localgpt.game.animation.submit` for one pregenerated 2-12 frame sequence; never call the model once per animation frame.

Human, Shared and AI control modes all use the same authoritative game/session contracts. Presentation functions never own HP, collision, bracket progression, inventory, rules or winners.

## Existing and custom games

The same presentation contract applies to the seeded corridor/DOOM, arena, Green Dragon and Kernel Creature Tournament game projects, to hot-seat experiences and to Project-built/custom prompted ASCII games. A Project Game profile persists its color mode and default foreground/background values; the compiled `ProjectGameDefinition` carries them into the runtime session. No game needs a renderer-specific state model to gain color.

## Games as declarative content (7.0.0)

Since 7.0.0 the engine contains no game. ASCII DOOM, the ASCII arena, Green Dragon and the Kernel Creature Tournament are
ordinary Game projects whose rules live in database tables. One generic engine (`IGameEngineRuntimeService`) interprets
them; no rule row contains code. A user-authored game uses exactly the same tables, services and controller.

A Game project profile names the engine primitives it uses and their JSON parameters:

| Profile field | Meaning |
| --- | --- |
| `ViewKey` + `ViewParametersJson` | Renderer primitive: `first-person-raycast`, `text-template` or `duel-stage`. |
| `TurnModelKey` | `actions` (every control is a turn) or `council` (Council role answers drive the game). |
| `MapGeneratorKey` + `MapParametersJson` | Grid map primitive (`rooms-and-corridors`, `fixed-rooms-arena`); empty for no map. |
| `AutoplayKey` + `AutoplayParametersJson` | AI control primitive (`hunt-and-extract`, `action-cycle`); empty for none. |
| `PlayerArchetypeKey` | Archetype of the directly controlled actor. |
| `RulesetTemplateKey` | Game key whose rows this game inherits while a table of its own is empty. |

Seven child tables hold the rules: **stats** (key, range, initial value), **archetypes** (role, glyph, behaviour,
parameters; actors, enemies, rigs), **actions** (keyboard and gamepad labels, action primitive, parameters,
AI-selectable flag), **rules** (trigger, primitive, parameters, enabled), **levels** (size, difficulty, parameters),
**texts** (every player-visible line, caption and prompt) and **aliases** (action, axis, game and runtime-class aliases).

Rule triggers:

- Action games: `start`, `before-action`, `after-action`, `world-step`, `goal` and `style`.
- Advisory GameDirector subdirectors: `director.prediction`.
- Council-driven games: `council.progression` (the progression primitive, for example `single-elimination-team-duel`),
  `council.lineup` (which Council role answers seed the entrants) and `storyboard.initialize`, `storyboard.exchange`,
  `storyboard.exchange-resolved`, `storyboard.complete` (one presentation frame per row).

Text templates use `{token}` and `{token,align:format}`; unknown tokens stay literal and `{{` escapes a brace. Value
specifications in parameters may be a template string or an object with `when`/`then`/`else`, `firstOf`,
`join` + `values`, `value`, `clean` and `max`. Conditions support `==`, `!=`, `>=`, `<=`, `>`, `<`, `!`, `&&` and `||`.

The council roles of a team configuration make every AI decision: a workflow step marked `GameEngineInitializes`
starts the game, `LimitToActiveGameEntrants` limits a round to the active pair, and the `Context*` step settings
bound the transcript each role sees. The engine only resolves the consequences of the answers.

The four system games are seeded from the embedded documents in `src/LocalGPT/GameTemplates/Seeds/*.json` by
`GameProjectSeedService`. A seeded game the user has edited is never overwritten. The Projects page edits the profile
primitives, their JSON parameters and the complete rule rows; the same data is available through the REST API:

- `GET api/games/catalog`: every game project with its launcher data.
- `GET api/games/primitives`: the engine primitive keys by kind.
- `GET api/games/{projectId}/rules` and `PUT api/games/{projectId}/rules`: the own rule rows of one game (saving
  requires `userConfirmed`).

## Game Studio scene revisions in Chat (5.5.8)

The **Game Studio revisions** DevExpress list in the regular Chat game console is populated by `IGameProjectService.ListPlayableRevisionsAsync` from approved `LocalGptProjectRevision` business objects. It includes earlier completed revisions, not only the current project version. **Play revision** passes the selected project and exact revision IDs to `LaunchRevisionAsync`, then opens the ordinary shared Chat game screen. No example scene JSON is loaded into this workflow. Scene data comes from the selected revision's `ProjectStructureJson.scene`; an empty, unapproved, malformed or rejected scene cannot be played.

The same `CouncilGameSessionService` runs the session and obtains approval from `CouncilGameDirectorService` before each human/AI control. The `GameSceneEngine` then advances the authored world clock at its configured fixed tick rate; ASCII comes from the engine's scene preview and Pixel mode draws the very same scene, sprites, palette, parallax, actors and HUD using the existing Game Studio canvas renderer. One authored `GameSceneActor` may have `playerControlled: true` in the scene's business-object JSON: controls then move this actor through collision-checked world tiles while the camera follows. Other scenes keep free-camera navigation. The curator rejects more than one directly controlled actor; no game-specific character type is hardcoded. The `director.prediction` rule rows of engine-run games are excluded from this scene-driven path.

`src/LocalGPT/GameTemplates/GodotSceneRuntime.cs.txt` is **not** a C# application source file to compile into the LocalGPT server: it is an embedded C# script template for a separate Godot .NET project. `GameGodotWorkspaceService` reads the embedded resource when initializing a Godot worktree and writes its contents as `src/LocalGptSceneRuntime.cs`, referenced by `scenes/Main.tscn`. That **generated** `.cs` is compiled by Godot's C# project on the user's machine. LocalGPT's live Chat runtime instead compiles and executes `GameSceneEngine.cs` without requiring Godot assemblies. The sample in `docs/examples/game-studio-parallax-scene.json` is documentation only and never a runtime seed.

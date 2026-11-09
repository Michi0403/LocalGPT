# LocalGPT 7.0.0 — generic game engine, games as declarative content, DB-owned runtime keys

7.0.0 is one large drop. It removes all game-specific code from the engine, moves the three runtime key enums into
the database, and removes hidden static helpers. The four shipped games keep their behaviour; they are now ordinary
Game projects whose rules live in database tables.

**Not compiled.** No .NET SDK was available while this drop was written. Every edit was checked by reading and by
the static emulations listed at the end. Build the solution once and expect a small number of compile fixes.

## 1. No hidden static helpers

- The static local functions and static lambdas inside methods are gone. They are now private members of their
  services.
- `build/Invoke-ArchitectureAudit.ps1 -Mode static` now also reports static local functions, static lambdas and
  static anonymous delegates. Outside `Program*.cs` and the approved `*Extensions` classes, nothing static remains.

## 2. Runtime keys, collections and patterns are database rows

- The enums `LocalGptRuntimeValue` (231 members), `LocalGptRuntimeCollection` (27) and `LocalGptRuntimePattern`
  (138) are deleted.
- Every runtime value, collection and regex pattern is a database row looked up by its string key:
  - values and collections are `SystemVariables` rows (`DataType System.String[]` marks a collection);
  - patterns are `RegexPatterns` rows, using their `Name` (for example `builtin.name-cleaner`).
- The policy store loads all rows; you add or change keys in the database without new code. A database-only regex
  that does not compile is skipped and logged.
- `LocalGptRuntimePolicyModels.cs` is split into one file per type.

## 3. One generic game engine

`IGameEngineRuntimeService` and its primitives replace the game-specific partials of `CouncilGameSessionService`.

**Deleted files**
- `CouncilGameSessionService.Rules.cs`, `.Combat.cs`, `.Campaign.cs` and `.Rendering.cs`
- `CouncilGameSessionService.KernelTournament.cs`, `.KernelTournamentRules.cs` and `.KernelTournamentBattleView.cs`
- `MultiModelCouncilService.KernelTournament.cs`
- The grouped model files `CouncilGameModels.cs`, `CouncilGameDirectorModels.cs`, `CouncilGameDisplayModels.cs`,
  `CouncilAsciiColorModels.cs`, `CouncilServiceStateModels.cs` and `GameProjectModels.cs`. Every remaining type has
  its own file.
- All tournament, enemy and level types (`KernelTournamentSpritePolicy`, `CouncilGameLevelProfile`, the tournament
  entrant and state types), `ICouncilGameSubdirector` and its implementations, and the `RuntimeProfile` switch.

**Engine primitives**, each in its own file under `Services/GameEngine`:

| Kind | Keys |
| --- | --- |
| Maps | `rooms-and-corridors`, `fixed-rooms-arena` |
| Actions | `turn`, `move-relative`, `move-grid`, `hitscan`, `toggle-flag`, `set-text`, `use-marker` |
| Rules | `clear-move-block`, `normalize-facing`, `decay-variable`, `set-variable`, `marker-goal`, `chase-and-contact` |
| Styles | `character-style`, `token-style`, `line-style` |
| Autoplay | `action-cycle`, `hunt-and-extract` |
| Views | `text-template`, `first-person-raycast`, `duel-stage` |
| Council progressions | `single-elimination-team-duel` |

The duel module (rigs, roster, bracket, exchange resolver, storyboard, battle and classic renderers, summary) is
generic. The terms owner and member stand for whatever the game calls them; trainer and creature exist only in the
tournament's content.

**Council games.** A Council-driven game has `TurnModelKey` `council`. `ICouncilGameSessionService.AdvanceCouncilTurnAsync`
replaces `AdvanceKernelTournamentAsync`. `IGameCouncilBridgeService` connects a Council run to its game:
- it builds the lineup from the role answers named by the game's `council.lineup` row;
- it limits a round to the active entrants;
- it runs the engine step;
- it plans the sprite check.

The execution mode `SystemKernelTournamentResolution` is renamed `SystemGameEngineResolution`. The old name is still
accepted when an older team export is imported. That alias is the only remaining old game name in code; it can be
removed if old exports no longer matter.

## 4. Games are content

**Profile.** A Game project profile names its primitives and parameters: `ViewKey`, `TurnModelKey`,
`MapGeneratorKey`, `AutoplayKey`, `PlayerArchetypeKey`, `RulesetTemplateKey` and the matching `*ParametersJson`
columns.

**Rule tables.** Seven tables hold the rules:
- stats
- archetypes (actors, enemies and rigs)
- actions (keyboard and gamepad labels)
- rules (trigger + primitive + JSON parameters)
- levels
- texts (every visible line and prompt)
- aliases

`GameRulesetStoreService` composes the ruleset. A game with a `RulesetTemplateKey` inherits every table it leaves
empty.

**Shipped games.** The four games are seeded from embedded documents in `GameTemplates/Seeds`:

| Game key | View | Notes |
| --- | --- | --- |
| `ascii-doom` | first-person raycast | rooms-and-corridors campaign, hunt-and-extract autoplay |
| `ascii-arena` | first-person raycast | fixed-rooms arena |
| `green-dragon` | text template | story actions, action-cycle autoplay |
| `kernel-creature-tournament` | duel stage | Council-driven single-elimination team duel |

The tournament's content includes:
- 19 storyboard frames;
- the move and trainer-action tables;
- the trainer and creature rigs and the arena palette;
- the sprite-check prompt;
- the timeline, summary and view texts.

All of it matches the 6.9.9 strings and numbers. The sprite-check settings that used to be in the runtime value
`KernelTournamentSpritePolicyJson` are now the tournament's `asset.*` texts and the `assetCheck` lineup parameters.
The old database row is no longer read; delete it on the runtime-policy page if you want.

`GameProjectSeedService` refreshes an untouched system game when its seed version rises. It never overwrites a game
you edited (`IsUserModified`).

**Advisory GameDirector subdirectors** are `director.prediction` rule rows. Creature counts, archetypes and texts are
content. `CouncilGameDirectorService` still has the final say.

**Your own games.** Build them with the same tables:
- **Projects page:** edit the primitive keys, the JSON parameters and the complete rule rows (JSON rules editor).
- **REST:**
  - `GET api/games/catalog`
  - `GET api/games/primitives`
  - `GET api/games/{projectId}/rules` and `PUT api/games/{projectId}/rules` (the PUT requires `userConfirmed`)

`docs/reference/ascii-game-authoring.md` describes triggers, tokens and value specifications.

## 5. Team configuration drives Council games

`CouncilWorkflowStepDefinition` has its own file and new step settings:
- `GameEngineInitializes`: this step starts the game.
- `LimitToActiveGameEntrants`: only the active pair answers in this round.
- `ContextStepKeys`, `ContextIdentityStepKeys`, `ContextIdentityStepLimit` and `ContextRecentStepLimit`: which
  earlier answers a role sees.

The shipped tournament team uses them. Before 7.0.0 the same behaviour was hard-coded by step key.

## 6. Chat game console

- The launcher lists the game catalog instead of fixed start buttons.
- Keyboard, gamepad and touch controls come from the game's action bindings:
  - `IGameInputMapService` turns the binding labels into a browser input map;
  - `IGameTouchLayoutService` builds the touch pad from axis aliases and `touch.*` texts.
  - `ChatGameConsole.razor.js` no longer names any game action.
- The console CSS class `chat-game-console-tournament` is now `chat-game-console-council-stage`.

## 7. Database migration `20261108200000_AddGameEngineRules`

- Adds the seven rule tables and the profile columns.
- Maps existing Game project profiles to a rules template: `Corridor` → `ascii-arena`, `Story` → `green-dragon`. The
  `RuntimeProfile` column is dropped.
- Renames the execution mode in stored team steps.
- Adds the new step settings to the shipped tournament team.
- Renames the old `pokemon-tournament` seed key.

`Down` restores the `RuntimeProfile` column from the view key, reverses the mode rename and drops the new tables and
columns.

## 8. Changes you may notice

- Runtime-class short aliases `doom.*` and `lotgd.*` are now `ascii-doom.*` and `green-dragon.*`. The full
  `games.ascii.*` keys are unchanged, and team roles keep working.
- Green Dragon: `left`/`A` and `right`/`D` now select the previous and next choice (`turn-left`, `turn-right`). Before,
  they became `strafe-left`/`strafe-right`, which Green Dragon rejected.
- ASCII DOOM keeps D-pad support through its gamepad labels ("… / D-pad Up").
- The team editor's loop-marker example now reads `[[GAME_COMPLETE]]`. The tournament itself still uses
  `[[TOURNAMENT_COMPLETE]]` and `[[TOURNAMENT_CONTINUE]]`.

## 9. Guards and documentation

- `Assert-CouncilXRoundWiring.ps1` checks the generic console class and `MultiModelCouncilService.GameEngineResolution.cs`.
- The JavaScript diagnostics manifest hash for the reviewed `ChatGameConsole.razor.js` is refreshed.
- `docs/reference/ascii-game-authoring.md` has a new section, "Games as declarative content".

## Checks performed (no compiler)

- Brace, parenthesis and bracket balance of every changed C# file.
- XML documentation:
  - an emulated CS1591/CS1572/CS1573 check on changed files, including primary-constructor parameters;
  - every interface member;
  - file-level summary coverage.
- Interface implementation completeness, and calls on injected interfaces, across the whole application.
- Property access on the main game types.
- Emulated guards:
  - static policy, including local functions and lambdas;
  - Razor attribute expressions and DevExpress native controls;
  - text-service ownership;
  - localization literals;
  - EF snapshot consistency;
  - embedded resources;
  - discarded async work.
- Every guard path and token list in `build/*.ps1` resolves.
- `node --check` on the console module.
- Seed documents:
  - every primitive is registered and every trigger is known;
  - every parameter name matches its reader (lineup, sprite, storyboard, rigs, palette, style, director);
  - the sprite prompt and drawer texts are byte-identical to 6.9.9.

## Version

`6.9.9` → `7.0.0`.

# LocalGPT 6.9.3 — Member-drawn Kernel Creature sprites and ANSI battle view

## Requirement

The Kernel Creature Tournament keeps no engine-invented creature art. Every creature image is drawn by its own Kernel Creature Council member. The tournament is presented as a colored, handheld-battle style screen; the 70s ASCII look stays for the other games.

## Member-drawn sprites

- Workflow step `creature-introduction` (seed `kernel-creature-tournament`) now asks each Kernel Creature member to output, after `NAME/FORM/TRAIT/VOICE`:
  - `COLORS: body=N shade=N accent=N eye=N outline=N` (xterm-256 numbers),
  - `SPRITE:` followed by 8–16 rows of 8–20 pixel characters: `.` empty, `#` outline, `+` body, `-` shade, `*` accent, `o` eye.
- `MultiModelCouncilService.KernelTournament.ReadSprite` reads the member's own output (after HTML decoding). Common aliases are normalized (`@`/`X` outline, `=`/`█` body, `~`/`:`/`▓`/`░` shade, `%`/`^`/`▒` accent, `O`/`0` eye). Empty border rows/columns are trimmed; the result is limited to 20×16 pixels. If the rows are not legend pixels but printable ASCII, they are kept as member-authored ASCII art (`SpriteKind = ascii`).
- Reserve partners (`… Echo`, `… Vanguard`) keep the member's drawing; only the member's own palette entries are rearranged.
- No usable sprite → a neutral grey silhouette with `?` and the label `no sprite drawn by <model>`. The engine never substitutes invented creature art.
- New BusinessObject fields: `CouncilKernelTournamentContestantSeed.CreatureSprite/CreatureSpriteKind/CreatureSpritePalette` and `CouncilKernelTournamentCreatureState.SpriteRows/SpriteKind/SpritePalette/SpriteAuthorModelName` (in-memory/JSON session state; no EF schema change). Snapshot cloning copies them.

## ANSI battle view

- New `CouncilGameSessionService.KernelTournamentBattleView.cs` renders the 168×44 tournament session as an indexed-256 screen with per-cell style runs: title bar, sky/grass field, opponent top-right and player bottom-left on platforms, cream info boxes (name, species, colored HP bar, roster balls, switches, trainer command), double-line message box (consequence, trainer lines, timeline) and a trainer-command panel.
- Poses animate the member sprite: attack lunges toward the foe, hit flashes white and recoils, victory hops, a fainted creature turns grey.
- `MakeFrame` dispatches to the battle view; the previous ASCII arena is kept as `MakeClassicFrame` and used only for terminals smaller than 72×26. ASCII Team Artist frames keep the semantic colorizer.
- Tournament console CSS: `text-shadow` disabled for the tournament screen so colored block pixels do not bleed.

## Seed propagation

`CouncilTeamConfigurationService.CurrentSeedVersion` 49 → 50, so an untouched system seed of the Kernel Creature Tournament team receives the new introduction prompt on the next start. A team the user edited keeps its own prompt (existing lossless seed policy); add the `COLORS`/`SPRITE` lines there or restore the default template. A tournament session that is already running keeps its existing creatures; sprites appear from the next tournament start.

## Version

`6.9.2` → `6.9.3`.

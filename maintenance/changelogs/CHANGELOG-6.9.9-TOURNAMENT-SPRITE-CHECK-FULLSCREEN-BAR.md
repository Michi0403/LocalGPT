# LocalGPT 6.9.9 — tournament sprite check, fullscreen control bar, JSON marker detection

## 1. Kernel Creature Tournament: sprite check before the lineup

Creatures without a drawn sprite showed the grey "no sprite drawn by …" placeholder for the whole tournament, and nothing noticed it. Two creatures could also show the same drawing.

Before the lineup (`arena-lineup-engine`), LocalGPT now checks every creature's sprite:

- **Missing:** the creature has no usable sprite (nothing drawn, wrong characters, or no shape).
- **Copy:** the sprite matches an earlier creature's sprite in at least 85 % of its drawn cells. The earlier creature keeps its drawing.

For each such creature, a short drawing turn runs:

1. The creature member itself draws again.
2. If that fails, the members of the team's artist role draw it, one after another. That is the role of the `team-building-ascii` step, "ASCII Team Artist" in the shipped team.

Each turn sees the other creatures' sprites and is told not to copy or mirror them. A drawing is accepted only if it is a usable sprite and not a copy of any other creature's. The turns appear in the transcript and the lanes as "Creature sprite check · <creature> · <n>". They run with tools off and only use members of this run. When an artist draws, the creature keeps the drawing and the game records the artist as its author.

Settings in the new runtime value `KernelTournamentSpritePolicyJson` (`KernelTournamentSpritePolicy`):

| Setting | Default |
|---|---|
| `maximumDrawAttempts` | 3 turns per creature; 0 switches the check off |
| `copySimilarity` | 0.85 |
| `phase`, `promptTemplate`, `selfDrawerLine`, `artistDrawerLine`, `noOtherSpritesText` | editable labels and prompt |

Existing databases receive the value through the normal start-up seeding of missing runtime values. Logs: `Kernel Creature Tournament sprite check for run … has no usable sprite / a copy …` and `… drawing n … by … was accepted / not a usable sprite / too similar …`.

## 2. Game console fullscreen: control bar above the game

In fullscreen the compact control bar (Chat, Game, operator, Fullscreen, End game, Close) floated over the top of the arena and covered its title row, while the same height stayed empty below the status bar. The bar now has its own row above the game, as outside fullscreen. The arena uses exactly the space below it.

## 3. JSON detection ignores marker text

Chat text containing markers such as `[[TOURNAMENT_CONTINUE]]` looked like a JSON array to `StructuredTextTranslationService.TranslateJson`. The parser then threw `'T' is an invalid start of a value`, which was caught but stopped the Visual Studio debugger on every tournament turn. The bracket check now also rejects bare words outside strings (anything other than `true`, `false`, `null` or a number exponent) before calling the parser. Valid JSON is translated as before.

## Version

`6.9.8` → `6.9.9`.

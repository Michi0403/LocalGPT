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

The same presentation contract applies to maintained corridor/DOOM, Green Dragon, Kernel Creature Tournament and hot-seat experiences and to Project-built/custom prompted ASCII games. A Project Game profile persists its color mode and default foreground/background values; the compiled `ProjectGameDefinition` carries them into the runtime session. No game needs a renderer-specific state model to gain color.

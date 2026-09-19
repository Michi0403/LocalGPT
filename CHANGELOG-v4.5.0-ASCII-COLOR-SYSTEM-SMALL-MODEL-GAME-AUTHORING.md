# LocalGPT 4.5.0 — ASCII color system and small-model game authoring

## Shared ASCII color contract

- Added `CouncilAsciiColorMode` with `TerminalDefault`, `Ansi16` and `Indexed256` modes.
- Added separate presentation models for text styles, bounded horizontal style runs, frame presentation and compact palette discovery.
- Kept canonical frame/animation text free of ANSI/control escapes and HTML. Styling is metadata and never becomes game state.
- Made `TerminalDefault` the compatibility default: unstyled cells inherit the exact existing LocalGPT black/green terminal surface, while explicit style runs can still use indexed-256 colors. ANSI-16 falls back to bright-green foreground `10`; indexed-256 falls back to green `46`; both use background `0`.
- Normalized ANSI-16 indexes to `0..15`, indexed-256 indexes to `0..255`, clipped style coordinates to the fixed surface, and bounded style-run transport to 4096 runs per frame.

## Existing games and modes

- ASCII corridor/DOOM deterministic frames now receive semantic colors for terminal borders, enemies, player, exit, HP, ammo and selected HUD/control/combat tokens while the raycast glyph text remains authoritative.
- Green Dragon/story frames receive semantic colors for the title, DRAGON marker and numbered choices while story text remains plain canonical ASCII.
- Kernel Creature Tournament frames and one-shot movies receive semantic title/round/fighter/HP/impact/bracket/champion colors plus held subtitle styling while the deterministic tournament engine continues to own all HP, damage, guard/recovery, advancement and winner state.
- The maintained two-human hot-seat ASCII showcase receives the palette function and bounded regex/knowledge helpers; its prompts now require stable semantic colors without making the presentation host authoritative for multiplayer rules.
- Human, Shared and AI control modes continue to use the same game/session contracts; color does not create a second controller path.

## Open custom/prompted Game-project support

- Added `AsciiColorMode`, `DefaultForegroundColor` and `DefaultBackgroundColor` to the Project-owned Game authoring profile, compiled project definition and profile-save request.
- Extended the `/projects` Game-project workbench so humans can select terminal-default/ANSI-16/indexed-256 presentation, edit bounded foreground/background indexes, and reload those values from saved profiles or compiled definitions.
- `GameProjectService` validates and clamps palette settings, compiles them into the build artifact, validates them on read, and carries them into `project.game.start`.
- Added EF migration `20260919133000_AddGameProjectAsciiColor` and matching model-snapshot fields.
- Custom games therefore use the same renderer/session architecture as maintained built-ins instead of hard-coded game-name color paths.

## AI/DXFunction authoring surface

- Added read-only `localgpt.game.display.palette.get` with named ANSI-16 entries, indexed-256 range, explicit per-mode green defaults/terminal-theme inheritance, and compact authoring rules.
- Added optional `style` objects to display text, cell, fill and blit functions.
- Added optional palette/default fields and bounded style runs to full-frame submission.
- Added optional palette/default fields, per-frame style runs and subtitle style to pregenerated animation submission.
- Display readback now returns active palette defaults and clipped/rebased style runs together with plain text.
- `localgpt.ascii.surface.get` advertises ANSI-16/indexed-256 support, plain-text canonical behavior and the corresponding evidence functions.

## Browser rendering and compatibility

- Added safe ANSI-16 and xterm-256 palette conversion to `localgpt-game-console.js`.
- Styled frames are rendered from text nodes and `<span>` elements; the renderer does not interpret model-authored HTML or ANSI escape codes.
- Static game frames and browser-local animation frames share the same style contract.
- Conversation/Operator text remains plain terminal text; game styling is additive and does not rewrite transcript history.
- `@rendermode InteractiveServer` routes and the existing Chat/ASCII shell remain unchanged in intent and are covered by source validation.

## Small local model support

- Updated Game Project Discovery, Game Project Development, Game Engine Extension, Game Project Playtest and ASCII hot-seat workflows to keep 0.8B-2B roles narrow and evidence-driven.
- Project requirements/profile/build records are treated as the authoritative design source; palette/surface functions provide rendering contracts; `localgpt.knowledge.list` supplies reusable LocalGPT facts; `localgpt.regex.list/get/test` supplies tested parsing rules only when needed.
- Added `docs/reference/ascii-game-authoring.md` to the approved Council knowledge set and embedded it for publish layouts, so small models can retrieve the same compact guidance without broad repository context.
- Guidance favors styled text/blit/fill operations when they are cheaper than calculating many coordinate runs and explicitly forbids raw ANSI escape generation.

## Versioning and validation

- Advanced LocalGPT from 4.4.9 to 4.5.0 per the maintained single-digit minor/patch slot policy; no `4.4.10` release was introduced.
- Advanced maintained Council team seed data to 33 and runtime-class seed data to 7.
- Updated browser cache keys, application/package versions, user-agent/documentation version references and JavaScript diagnostics manifest.
- Validation is source-only by request: no .NET build, restore, publish or GitHub/online repository access was used.

# Runtime-class ASCII game presets

LocalGPT 2.0.2 adds configuration examples that reuse the existing Council workflow instead of adding a second game engine.

## ASCII DOOM Council Adventure

This preset is intentionally not a conventional real-time 3D renderer. The Map Architect creates an original room graph, the Player Controller emits one intent, World Actor members each own one active runtime-class instance, the State Judge resolves one meaningful world step, and exactly one ASCII Frame Renderer emits the complete fixed-width frame.

The optional `id-Software/DOOM` learning source can be imported individually with `--repo id-Software/DOOM`; it is source-study material. The preset does not claim to execute the original C engine and does not redistribute or require commercial WAD data for its generated maps.

## Green Dragon Runtime Story

This preset treats world state, locations/houses, NPCs, events, the player, and the terminal frame as separate runtime classes. Bounded Council members act only as their assigned instance. A Story Director orchestrates continuity, a State Keeper resolves the canonical turn, and exactly one renderer builds the terminal scene.

The optional `lotgd/lotgd` repository can be imported individually with `--repo lotgd/lotgd`; it is a learning and configuration reference. Runtime play does not require the PHP application or copied story text.

## Runtime-class field ownership

Each database-backed runtime class describes every field with:

- data type and default value;
- AI and human assignability;
- optional or required human ownership;
- whether missing required human input blocks the dependent next round;
- keyboard and gamepad binding metadata;
- recommended DXFunctions and optional source references.

Roles select runtime classes in the Council Team editor. The same editor exposes categorized best-use DXFunctions. Runtime definitions are available to AIs through `localgpt.runtime-class.list` and `localgpt.runtime-class.get`.

## ASCII frame contract

A frame-producing workflow step must use a single-member execution mode. The renderer emits:

```text
[[ASCII_FRAME width=80 height=25]]
<one complete fixed-width frame>
[[/ASCII_FRAME]]
```

The chat renderer converts this marker into a stable, non-wrapping terminal panel. Large or still-live Council streams skip automatic structured-JSON translation so the Blazor circuit and Stop control remain responsive; explicit structured-text controller and DXFunction calls remain available.


## Individual source imports

The installer keeps these large learning sources out of the bulk recommended list. Import only what is useful:

```text
--setup-learning-base --repo id-Software/DOOM
--setup-learning-base --repo lotgd/lotgd
--setup-learning-base --repo php/doc-en
--setup-learning-base --repo llvm/llvm-project
```

The PHP and LLVM/Clang repositories are optional language-level study material for the PHP and C-family code used by the game references.

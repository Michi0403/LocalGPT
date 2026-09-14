# Embedded planning, GameDirector, and creative runtimes

## Embedded firmware planning

LocalGPT can turn a board description, pin layout, sensor/actuator roles, transport requirements, and policy constraints into a reviewable firmware plan.

The planner is transport-neutral. GPIO, ADC, PWM, I²C, SPI, UART, CAN, RS-485, physical 1-Wire, and LocalGPT logical telemetry are capabilities—not assumptions.

A plan can include:

- board and pin assignments;
- electrical or protocol findings;
- firmware module layout;
- telemetry contract;
- generated source artifacts;
- compiler and flashing prerequisites.

Planning, compilation, serial access, flashing, and actuator control are distinct operations. The later stages require the matching workspace and approval checks.

## Organic 1-Wire

LocalGPT's “organic 1-Wire” is an application protocol for approved peers, publishers, add-ons, and devices. It is not limited to the Dallas/Maxim physical bus. Transport adapters can use TCP, HTTP/JSON, or a gateway while the application contract keeps identity, replay protection, approval, and capability routing explicit.

## GameDirector

GameDirector is the authoritative game engine. Player controllers, creature Councils, and reactive map objects submit proposals; they do not mutate world state directly.

The deterministic resolver validates legality, turn order, movement, and state transitions. A configured model can enrich narration or predict consequences without owning the authoritative frame.

Runtime classes describe sessions, maps, players, directors, creatures, objects, and frames. Council roles can be assigned to world actors while the actual transition remains inside GameDirector.

## ASCII play surface

The Chat ASCII console is both a normal conversation companion and a fixed-cell game/presentation surface. Council game sessions expose their own bounded live dimensions, while legacy deterministic game frames remain compatible with the established 80×25 path. Renderers can inspect the current display, update a single cell, write text, fill or blit a region, submit a complete frame, or submit a pregenerated 2–12 frame animation for browser-local playback. Presentation state never replaces the canonical conversation transcript or authoritative game state.

### ASCII DOOM Council Adventure

The preseeded `ascii-doom-council-adventure` team now treats `CouncilGameSessionService` as the only game engine. Starting the team creates or recovers one human-owned deterministic corridor session with a connected map, persistent hostile actors, opening line-of-sight contact, ammunition/health, extraction coordinates, collision feedback, and a tactical radar. The Council observes and explains that state; it does not run a second 24-turn model-driven simulation.

Human control is the default and AI-origin movement is rejected while the session remains in Human mode. Selecting **AI hunter** or **Human + AI hunter** in the Game tab is an explicit ownership choice. The hunter reads the authoritative map, turns toward visible hostiles, shoots through validated line of sight, pathfinds around walls and occupied hostile cells, and routes to extraction after combat. Shooting, enemy HP, enemy pursuit/contact attacks, death, and extraction are deterministic service-owned transitions rather than model narration.

`localgpt.game.session.get` and `localgpt.game.display.get` can resolve the active conversation game when `sessionId` is omitted, so Council members must not guess GUIDs or ask the human for internal session identity. The renderer mirrors the canonical service-owned frame; optional decoration and 2–12-frame browser-local animations remain presentation-only.

### ASCII Hot Seat — Neon Relay Arena

The preseeded `ascii-hot-seat-showcase` Council team is a two-player local hot-seat demonstration. `Player 1 Hot Seat` and `Player 2 Hot Seat` are `HumanOnly` Council roles, so LocalGPT pauses for each person in turn instead of simulating a missing player. One AI `Arena Referee` resolves both commands deterministically and emits the canonical `HOTSEAT_STATE`; a separate `ASCII Display Director` owns all terminal mutations.

The showcase intentionally exercises the ASCII stack: full-frame intro rendering, incremental text/cell/fill/blit updates, display readback for continuity, a score/HUD, sprites, and short pregenerated animations. The underlying Council game session is used only as the supported display host; the hot-seat multiplayer state remains referee-owned Council state and is not advanced through the single-player `localgpt.game.control` path.

## Minecraft Mod AI Builder

The Minecraft builder applies the same project principles: source knowledge is versioned, generated artifacts are reviewable, dependencies are explicit, and a generated mod/datapack is not presented as tested until the correct toolchain has actually built or validated it.

Historical source comparisons and early game presets are retained as internal notes, while this page describes the maintained runtime contract.

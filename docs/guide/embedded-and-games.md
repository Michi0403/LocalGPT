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

The Chat ASCII console presents a stable 80×25 authoritative frame with responsive Fit, Width, and Native modes. It can be mounted or closed without deleting the underlying session. Fullscreen exit occurs before the component is removed so the normal conversation layout is restored correctly.

## Minecraft Mod AI Builder

The Minecraft builder applies the same project principles: source knowledge is versioned, generated artifacts are reviewable, dependencies are explicit, and a generated mod/datapack is not presented as tested until the correct toolchain has actually built or validated it.

Historical source comparisons and early game presets are retained as internal notes, while this page describes the maintained runtime contract.

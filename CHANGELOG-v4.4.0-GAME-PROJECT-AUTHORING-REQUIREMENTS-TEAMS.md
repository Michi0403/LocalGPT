# LocalGPT 4.4.0 — Game-project authoring, requirements, migrations, and team presets

## Added

- Promoted Game-project authoring from a transient build form into first-class Project-system persistence with `LocalGptGameProjectProfile`.
- Added EF migration `20260916204500_AddGameProjectAuthoringProfiles` plus matching `LocalGptMemoryDbContextModelSnapshot` and one-to-one `LocalGptProject.GameProfile` mapping.
- Added the persisted Game profile to `LocalGptProjectDetails`, so the normal Project read model carries the project-owned game design alongside revisions, requirements, artifacts and workspace state.
- Added separate approval contracts for **saving game design** (`SaveGameProjectProfileRequest`) and **building** (`BuildProjectGameRequest`). A build no longer accepts editable design fields and cannot silently change the saved project design.
- Added build traceability in `ProjectGameDefinition`: source authoring-profile id, current project revision id, and the ids of user-approved Project requirements visible when the definition was compiled.
- Added `project.game.profile.get` and approval-gated `project.game.profile.save` DX functions plus matching Project HTTP endpoints.
- Added maintained Game-project Council presets:
  - `game-project-discovery` — collects user intent and persists only confirmed Project requirements;
  - `game-project-development` — develops against the saved design and approved requirement baseline;
  - `game-engine-extension-development` — handles reusable lower-layer engine work with explicit migration/architecture review;
  - `game-project-playtest` — tests built artifacts against durable requirements using canonical runtime state.
- Added Projects workbench **Save Game Design** behavior and made **Build Game Definition** operate only on a persisted design profile.

## Architecture

The ownership direction is now explicit:

`Project system -> requirements/revisions/game profile -> ProjectGameDefinition build artifact -> GameDirector/runtime -> input/render adapters`

The runtime remains below the Project layer. It receives a compiled definition and never owns editable project design, user requirements, revisions, or build policy. ASCII remains the primary renderer/control surface, not the owner of the engine or project model.

## Migration and compatibility

The new authoring profile is additive. Existing databases receive the dedicated table through the normal EF migration sequence; existing project/build/session data is not rewritten. The migration uses a restrictive Project foreign key, a unique `ProjectId` index, and an update-time index. The maintained EF snapshot architecture guard was strengthened to verify the singular `GameProfile` navigation and mapping rather than weakened or bypassed.

The legacy database compatibility service does not require a synthetic legacy signature for this new migration: databases that do not yet have the table reach the migration as a normal missing later schema step and EF applies it in order.

## Preserved from 4.3.8 / 4.3.9

- InteractiveServer component/page contracts and the exact operational diagnostics boundary remain intact.
- ASCII Chat/Game/Operator fullscreen, in-ASCII Council/human interaction, direct game controls, slider commit behavior, `/chat` scrolling/sidebar stability, and Council-owned game cleanup remain intact.
- Project-built games still launch from ASCII Operator mode using `:game project <project name or id>`.
- Built-in corridor and story games remain compatible while project-built games continue to select lower runtime behavior through `CouncilGameRuntimeProfile` rather than hard-coded game-name switches.

## Versioning

The previous working release line was 4.3.9. Per the repository version rule, the next release is **4.4.0**, not 4.3.10.

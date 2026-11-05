# LocalGPT 4.3.9 — Game project build/runtime boundary

LocalGPT 4.3.9 turns the already-existing ASCII game engine/runtime into a first-class **Project-system consumer** instead of letting runtime game identities remain the authoring boundary.

## Added

- `ProjectType = Game` is now a first-class project-authoring mode in the Projects workbench.
- Added the serializable `ProjectGameDefinition` build output and `CouncilGameRuntimeProfile` contract.
- Added `IGameProjectService` / `GameProjectService` so project metadata, revisions, requirements, workspace content and artifacts remain above the runtime boundary.
- Game builds are persisted through the existing approved project-artifact system as `GameBuild / Runtime Definition` (`application/vnd.localgpt.game+json`), so no parallel persistence model or EF schema migration was introduced.
- Added Projects-page Game build controls for runtime profile, game identity, Council/default controller settings, frame geometry, map seed and scenario.
- Added `project.game.get`, `project.game.build`, and `project.game.start` DX functions. Builds use the existing one-use human approval path; starting is coordination-only and consumes an already-approved build.
- Added `/api/projects/{projectId}/game/build` GET/POST and `/api/projects/{projectId}/game/start` endpoints.
- Added ASCII Operator launch support: `:game project <project name or id>` starts the latest approved project build directly inside the existing ASCII terminal.

## Runtime layering

- The Project system owns game authoring and the compiled game definition.
- GameDirector / `CouncilGameSessionService` consumes that definition and owns only authoritative runtime/session state.
- Rules/rendering are selected through the built definition's runtime profile rather than requiring runtime behavior to branch on the historical `ascii-doom` / `green-dragon` keys.
- The two existing built-in games remain backwards-compatible launch paths. Project-built games may use their own stable game key while targeting the existing `Corridor` or `Story` runtime profiles.
- Project/version identity is carried into game snapshots so runtime sessions can be correlated to the build that produced them.

## Preserved

The 4.3.8 architecture repairs and requested ASCII behavior remain in place: maintained `SafeErrorBoundary @key="NavigationManager.Uri"`, `InteractiveServer`, service-owned text/operator parsing, no new application-static helpers, repository async-continuation policy, in-ASCII human/AI interaction, fullscreen Chat/Game/Operator surfaces, sidebar-safe game state, slider commit behavior, and Council-owned runtime cleanup.

## Validation scope

This handoff was validated with the repository's source-side Python architecture, resilience, async-continuation and XML-documentation audits plus a direct equivalent of the text-service ownership guard and a dedicated 4.3.9 release audit. No `dotnet` restore/build/publish and no GitHub or online repository access were used.

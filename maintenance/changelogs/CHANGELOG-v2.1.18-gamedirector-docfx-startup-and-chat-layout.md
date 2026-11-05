# LocalGPT 2.1.18 — authoritative GameDirector, generated documentation, startup seed correction and Chat workspace sizing

## Runtime games

- Every human or AI controller action is now a proposal. `CouncilGameSessionService` asks `ICouncilGameDirectorService` for a decision before it mutates the authoritative session.
- The decision carries the expected turn, normalized action, approval reason and bounded creature/reactive-object predictions. A second turn check prevents a reviewed proposal from being applied after the world has already advanced.
- Creature and reactive-object subdirectors are separate DI registrations. A bounded actor-runtime factory maps creature and reactive-object instances to `World Actor` Council assignment slots without granting those actors state-mutation authority. Runtime-class seeds now include `games.ascii.doom.director`, `.creature` and `.reactive-object` beside the existing session, map, player, controller, actor and frame contracts.
- The game-start DXFunction accepts `directorMode`, `gameDirectorModelName` and `creatureDirectorCount`. Deterministic authority remains final; `CouncilModelPreferred` records the user-selected low-parameter reviewer without allowing a model to bypass legal-action or turn checks.
- Added read-only previews through `POST /api/council/games/control/preview` and `localgpt.game.control.preview`.

## Startup database seeding

- Existing LocalGPT Core and Humanitarian projects are now read with `AsNoTracking`.
- Seed helpers still inspect the complete existing child collections, but only genuinely missing child rows are attached as `Added` records.
- Existing project parents and existing child rows are never attached for update during deterministic startup seeding. This removes the stale tracked-row update that produced repeated `DbUpdateConcurrencyException` messages while preserving durable user values.
- The bounded concurrency reconciliation remains as a defensive fallback for genuinely concurrent writers.

## Chat and 4K layout

- Expanded Chat Configuration and Running Session Tools now become large viewport workspaces with one internal scroll owner instead of consuming a short 24–42 dVH ribbon.
- The transcript keeps the available page space at normal 100% browser zoom.
- Visible DevExpress Chat configuration surfaces and dialogs are marked by the existing guarded browser adapter and receive large responsive dimensions. Narrow screens use an almost full-screen layout.

## XML comments and DocFX

- `LocalGPT.csproj` emits compiler XML documentation.
- A repository-local .NET tool manifest pins DocFX 2.78.5.
- Every normal Windows LocalGPT build runs DocFX metadata and HTML generation, then attempts PDF generation with Node.js 20 or later.
- Release builds require the PDF by default. The versioned artifact is named `LocalGPT-2.1.18.pdf` and is copied into both the source web root and the active build output.
- The old Help cards are replaced by a generated-documentation launcher and live build status.
- `DocumentationCatalogService` exposes generated status, the versioned PDF and bounded XML-comment search through `DocumentationController`.
- `DocumentationTranslationAdapter` decorates XML comment text through the existing localization service. `DocumentationUpdatedAttribute` supplies the last-reviewed LocalGPT version without changing the localization service contract; unannotated legacy comments are reported honestly as `unversioned`.

## Version alignment

- Application package and runtime context: `2.1.18`
- Organic 1-Wire application advertisement: `2.1.18-organic-wire`
- The separately versioned `LocalGPT.WireProtocolVersion` package is unchanged.
- Seed history retains every prior version and appends `seed-v2.1.18`.

## Validation boundary

The source passed the repository's Python async-continuation and architecture audits, JavaScript syntax checks, JSON/XML parsing, text-service ownership emulation and C# lexical closure checks in the repair environment. A real .NET 10, Windows, DevExpress and DocFX build was not available there and remains the authoritative semantic/build validation.

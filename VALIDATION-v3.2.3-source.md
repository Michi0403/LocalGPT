# LocalGPT 3.2.3 source validation

Status: **SOURCE-NOT-COMPILED** in the preparation environment. No `dotnet`, MSBuild, NuGet restore/build/publish/pack or EF migration command was executed.

## Static checks

- Application architecture policy audit: passed.
- Async continuation audit: 255 source files; 2,904 await tokens; 2,617 `ConfigureAwait(false)`; 72 renderer-affine `ConfigureAwait(true)`; 210 explicitly configured await-using disposals; 5 configured async streams.
- Service resilience audit: 2,130 service methods own try/catch + diagnostics.
- Provider-qualified Council audit: 282 checks passed.
- Configurable Council behavior-policy audit: passed.
- X-Round/heartbeat wiring audit: passed.
- Code-generation/DXFunction wiring audit: passed.
- Provider stream repetition policy audit: 512-token long-cycle coverage and historical short-loop behavior passed.
- C# XML documentation: 9,972 direct declarations across 634 maintained C# files.
- Razor XML documentation: 45 component types / 766 direct `@code` declarations.
- Localization catalog parity: 1,994 keys in each of de-DE, en-US, es-ES, fr-FR, ja-JP and uk-UA.
- JavaScript diagnostics manifest and JavaScript syntax are checked during package validation; maintained browser files are unchanged except version cache identifiers where applicable.

## Release-specific checks

- LocalGPT, InstallerConsole and WebView wrapper versions are 3.2.3.
- SDK policy remains `10.0.400`; LocalGPT target framework remains `net10.0`; 1-Wire protocol remains 2.1.1.
- Simple JSON/OData user-function mode is backed by `user-source.*` connector/pipeline adapters; advanced pipeline mode remains present.
- Direct X Functions & automation navigation resolves to the existing Council workflow/X-Round controls.
- Learning maintenance defaults project synchronization on and returns synchronized project records.
- Chat upload source synchronization persists exact project/version/revision/workspace root/full tracked-file structure, source hash, SDK and target frameworks using existing project tables.
- Stale repository-derived runtime requirements are marked superseded/historical.
- Project briefing policy forbids invented runtime/framework/version claims when source evidence exists.
- Reported nullable `xRoundCause` recovery handoff uses `xRoundCause ?? string.Empty`.

## Protected-source checks

- `Chat.razor` SHA-256 remains `0d9ab6ed72f41eebbbf8839c54b5fda9a409d424a1fa11c87d2994352c837569`.
- `Chat.razor.css` SHA-256 remains `2a620187aa41712f53dddab92ee2ab834c4f46fe512925dce94efb387f28b0e4`.
- EF migration tree is byte-identical to the 3.2.2 baseline.

Compilation success is intentionally not asserted because a .NET/DevExpress compiler was not used.

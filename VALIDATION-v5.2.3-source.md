# LocalGPT 5.2.3 source validation

## Scope

This is a source-only architecture-gate release. No .NET compilation was performed by the assistant.

## Async-only guard verification

`build/audit_async_only_architecture.py` was executed directly against `src/LocalGPT`.

Expected result: **non-zero**, because the new rule deliberately has no baseline and exposes pre-existing violations.

Observed inventory:

- 613 reviewed Component/Service/HostedService/Interface source files.
- 3,376 findings total.
- ASYNC001: 2,743 synchronous method signatures.
- ASYNC003: 310 computed getter/property workflow calls.
- ASYNC004: 4 `.Result` accesses.
- ASYNC005: 4 `GetAwaiter().GetResult()` calls.
- ASYNC008: 76 `lock` sites.
- ASYNC009: 54 `Interlocked`/`Volatile`/`Monitor` sites.
- ASYNC011: 20 discarded async/fire-and-forget sites.
- ASYNC012: 53 Razor components missing `IAsyncDisposable`.
- ASYNC013: 55 Razor components missing `DisposeAsync`.
- ASYNC014: 1 `DisposeAsync` path not calling private `Dispose()`.
- ASYNC015: 56 Razor components missing private `Dispose()`.

Spot checks confirm the guard reports:

- `Components/Shared/LocalGptFormLayoutSurface.razor`: synchronous `EffectiveCanHide => GetEffectiveCanHideForRender()` and its synchronous helper.
- `Components/Shared/LayoutStudio.razor`: synchronous `Clone` returning `LocalGptUiLayoutSurface`.
- `Components/Shared/LocalConsoleFeed.razor`: synchronous lifecycle/event methods, `Interlocked`/`Volatile` coordination, discarded async workers, and missing async-disposal ownership.

The Razor maintenance audit was also executed after removing its old contradictory "delegate computed property to a synchronous helper" requirement and passes for all 55 maintained Razor components. The async-only audit is now the sole owner of procedural getter-call enforcement.

`Directory.Build.targets` wires the zero-baseline async-only guard into normal LocalGPT builds without a normal-build skip switch, and `build/Invoke-RepositoryValidation.ps1` invokes the same guard explicitly.

Python syntax validation and XML parsing of `Directory.Build.targets` were performed. No `dotnet`, MSBuild, NuGet restore/publish, GitHub, or online source access was used.

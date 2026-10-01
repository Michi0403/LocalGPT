# LocalGPT 5.2.3 — async-only component/service architecture gate

## Architecture rule

- Components, Services, HostedServices, and service Interfaces are zero-baseline asynchronous ownership boundaries. Declared methods must return awaitable/async-stream contracts rather than synchronous `void` or value-returning helpers.
- The only synchronous method exception is component/service-local `private void Dispose()`. Every Razor component must implement `IAsyncDisposable`, declare `DisposeAsync`, and at minimum call its private `Dispose()` helper from `DisposeAsync`.
- Computed properties/getters may not invoke workflow/render helpers synchronously. Patterns such as `private bool EffectiveCanHide => GetEffectiveCanHideForRender();` are reported; awaited lifecycle/event/service work must materialize passive render state instead.
- `.Result`, `.Wait(...)`, `WaitOne(...)`, `Task.WaitAll`, `Task.WaitAny`, `GetAwaiter().GetResult()`, `Thread.Sleep`/`Join`, `lock`, `Monitor`, `Interlocked`, `Volatile`, reset/countdown events, spin waits, reader/writer locks, and discarded async/fire-and-forget starts are rejected in the maintained async ownership layers.
- Long-lived work must be explicitly owned by an awaitable worker/hosted service or retained and awaited through async disposal. Renderer callbacks plus atomics plus recursive `_ = SomeAsync()` scheduling are not accepted as async ownership.

## Guard

- Added `build/Assert-AsyncOnlyArchitecture.ps1` and `build/audit_async_only_architecture.py`.
- Wired the guard directly after async-continuation validation and before method diagnostics in normal `LocalGPT` builds, with no normal-build skip switch and no grandfathered baseline.
- Added the same guard to `build/Invoke-RepositoryValidation.ps1`.
- Removed the contradictory Razor-maintenance rule that previously required expression-bodied computed properties to delegate to synchronous helper methods. The Razor guard now remains focused on Razor layout/diagnostics while the async-only guard owns getter/workflow-call enforcement.
- Updated `AGENTS.md` so passive render state, async disposal, and awaited ownership are the canonical repository rules.

## Initial source audit

The source-only audit currently reports **3,376 existing findings across 613 reviewed LocalGPT Component/Service/HostedService/Interface source files**. This is the requested migration inventory, not a passing-baseline claim.

- `ASYNC001`: 2,743 synchronous method signatures.
- `ASYNC003`: 310 computed getter/property workflow calls.
- `ASYNC004`: 4 `.Result` uses.
- `ASYNC005`: 4 `GetAwaiter().GetResult()` uses.
- `ASYNC008`: 76 `lock` sites.
- `ASYNC009`: 54 `Interlocked`/`Volatile`/`Monitor` coordination calls.
- `ASYNC011`: 20 discarded async/fire-and-forget starts.
- `ASYNC012`: 53 Razor components missing explicit `IAsyncDisposable` ownership.
- `ASYNC013`: 55 Razor components missing `DisposeAsync`.
- `ASYNC014`: 1 existing `DisposeAsync` path that does not call private `Dispose()`.
- `ASYNC015`: 56 Razor components missing the private `Dispose()` cleanup helper.

The audit explicitly catches the supplied examples: `LocalGptFormLayoutSurface.EffectiveCanHide => GetEffectiveCanHideForRender()`, synchronous `Clone(...)` helpers such as `LayoutStudio.Clone(LocalGptUiLayoutSurface)`, and the `LocalConsoleFeed` mix of `Interlocked`/`Volatile` with discarded `RefreshRenderAsync()` workers.

No application behavior was intentionally refactored in 5.2.3; this release establishes the rule and exposes the existing debt for the controlled follow-up conversion.

No `dotnet`, MSBuild, NuGet restore/publish, GitHub, or online repository access was used for this source handoff.

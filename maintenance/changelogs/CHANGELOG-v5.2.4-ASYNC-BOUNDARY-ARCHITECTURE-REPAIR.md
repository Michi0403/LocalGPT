# LocalGPT 5.2.4 — async-boundary architecture repair

## Corrected maintenance architecture

- Repaired the 5.2.3 async-only maintenance gate instead of converting thousands of legitimate synchronous .NET/Blazor contracts into artificial `Task` wrappers.
- Pure in-memory helpers, framework-mandated synchronous callbacks/overrides, bounded `lock`/`Interlocked`/`Volatile` state protection, and components that own only synchronous disposal are valid again.
- Methods ending in `Async` must still expose awaitable contracts and `async void` remains forbidden.
- Properties/getters may not start asynchronous work. Renderer/awaitable orchestration paths may not use Task `.Result` or `GetAwaiter().GetResult()`.
- Blocking coordination (`Task.WaitAll/WaitAny`, `WaitOne`, `Thread.Sleep/Join`, blocking `Monitor` coordination) remains forbidden in maintained application workflows.
- Direct discarded asynchronous work remains forbidden. Intentional concurrency must be awaited or transferred to explicit ownership such as `ISupervisedTaskRunner`.
- Async disposal is required only when cleanup actually owns asynchronous resources/workers; the gate no longer fabricates empty `IAsyncDisposable` requirements for every Razor component.

## Audit correctness fixes

- `.Result` detection now requires a task-shaped receiver, so domain members such as `functionCall.Result.Result` are not reported as sync-over-async.
- `_ => await ...` lambdas are no longer misread as discard assignments.
- `_ = await SomeOperationAsync()` is recognized as an awaited operation whose result value is intentionally discarded, not fire-and-forget.
- `Interlocked` and `Volatile` are no longer blanket failures; the existing architecture audits continue to own synchronization/lifetime rules around them.
- The historical script/wrapper filenames remain unchanged for build compatibility, but their contract is now an async-boundary audit rather than an “every method must be async” signature rule.

## Real LocalGPT ownership repairs

The false-positive-heavy inventory exposed a smaller set of real unowned asynchronous starts. LocalGPT now transfers all maintained direct `_ = ...Async()`, `_ = InvokeAsync(...)`, and `_ = Task.Run(...)` starts to the existing supervised task owner.

- Council spooler UI renderer dispatch is supervised.
- Main-layout navigation renderer dispatch is supervised.
- Chat ASCII-console close rerender dispatch is supervised.
- Council Teams provider discovery keeps its intended background behavior but is now supervised.
- Project Maintenance selection/revision refresh work launched from synchronous property bindings is supervised.
- Chat game-console, local-console, and operational-feed coalesced renderer workers are supervised and remain cancellation-owned by their component tokens.
- Documentation viewer refresh work is supervised.
- Council spooler delayed checkpoint persistence is supervised by the process-level task runner while retaining its existing cancellation/debounce token.
- Council game autoplay loops are supervised rather than started through an unobserved `Task.Run`.

No render-mode declarations, MCP/HTTP host ownership, provider wiring, persisted formats, or DevExpress control structure were intentionally changed.

No `dotnet`, MSBuild, NuGet restore/publish, GitHub, or online repository access was used for this source handoff.

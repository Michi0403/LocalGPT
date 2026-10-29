# LocalGPT 6.6.4 source-only validation and integration evidence

- Input: `LocalGPT_6_6_3_source.zip`, flat source-root archive. All 2,306 original file entries are preserved; modifications are additive to the 6.6.3 baseline.
- New execution provider: `HyperlightUnikraftSessionSandboxProvider` shells out only to `hluk run --runtime ...`; it never uses host `python`, `dotnet`, C/C++ compilers or fallback interpreters to execute untrusted source.
- New backend registry: `SessionSandboxExecutionService` singleton/hosted service owns guest processes independent of Blazor observers, bounds concurrency and captured evidence, terminates guest process trees on stop/timeout/shutdown, and writes session-scoped JSON receipts under LocalGPT user application data.
- New UI: `Chat` session tools display/select sessions, start/stop/refresh isolated Python or .NET JIT C# runs, and stage executable C# examples using `ICodeDomSourceService` without executing them. Source hash identifies the submitted bytes without storing executable source in receipts.
- Existing reviewed project CodeDOM generation: `CodeGenerationWorkflowService` now uses the *same injected C# CodeDOM emitter* as session source staging; the existing explicit C# source fallback on CodeDOM provider failure is retained for reviewed artifact generation, **not** used for sandbox execution.
- Source contracts: `build/Assert-SessionSandboxIsolation.ps1`, with `-RunGuestSmoke` to actually execute Python and C# in the guest and verify denied network on a properly configured host. No new Python or Node source tests.
- Reference comparison: supplied `WasmSharp-main.zip` README demonstrates browser-side Roslyn C# run/diagnostics and is useful for future editor diagnostics, not backend micro-VM ownership. Supplied `hyperlight-main.zip` describes the low-level micro-VM VMM with minimal guests; the maintained **Hyperlight Unikraft** CLI supplies ordinary Python and .NET runtime images.
- C++ remains an **open provider/toolchain requirement**: CodeDOM's AST can be rendered via a C++ language provider where installed, but the current .NET 10 application has only the C# CodeDOM emitter and no verified isolated C++ compiler. The published `c` guest image is not a guarantee that it will compile arbitrary C++ source inside the VM. Do not host-compile untrusted submissions as a substitute.
- macOS: upstream Hyperlight 0.17.0 added Apple Silicon Hypervisor.framework support, with more limited parallelism. Do not claim Intel macOS parity. Windows and Linux paths and builds remain unchanged in this release.

## Not executed in this environment

`dotnet`, `pwsh`/`powershell`, and `hluk` are unavailable here; therefore compiler output, mandatory repository PowerShell checks, Hyperlight guest smoke checks, shutdown behavior, UI event binding and actual Windows/Linux/macOS guest execution are **not verified**. Review these explicitly before treating the feature as production-ready.

## Acceptance on each supported host

Run the existing Debug and Release build pipelines (including their mandatory PowerShell gates), then `./build/Assert-SessionSandboxIsolation.ps1 -RunGuestSmoke` with the official `hluk` installed, configured and runtime images available. Verify CodeDOM-generated C# output in the session UI, denied guest network, completed and failed source exit codes, interrupted/stopped runs, reconnect to sessions, host shutdown, receipt reload after restart, and no host-side execution if `hluk` is missing. C++ integration needs a separate isolated compiler provider and live guest tests.

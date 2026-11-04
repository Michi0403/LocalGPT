# LocalGPT 6.8.6 — Council source guard and Razor maintenance build repair

Baseline: LocalGPT 6.8.5 source ZIP. No GitHub access, no .NET build.

## Fixed

- `build/Assert-CouncilXRoundWiring.ps1`: replaced the stale exact `Task.Delay(750, cancellationToken)` text check with a method-scoped contract for `ScheduleLiveCouncilListRefresh()`. The check requires a bounded 500–5000 ms cancellable delay, atomic scheduling/reset and cancellation/circuit readiness checks. The existing 1500 ms debounce remains unchanged; it was deliberately increased in 6.8.5 to limit streaming-time UI refresh traffic.
- `src/LocalGPT/Components/Pages/Chat.razor`: changed the live Council transcript wrapper from a native `<section>` to a `<div role="region">` inside the existing maintained `LocalGptFormLayoutSurface`. Preserved CSS class, test identifier, accessible name, live transcript output, participant/activity loop and child elements. The native section/dialog prohibition in `Assert-RazorMaintenanceArchitecture.ps1` remains enforced.
- `src/LocalGPT/LocalGPT.csproj`: bumped application version 6.8.5 → 6.8.6.

## Kept unchanged

- Live transcript source, server-owned activity snapshots, rejoin/reconnect implementation, ASCII Chat conversion, console popup, fullscreen CSS, game/tournament engine, role pairing, participant execution, prompt and tool policies, repetition watchdog.
- Existing architectural preflight scripts remain enabled; neither failing guard was bypassed.

## Validation scope

- Offline source contract tests (19 positive/negative controls), plus 89 declared Council wiring tokens in 32 source contracts and the Razor maintenance scan across 62 component files.
- Three existing source files changed; two release documents added. No unrelated sources changed.
- ZIP member CRC, source-file content hashes and project XML verified after packaging.

**Not verified:** actual Windows PowerShell 5.1 / pwsh execution, C#/Razor compilation, DevExpress binding, Windows/macOS/Linux release, Council runtime, and browser behavior. These require execution in the user's environment. This release fixes the two reported *preflight* blockers; it is not a claim that a full build or runtime is proven clean.

# LocalGPT 3.0.8 — all-model benchmark authority and browser recovery

## Benchmark workflow

- The initial hardware benchmark now describes and enforces one frozen provider-qualified target set and one consolidated four-section benchmark suite.
- The four task sections are explicitly one shared assignment, not four model packs, quartets, representative subsets, or separate benchmark slides.
- Four token profile points remain available as measurements of the same subject/suite. They are not target partitions.
- Every benchmark-capable selected Benchmark Subject is passed to the deterministic calibration engine. The engine now verifies that every benchmark-capable frozen identity is present in the returned measurement report and stops instead of accepting silent coverage loss.
- The workflow executor independently freezes/distincts the exact Council model-selection set before calibration and verifies that calibration preserves the same requested target count.
- Independent provider/physical hosts still run in parallel; models sharing one host remain sequential so LocalGPT does not manufacture VRAM contention just to look parallel.
- Per-subject progress no longer creates a new Markdown heading for every host/model; subjects remain progress entries inside one measurement phase.
- Initial setup no longer truncates the explicitly selected benchmark subject list to 128 or the user-supplied preferred curator pool to four identities. Benchmark Subject roles still receive the full selected pool while stronger curator/director/reviewer/auditor/analyst/synthesizer roles use the preferred pool.

## Browser / reconnect hardening

- Blazor startup now preserves the server-rendered DOM while InteractiveServer attaches instead of destructively replacing the shell. A slow or recovering browser renderer therefore retains a usable shell instead of exposing an empty surface during attach failure.
- Reconnect handling now checks the interactive shell after a previously healthy tab returns from the background and attempts non-destructive `Blazor.reconnect()` before asking for Reload & rejoin.
- Running Council/benchmark work remains server-side/spooler-owned during UI reconnect attempts.
- Existing BFCache safety reload/rejoin behavior is retained.

## Build warning cleanup

- Removed stale class-level XML `<param>` tags from MinecraftDatapackService and MinecraftProjectService after their constructors had been moved to explicit constructor declarations.
- Removed the stale compiler-discovery XML block that was attached to NormalizeCompilerSearchRoots.
- These were documentation warnings only; service registrations/wiring remain intact.

## Preserved

- LocalGPT 1-Wire protocol version remains 2.1.1.
- Existing InteractiveServer render boundaries are unchanged from 3.0.7.
- Existing benchmark quality review, coverage review, performance analysis, profile synthesis, Low/Middle/High/Expert profile persistence, provider-qualified identity, retries, failure evidence, and Council spooler behavior remain intact.

## Source-only note

This repository was edited and statically validated without invoking dotnet restore/build/publish/pack and without GitHub access.

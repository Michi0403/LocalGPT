# LocalGPT 6.8.5 — offline validation

Source: the provided LocalGPT 6.8.4 repository ZIP. No GitHub access, no dotnet build, and no use of a hosted repository.

## Checked

- Project version 6.8.5 (6.8.4 -> 6.8.5; no double-digit patch slot).
- Source contract checks for: transcript independent of cached DXAIChat template, empty-normal-message ASCII participant rendering, game auto-open renderer dispatch, exact-run spooler restoration, rejoin rebinding an already-attached run, reduced DXAIChat SaveMessages work, tab-scoped reconnect ID, tournament-only palette and fullscreen popup chain.
- Negative controls: original 6.8.4 sources lack the new recovery/presentation contracts.
- Browser JavaScript syntax (`node --check`) and 28 source/manifest SHA-256 entries.
- C# game execution, tournament rules, Council pairing, bootstrap and repetition watchdog files are byte-for-byte the same as 6.8.4.
- Project XML and `Directory.Build.targets` parsed successfully.
- ZIP CRC verification after packaging.

## Not tested

- Windows PowerShell 5.1, PowerShell Core execution and source guards.
- Actual .NET C#/Razor compilation, DevExpress component binding, cross-platform release.
- Blazor reconnect during a 14-model Council, real provider function calls, fullscreen browser rendering and ANSI game appearance.

The regression is addressed in source but cannot be called runtime-verified until the host application is tested. `ConfigureAwait(true)` is not inherently a blocking call; UI starvation must be diagnosed in the surrounding dispatcher/render workload rather than by changing all awaiting patterns indiscriminately.

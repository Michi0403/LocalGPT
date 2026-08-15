# LocalGPT 2.9.0 Council rejoin compile repair

LocalGPT 2.9.0 is a focused source compile repair for the 2.8.9 Council rejoin/circuit-recovery release. The rejoin design remains intact; only the renderer-dispatched composer-draft capture is corrected so the result is not treated as the return value of `ComponentBase.InvokeAsync(Func<Task>)`.

No Council membership, workflow, provider routing, role coordination, function policy, trace visibility, SignalR timing, or render-mode behavior is intentionally changed by this release.

## Versions

- LocalGPT: 2.9.0
- LocalGPTWebviewWrapper: 2.9.0
- LocalGPTInstallerConsole: 2.9.0
- LocalGPT Wire Protocol: 2.1.1 (unchanged)

See `CHANGELOG-v2.9.0-REJOIN-COMPILE-REPAIR.md` and `VALIDATION-v2.9.0-source.md`.

# LocalGPT 6.5.3 — Upload Council routing and disconnected-chat recovery

Base: full LocalGPT 6.5.2 source archive supplied by the user. No GitHub fetch, dotnet restore/build/test, or deployment was performed.

## Repairs

- The upload advisor's deterministic fallback now prefers an **enabled, configured Council team that advertises `chat.upload_workspace_curate`**, ahead of repository-development matches. AI-assisted recommendations with **no explicit user goal** retain the fallback's review-first team instead of silently choosing a development workflow. A user-specified processing objective and its validated recommended team remain under the existing selection rules.
- The upload-to-Council component no longer silently equates a failed browser dispatch with successful Council submission; it warns that the prompt was not confirmed. Direct Council startup catches `JSDisconnectedException` and `TaskCanceledException` without cascading the failure into the interactive page.
- Increased disconnected Blazor circuit retention from two to ten minutes to give the already-existing reconnect/rejoin flow more room after temporary transport stalls. This is a recovery window, **not** a fix for every timed-out Blazor stream.
- When JavaScript detects the reported SignalR `Cannot send data if the connection is not in the 'Connected' State` failure, the browser-owned reconnect UI is exposed. JavaScript diagnostics stop forwarding to a dead circuit, bound queued diagnostics to 100 unique entries, and resume sending once the reconnect modal reports reconnection. The independently stored active Council run ID remains the source for Reload & rejoin.

## Guarded behavior retained

No changes to OneWire trust/MFA, tool approval boundaries, runtime model/team persistence, original ZIP quarantine, or the server-owned Council run spooler. The upload advice/selection change does **not** promote quarantined repositories or authorize builds.

## Offline verification

- `python -m unittest discover -s tests/static -p 'test_*.py'`: **122 passed**, including four new assertions covering the affected routes and browser recovery.
- `node --check` on the changed browser JavaScript files: passed.
- Project XML parsed; complete source ZIP integrity verified after packaging.
- **Not verified**: .NET/Razor compilation, Windows/DevExpress integration, live SignalR/Blazor circuit reproduction, load test, or live Council confirmation. A pending `The stream with ID ... is not available` exception may still require a runtime reproduction and file-stream root-cause fix.

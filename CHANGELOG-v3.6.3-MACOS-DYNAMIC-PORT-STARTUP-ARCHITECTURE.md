# LocalGPT 3.6.3 — macOS dynamic-port startup and native-architecture guard

- Fixes the macOS launch path that could sit for five minutes after `Configured middleware and endpoints` without ever publishing the LocalGPT runtime endpoint.
- The packaged macOS launcher now starts LocalGPT with `--port 0`, letting LocalGPT choose a free loopback port instead of assuming TCP 5000. This avoids the common macOS AirPlay Receiver collision on port 5000 and prevents the browser from being sent to an unrelated local 403 response.
- Launcher readiness now probes the lightweight `/health` endpoint from the runtime `server.json` URL and opens the browser at the actual LocalGPT base URL only after Kestrel is healthy.
- Converts database initialization, runtime-capability synchronization, and DX AI-function catalog synchronization from startup-blocking `IHostedService` work into background services with an explicit first yield. Slow first-run database migration/seeding can continue without holding the Kestrel listener offline; database-backed services still coordinate through the same singleton initializer.
- Stops stale LocalGPT processes from the installed payload when no valid runtime endpoint exists, and terminates a launcher-owned process after the five-minute hard failure instead of deliberately leaving a hung instance behind to retain SQLite/port locks for the next launch.
- Strengthens the visible macOS console opener with an AppleScript Terminal fallback when `open -a Terminal` cannot launch the generated `.command` helper directly.
- Adds a runtime architecture guard: Apple Silicon will refuse to invoke an Intel-only LocalGPT payload and direct the user to the `osx-arm64` package instead of silently relying on Rosetta.
- Adds build-time macOS native-architecture validation. Non-target `runtimes/osx-*` folders are removed from each RID-specific `.app`, every remaining Mach-O component must contain the requested architecture, and packaging fails rather than shipping an arm64 bundle with an Intel-only component (or the inverse).
- Preserves the 3.6.2 user-data permission repair, visible console, Ollama/LM Studio helper, durable DocFX cache, and release-workspace cleanup.
- Version advanced from 3.6.2 to 3.6.3.

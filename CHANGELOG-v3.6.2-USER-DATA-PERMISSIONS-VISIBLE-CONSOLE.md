# LocalGPT 3.6.2 — user-data permissions and visible console

- Makes the installed macOS launcher open its Terminal log console immediately by default, so Finder/Applications launches retain the visible diagnostics users already get from console-hosted Windows/Linux starts. Set `LOCALGPT_SHOW_CONSOLE=0` for an intentionally quiet macOS launch.
- Creates and write-probes the per-user `~/Library/Application Support/LocalGPT`, runtime, Logs, and Caches directories before starting the application.
- If one of those LocalGPT-owned user directories has incorrect ownership, the macOS launcher can request a scoped administrator repair for that directory only; it does not make `/Applications/LocalGPT.app` user-writable.
- Creates the launcher log before Terminal starts following it, avoiding a first-run `tail` race.
- Keeps the five-minute HTTP/runtime-endpoint readiness behavior and browser opening from 3.6.1.
- Changes 1-Wire secret storage to prefer per-user application data for new installations while preserving an existing writable portable secret when one already exists.
- Linux AppImage desktop launches now request a visible terminal and the AppRun wrapper verifies per-user XDG data/state/cache directories before launching the application.
- Existing Windows console behavior is preserved.
- Version advanced from 3.6.1 to 3.6.2.

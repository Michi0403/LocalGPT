# LocalGPT 3.6.2 source validation

Static validation only; no .NET build was run.

- Confirmed LocalGPT project, installer-console, and WebView wrapper versions are 3.6.2.
- Confirmed the generated macOS launcher has an immediate visible Terminal log console by default, creates the log before tailing it, keeps the HTTP/runtime-endpoint readiness probes, and supports `LOCALGPT_SHOW_CONSOLE=0`.
- Confirmed the macOS launcher write-probes only LocalGPT-owned per-user Application Support/runtime/Logs/Caches directories and scopes any administrator ownership repair to the failing user directory rather than `/Applications/LocalGPT.app`.
- Confirmed new 1-Wire secret storage prefers LocalApplicationData while preserving an existing writable portable secret.
- Confirmed Linux AppImage desktop metadata uses `Terminal=true` and the AppRun wrapper checks writable XDG data/state/cache directories.
- Confirmed the generated macOS launcher and Linux AppRun shell bodies pass `sh -n` after placeholder substitution.
- Confirmed no GitHub access or .NET compilation was used for this patch.

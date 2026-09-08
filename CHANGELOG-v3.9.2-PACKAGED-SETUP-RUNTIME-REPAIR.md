# LocalGPT 3.9.2 — packaged Setup runtime repair

## Fixed

- Repairs the installed macOS Setup guide failure where `HardwareInventoryService` inherited a current working directory that no longer existed and `Process.Start` failed through `Interop.Sys.GetCwd()` before the Setup snapshot could be rendered.
- Changes the generated macOS launcher to start LocalGPT from the durable per-user `~/Library/Application Support/LocalGPT/runtime` directory instead of making the replaceable `/Applications/LocalGPT.app/Contents/Resources/app` bundle the process working directory.
- Adds a startup repair for an already-invalid inherited current directory without changing normal source/development launches whose current directory is still valid.
- Makes all Setup hardware process probes use the same durable LocalGPT runtime working directory and treats optional NVIDIA/platform GPU probe failures as non-fatal. A missing `nvidia-smi` on macOS can no longer abort the Setup Assistant.
- Isolates optional hardware discovery while building the Setup snapshot. Provider profiles, Ollama controls, CanIRun.ai recommendations, model installation, and benchmark setup remain available even when hardware probing is temporarily unavailable.
- Removes `Directory.GetCurrentDirectory()` from the optional file logger. The default log now lives under the canonical per-user LocalGPT logs directory; a configured path inside the read-only application bundle is redirected to the user log path, and an OS temp fallback remains available if user-data path resolution itself fails.
- Makes the shared bounded console use the durable per-user runtime directory when a command does not explicitly supply a working directory. This covers provider installation and one-click model downloads without relying on launcher state.
- Removes the remaining Ollama process fallback to `Environment.CurrentDirectory`.
- Fixes a CanIRun recommendation parser overflow: the default effectively-unlimited recommendation policy (`Int32.MaxValue`) was multiplied by four, wrapped negative, and could stop parsing after the first recommendation. Candidate traversal remains explicitly bounded to 512 JSON objects, while all parsed/deduplicated recommendations can now flow into Setup.
- Removes the separate 96-item service truncation from provider model-choice mapping, so the unified Setup form can offer an install action for every recommendation returned by the maintained CanIRun response bound.

## Preserved

- The Setup guide remains the single guided workflow for reviewed hardware, optional CanIRun.ai recommendations, provider selection, Ollama Start/Stop/Restart/Refresh, endpoint registration, explicit model-ID installation, recommendation-driven one-click model installation, and benchmark-team creation. All recommendations returned by the maintained bounded CanIRun policy are shown in the unified form instead of being hidden by an additional Razor-only 24/32-item display truncation.
- CanIRun.ai remains explicit opt-in only and receives only the reviewed hardware facts described in the Setup UI.
- Ollama remains explicitly user-started; LocalGPT does not silently start the runtime.
- Existing provider-profile v2 parsing, explicit first-model installation, Apple unified-memory probing, 15 `@rendermode InteractiveServer` boundaries, eight hosted services, and the executable 60-row Council SQL seed remain unchanged.
- The 3.9.1 build-guard repairs remain intact; no ownership or system-variable guard is weakened or baselined away.

## Version

- LocalGPT: `3.9.2`
- LocalGPTInstallerConsole: `3.9.2`
- LocalGPTWebviewWrapper: `3.9.2`
- LocalGPT.WireProtocolVersion: unchanged (`2.1.1`)
- LocalGPT.ReleasePackaging: unchanged (`1.0.2`)

## Validation boundary

The supplied installed-app log and saved Setup HTML are treated as authoritative runtime evidence for the 3.9.1 failure. This environment does not run the .NET/PowerShell release build; 3.9.2 is therefore validated here through deterministic source/static checks and a regression audit for the exact current-directory failure path.

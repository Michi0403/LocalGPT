# LocalGPT 3.6.3 source validation

Static/source validation only; no .NET build was run.

- Confirmed LocalGPT project, installer-console, and WebView wrapper versions are 3.6.3.
- Confirmed the generated macOS launcher starts the packaged binary with `--port 0`, reads the real runtime endpoint, probes `<baseUrl>/health`, and has no fixed-port-5000 fallback.
- Confirmed the launcher stops stale installed LocalGPT payload processes with no healthy runtime endpoint and terminates its own stuck child after the five-minute failure instead of leaving it running.
- Confirmed the visible Terminal helper remains default-on and now falls back to AppleScript Terminal activation when direct `open -a Terminal` fails.
- Confirmed `DatabaseInitializationHostedService`, `RuntimeCapabilityDirectoryHostedService`, and `DxAiFunctionCatalogHostedService` derive from `BackgroundService`, explicitly yield before long work, and no maintained LocalGPT runtime service still derives directly from `IHostedService`.
- Confirmed macOS packaging removes opposite-architecture `runtimes/osx-*` directories and verifies every Mach-O file contains the requested RID architecture before codesigning.
- Confirmed the installed launcher refuses an Intel-only LocalGPT runtime on Apple Silicon instead of invoking Rosetta.
- Confirmed the generated macOS launcher body passes `sh -n` after placeholder substitution.
- Confirmed project/XML/JSON version-bearing files parse successfully and the release audit script passes.
- Confirmed no GitHub access or .NET compilation was used for this patch.

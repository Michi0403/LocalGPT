# LocalGPT 4.0.2 — macOS updater handoff, runtime identity and setup recovery

## Why this release exists

A packaged macOS test reached Ollama successfully but the `/install` assistant remained on its recovery/loading state. The supplied launcher log showed the setup snapshot failing through an obsolete `HardwareInventoryService.RunProbeAsync` / NVIDIA path after `Interop.Sys.GetCwd()` failed, while the same running process attempted to write `LocalGPT.log` inside the read-only `/Applications/LocalGPT.app` bundle. The maintained 4.0.1 source no longer owned those old probe methods, so the process actually serving the browser was not the freshly installed runtime.

The previous macOS launcher could reuse any responding `server.json` endpoint before proving that the endpoint belonged to the newly installed version. A PKG could therefore replace the bundle while the old process remained alive, and reopening LocalGPT could reconnect to that old process.

## Changes

- **macOS update handoff**
  - PKG packaging now emits `preinstall` and `postinstall` lifecycle scripts.
  - `preinstall` finds the running application process under `/Applications/LocalGPT.app`, requests termination, escalates only when needed, and removes the console user's stale runtime endpoint before bundle replacement.
  - A small marker records whether LocalGPT was running before the update.
  - `postinstall` clears the endpoint again and reopens the newly installed application in the logged-in user's launch context when the old version had been running.

- **Runtime/process identity**
  - Unix publish verifies the semantic version embedded in the freshly published `LocalGPT.dll` before native packaging.
  - Unix runtime payloads carry `RELEASE-VERSION.txt` and `SOURCE-SHA256.txt`.
  - The generated macOS launcher embeds the expected version and rejects missing, malformed or mismatched payload identity.
  - `server.json` now publishes `Version` and `ExecutablePath` in addition to process/port data.
  - A responding endpoint is reused only when its PID is alive, its semantic version matches the installed launcher, and its executable path is exactly the current packaged apphost.
  - Missing identity from an older endpoint is treated as stale: the old owner is terminated and the endpoint is removed before the new runtime starts.
  - Application startup logs assembly version, executable path, base directory and working directory.

- **Writable/stable macOS process context**
  - The packaged launcher starts LocalGPT from `~/Library/Application Support/LocalGPT/runtime` and exports that directory as `PWD`.
  - `LoggingCore__FileCore__FilePath` is routed to `~/Library/Application Support/LocalGPT/logs/LocalGPT.log`.
  - This prevents a process from inheriting a current directory inside a bundle that an updater may replace.

- **Setup hardware fail-soft**
  - Optional NVIDIA discovery has its own exception boundary in `HardwareInventoryService`.
  - Cancellation still propagates.
  - An unavailable vendor probe contributes no NVIDIA rows instead of aborting the full setup snapshot.

## Preserved behavior

- Windows Ollama install/update keeps the 4.0.1 direct streaming `HttpClient` download and visible percentage progress.
- macOS/Linux Ollama provider profiles keep the maintained vendor installation/update commands.
- Ollama runtime mutation still requires explicit confirmation.
- Provider-model catalog resolution, CanIRun.ai compatibility scoring, model installation, console progress normalization, Council recovery and circuit-retention behavior are unchanged.
- Routed pages retain their existing InteractiveServer ownership; `InitialSetupAssistantPanel.razor` remains an inherited child component with no nested render boundary.

## Validation boundary

This handoff is source/static validation only. No `dotnet build`, `dotnet publish`, native macOS packaging, signing/notarization, PKG execution or GitHub access was performed here. The next Mac package test should show matching launcher/runtime identity before `/install` behavior is evaluated.

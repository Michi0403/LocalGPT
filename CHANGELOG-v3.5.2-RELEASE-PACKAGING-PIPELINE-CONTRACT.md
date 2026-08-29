# LocalGPT 3.5.2 — Release Packaging Pipeline Contract

## Fixed

- Fixed the Windows release-build failure that appeared only after all Windows targets had published successfully and the first Linux payload reached `NativeReleasePackaging.ps1`.
- `Publish-ReleasePackagingPackage.ps1` and `Ensure-ReleasePackagingPackage.ps1` now route `dotnet pack` / `dotnet tool install` console text to the host instead of returning it through PowerShell's success pipeline.
- `Build-Release.ps1` now requires the release-packaging helper to return exactly one value: the installed tool executable path. A contaminated helper result fails immediately with a precise diagnostic instead of surfacing later as a `[string]` parameter-conversion error.
- Package publication inside `Ensure-ReleasePackagingPackage.ps1` has the same one-result contract, preventing `dotnet pack` progress output from being mistaken for the `.nupkg` path.

## Cross-platform installer/release contract reviewed

- Windows keeps the dependency-light `LocalGPTInstallerConsole` one-click setup lane for `win-x64`, `win-x86`, and `win-arm64`.
- Linux remains setup-console-free and produces Full/Light payloads plus `.tar.gz`, `.deb`, `.rpm`, and `.AppImage` packages. The package formats are the Linux installation lane; the payload also contains the optional dependency helper.
- macOS remains setup-console-free and produces Full/Light `.app` bundles and `.tar.gz` packages, with `.dmg` materialization as the native macOS finishing step.
- RPM/AppImage creation continues to use native tools when available and Docker/Podman fallbacks otherwise. DMG creation still requires a macOS host with `hdiutil`.
- The shared `LocalGPT.ReleasePackaging` .NET tool remains LocalGPT-owned and is published/cached alongside the authoritative 1-Wire NuGet package for PublisherStudio to consume.

## Preserved

- Existing runtime-policy repairs, OCR policy, provider repetition watchdog default, explicit `InteractiveServer` boundaries, release documentation flow, and all seven maintained RIDs are unchanged.

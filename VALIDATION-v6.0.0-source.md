# LocalGPT 6.0.0 source validation

- Baseline: complete user-provided LocalGPT 5.9.9 source ZIP.
- All original entries retained; source version and tool pack manifest/scripts modified only.
- XML parsed and `PackTool` notice contributions resolved to existing source paths.
- Tool package version `1.1.1` is consistent in helper project and both helper script defaults.
- Main application version `6.0.0`; no other SDK/helper project bumped.
- Existing NuGet notice guard remains byte-identical and still requires SHA-256 equality for both entries.
- All ZIP entries CRC checked; original asset bytes unchanged.
- **No .NET, pwsh, NuGet packaging, Windows/macOS, or Edge execution** was performed.

macOS acceptance: `pwsh ./build/Publish-ReleasePackagingPackage.ps1 -Version 1.1.1` (generates and independently checks nupkg) followed by `pwsh ./Build-Release.ps1`. Verify that the archive is actually accepted by the unchanged SHA-256 guard.

# LocalGPT 7.0.8 — Council workspace cross-platform path comparison

Source revision based on LocalGPT 7.0.7. No changes to PublisherStudio, MeowReal, or LocalAI.

## Fixed

- Replaced the new direct `OperatingSystem.IsWindows()` conditional in `CouncilRuntimeService.ArtifactWorkspaceRuntime.ResolveWorkspaceTextFile` with the existing injected `IPlatformRuntimeService.PathComparison` contract.
- Preserved the staged workspace containment (`platform.IsSameOrDescendantPath`) and reparse-point checks. The loop now respects Windows ordinal-ignore-case and Unix ordinal-sensitive filesystem path comparisons without an OS branch in the shared Council service.
- Advanced the LocalGPT version from `7.0.7` to `7.0.8`, without changing Council system-seed or runtime configuration data.

## Root cause

The maintainer's macOS `pwsh Build-Release.ps1` stopped at `build/Assert-CrossPlatformBoundaries.ps1` because version 7.0.7 had an OS check at line 226 of `CouncilRuntimeService.ArtifactWorkspaceRuntime.cs`, violating the established platform-service ownership rule. The audit correctly identified the regression before compilation. The existing `IPlatformRuntimeService` interface already provides `PathComparison`, implemented separately by `WindowsPlatformRuntimeService` and `UnixPlatformRuntimeService`; no new platform abstraction is necessary.

## Validation and limits

- Source inspection confirms the failing direct OS call is absent and the service now uses `platform.PathComparison`; Windows and Unix platform implementations provide the expected values.
- Run `./build/Assert-CrossPlatformBoundaries.ps1` using macOS `pwsh` and then `pwsh ./Build-Release.ps1` from a **freshly extracted** source tree to establish real compatibility.
- PowerShell, .NET SDK, macOS and Windows runtime tests are unavailable in the editing environment; a successful build is **not** claimed. Any further guard or C# compilation failures should be reported directly.

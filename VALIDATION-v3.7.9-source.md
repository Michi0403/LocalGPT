# LocalGPT 3.7.9 source validation

This source package was validated statically without running a .NET build and without GitHub access.

## PowerShell compatibility checks

- Scanned every maintained `.ps1`/`.psm1` for parser-sensitive and runtime compatibility patterns covered by `build/Assert-PowerShellCompatibility.ps1`.
- Confirmed there is no direct `String.Contains(value, StringComparison)` call.
- Confirmed there is no direct `System.IO.Path.GetRelativePath(...)` call.
- Confirmed there is no direct `ProcessStartInfo.ArgumentList` access.
- Confirmed there is no direct `Process.Kill(true)` call.
- Confirmed portable reflection/fallback helpers exist for relative paths, process arguments, and process termination.
- Existing cross-platform path and protected platform-variable rules remain enabled.

## Release preservation

- The 3.7.8 chunked PDF cache/resume and notarization recovery changes were kept.
- Current version references were advanced to 3.7.9 while historical changelog/validation files were preserved.
- No .NET build was performed in this environment.

# LocalGPT 3.5.2 Source Validation

This source package is statically validated in an environment without the .NET SDK or a PowerShell runtime. The supplied Windows release log is the authoritative runtime evidence: LocalGPT itself compiled and all three Windows application/setup targets published before the release process failed at the first Linux packaging invocation. The user's next Windows release run remains the authoritative end-to-end packaging check.

## Reported failure addressed

- The supplied release log reaches `LocalGPT net10.0` successfully, publishes win-x64, win-x86 and win-arm64 application/setup artifacts, and then fails when `Publish-UnixRuntime` passes a multi-value PowerShell result to the string `PackagingTool` parameter.
- All `dotnet pack` and `dotnet tool install` console output in the LocalGPT release-packaging helper path is now host-only output.
- Both package-path and tool-path helper calls enforce exactly one captured success-pipeline value and verify the resulting filesystem path.

## Installer/release matrix checked

- Windows: installer console + application/setup ZIP lanes for x64, x86 and arm64.
- Linux: Full/Light publish lanes and TAR.GZ/DEB/RPM/AppImage packaging, no setup console.
- macOS: Full/Light publish lanes, `.app` bundles, TAR.GZ and native DMG finishing, no setup console.
- Shared LocalGPT release-packaging NuGet tool remains the owner of managed TAR/DEB/checksum mechanics and remains available to PublisherStudio.

## Static validation

The release audit verifies the compiler follow-up fixes, all seven maintained RIDs, explicit `InteractiveServer` boundaries, runtime-policy safeguards, shared release-packaging ownership, the single-value PowerShell helper contract, and all Linux/macOS package-format markers. The architecture, async, service-resilience, cross-platform, configurable behavior, provider repetition-policy, structured-file and documentation audits are rerun on the final source tree and again after extracting the exact ZIP.

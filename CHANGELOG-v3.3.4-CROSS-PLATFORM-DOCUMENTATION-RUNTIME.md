# LocalGPT 3.3.4 — cross-platform documentation runtime bootstrap

## Summary

LocalGPT 3.3.4 completes the Windows/macOS/Linux PowerShell documentation prerequisite path after a macOS release build reached the PDF stage and exposed a missing Node.js runtime.

## Changes

- Added `build/NodeRuntime.Common.ps1` as the shared Node.js discovery/provisioning implementation used by the documentation pipeline and build prerequisite preflight.
- Added `build/Initialize-BuildPrerequisites.ps1` and wired both `Build-Release.ps1` and `Build-LocalDevelopment.ps1` through it before the long build begins.
- Compatible existing Node.js 20-22 installations are reused from `PLAYWRIGHT_NODEJS_PATH`, `PATH`, and common platform locations; newer unsupported Node majors are not silently substituted if the compatible-runtime bootstrap fails.
- If no compatible runtime exists, LocalGPT provisions portable Node.js **22.23.2** into the current user's LocalGPT documentation-tool cache without requiring administrator/root installation.
- Automatic provisioning now supports:
  - Windows x64, x86 and ARM64 ZIP distributions.
  - macOS Intel and Apple Silicon (`darwin-x64` / `darwin-arm64`) tarballs.
  - Linux x64 and ARM64 tarballs.
- Node downloads are verified against the official version-specific `SHASUMS256.txt` manifest before extraction.
- The selected Node executable is exported as `PLAYWRIGHT_NODEJS_PATH` and its directory is prepended only to the current build process `PATH`.
- The DocFX PDF fallback now uses the same cross-platform Node runtime resolver instead of a Windows-only ZIP implementation.
- Large documentation sets on macOS/Linux skip direct Chromium-family print attempts above 1000 source pages and go straight to the DocFX PDF plug-in. Windows retains the existing 1500-page direct-browser threshold.
- Intentional threshold routing is now informational rather than recorded as a warning.
- `documentation-status.json` records the Node platform and architecture used for PDF generation in addition to version/provisioning state.
- The 3.3.3 DocFX assembly-reference closure repair remains in place, including `CopyLocalLockFileAssemblies=true`, shared-runtime dependency repair, and zero-unresolved-reference release guards.
- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper are versioned **3.3.4**.

## Rationale

A release build must not reach the final PDF stage after tens of seconds/minutes only to tell a macOS or Linux developer to install Node manually. Node is a build-time documentation dependency here, so the PowerShell build tooling owns a deterministic per-user bootstrap path on every supported host OS while still respecting an already installed compatible runtime.

## Validation policy

No .NET or PowerShell build was executed while preparing this source archive. Static source validation verifies the version bump, platform/architecture distribution mapping, checksum-manifest verification, early build prerequisite wiring, DocFX resolver reuse, large-document routing, preservation of the 3.3.3 assembly-reference guards, and preservation of existing Interactive Server render-mode declarations.

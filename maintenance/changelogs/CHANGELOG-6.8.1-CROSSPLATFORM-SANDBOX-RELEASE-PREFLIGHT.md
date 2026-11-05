# LocalGPT 6.8.1 — Cross-platform sandbox and early release guards

## Fixed

- **macOS release preflight failure** (`Assert-CrossPlatformBoundaries.ps1`): `HyperlightUnikraftSessionSandboxProvider` and `SessionSandboxExecutionService` no longer call `OperatingSystem.IsWindows()` from application services. Both resolve the bundled `hluk` filename through the injected `IPlatformRuntimeService` that already encapsulates platform-specific behavior.
- **Windows and Unix bundled executable selection**: the Windows platform implementation resolves `hluk` to `hluk.exe`; the Unix/macOS/Linux implementation preserves `hluk`. The same resolved filename is used for readiness checks and actual launch, preserving the existing file-existence checks, rootfs/hash verification, isolation flags, and fail-closed behavior.
- **Late release preflight diagnostics**: the cross-platform boundary audit and sandbox isolation source-contract guard now run immediately after the PowerShell compatibility guard and before DevExpress/Node prerequisites in `Build-Release.ps1` and `Build-LocalDevelopment.ps1`.

## Regression guard improvements

- `Assert-CrossPlatformBoundaries.ps1` verifies the platform abstraction contract, both OS-specific executable naming implementations, and both sandbox consumers without approving new OS-branch exceptions.
- `Assert-SessionSandboxIsolation.ps1` also asserts the portable CLI resolution and preserves all existing requirements for isolated guest execution.
- The Windows PowerShell 5.1 `ProcessStartInfo.ArgumentList` false-positive reported in the **first** log was already corrected in the provided 6.8.0 source via PowerShell AST checking (the **second** log confirms this preflight passes). That correction is retained; there is no redundant or ineffective change to it in 6.8.1.

## Version and scope

- Application source version **6.8.0 -> 6.8.1** (`src/LocalGPT/LocalGPT.csproj`).
- PublisherStudio remains unchanged at 4.7.1; no PublisherStudio archive is produced.
- No change to runtime execution authorization, process arguments, external CLI provisioning, SHA-256 checks, guest network/mount restrictions, or the existing AI/Council recovery architecture.

## Validation limitation

Source-only checks cover the two reported boundary violations and sandbox wiring. No .NET compilation, PowerShell execution, Windows PowerShell 5.1 execution, macOS release build, or guest VM integration test was performed in this environment. A successful end-to-end release has **not** been asserted. See `VALIDATION-6.8.1-OFFLINE.md`.

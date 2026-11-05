# LocalGPT 4.1.9 - macOS release child-process flood repair

LocalGPT 4.1.9 follows 4.1.8 with a narrowly scoped macOS release-packaging repair. The supplied 4.1.8 release transcript showed `MallocStackLogging: can't turn off malloc stack logging because it was not enabled.` hundreds of times with a new `pwsh(...)` PID on each line. The burst continued after DMG and PKG notarization and became extreme again immediately after the osx-arm64 publish/documentation stage.

## Fixed

- Replaced the macOS bundle architecture probe's one-`file`-process-per-bundle-file loop with bounded 96-file brief-mode batches.
- Reuses the validated Mach-O inventory for Developer ID signing instead of rescanning the complete app bundle a second time.
- Keeps the exact architecture manifest, Mach-O architecture mismatch failure, per-component Developer ID signing, per-component signature verification, enclosing app verification, DMG/PKG signing, Gatekeeper checks and notarization flow intact.
- The optimization reduces native child launches from roughly one per file per scan to one per bounded batch. On documentation-bearing app bundles this removes the source-side amplification that turned an intermittent macOS/PowerShell malloc diagnostic into hundreds or thousands of terminal lines.

## Evidence

The supplied transcript contained 860 malloc-stack diagnostics in a 999-line capture. After the x64 PKG completed, the arm64 runtime publish and documentation copy succeeded; the remaining tail then contained approximately 730 consecutive `pwsh(...) MallocStackLogging` lines. This aligns with the former `Assert-MacBundleArchitecture` implementation invoking `/usr/bin/file` separately for every file and `Sign-MacBundle` performing the same complete scan again.

## Preserved

- LocalGPT 4.1.8 Ollama catalog, CanIRun comparison and provider-workbench repairs remain unchanged.
- LocalGPT 4.1.7 cancellation, database-init churn, hardware-road and transient-provider recovery fixes remain unchanged.
- No Apple notarization retry/upload semantics were changed.
- No signing requirement or architecture validation was weakened.

## Versioning

LocalGPT advances from 4.1.8 to 4.1.9. The one-digit minor/patch policy remains satisfied; the next patch after 4.1.9 must roll to 4.2.0.

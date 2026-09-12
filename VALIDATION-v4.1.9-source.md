# LocalGPT 4.1.9 source validation

This package is source-audited only. The validation environment does not run the LocalGPT .NET build, macOS codesigning, Apple notarization, or the owner's PowerShell release pipeline.

## Reproduced cause

The supplied 4.1.8 macOS transcript contains 860 `MallocStackLogging` diagnostics in 999 captured lines. The largest burst starts after the osx-arm64 publish/documentation stage and consists almost entirely of fresh `pwsh(...)` PIDs. The maintained macOS packager performed a native `/usr/bin/file` invocation once for every app-bundle file in `Assert-MacBundleArchitecture`, then repeated the same complete bundle scan in `Sign-MacBundle`.

## Source repair

`build/NativeReleasePackaging.ps1` now:

- inventories app-bundle files with `file -b` in bounded batches of 96 paths;
- verifies that every batch returns exactly one description per input file;
- preserves the existing Mach-O architecture manifest and mismatch failure behavior;
- returns the validated Mach-O file set from the architecture check; and
- passes that set directly to `Sign-MacBundle`, avoiding the former second full bundle scan.

Signing and notarization are intentionally unchanged after inventory discovery: each Mach-O component is still signed and verified, nested code bundles remain deepest-first, the app bundle is still signed/verified last, and DMG/PKG notarization/stapling/Gatekeeper behavior is untouched.

## Static validation

The 4.1.9 release audit guards the batched inventory, exact result-count validation, reuse of the validated inventory, absence of the old per-file `file` probes, semantic version policy, and all inherited 4.1.8/4.1.7 contracts. The existing architecture, cross-platform, Council, configuration, DXFunction/codegen, X-Round, ConfigurationRoot, async, service-resilience, localization, render-mode, XML/Razor documentation and PowerShell interpolation source guards remain applicable.

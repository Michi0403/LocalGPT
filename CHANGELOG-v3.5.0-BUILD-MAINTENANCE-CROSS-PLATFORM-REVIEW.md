# LocalGPT 3.5.0 — build maintenance and cross-platform release review

## Versioning

- Rolled LocalGPT from 3.4.9 to **3.5.0** because the repository policy does not allow two-digit minor or patch slots (`3.4.10` is represented as `3.5.0`).
- Updated the application, web-view wrapper, installer console, documentation metadata, browser cache-busting version, and outbound LocalGPT user-agent identity.

## Build and maintenance repairs

- Repaired the malformed `EmbeddedTelemetryIngressService` member declaration that caused the reported `CS0145`, `CS1003`, `CS1002`, `CS1001`, `CS1031`, `CS8124`, and `CS1519` compiler cascade.
- `EmbeddedTelemetryIngressService` now consumes `ILocalGptRuntimePolicyDataService` through DI and uses the persisted `EmbeddedTelemetryMaximumSnapshots` operator policy for both queue retention and `GetRecent` bounds instead of reintroducing a source-owned `500` ceiling.
- Re-ran the repository XML-documentation enhancer/coverage gate and filled the source declarations that would otherwise become the next documentation-maintenance failure after compilation progressed.
- Hardened the provider-stream repetition policy audit so its disabled fast-path check accepts the existing combined `!enabled || ...` guard rather than falsely demanding a standalone `if (!enabled)` statement.
- Hardened release-documentation diagnostics against `Set-StrictMode` empty-collection property failures.
- Hardened Debug documentation/Pages synchronization: when Debug intentionally produces HTML documentation without a PDF, the Pages step validates the HTML-only output and leaves the tracked release snapshot untouched. Release builds still require the complete versioned PDF.

## Cross-platform review against the supplied 3.3.0 baseline

- Preserved all 15 explicit `@rendermode InteractiveServer` boundaries that existed in the supplied LocalGPT 3.3.0 source baseline.
- The main application remains `net10.0` with all seven maintained runtime identifiers: `win-x64`, `win-x86`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`.
- Platform-specific host behavior remains behind the maintained Windows/Unix DI boundaries; the cross-platform boundary audit reports no platform leaks.
- Windows remains the only setup-console publishing path. macOS/Linux release lanes publish the application payload directly and do not attempt to run a Windows-style setup console.
- macOS Full + Light lanes retain `.app`/`.tar.gz` output and native `.dmg` finishing through Apple `hdiutil`.
- Linux Full + Light lanes retain `.tar.gz`, `.deb`, `.rpm`, and `.AppImage` output, executable-mode restoration, dependency-helper delivery, and versioned SHA-256 checksums.
- The repository-owned `LocalGPT.ReleasePackaging` tool continues to own TAR.GZ, DEB, and checksum materialization; `dpkg-deb` is not required. RPM/AppImage continue to use their native packaging tooling when present, with Docker/Podman fallback where the host cannot materialize those formats directly.

## Validation boundary

The handoff is source-only as requested. This environment has no .NET SDK or PowerShell runtime, so no compiler success is claimed here. Maintained Python source/architecture/async/resilience/XML/cross-platform/release audits are run against a fresh extraction of the exact delivered ZIP. See `VALIDATION-v3.5.0-source.md`.

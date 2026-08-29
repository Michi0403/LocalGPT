# LocalGPT 3.4.9 — operator policy and release packaging

## Changed

- Increased the LocalGPT application, web-view wrapper, and installer-console version from 3.4.8 to 3.4.9. The version sequence keeps every numeric slot at one digit; a future 3.4.10 therefore rolls to 3.5.0.
- Replaced the local OCR service's hidden 6 MiB image ceiling with `LocalVisionMaximumImageBytes`, persisted through the LocalGPT runtime-policy data model. The shipped value is `Int32.MaxValue`; the operator may lower it explicitly.
- Local OCR request timeout now uses the persisted runtime policy and ships as `0` (no HttpClient timeout). Output-token and diagnostic limits are persisted and ship permissively.
- Provider-stream repetition termination is an explicit persisted policy and is disabled by default rather than silently ending long local generations.
- Removed the remaining hidden 120,000-character structured-chat translation gate and reused the persisted structured-text input policy instead.
- Moved Council role-evidence per-member and total character ceilings into persisted runtime-policy values, both shipping at `Int32.MaxValue`.
- Removed the obsolete `RemoteControlLimits` hardcoded ceiling container. The only remaining Remote Control polling minimum is operator-owned runtime policy; the shipped enabled minimum is one second and `0` remains the explicit disabled value.
- Kept 1-Wire replay tracking capacity, console retention/capture, CanIRun response retention, localization size/count, Council live text, telemetry, and related operator limits in persisted runtime policy with permissive shipped maximums.

## Release packaging

- Added the repository-owned `LocalGPT.ReleasePackaging` .NET tool/package source and resolver/cache scripts.
- TAR.GZ, DEB, and SHA-256 package work is routed through the repository-owned packaging tool; the release path no longer requires `dpkg-deb`.
- Windows remains the only setup-console publishing path (`win-x64`, `win-x86`, `win-arm64`).
- macOS (`osx-x64`, `osx-arm64`) builds Full + Light application payloads with `.tar.gz` and branded `.dmg` materialization on macOS via `hdiutil`.
- Linux (`linux-x64`, `linux-arm64`) builds Full + Light `.tar.gz`, `.deb`, `.rpm`, and `.AppImage` packages. RPM/AppImage use their native tooling when available and Docker/Podman only as fallback.
- Versioned release output retains SHA-256 checksum generation.

## UI / rendering safety

- The existing Interactive Server render boundaries from the supplied 3.4.7 source baseline are preserved. No component that had `@rendermode InteractiveServer` in that baseline lost it in this release line.

## Validation boundary

This handoff is source-only by request. No GitHub access and no .NET build are used as release evidence. The maintained Python source audits are run on a fresh extraction of the exact delivered ZIP before handoff; see `VALIDATION-v3.4.9-source.md`.

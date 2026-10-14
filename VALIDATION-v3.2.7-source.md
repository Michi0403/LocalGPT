# LocalGPT 3.2.7 source validation

This archive is **source-only and not compiled** in the preparation environment. No `dotnet`, MSBuild, restore, publish or runtime launch was performed.

## Scope

3.2.7 is the paired handoff version for the PublisherStudio build-repair round. It intentionally carries forward the LocalGPT 3.2.6 Remote Control implementation unchanged apart from release/version identity markers.

## Checks

- LocalGPT, InstallerConsole and WebView wrapper versions resolve to **3.2.7**.
- Minor and patch slots remain single digit.
- Browser cache and outbound LocalGPT product User-Agent version markers resolve to 3.2.7.
- `audit_release_3_2_7.py` passes.
- Applicable architecture, async-continuation and service-resilience source audits remain the authoritative source-only checks available in this environment.

The user's Windows .NET build remains authoritative for compilation and runtime validation.

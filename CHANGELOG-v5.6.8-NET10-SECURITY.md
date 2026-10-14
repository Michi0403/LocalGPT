# LocalGPT 5.6.8 – .NET 10.0.13 security servicing

Released: 2026-10-14. Source baseline: 5.6.7.

## Change

- Replace direct Microsoft/.NET 10 NuGet package pins `10.0.12` with `10.0.13`, including affected cryptography and SignalR packages.
- Update repository `global.json` to **SDK `10.0.402`**, the servicing SDK for runtime `10.0.13` in the existing `10.0.4xx` band.
- Keep target frameworks `net10.0` / Windows-specific net10.0 targets unchanged.
- Keep the existing **DevExpress 25.2.10** package set and browser assets unchanged.
- Update app version/cache-busting markers to 5.6.8, with no intended functional UI change.

- `LocalGPTWebviewWrapper` directly floors `Microsoft.AspNetCore.SignalR.Common` and `System.Security.Cryptography.Pkcs` at `10.0.13`, guarding against stale transitive versions when a third-party package still declares an older minimum.
- Documentation-only `System.Formats.Nrbf` dependency upgraded for the DocFX assembly probe.

## Explicit non-changes

- No changes to authorization, persistence, database schema, controllers, device access, GIL policy, Razor event handlers or application logic.
- `System.ServiceModel.*` packages with independent `10.0.652802` versions and Windows SDK BuildTools were **not** relabeled as .NET servicing packages.
- Older release-audit scripts and generated EF migration `ProductVersion` metadata keep their historical release provenance; they are not active package pins.
- No NuGet vulnerability filtering, warning suppression, SDK roll-forward broadening or disabling of existing security gates.

## Runtime proof still required

This is a source-only servicing migration, not a compiled release. Install SDK 10.0.402, restore all projects, rebuild and review the **transitive** NuGet vulnerability report. Confirm none of the mentioned NU1903 advisories remain.

References: https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.13/10.0.13.md and https://dotnet.microsoft.com/en-us/download/dotnet/10.0

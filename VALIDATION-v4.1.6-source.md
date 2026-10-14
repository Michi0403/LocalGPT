# LocalGPT 4.1.6 source validation

Source-only validation for the PowerShell interpolation parser repair.

- Confirm the documentation retry diagnostic uses `${maximumAttempts}:` and no `$maximumAttempts:` form remains.
- Scan all PowerShell source for non-scope `$name:` variable references that can trigger parser ambiguity.
- Re-run the maintained release, architecture, async-affinity, service-resilience, cross-platform, Council/provider, Matrix console, XML-documentation, ConfigurationRoot qualification, code-generation and configurable-behavior audits.
- Verify the source ZIP from a fresh extraction and keep build outputs/caches out of the archive.

`pwsh`, `dotnet`, signing and notarization are not executed by this source validation environment; the macOS release build remains the authoritative runtime/compiler test.

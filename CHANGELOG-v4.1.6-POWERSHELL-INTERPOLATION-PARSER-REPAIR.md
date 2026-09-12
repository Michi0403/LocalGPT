# LocalGPT 4.1.6 - PowerShell interpolation parser repair

LocalGPT 4.1.6 is a narrow build-script repair on top of 4.1.5. The bounded documentation browser retry path emitted retry diagnostics with a double-quoted `$maximumAttempts:` variable reference, which PowerShell parses as an invalid scoped-variable reference.

## Changed

- Delimits the retry-count variable as `${maximumAttempts}:` in the documentation renderer diagnostic string.
- Preserves the 4.1.5 isolated browser profiles, bounded low-memory timeouts, durable chunk reuse, per-chunk retries, and no-monolithic-fallback policy unchanged.
- Adds release-time scanning for invalid non-scope `$name:` PowerShell variable references across every maintained `.ps1` file.

No LocalGPT runtime/provider/UI behavior changes in this release.

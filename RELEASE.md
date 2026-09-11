# LocalGPT 4.0.9

LocalGPT 4.0.9 repairs two macOS release-pipeline defects exposed by the real 4.0.8 coordinator run.

The apphost JIT entitlement is now a checked-in minimal plist rather than PowerShell-generated XML. The same file is validated with `plutil` during the early macOS trust preflight, then normalized and linted again immediately before `codesign`. A malformed entitlement therefore fails before the expensive documentation lane instead of at the first signed macOS payload.

The browser PDF path also stops treating its 480-second safety timeout as a normal completion wait. It observes the output while the browser is alive and accepts a stable, structurally complete PDF as soon as it has a `%PDF-` header and `%%EOF` trailer, closing only the lingering browser process. Durable chunk reuse and the genuine timeout path remain unchanged.

The 4.0.8 documentation-PDF version hygiene and 4.0.7 installer preflight repairs are retained.

See `CHANGELOG-v4.0.9-MACOS-SIGNING-PDF-RENDER-LATENCY-REPAIR.md` and `VALIDATION-v4.0.9-source.md`.

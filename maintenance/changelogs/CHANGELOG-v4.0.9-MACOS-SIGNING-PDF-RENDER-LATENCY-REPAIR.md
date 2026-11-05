# LocalGPT 4.0.9 - macOS signing and PDF render latency repair

## Fixed

- Replaced the runtime-generated apphost entitlement XML with a checked-in, minimal `build/assets/mac-apphost-entitlements.plist` containing only `com.apple.security.cs.allow-jit`.
- macOS release trust preflight now requires `plutil` and validates the exact entitlement asset before documentation generation or native packaging begins.
- Native packaging normalizes that same entitlement asset through `plutil -convert xml1`, lints the normalized copy, and only then supplies it to `codesign`. This prevents malformed PowerShell-generated entitlement content from surviving until the first macOS RID after hours of documentation work.
- Browser PDF generation no longer waits the full 480-second renderer timeout after a complete PDF has already been written. While the browser process is alive the build now watches for a stable PDF with a valid `%PDF-` header and `%%EOF` trailer, then closes the lingering browser process and continues. The existing timeout remains a hard failure ceiling for genuinely incomplete renders.

## Preserved

- The 4.0.8 generated-documentation PDF version hygiene and strict Pages validation are unchanged.
- The 4.0.7 installer compile-surface repair and early installer compile preflight remain intact.
- Developer ID nested Mach-O signing order, Hardened Runtime, notarization, architecture validation, durable PDF chunk caching, and source/version fingerprinting remain in place.

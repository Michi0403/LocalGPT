# LocalGPT 3.6.7 — macOS Gatekeeper signing and notarization

## Fixed

- Replaced the release-only ad-hoc macOS signing path with Developer ID aware signing. `build/NativeReleasePackaging.ps1` now uses a `Developer ID Application` identity when one is installed or supplied through `MACOS_DEVELOPER_ID_APPLICATION`, enables the hardened runtime, timestamps the signature, and verifies the completed `.app` bundle.
- The actual .NET apphost and every nested Mach-O payload are signed before the enclosing bundle. The non-NativeAOT apphost receives the `com.apple.security.cs.allow-jit` entitlement required by current .NET macOS guidance under Hardened Runtime.
- PKG creation now uses a `Developer ID Installer` identity when available through the keychain or `MACOS_DEVELOPER_ID_INSTALLER` and validates the resulting package signature.
- Added Apple notarization for generated DMG and PKG artifacts through `xcrun notarytool`, followed by `stapler staple` and `stapler validate`.
- Supported notarization credentials are, in priority order: `MACOS_NOTARY_KEYCHAIN_PROFILE`; App Store Connect API key variables `APPLE_NOTARY_KEY_ID`, `APPLE_NOTARY_ISSUER`, and `APPLE_NOTARY_KEY_PATH`; or Apple ID variables `APPLE_NOTARY_APPLE_ID`, `APPLE_NOTARY_TEAM_ID`, and `APPLE_NOTARY_PASSWORD`.
- Added `MACOS_REQUIRE_NOTARIZATION=1` as a public-release gate. With it enabled, a macOS artifact is not allowed to leave the packaging step without the required Developer ID identity and notarization credentials.
- Local/developer builds without Apple distribution credentials remain possible. They receive an explicit warning that ad-hoc/unnotarized artifacts can be blocked by Gatekeeper after a browser, WhatsApp, Mail, or other quarantine-marking download.

## Why

A package built and opened on the same Mac can appear to work because it was never quarantined in the same way as a downloaded copy. A copy received over the Internet can carry `com.apple.quarantine`, at which point Gatekeeper expects a valid Developer ID signature and Apple notarization. Ad-hoc `codesign --sign -` does not establish distributable trust and cannot satisfy that requirement.

## Preserved

- LocalGPT runtime, Blazor render-mode boundaries, Apple-Silicon/Rosetta detection, exact Mach-O architecture manifest, dynamic macOS port handling, Future2 positioning, DevExpress licensing clarification, documentation caching/PDF fallback, and other package formats are unchanged.

## Version

- Version advanced from 3.6.6 to 3.6.7. This is required because the macOS release-packaging script changed.

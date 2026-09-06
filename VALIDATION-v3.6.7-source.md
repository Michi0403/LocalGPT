# LocalGPT 3.6.7 source validation

Static/source validation only; no .NET restore, build, publish, DocFX render, GitHub access, Apple notarization submission, or macOS package production was performed in this environment.

- Confirmed application, installer-console, WebView wrapper, documentation metadata/PDF name, cache-buster, and LocalGPT user-agent version are 3.6.7.
- Confirmed macOS application packaging resolves `Developer ID Application` and `Developer ID Installer` identities from explicit environment variables or the macOS keychain.
- Confirmed Developer ID application signing uses hardened runtime, timestamping, and post-signature verification; ad-hoc signing remains only as a clearly warned local-development fallback.
- Confirmed nested Mach-O payloads are signed before the enclosing bundle and the .NET apphost receives `com.apple.security.cs.allow-jit` for Hardened Runtime JIT compatibility.
- Confirmed PKG generation passes the Developer ID Installer identity to `pkgbuild` when available and retains payload validation.
- Confirmed DMG and PKG completion supports `xcrun notarytool submit --wait`, ticket stapling, and ticket validation.
- Confirmed notary credentials can come from a keychain profile, App Store Connect API key variables, or Apple ID/team/app-specific-password variables without embedding credentials in source.
- Confirmed `MACOS_REQUIRE_NOTARIZATION=1` converts missing distribution signing/notary credentials into a release failure rather than silently producing a public artifact that Gatekeeper will reject.
- Confirmed the 3.6.5/3.6.6 architecture and documentation fixes remain present.
- Confirmed the maintained `@rendermode InteractiveServer` occurrence count remains 15.
- Confirmed no repository-local `bin` or `obj` directories are included in the delivered source tree.

- Architecture policy audit passed for the maintained application boundaries.
- Async-continuation policy audit passed for the maintained source tree.
- Service-resilience audit passed for maintained service methods.

# LocalGPT 3.6.8 — automatic macOS public-release trust and signing

## Fixed

- A normal `Build-Release.ps1` run on macOS now treats selected `osx-*` outputs as public distribution artifacts by default. The build automatically enables the notarization requirement instead of relying on the operator to remember `MACOS_REQUIRE_NOTARIZATION=1`.
- Added an early macOS trust preflight before the expensive documentation and RID publishing work. It discovers the installed `Developer ID Application` and `Developer ID Installer` identities, exports them to the packaging child processes, and validates the notarization credentials before hours of build work can be spent on an unusable package.
- When no explicit notarization credential variables are supplied, the build automatically uses the shared `future2-notary` keychain profile. `MACOS_NOTARY_KEYCHAIN_PROFILE` and the existing API-key/Apple-ID credential paths remain supported overrides.
- Added `-AllowUnsignedMacPackages` as an explicit local-development escape hatch. Public macOS release trust is therefore safe by default while source contributors without an Apple Developer membership can still intentionally produce local-only packages.
- macOS application signing now explicitly signs every discovered Mach-O payload, signs nested framework/plugin/XPC/app code containers deepest-first, verifies every Mach-O signature, and signs/verifies the enclosing application bundle last.
- DMG files are now signed with the same `Developer ID Application` identity before notarization. After notarization and stapling, the DMG signature and Gatekeeper open assessment are validated.
- PKG files continue to use the `Developer ID Installer` identity and now also run a local Gatekeeper installer assessment after the hard notarization, staple, and `pkgutil --check-signature` checks. The `spctl` PKG result is diagnostic because current macOS policy releases can disagree with `spctl` even when Apple's notarization service accepted the package; the hard trust gates remain the Apple notary result, staple validation, and Installer signature validation.
- The release console states explicitly that Apple Developer ID certificates are macOS identities and are not used to sign Windows PE output. Windows public trust requires a separate Authenticode code-signing certificate.

## Operator behavior

With the two Developer ID certificates installed and a working keychain profile created once as `future2-notary`, a normal macOS release is now simply:

```powershell
pwsh Build-Release.ps1
```

No per-terminal signing or notarization environment variables are required for that standard setup. An alternate keychain profile can still be selected with `MACOS_NOTARY_KEYCHAIN_PROFILE`.

## Preserved

- LocalGPT application behavior, 15 reviewed `InteractiveServer` directives, Apple-Silicon/Rosetta runtime detection, exact native architecture inventory, dynamic-port startup, Future2/licensing documentation, DocFX cache/PDF fallback, Windows/Linux release lanes, and shared `LocalGPT.ReleasePackaging` 1.0.1 remain unchanged.

## Version

- Version advanced from 3.6.7 to 3.6.8 because release PowerShell behavior changed.

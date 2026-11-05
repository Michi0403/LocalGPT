# LocalGPT 6.4.5 — certificate enum editor restoration

## User-visible restoration
- The `/install` certificate editor now exposes RSA key size, hash, certificate-store location and store name via single-choice DevExpress `DxListBox` controls.
- X.509 Key Usage and Extended Key Usage are restored as two **independent, combinable, multi-selection DevExpress DxListBox** controls with checkboxes and select-all.
- All offered individual X.509 key-usage enum flags and every supported extended-usage role are reflected in the generated certificate extensions, rather than silently ignored.
- Newly generated TLS-server certificate presets use RSA-3072, SHA-256, DigitalSignature + KeyEncipherment, and ServerAuthentication EKU; `CurrentUser/My` is the default optional store and the canonical PFX output location remains under user data.
- The optional CA checkbox is explicit. Cryptographically inconsistent combinations are rejected before overwriting the previous PFX: CA requires KeyCertSign and no EKU, leaf certificates reject CA signing flags, encipher-only needs key agreement, and assigning a certificate to the Kestrel HTTPS endpoint requires server authentication and an appropriate key usage.
- All six maintained localization catalogs include the new UI keys.

## Maintenance
- The build/release-wired PowerShell certificate contract now checks for both multi-selection listboxes, defaults, actual generated KU/EKU usage, CA consistency, HTTPS assignment checks, and locale catalogs.
- Offline tests check UI declarations, issuance OID mapping, safe presets, localization and guard wiring.
- Installer canonical layout and optional HTTP endpoint fallback from 6.4.4 remain unchanged.

## Validation limitations
- .NET and PowerShell runtime are not available in this offline container: build, real X509 issuance and UI interaction must be tested on Windows before release.

## Installer debris recovery
- After confirming that `winx64/LocalGPT.exe` and `setupwinx64/Start.cmd` exist, the installer now detects the previously generated `LocalGPT-<version>-win-x64` and `LocalGPT-Setup-<version>-win-x64` folders in the AppData root.
- Only exact three-part release-wrapper directory names with their expected root executable are recovered; symlink/reparse roots and unrelated user folders are skipped.
- Old payloads are **moved, not deleted**, to `runtime/recovered-installer-payloads/` to eliminate ambiguous executable directories without risking user data loss. Locked files are reported and left untouched.
- An installation must succeed before recovery can start. The mandatory PowerShell installation guard and offline regression tests protect the cleanup boundaries and no-delete guarantee.

- Installer console bumped independently from 4.9.8 to 4.9.9 for the legacy release-folder recovery behavior.

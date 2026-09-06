# LocalGPT 3.8.0 - Notary orchestration recovery

## Fixed

- Removed the redundant macOS trust/keychain preflight that had been added inside every `Publish-UnixRuntime` macOS RID lane.
- Removed the blocking `Read-Host` notarization recovery loop that could leave an unattended release parked after hours of successful documentation work.
- Removed redundant `notarytool history` validation immediately before each real submit/resume operation; the real operation is now the authentication test.
- `xcrun notarytool submit/info/log` automatically retry the intermittent `No Keychain password item found for profile` condition and transient Apple/network errors while preserving completed release work.
- Corrected all operator guidance to use `xcrun notarytool`; no bare `notarytool` PATH assumption remains.
- Added optional `MACOS_NOTARY_KEYCHAIN_PATH` forwarding for deliberately configured file-based keychains.

## Preserved

- PowerShell 5.1/modern pwsh compatibility hardening from 3.7.9.
- Durable Edge browser PDF chunk cache and shared managed PDF merge.
- Notarization submission-state resume.
- Existing signing identities, certificates, application code, and InteractiveServer render-mode architecture.

# LocalGPT 3.8.1 - Notary submit status repair

## Fixed

- Fixed the fatal StrictMode crash immediately after a successful non-waiting `xcrun notarytool submit --output-format json` upload. The submit response is now required to provide only the durable submission `id`; the release no longer assumes that the submit JSON also contains a `status` property.
- After persisting the submission ID and artifact SHA-256, fresh submissions now enter the same `notarytool info` polling path used for resume. This preserves the already-uploaded Apple submission if the release process stops after upload.
- Resume no longer duplicates one-off status handling before entering the polling loop. A stored submission ID goes directly through the common polling path, so Accepted, In Progress, terminal failures, credential retries, and Apple/network retries have one implementation.
- Added StrictMode-safe property access for Apple notary JSON and local notary-state JSON. Optional/missing properties no longer cause raw `PropertyNotFoundStrict` termination.
- If `notarytool info` exits successfully and returns valid JSON but temporarily omits `status`, the release warns and retries instead of aborting.

## Evidence from the 3.7.9 failure

The DMG was Developer-ID signed and verified, uploaded successfully, and submission `baa59d28-b34d-4fb6-bf41-ad4871f7f517` was persisted before the old code crashed while reading a missing `submit.status` property. This repair targets that exact post-upload failure boundary.

## Preserved

- Apple credentials, certificates, Developer ID signing, stapling, and Gatekeeper validation behavior.
- Automatic keychain/service retry from 3.8.0.
- PowerShell 5.1 and modern pwsh compatibility.
- Durable Edge/Chromium PDF chunk cache and shared LocalGPT.ReleasePackaging ownership.
- Application behavior and InteractiveServer architecture.

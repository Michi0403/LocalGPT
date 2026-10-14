# LocalGPT 3.7.7 — macOS notarization keychain regression repair

- Advanced LocalGPT from 3.7.6 to 3.7.7 without changing the established version-slot rollover policy.
- Fixed the release regression where the early `future2-notary` preflight could succeed but the later DMG upload failed with `No Keychain password item found for profile` after the long documentation/build phase.
- Revalidates the exact Apple notarization/keychain context immediately before every macOS RID release lane and again immediately before notarization.
- Restored the proven Apple workflow for fresh artifacts: `notarytool submit --wait --timeout ...` now owns upload and waiting in one process. This avoids the newer split upload/custom-poll path repeatedly reopening the keychain profile during a fresh submission.
- A fresh keychain-profile lookup failure is revalidated and retried once only for Apple's explicit `No Keychain password item found` error.
- Existing SHA-256-bound `.notary-state.json` files remain supported: unchanged artifacts with an existing submission ID resume through status polling rather than being re-uploaded.
- Preserved the 3.7.6 adaptive `html-browser-chunked` PDF release validation, embedded compressed documentation, Developer ID signing, stapling, Gatekeeper checks, cross-platform packaging, and InteractiveServer boundaries.

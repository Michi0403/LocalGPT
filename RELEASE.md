# LocalGPT 3.8.2

LocalGPT 3.8.2 makes Apple notarization an artifact-local transaction for every macOS DMG and PKG. The 3.8.1 run proved that `notarytool submit` can create an Apple submission even when the local process reports a keychain/profile error. Therefore `submit` is now treated as non-idempotent and is never executed through the generic retry loop.

Each artifact writes SHA-256-bound `submit-pending` state before upload, including the exact artifact name, transaction start time, and a baseline snapshot of Apple submission IDs. The artifact is submitted once. If the local submit result is ambiguous or does not expose an ID, the release reconciles Apple history for that exact transaction and adopts the recovered submission ID instead of uploading the artifact again. Only idempotent `history`, `info`, and `log` operations retry transient keychain/network failures.

See `CHANGELOG-v3.8.2-ARTIFACT-LOCAL-NOTARY-TRANSACTIONS.md` and `VALIDATION-v3.8.2-source.md`.

## Release behavior

- Every DMG and PKG has an independent notarization state machine; there is no run-global submission identity.
- The startup trust check remains an early sanity check only. Every artifact transaction obtains its own read-only Apple history baseline immediately before submit.
- `notarytool submit` is invoked exactly once per new artifact transaction.
- Pending state is persisted before submit and bound to the artifact SHA-256, so changed bytes cannot inherit an older submission.
- An ambiguous submit result enters history reconciliation; no second upload occurs while that transaction is unresolved.
- A recovered or directly returned submission ID is persisted and all further waiting uses idempotent `notarytool info` polling.
- Accepted/stapled artifacts retain their hash-bound state and are reused on reruns. Locally validated completed artifacts are skipped rather than resubmitted.
- DMG and PKG completion still calls the notarization state machine separately for each artifact.
- Apple tooling is invoked through `xcrun notarytool`.
- Optional `MACOS_NOTARY_KEYCHAIN_PATH` remains supported, but the existing `future2-notary` profile does not require migration for this release.
- Edge chunked documentation rendering, LocalGPT.ReleasePackaging 1.0.2, signing identities, application behavior, and InteractiveServer architecture are unchanged.

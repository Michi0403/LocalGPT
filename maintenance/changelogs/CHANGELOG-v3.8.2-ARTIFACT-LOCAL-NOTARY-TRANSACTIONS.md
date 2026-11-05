# LocalGPT 3.8.2 — artifact-local notarization transactions

- Treats every macOS DMG/PKG as an independent notarization transaction with a SHA-256-bound state file.
- `notarytool submit` is invoked exactly once per transaction. The generic retry loop is now restricted to idempotent `history`, `info`, and `log` operations.
- Saves `submit-pending` state before upload, including the artifact hash, start time, and baseline Apple submission IDs.
- If submit exits ambiguously, reconciles Apple `history` by exact artifact name, start time, and baseline IDs; a recovered ID is persisted and polled instead of uploading again.
- Pending/submitted/accepted/complete state survives reruns for unchanged bytes. Completed artifacts are locally stapler-validated and skipped.
- No change to Edge documentation rendering, signing identities, application architecture, InteractiveServer rendering, or LocalGPT.ReleasePackaging 1.0.2.

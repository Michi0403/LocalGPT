# LocalGPT 3.7.8 - release orchestration resilience

## Fixed

- Changed Apple notarization credential handling from fail-fast to operator-aware recovery for recoverable Keychain/profile access failures. A temporarily unavailable `future2-notary` profile now pauses an interactive release and retries after the operator completes the Keychain/password interaction instead of immediately throwing away a multi-hour release run.
- Restored the crash-resumable notarization flow for fresh artifacts: upload once, persist the returned submission ID and artifact SHA-256 immediately, then poll the saved submission. The 3.7.7 single-process `submit --wait` experiment is removed from the default path.
- Changed `MACOS_NOTARY_WAIT_TIMEOUT` from a fatal local deadline into a recurring progress checkpoint. Apple `In Progress` can continue for hours without turning a healthy release into an exception. An explicit operator stop still leaves the sidecar state available for the next run.
- Applied the same Keychain recovery behavior to the early trust preflight, the per-macOS-RID trust revalidation, submission status queries, and notarization-log retrieval.

## Documentation pipeline stability

- Kept the proven adaptive Chromium/Edge chunk renderer as the default PDF path.
- Made complete browser-rendered PDF chunks durable under the documentation cache, keyed by the existing documentation hash plus page count and chunk size. A retry renders only missing/incomplete chunks instead of restarting at page 1.
- Added per-chunk elapsed-time and byte-size diagnostics and a merge summary reporting rendered versus reused chunks.
- Preserved the managed PDFsharp merge path, the `html-browser-chunked` release validator, embedded offline documentation, qpdf optimization, Ghostscript size fallback, and the 256 MiB release ceiling.

## Investigation / next optimization lane

- Reviewed WeasyPrint as a future opt-in renderer because it provides print-focused HTML/CSS-to-PDF, image optimization, DPI controls, and tagged-PDF support without requiring a full browser. It is intentionally not enabled automatically in 3.7.8.
- Kept qpdf as the preferred loss-minimizing structural optimizer and Ghostscript as the aggressive fallback. MuPDF `mutool clean` is a technically capable additional external optimizer but its AGPL licensing and overlap with Ghostscript make it a lower-priority default candidate.
- The exact macOS `MallocStackLogging: can't turn off malloc stack logging because it was not enabled` line is not promoted to a release failure; no repository code sets `MallocStackLogging`, and the current observation does not show a browser/PDF crash by itself.

## Additional orchestration hardening

- Transient Apple/network notarytool failures (timeouts, temporary service errors, common 408/429/5xx transport failures) now stay in a retry loop instead of terminating the coordinator; genuine terminal notarization statuses still fail explicitly.
- PowerShell native-command error promotion is locally disabled around notarytool probes so recoverable native exit codes reach the classifier instead of being converted into an early terminating error.
- Durable browser-PDF chunks now require both a valid PDF header and an end-of-file marker before reuse, so a large interrupted Edge output cannot be mistaken for a completed chunk.

## Versioning

- LocalGPT version advanced from 3.7.7 to 3.7.8. The one-digit minor/patch policy remains satisfied.

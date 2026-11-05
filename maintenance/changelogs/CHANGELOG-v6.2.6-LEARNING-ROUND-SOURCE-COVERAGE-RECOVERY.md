# LocalGPT 6.2.6 — Learning Round source coverage and database evidence repair

## Baseline and reported failure

- Based on full LocalGPT 6.2.5 source, compared against user-provided LocalGPT 6.0.12 source and 23 October 2026 Council log.
- The upload workspace successfully extracted three attached ZIPs and indexed 4,600 files in the recorded run, but the seeded Learning Round's `learning-inventory` step failed without a successful `chat.upload_workspace_file` receipt.
- The previous implementation declared the source-review step `stalled` when its **initial** read ledger was empty, thereby skipping even a configured nonzero recovery attempt count. The seed also disabled bounded actual source excerpts on recovery. This could prevent the later knowledge/regex/project/file checklist steps from running.

## Changes

1. **Allow an initial verified-source recovery attempt:** An empty starting read ledger no longer skips recovery. The existing progress-only retry limiter still stops further turns that make no verified progress; missing verified reads still fail rather than turning archive inventory into a false learning success.
2. **Restore resettable, persisted learning-team recovery configuration:** Bump the supplied Council team seed version to 48. The built-in `learning-round` team now opts into bounded actual source excerpts on recovery. The existing seed migration updates only unchanged system-seed rows; saved user-owned team configurations are preserved.
3. **Generic workspace content classification:** Select review excerpts through the existing upload workspace's `IncludedInPrompt` and package-kind classifiers instead of an extension-only `.cs`, `.md`, `.json`, etc. list. Give one candidate from each represented archive priority before filling other slots, under the existing size and pagination bounds.
4. **ZIP-based package pages in workspace tools:** `chat.upload_workspace_file` now delegates to the shared semantic package reader when the selected file is a ZIP-compatible container, including Office/OpenDocument/EPUB and nested ZIPs. The pagination cursor advances over normalized semantic text, not compressed binary. Existing package entry/path/expanded-size limits remain authoritative.
5. **No unrelated learning-workspace substitution:** An explicitly named learning workspace that no longer exists will no longer silently import a different newest workspace instead. No file or project is treated as learned without the source that actually supplied its evidence.
6. **Machine/volume distinction in the user-resettable Learning Round prompt:** Build storage, a mounted disk, or a source path is not automatically a model host, machine, GPU, or runtime identity. Host claims require configured hardware/provider evidence. No machine-specific disk label was added.
7. **Verified file-read receipts must contain source text:** The Council's function gateway and Ollama truncated transport metadata now preserve the source-reader `kind` in verified metadata. The completeness verifier rejects binary/diagnostic responses and zero-character reads, avoiding false semantic coverage from a non-text or empty archive summary.
8. **Regression test:** New `tests/static/test_learning_round_source_recovery.py` covers the source-gate retry, classifier, ZIP pagination, seed-upgrade ownership, absent-workspace behavior, and absence of machine-specific paths.

## Out of scope / not claimed

- This does not read thousands of files into an LLM in one request. The existing durable tracked-file checklist remains the mechanism for incremental, hash-verified reads and review, with unfinished files remaining pending for subsequent work.
- The workflow still requires actual executable read receipts. ZIP extraction/hash inventory is **not** accepted as semantic file review.
- Existing user-modified teams remain unchanged until the operator edits or resets them. Media OCR, video analysis, proprietary RAR/7z decompressors and generic binary comprehension are not provided by this source repair.
- This release does not import, overwrite, or repair the user's SQLite database. The JSON export was used only for a read-only aggregate integrity audit.
- A .NET build, PowerShell source gates and a live Council/model run could not be executed in this environment; validate under the target .NET 10 environment.

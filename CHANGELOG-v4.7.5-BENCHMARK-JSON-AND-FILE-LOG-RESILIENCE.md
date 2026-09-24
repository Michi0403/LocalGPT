# LocalGPT 4.7.5

## Provider benchmark JSON exception-flood repair

- Reworks the provider benchmark structured-output scanner so it no longer invokes `System.Text.Json` on every brace found in arbitrary model prose/code.
- Finds a plausibly JSON-shaped opening object, performs a non-throwing quote/escape-aware brace-balance scan, and only hands a complete candidate to `JsonDocument.Parse`.
- Skips malformed balanced candidates as benchmark evidence and resumes after the candidate instead of retrying every nested/opening brace.
- Adds the same completeness guard to textual DX-function recovery so incomplete model-carried JSON is not routinely sent into the throwing parser.
- This directly targets the `JsonReaderException` first-chance storm observed during the long-running provider benchmark. It is separate from 4.7.4's `ReadLineAsync`/`TaskCanceledException` repair.

## File-log resilience

- Replaces the former one-background-thread/one-file-writer-per-logging-category arrangement with one provider-owned queue and one serialized file writer shared by all categories.
- Keeps one append stream open with `FileShare.ReadWrite | FileShare.Delete`, reducing cross-category append races and file-sharing failures under heavy Council/benchmark logging.
- Transient I/O failures close and reopen the writer with bounded retries instead of terminating file logging.
- If the configured log remains unavailable, entries are written to the per-user temp fallback `LocalGPT/LocalGPT-fallback.log`; write-failure diagnostics are throttled so logging failure cannot create another console flood.
- The shared sink drains queued messages during provider disposal and does not dispose its queue out from under a still-draining background writer.
- Development file logging now defaults to `Information` rather than `Error`, so debugger-attached overnight runs keep producing current file timestamps and useful progress evidence. Release defaults remain unchanged to avoid unexpectedly increasing normal installed log volume.

## Clarification for already-running benchmarks

- A Council/benchmark process that was already running before LocalGPT is restarted cannot acquire these source changes. The old process continues executing the already-loaded assembly until LocalGPT is restarted with 4.7.5.
- 4.7.4 fixed the framework `ReadLineAsync` cancellation leak that could pause the debugger; it did not eliminate the benchmark parser's first-chance `JsonReaderException` noise. 4.7.5 addresses that separate path.

## Compatibility and scope

- Preserves 4.7.4 Council cancellation/debugger repair.
- Preserves 4.7.3 ASCII popup dropdown, scrolling, fitting, large-Council projection and responsive modal repairs.
- Preserves 4.7.2 Council provider/model binding repair and Kernel Creature Tournament improvements.
- Preserves existing `@rendermode InteractiveServer` coverage.
- PublisherStudio source is unchanged in this release.
- No GitHub/network repository access or .NET build tooling was used while preparing this source package.

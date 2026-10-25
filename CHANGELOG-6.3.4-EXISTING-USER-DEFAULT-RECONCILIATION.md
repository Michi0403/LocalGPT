# LocalGPT 6.3.4 — existing-user default knowledge and Council reconciliation

Base: LocalGPT 6.3.3 source. This is **not** a compiled or Windows-tested release.

## Fixed

- Existing installations now run the release-owned core seed stages (knowledge, regex, prompts, variables, core projects and model presets) **and** explicitly reconcile the previously lazy-only Council team workflows and game/runtime class templates on every process start after updating. This does not require the user to visit the Council or game UI.
- The supplied Council team and game/runtime class catalogs now verify their required keys from the persisted database after changes are saved. Empty shipped Council or game catalogs fail loudly instead of appearing complete.
- The core initializer now verifies the six maintained Council model presets and both reserved core project identities after each corresponding seed stage commits, in addition to the 6.3.3 knowledge/prompt/regex/system-variable checks. Model presets are defined once and shared with verification.
- A second post-reconciliation audit checks **eight** categories of maintained default records by stable identifiers. A missing required record prevents claiming successful default reconciliation.
- Each startup attempts to write a privacy-safe `%LOCALAPPDATA%\LocalGPT\runtime\seed-reconciliation.json` status report (using the application's platform-aware runtime folder), with the release version, UTC timestamp, status, and per-category expected/present counts. This is a report, not a database export. Failures are logged critically and marked `Failed` if the runtime report is writable; supplemental failures leave unrelated application facilities usable but explicitly degraded, while core migration failures remain fatal.

## Existing-user data policy

- Adds missing package-defined defaults. Does not import another user's database.
- Preserves existing custom Knowledge, prompts, regexes, variables, Council teams and game classes; seed-owned unedited references may be updated only under the existing source-hash policy.
- Explicit Council-team deletion tombstones remain deleted. The existing team preservation strategy retains a modified supplied team as a user-owned copy when upgrading the system template.
- Does not install Ollama model weights, overwrite provider URLs, change hardware roads, or copy logs, chats, system-specific paths, active games, or learned project content.

## Deployment required

Older binaries do not contain these changes or the 6.3.2/6.3.3 knowledge resources. **Users who installed yesterday's release must receive and start a newly built/published 6.3.4 update under their existing Windows user account and database**. Starting the old binary does not magically fetch new seeds; an installer/updater is needed for delivery. Do not delete `%LOCALAPPDATA%\LocalGPT` or replace the existing database when updating.

## Validation boundary

Offline Python source/regression tests and ZIP integrity only. No dotnet SDK, compiler, EF migration, installed Ollama service, Windows installer, or public-test runtime was exercised here. Startup report status `Complete` is the criterion for one actual upgraded installation; it is not a guarantee of model-weight availability or AI model comprehension.

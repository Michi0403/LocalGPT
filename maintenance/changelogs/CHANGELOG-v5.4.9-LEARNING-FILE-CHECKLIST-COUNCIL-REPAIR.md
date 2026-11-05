# LocalGPT 5.4.9 — Learning Council file checklist and function repair

## Repairs

- Resolves Ollama's automatic DXFunction calls by either the registered canonical name (`chat.upload_workspace_files`) or its advertised transport alias (`chat_upload_workspace_files`). Resolution remains restricted to the current automatic-function allow-list and functions that explicitly support automatic invocation.
- Extends `localgpt.learning.maintain.regexPatterns` with optional `description` and literal `sampleText` fields. Provided samples must match the pattern before it is stored. Learning retains the existing regex curator, timeout validation, untrusted suggestion status and user-review boundary.
- Preserves the deterministic upload-workspace hash/curation gate as byte-coverage evidence; it is not counted as completed semantic learning.

## Per-file learning checklist

- Extends existing database-first project tracked-file identities with durable `Pending`, `Reading`, `NeedsReview`, `Blocked` and `Completed` states, stored read cursor, EOF flag, reviewed SHA-256, concise source-backed findings, blocker explanation and timestamps.
- A new EF Core migration updates existing SQLite databases and marks their already tracked files **Pending**, rather than assuming they were learned before this feature existed. The model snapshot and indexes were updated with the migration.
- Project scan and learning workspace synchronization create new pending entries, reopen changed hashes and preserve previously reviewed hashes when unchanged. Missing/excluded files remain visible as blocked tracked entries.
- Adds bounded, policy-approved DXFunctions `localgpt.learning.files`, `localgpt.learning.file.read` and `localgpt.learning.file.review` for inspection, resumable text reading and model-suggested per-file findings. EOF and the current source hash are checked before completion; binary/unavailable sources remain blocked rather than falsely completed.
- Project maintenance gains a filterable DevExpress grid with one row per tracked revision file and aggregate open/reviewed/blocked counters. New UI labels are supplied across all six existing localization catalogs, including real German translations. The original file-pattern editor and all other maintenance components remain intact.
- The supplied Learning Round Council seed now lists the checklist functions and instructs reviewers to continue open tasks, preserve exact remaining counts when model/session limits intervene, and never equate archive hashing with semantic review. Existing user-edited Council configurations remain authoritative.

## Scope and limitations

- The new persistent checklist covers files in the existing database-first **project/revision tracked-file structure**; original attachments that do not synchronize into a project remain represented by the existing upload-workspace curation report.
- The AI Council can resume the saved cursor during subsequent learning runs. This release does not claim an unattended always-on AI worker or that thousands of files can be semantically learned within a single bounded Council session.
- File review results remain model-suggested evidence, not human approval or authorization for builds/network/process/source writes. The shared permission gate is unchanged.
- No .NET build, restore, NuGet download, publish, runtime UI or Ollama integration test was executed in the release-preparation environment.

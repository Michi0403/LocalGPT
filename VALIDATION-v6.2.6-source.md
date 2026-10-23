# LocalGPT 6.2.6 validation (source-only)

## Scope

Complete source archive based on 6.2.5. User-provided 6.0.12 source, Council log and SQLite JSON export were reviewed locally. Uploaded database/logs are NOT included in this release archive. No production database write occurred.

## Confirmed source behavior

- Existing provenance gate (`GetVerifiedWorkspaceReadProgress`) continues to require completed registered file-read evidence, not a curator hash/list response. Binary/diagnostic and zero-character receipts cannot count as a completed semantic source read.
- An empty first ledger now permits the first configured recovery attempt; subsequent attempts are stopped when the prior turn added zero progress.
- Supplied Learning Round seed now includes bounded source excerpts and uses the existing seed-version upgrade that does not overwrite user-modified team rows.
- Workspace source candidate selection uses existing file-kind metadata (including ZIP-compatible packages), not a fixed set of code extensions.
- Workspace paged file reading uses the shared `ArchivePackageService.ReadSemanticPageAsync` for ZIP containers with the original package safety limits.
- An unknown explicitly named workspace never gets silently replaced with another user's or project’s newest available workspace.
- Existing PowerShell build/audit scripts, permissions, and 1-Wire approval gates were not changed.

## Executed

- Python unittest discovery: 20 tests passed (13 preexisting plus 7 new).
- Original source-file retention and ZIP entry integrity: verified during packaging.
- JSON-export read-only count/foreign-key checks: completed. Details in `DB-AUDIT-v6.2.6.md`.

## Not executed

- dotnet build/test: .NET SDK unavailable.
- Windows/macOS PowerShell architecture guards: PowerShell unavailable.
- Live Ollama/model invocation and database migrations: unavailable and not simulated.

## Recommended acceptance

1. Back up the production SQLite database before installing any new source build.
2. Confirm the supplied `learning-round` seed advances to v48 without editing `learning-round2` or other saved user-custom teams.
3. Run Learning Round with multiple uploaded archives: check that each archive root is visible, the inventory step makes a read attempt before declaring stalled, and an actual `chat.upload_workspace_file` function receipt exists.
4. Confirm normal project synchronization reaches the per-file Learning Checklist and that `Pending → Reading → NeedsReview → Completed` advances only on source-backed, hash-stable reads and review.
5. Verify DOCX/XLSX and nested ZIP source pages have stable `NextOffsetCharacters`, and that symlinks/traversal/oversized ZIPs are still rejected.
6. Distinguish source-root/mount paths from registered AI-host hardware identities in user-visible results.

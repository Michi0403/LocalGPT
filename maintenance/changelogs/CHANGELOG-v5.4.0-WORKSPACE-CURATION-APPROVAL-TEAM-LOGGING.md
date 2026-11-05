# LocalGPT 5.4.0 — workspace curation, research approval, Council sampling, and durable logs

## Multi-archive upload curation

- Introduced a deterministic, nonexecuting `IChatUploadWorkspaceService.CurateWorkspaceAsync` gate with per-upload and per-ZIP archive extraction coverage. Each original and safely extracted file is streamed to EOF and SHA-256 hashed, with an on-disk `curation.json` audit and bounded `curation.md` summary.
- Individual archives retain their own safe extracted roots and file counts. Missing, unsafe, excess, unreadable or otherwise incomplete extracted entries fail the gate rather than silently advancing.
- Upload manifests record the number of selected original attachments; curation rejects missing stored originals, even when earlier prompt-context truncation could have concealed an attachment. Upload batches exceeding configured file count now fail visibly rather than silently dropping later files.
- File-list DXFunctions put original uploads before extracted content, avoiding the old failure where a large first ZIP filled the listing before the second original attachment appeared.
- A real `chat.upload_workspace_curate` DXFunction exposes the bounded report. A workflow-step option requires the deterministic gate to complete before that step can run; unknown explicitly referenced workspaces cannot pass.
- Byte-level completeness and semantic understanding are distinct. Downstream models must use the existing exact read-only source-file DXFunctions to verify project metadata and source meaning; no claim of having semantically read thousands of files is made simply because they were hashed.

## Council research judge and human approval

- The default Learning Round adds a **Workspace curator** control role and a **Web research necessity judge** role before analysis/verification.
- The judge prefers available local evidence and can propose the minimum `localgpt.web.search` query only if current public information is actually needed. This pre-existing DXFunction uses the current human-approval request/feedback UI and does not execute a DuckDuckGo request until approved.
- New per-step `WaitForDeferredApprovalsBeforeNextStep` waits on persisted approval state using the existing deferred-DXFunction and human collaboration services. Approved results become attributed, untrusted downstream evidence; a decline permits local-only continuation.
- User-edited team workflows remain user-owned; the new capabilities are editable in the existing DevExpress Council Teams page.

## Seed team-size ownership

- Supplied, non-user-modified Council roles that previously selected *all* AI participants now use `RandomRange` with persisted runtime-policy minimum/maximum defaults **2–3**. Existing deliberately specialized 1-member or paired game roles and user-edited presets are not flattened.
- Existing `LocalGptRuntimeValue` policy, runtime seed catalog and EF migration `20261009170000_AddCouncilCurationAndSeedSamplingPolicy` own the configurable defaults (`INSERT OR IGNORE`). Council team seed revision is incremented, preserving user modifications and deletion tombstones.

## Application file logging

- Fixed production `LoggingCore:FileCore:CoreLogLevel` being set to `Error`, which filtered successful Council `Information` events from `LocalGPT.log` even as durable Council Markdown files continued updating. Production file logging now includes Information, using the already-owned `FileLoggerCoreOptions` configuration.
- After atomic Council Markdown log persistence, a structured Information event records the run ID and audit-file path in the normal LocalGPT file logging pipeline. No prompts, source payloads, credentials or model thinking are logged.

## Validation boundary

- Source-only architecture, async-boundary, async-continuation, service-resilience, provider-qualified Council and configuration/coverage checks completed in the provided development environment without .NET or NuGet execution. The user's Windows build and runtime remain the authoritative compiler/startup checks.
- PublisherStudio is unchanged.

# LocalGPT 5.4.4 — Council source truth, upload continuation, progress and renderer repair

- Stop re-opening and eagerly analyzing every extracted ZIP entry during the upload transaction. Preserve safe per-archive extraction and indexed on-demand source access. Keep a bounded manifest preview; full inventories remain available with search and pagination.
- The deterministic curation report now reads real `.csproj` TargetFramework/TargetFrameworks declarations and includes exact paths. Framework facts from current source are authoritative over stale .NET 8 knowledge. Large multi-project workspaces retain full hashes/files in the report, while prompting is bounded.
- Strengthen the research judge's requirements for version-matched documentation, LearningBase/RemoteSources and human approval. When a required source is unavailable, use the existing `human.collaboration.request` blocking resource question rather than an unsupported assertion.
- Surface live Council status and continuation upload indexing in Chat while a model is running. Persist accepted human follow-up messages immediately, so other browser tabs can recover them from SQLite memory instead of waiting for auto-save.
- Protect the delayed Chat Council-list refresh against renderer/circuit disposal at dispatcher entry and suppress expected shutdown races. Preserve supervised cancellation.
- No changes to PublisherStudio.

- Follow-up ZIP batches are curated at the next human heartbeat **before** the queued user message is drained. The evidence gate checkpoints the result into the Council log, reports progress to the active session, and carries source metadata to later steps; incomplete batches remain queued.
- Each configured learning phase after inventory uses the existing next-phase human collaboration boundary, allowing blocked source requests rather than silent assumptions. Official GitHub/LearningBase imports remain human approval gated.
- On exact run rejoin, the Chat page tries to recover only SQLite conversations with the matching live-Council marker; it never picks an unrelated newest conversation.

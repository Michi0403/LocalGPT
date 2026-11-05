# LocalGPT 6.0.8 — architectural repair of same-team source recovery

Full source release based on the previously supplied LocalGPT 6.0.7 source archive. Original team approval and tool authorization boundaries remain in force; no assertion or PowerShell maintenance guard was relaxed.

## Corrected

- Fixed the build-breaking forbidden application-static declarations in `MultiModelCouncilService.SourceReviewRecovery.cs`: helper methods are now instance-owned service operations with method-level `ILogger` exception diagnostics.
- Fixed a substantive archive-recovery defect: `QueryFiles(take: 20000)` did not imply that 20,000 files were returned because the workspace service enforces runtime-policy pagination. The recovery now walks bounded pages using `NextOffset`, scans actual extracted source paths, prioritizes human-readable project entry points, reads bounded excerpts through the maintained workspace read service, and records warnings if the safety limit is reached.
- Removed the inaccurate source-review shortcut introduced in 6.0.7: an AI merely echoing a known file name or injected excerpt **does not** count as successful `chat.upload_workspace_file` function evidence. Completion remains tied to actual registered function receipts.
- Preserved same-team X-round recovery and bounded build/playtest verification without accepting unsupported model claims as successful builds. This does not bypass user approvals, capability policy, or source curation.
- Incremented version from 6.0.7 to 6.0.8.

## Verification/limitations

- Source-level audit against repository application-static policy; reviewed configured gate and workspace pagination interfaces. No .NET build, live Ollama Council session, or Windows runtime run was possible here. This release is **not** verified compilation-clean or game-playtest-complete.
- The source-review recovery scan remains bounded and may report incomplete coverage for very large workspaces; it must not silently claim full inspection.
- Automatically spawning additional Council teams/sessions or opening the requested approval poll is **not implemented** by this maintenance release. A verified tool result remains mandatory for build/playtest claims.

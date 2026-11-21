# LocalGPT 7.6.4 — requirements-to-source closure

**Source-only candidate. No .NET compilation or PowerShell execution by the source editor.**

This change directly addresses the November 20 TwitchManager incident recorded in `maintenance/workflows/TWITCHMANAGER-SOURCE-CLOSURE-7.6.4.md`. It does not claim a successful application artifact.

## Included changes

- Persisted run-scoped non-approval human answers (including `Consumed`) are now read through the existing DI Human Collaboration service with stable decision identity/provenance. Latest decisions enter every architect planning attempt and the immutable shared contract; no cross-run fallback or approval escalation.
- New configurable seeded Council instruction treats recorded database provider/name and other human choices as protected project requirements, not model opinions or side-effect authorizations.
- Source manifest gate rejects missing ASP.NET/Blazor host, root component, explicitly requested Twitch/timeline implementation, selected SQLite data work, missing solution or incorrect selected solution name. It rejects before the draft is persisted, using the existing bounded architect retry path.
- Per-file deterministic `.sln`, `.slnx` and project-reference validation rejects missing solution header/configuration, GUID placeholders, self/missing/out-of-root project references, invalid XML and missing project files from manifest. It runs before source hashing/inspection and again before final review.
- Complete draft submission revalidates exact latest human decisions, full reviewed project graph, SQLite provider/source implementation and project-owned Razor injected interfaces. Immutable review and approval are blocked for missing source; already valid source is retained.
- ExecuteReviewAsync validates existing source reviews **before** consuming human approval and filesystem writes, preventing old incomplete reviews from slipping through new gates.
- Distinguishes `BuildDisabled`, `BuildNotAttempted` and `BuildFailed`; preserves source-only ZIP and review/repair behavior, never claims verified build without the original positive receipt.
- Updated original PowerShell Council architecture assertions (order, consumer/producer, source references) without disabling build gates.

## Remaining work

The change is intentionally conservative. General user-choice parsing for arbitrary storage providers, full architecture-driven completeness beyond the observed Blazor/Twitch/SQLite case, late additive manifest amendment, database revision structure refresh, distinct verification receipts and runtime-tested multiproject solutions remain **open** in cumulative TASKS.md. No hardware-road, provider limits, seed overrides, network GitHub or PublisherStudio changes were made.

## Owner verification

Run existing PowerShell architecture checks, compile on .NET 10, test current run-scoped/consumed human answers and negative/positive fixtures in workflow document. Test owner-side source-only and explicitly disabled builds separately from a real compile. Observe no file/approval side effects on a rejected manifest or review.

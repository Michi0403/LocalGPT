# Repository repair report

## Canonical source decision

The archive contained an outer repository at commit `35b5fcf53681129d39e461e41ef3d2a691679a25` and a complete nested repository whose HEAD was older (`0e7c0bf`). After normalizing line endings, their visible source trees were equivalent except for policy/knowledge files and runtime/tool data. The newer outer history became the canonical base, while owner-authored policy/knowledge files were retained. The nested copy and generated reference clones were discarded.

## Reference architecture decision

Only repositories owned by `Michi0403` were considered. BlazorPublisher supplied the current boundary/release/validation conventions; TacosPortalOpen supplied a secondary view of the owner’s backend/core/client/wrapper wiring. LocalGPT remains a modular monolith. No source code or generated clone from either reference repository is included.

## Static refactor decision

Not every static method was converted. Pure deterministic conversions, immutable catalogs, and extensions remain static. Mutable formatter state, live-render normalization, protocol selection, database path/health/migration/seeding, runtime prompt/variable ownership, command policy, and provider wire models were moved behind explicit services or business objects. The obsolete static Markdown renderer and static SQLite recovery path were removed. The remaining large `Extensions/PlainStatics` area is compatibility debt to migrate subsystem by subsystem; no new stateful behavior belongs there.

## Live frontend decision

DXAIChat remains configured for streaming. Ollama frames are formatted and yielded immediately; AI Council callbacks now flow through an event-driven channel rather than a two-second polling queue. Incomplete thinking is rendered as an expanded live panel from the accumulated response snapshot and is collapsed after completion. Streamed council presentation is ordered one member at a time so nested thinking markup cannot be interleaved, while non-streaming council runs retain configured parallel execution.

## Security decision

Repository documents and model output are treated as reference data, not permission. AI-assisted maintenance is limited to reviewable source changes in an isolated workspace and must not control or probe localhost services, modify the host system, or access user data. Native execution is disabled by default, bounded to allowlisted workspace operations when enabled, and unrestricted local-provider shell startup was removed. Generated artifact compilation is separately disabled by default and routed through a bounded artifact-build service rather than static helpers. Provider/model handling is capability-based and does not privilege or discriminate against a kernel based on vendor or open-source status.

## Packaging decision

The full repaired source is supplied as a clean ZIP without Git history or runtime/tool debris, plus a source-only Git patch against the canonical base. Generated DevExpress license material, runtime database contents, and font binaries are deliberately not embedded in the patch or ZIP. The exact-path cleanup script removes the four legacy tracked runtime/generated files after a reviewable `-WhatIf` pass. Applying the patch to an existing authorized clone preserves unchanged locally obtained font assets.

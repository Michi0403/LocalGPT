# LocalGPT v0.1.4 — unified release

This release is based on `LocalGPT-v0.1.3-thinking-panels` and folds in the still-relevant DXFunctions, local-source, and missing-feature-report requirements without reintroducing the old static utility architecture.

## Added

- `IDxAiFunctionServiceClient`: a scoped, cancellable control-flow client over the existing DI-backed `IDxAiFunctionRegistry`; no loopback HTTP client or duplicate function system.
- Chat session context containing conversation, project, exact project-version, and LocalGPT application version identifiers.
- Project/version selection in the main Chat page. Saved conversations restore that context.
- Per-assistant-message helpful/not-helpful feedback and optional local comments, persisted in SQLite and retained across autosaves.
- EF Core migration `20260725150000_AddChatSessionControl` and synchronized model snapshot.
- Isolated model-family protocol profiles for Harmony/gpt-oss, DeepSeek, Gemma, Apple/OpenELM/MLX, generic `<think>` models, and plain text.
- Automatic protocol selection as the default. Explicit per-provider protocol selection remains supported.
- Protected-governance layer for Codex, Claude Code, and Copilot: immutable agent instructions, CODEOWNERS, SHA-256 source guard, CI enforcement, and optional local read-only attributes.
- `MISSING_FEATURE_REVIEW-v0.1.4.md`, separating implemented requirements from already-present, obsolete, or unsafe report suggestions.

## Preserved from v0.1.3

- Streamed thinking panels and final-answer separation.
- Project collaboration, project versions, topics, and reviewed knowledge links.
- DI-backed DXAIFunction discovery and confirmation-gated mutations.
- Database-backed code-generation change reviews and bounded artifact workspaces.
- Human-control and workspace-isolation rules.

## Deliberately not copied

- The former `PlainStatics` monolith and duplicate HTTP helper classes from older/local branches. Their still-useful behavior already lives behind injected services such as `NavigationUrlService`, `LoggingConfigurationService`, and the existing EF/service boundaries.
- Council report sample code that used async work in constructors, mismatched string/integer keys, invalid DevExpress component APIs, or duplicate `AppVersion`/chat tables.

## Database compatibility

The new migration adds nullable project/version fields and nullable feedback fields. Existing conversations receive `ApplicationVersion = "legacy"`; newly saved conversations receive the current application version.


## Reliability fixes found during final audit

- A failed chat-memory save no longer replaces the active conversation ID or displays a success state.
- Persisted feedback is retained only when message order, role, and content still identify the same assistant response; inserting or replacing messages cannot silently transfer a rating.
- The DXAI controller now routes through the typed client instead of bypassing its cancellation and context boundary.
- The new protocol fallback catalog no longer violates the repository's no-unreviewed-static-class source guard.

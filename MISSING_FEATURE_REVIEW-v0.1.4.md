# LocalGPT v0.1.4 missing-feature review

## Inputs reviewed

- `LocalGPT-v0.1.3-thinking-panels` — authoritative baseline.
- `LocalGPT-v0.1.2-dxfunctions-wired` — checked for DXAIFunction behavior superseded by the baseline.
- `260725_LocalGpt.zip` — checked source-only; generated workspaces, runtime databases, logs, binaries, and old `PlainStatics` code were excluded.
- The current and older AI Council missing-feature reports — treated as ideas and requirements, not trusted code.

## Implemented in v0.1.4

| Requirement | Resolution |
|---|---|
| Typed DXAIFunction client | Added scoped `IDxAiFunctionServiceClient` / `DxAiFunctionServiceClient` over the existing DI registry. It supplies operation IDs, deterministic cancellation, and conversation/project/version context without loopback HTTP. The public DXAI controller now uses this client. |
| Controlled chat feedback | Added assistant-response selection, helpful/not-helpful rating, optional comment, reload support, and SQLite persistence. Autosave preserves feedback only when the persisted response identity still matches. |
| Project-version linking | Reused the existing `LocalGptProject` and `LocalGptProjectVersion` domain instead of creating duplicate `AppVersion` tables. Chat sessions persist project ID, exact project-version ID, and LocalGPT application version. |
| Prompt/session history | Extended the existing `ChatMemoryConversation` and `ChatMemoryMessage` persistence instead of creating a parallel chat schema. |
| Model-format isolation | Added separate DI profiles for Harmony/gpt-oss, DeepSeek, Gemma, Apple/OpenELM/MLX, generic `<think>` tags, and plain text. Explicit provider configuration still wins; automatic model-name routing is isolated per profile. |
| Database evolution | Added migration `20260725150000_AddChatSessionControl` and synchronized the EF Core model snapshot. Existing rows receive the `legacy` application-version marker. |
| Agent governance | Added readable-but-immutable governance instructions for Codex, Claude Code, and Copilot; CODEOWNERS; SHA-256 validation; CI enforcement; and optional owner-run local read-only hardening. |

## Already present in the v0.1.3 baseline

The following report items were verified as existing and were not duplicated:

- Ollama and local-provider discovery returns explicit failure/status results instead of requiring a new fallback subsystem.
- Council knowledge already exposes user approval, verification status, last-verified timestamps, expiry/staleness handling, and a database UI workflow.
- Build-debug inventory and diagnostic routes already exist.
- DI-backed DXAIFunction discovery, automatic-safe read-only gating, fresh confirmation for mutations, and bounded change-review workspaces already exist.
- Streaming thinking/final-answer panels and browser state preservation already exist.

## Deliberately rejected from broken or obsolete reports

- Duplicate `AppVersion`, `ChatSession`, and `ChatMessage` tables that conflict with the existing EF domain.
- A loopback `HttpClient` to call LocalGPT's own DXAIFunction controller from the same process.
- Async work inside constructors, string/integer foreign-key mismatches, invalid DevExpress markup, and raw SQL migration stubs detached from the EF model.
- Reintroduction of `Extensions/PlainStatics`, duplicate HTTP helpers, global mutable state, or generated build folders.
- PublisherStudio screenshot control, audio/MIDI, browser automation, and multimedia-generation proposals. Those reports describe separate products or future integrations and are not required to unify LocalGPT v0.1.4.

## Remaining owner-environment validation

A licensed Windows build remains necessary because this workspace has no .NET 10 SDK, DevExpress private feed/license, Windows App SDK workloads, WebView2 runtime, or signing toolchain. Source-level validation is documented in `VALIDATION.md`.

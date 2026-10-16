# LocalGPT 5.9.1 — Council automatic-tool exposure regression repair

## Root cause verified by source-version comparison

In 5.5.0 and 5.6.1, `OllamaThinkingChatClient.GetAutomaticFunctions` allowed every DI-registered, AI-exposed, automatically callable function when `automaticFunctionAllowList == null`. By 5.7.1 the same `null` was silently redefined to advertise only handlers with `Source == "DxFunctionBroker"`, and this remained through 5.9.0. The handlers for `chat.upload_workspace_files` and `chat.upload_workspace_file` were still DI-registered. The Ollama adapter emitted `The requested automatic function is not registered` when its per-call tool-name resolver rejected a name *before calling the central DXFunction registry*. This is distinct from an absent workspace or corrupt upload.

## Repair

- Restore `null` to the verified 5.5.0 unrestricted default for AI-exposed, directly/automatically callable functions.
- Preserve normal-team preferred native lists and the catalog discovery/preview/invoke broker.
- Let normal teams with a broker-enabled preference list resolve additional registered and automatic-safe function names without treating preferences as a hard permission boundary. Avoid ambiguous sanitized tool names.
- Preserve strict team/step and live-game allowlists: no broker and no nonlisted fallback.
- Preserve handler DI registrations, scoped runtime service ownership, central function approval enforcement, source curation, and existing saved teams.
- Fix runtime diagnostics and XML parameter contracts to distinguish unrestricted native exposure from broker-only exposure; add a PowerShell source regression guard and an architectural Mermaid sequence diagram in the existing DocFX architecture chapter.

## Limits

This repairs the source-level function exposure regression but does not prove that an individual model will choose to invoke a tool, continue after invoking one, or finish a game rebuild. A real Windows/Ollama run must confirm successful structured `chat.upload_workspace_files` and `chat.upload_workspace_file` results, as well as the later build and playtest steps. No .NET or Ollama build was run while preparing this archive.

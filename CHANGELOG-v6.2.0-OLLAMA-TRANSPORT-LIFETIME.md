# LocalGPT 6.2.0 — Ollama streaming transport lifetime

## Changed

- Preserved but commented out `http.Dispose()` in `OllamaThinkingChatClient.Dispose()`. A streaming Ollama request may still be using the transport when a session is retired.
- Preserved but commented out chat-client disposal at the two premature retirement sites in `Chat.ProviderRuntime.razor.cs` and `CompositeChatClient.cs`.
- Left cancellation, response/request disposal and all existing Council/tool functionality unchanged.

## Important boundary

The requested comment-out prevents these identified disposal calls but intentionally postpones transport reclamation. The full follow-up is reference-counted transport leases and coordinated shutdown after all streaming enumerators finish. This change alone does not prove every possible disposed-object stack trace is resolved.

## Validation

No .NET build or Windows PowerShell policy gate was run; the toolchain is unavailable. The source file changes and the release archive have been inspected. See `VALIDATION-v6.2.0-source.md`.

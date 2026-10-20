# LocalGPT 6.0.10 — valid bounded Ollama tool-result JSON

## Observed failure

On a Windows Debug run using the 6.0.9 source, `CouncilRuntimeService.FormatJsonForUserVisibleCode` raised a first-chance `System.Text.Json.JsonReaderException` while displaying a `chat.upload_workspace_files` result. The logger confirmed that the underlying DXFunction had completed successfully. The error reported a character at byte 16004 after a JSON value.

## Root cause and repair

`OllamaThinkingChatClient.SerializeToolResult` previously truncated serialized JSON at the configured maximum and appended a second JSON object (`{ "truncated": true }`). The resulting message is malformed JSON, particularly when truncated in a quoted string, escaped sequence, array, or object. This affected both the content sent back to the model and the durable user-visible trace, not just pretty-printing.

The Ollama transport now constructs exactly **one** JSON value. Results fitting within the operator-configured cap remain byte-for-byte unchanged. Oversized results are wrapped in a size-bounded, valid JSON truncation envelope containing the original function name, status, success flag, original character count, and an explicitly partial serialized-result string. The retained prefix is binary-searched against the *serialized* envelope length, accounting for JSON escaping. UTF-16 surrogate pairs are not split. Results no longer masquerade as complete evidence when the cap is reached.

An extremely small operator cap unable to hold a complete envelope yields a minimal valid `{"truncated":true}` marker and a warning rather than malformed JSON. No source payload or tool arguments are logged. Workspace files can still be obtained through the existing filtered/paginated DXFunction contract.

## Compile repair

The new `CouncilTeams.RecoveryPolicies.razor.cs` partial now imports `LocalGPT.Components.Shared` so `LocalGptSelectionOption<T>` resolves correctly; this was reported after the 6.0.9 source delivery.

## Architecture and limitations

The change is generic Ollama wire-serialization behavior, not a team-key special case. Existing saved team policies, BusinessObjects, EF database configuration, model selection, source curation, approved function calls, and architecture guard scripts remain unchanged. No Python tool is added to the repository or required for builds.

A .NET SDK and Windows PowerShell are unavailable in the authoring environment. No Windows Debug build, runtime Council/Ollama integration test, or project-specific architecture guard run is claimed; see `VALIDATION-v6.0.10-source.md`.

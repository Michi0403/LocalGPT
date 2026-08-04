# Chat protocols and session context

## Protocol isolation

`IChatProtocolProfile` is a stateless DI boundary selected once for a response stream. Each family owns only its own model-name detection and control-token normalization:

- Harmony: `gpt-oss` and Harmony channel markers.
- DeepSeek: DeepSeek/R1 sentence and role markers, while retaining `<think>` boundaries for the streaming formatter.
- Gemma: beginning/end and turn markers.
- Apple: Apple/OpenELM/AFM/MLX role and turn markers.
- ThinkTags: generic `<think>...</think>` models such as QwQ/Qwen thinking variants.
- PlainText: no protocol transformation.

`Auto` is the default. An explicit provider setting overrides model-family detection. The stateful formatter is created per stream, so one model family cannot leak parser state into another.

## Chat session context

`IChatSessionContext` is scoped to the current Blazor circuit/request. It tracks:

- persisted conversation ID,
- optional LocalGPT project ID,
- optional exact project-version ID,
- LocalGPT application version.

`EfChatMemoryService` writes those identifiers into the conversation row. Reloading a conversation restores its project/version selection. `DxAiFunctionServiceClient` copies the same context into every direct function invocation.

## Message feedback

Feedback is stored on persisted assistant messages by conversation and sort order:

- positive, negative, or no rating,
- optional comment,
- UTC update timestamp.

Autosave replaces message rows but carries feedback forward by the unique conversation/sort-order key. Feedback never leaves the local SQLite database unless another explicitly user-approved feature exports it.

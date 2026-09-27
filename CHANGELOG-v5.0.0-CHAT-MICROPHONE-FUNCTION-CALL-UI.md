# LocalGPT 5.0.0

## Chat microphone visibility
- Moves `ChatMicrophone` out of unsupported absolute positioning over the private DxAIChat composer DOM.
- Places Voice in the existing DevExpress quick Council configuration row beside Team, Models and Performance.
- Changes the four quick controls to equal `ColSpanMd=3` layout slots.
- Keeps recording available with or without an installed speech-to-text model.
- Keeps provider-neutral `SpeechRecognition` routing, with managed Python/OpenAI Whisper preferred and compatible Hugging Face ASR selectable.
- Keeps transcribe/translate, language hint/chat-language selection, persisted audio and Council setup behavior.
- Replaces Open Iconic microphone/stop dependencies with self-contained SVG masks inside the existing DevExpress `DxButton`.

## Why the prior icon disappeared
- `ChatMicrophone` used `oi oi-microphone` and `oi oi-media-stop`.
- LocalGPT loads `site.css`, which imports `open-iconic-fallback.css`; that fallback only defines a small subset including plus/cog/reload/lock/people and does not define microphone or media-stop.
- The component was also an absolutely positioned sibling of `DxAIChat`, relying on undocumented internal composer geometry even though DxAIChat 25.2 has no composer-action template.

## DxAIChat function-call UI
- Keeps `IncludeFunctionCallInfo=true`.
- Adds `FunctionCallInfoContentTemplate` with expandable DevExpress-chat-integrated tool name, argument and result presentation.
- Keeps the existing `MessageContentTemplate`; no full `MessageTemplate` override is introduced because that would replace DevExpress message bubble rendering.
- Does not add a fake/inert `ResourceItemTemplate` until LocalGPT binds a real `AIChatResource` collection.

## Compatibility
- MCP gateway, OneWire, organic skills, approvals, project/workspace tools, code generation and all speech-to-text backend work remain unchanged.
- Version identity advances from 4.9.9 to 5.0.0 according to the single-digit minor/patch slot rule.

# LocalGPT 5.0.1

## DevExpress AI Chat function-call compile repair
- Corrects the `FunctionCallInfoContentTemplate` context handling introduced in 5.0.0.
- DevExpress supplies the owning `BlazorChatMessage` to this template; function-call details are therefore read from `BlazorChatMessage.FunctionCalls` rather than from nonexistent message-level `Request` / `Result` members.
- Each `AIChatFunctionCall` now renders its own request name, arguments and result inside the existing expandable LocalGPT panel.
- The fix directly addresses the Windows build diagnostics for `Chat.razor` lines that reported `BlazorChatMessage` has no `Request` or `Result` member; the generated lambda error was a downstream Razor consequence of the invalid template body.

## Preserved behavior
- Keeps `IncludeFunctionCallInfo=true` and the dedicated `FunctionCallInfoContentTemplate` instead of deleting the feature to make the build pass.
- Keeps the existing `MessageContentTemplate` and DevExpress message-bubble rendering.
- Keeps the Voice item in the normal-flow DevExpress Quick Council `DxFormLayout` beside Team, Models and Performance.
- Keeps provider-neutral speech-to-text, managed Python/OpenAI Whisper preference, compatible Hugging Face ASR selection, transcribe/translate/language controls, microphone recording and audio persistence.
- Keeps MCP, OneWire, organic skills, approvals, project/workspace tools and code generation unchanged.

## Version
- Advances LocalGPT from 5.0.0 to 5.0.1.

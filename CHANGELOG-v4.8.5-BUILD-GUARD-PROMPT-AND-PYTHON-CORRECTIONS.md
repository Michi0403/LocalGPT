# LocalGPT 4.8.5 — build-guard, prompt ownership and Python corrections

## Fixed

- Moved the Whisper setup Council instruction out of `Chat.Speech.razor.cs` into the persisted `IPromptConfigService` seed catalog as `WhisperSetupCouncilPrompt`.
- Kept microphone transcript submission dynamic while using existing localized microphone labels, avoiding new constructor-literal initialization outside the approved seed/configuration boundary.
- Removed an accidental reference to nonexistent `PythonRuntimeCandidate.Architecture`; Python configuration results now report the existing version, discovery source and runtime-library detection state.
- Added `localgpt-microphone.js` to the reviewed JavaScript diagnostics manifest.

## Function invocation

- Preserved generic DXFunction discovery: AI-visible direct-invocation functions are exposed to providers even when they are not automatically executable.
- Preserved registry enforcement of catalog policy, schema validation, automatic-invocation restrictions, exact human approval and deferred execution. Consequential functions can therefore be requested by the AI and become approval cards without becoming silently auto-authorized.

## Validation scope

- Source-only correction release. No .NET build, restore, publish, installer execution or GitHub access was performed by the assistant.
- The reported MSBuild failures were used as authoritative compiler/guard evidence for these corrections.

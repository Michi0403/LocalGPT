# LocalGPT 4.9.8 source validation

Source-only review was performed without invoking .NET, MSBuild, NuGet, publish, installer, packaging or GitHub.

Validated statically:
- version identity is 4.9.8 and follows the repository version-slot rule;
- MCP retains transport, admission, protocol, primitive, DXFunction and data-domain configuration values while the Razor surface uses DevExpress layout/accordion/edit controls;
- MCP/API/PFX secrets use DevExpress password editors;
- no native HTML input/select/textarea/button was added by this change;
- Chat still renders `ChatMicrophone` beside `DxAIChat`, and its main `DxButton` uses a bundled microphone/stop icon with an explicit high stacking context;
- microphone capture no longer exits early when no speech-recognition model exists;
- speech model selection is capability-based, Whisper-preferred and passes task/language into `LocalAiSpeechRecognitionRequest`;
- uploaded audio/media preserves the PublisherStudio route but selects local `SpeechRecognition` first when available;
- existing speech-team and setup-prompt keys are preserved for database compatibility while shipped definitions are generalized;
- the LocalGPT 1-Wire capability directory advertises the built-in `speech-to-text` organic skill without requiring a PublisherStudio connection for local transcription;
- PublisherStudio document handoff is constrained to advertised 1-Wire/organic capability contracts and does not fabricate an export path.

A local Windows build remains the compile/runtime authority because this handoff environment intentionally does not execute the .NET toolchain.

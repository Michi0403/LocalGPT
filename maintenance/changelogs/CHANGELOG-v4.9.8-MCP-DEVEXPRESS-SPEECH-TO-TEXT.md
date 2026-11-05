# LocalGPT 4.9.8

## MCP setup experience
- Reworks the MCP workbench into DevExpress `DxAccordion`, `DxFormLayout`, `DxFormLayoutGroup` and responsive `DxFormLayoutItem` sections instead of loosely stacked switches and editors.
- Adds endpoint summary tiles so the LocalGPT, OneWire, dedicated MCP and primary-loopback boundaries remain visible while editing.
- Masks MCP API keys, MCP PFX passwords, remote-endpoint certificate passwords and certificate-generation passwords with DevExpress password editors.
- Keeps the full MCP gateway, protocol compatibility, Human Collaboration, allow/deny filters, project filters, data-domain firewall, TLS and port features introduced in 4.9.6/4.9.7.

## Composer microphone and speech to text
- Restores the microphone as a persistent DevExpress icon button inside the Chat composer action area beside attachment/send/stop.
- Restores the Chat stacking/isolation contract so the microphone renders above the `DxAIChat` composer instead of occupying an invisible reserved slot.
- Uses bundled Open Iconic microphone/stop icons through `DxButton.IconCssClass` rather than a fragile scoped custom-mask child.
- Main microphone click always starts recording; missing transcription models no longer replace recording with a setup prompt.
- Speech options include any installed `SpeechRecognition` model, source-language/auto-detect hint and transcribe/translate task.
- Managed Python/OpenAI Whisper is ordered first when installed, while compatible Hugging Face/Transformers ASR models remain valid choices.
- Microphone transcription passes selected language/task into the existing generic runtime bridge and uses provider-neutral operator messages.

## Speech-to-text skill flow
- Generalizes the stable `whisper-assistant` Council blueprint into the visible **Speech to Text Team** without breaking persisted keys.
- Adds a built-in **Speech to Text** organic skill to the LocalGPT 1-Wire capability directory, describing local transcription/translation plus conditional PublisherStudio document handoff capabilities.
- Updates the persisted `WhisperSetupCouncilPrompt` to capability-driven speech recognition and supplies its previous shipped value as a legacy seed so untouched databases upgrade without overwriting user edits.
- Directs the team to return faithful transcript-only results, use recognized microphone text as prompt input when requested, and use PublisherStudio through actual advertised 1-Wire/organic capabilities for polished document workflows.
- Adds the already-implemented `publisher.website.content.request` contract to PublisherStudio offline capability discovery.
- Routes admitted audio/media uploads to `localai.audio.transcribe.workspace` before PublisherStudio when a local speech-recognition model exists; otherwise the existing PublisherStudio media fallback remains intact.
- Extends default admitted media extensions for common AAC/FLAC/M4A/OGG/Opus/WebM speech sources.

## Compatibility
- No MCP, DXFunction, OneWire, PublisherStudio fallback, Chat attachment, Council, project/knowledge or approval capability was removed.
- Version advances from 4.9.7 to 4.9.8.

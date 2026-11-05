# LocalGPT 4.9.9

## DevExpress MCP compile repair
- Fixes Razor compiler error `RZ9999` in the MCP `/install` workbench.
- Gives each `DxAccordionItem.ContentTemplate` an explicit section-specific template context instead of relying on the implicit `context` parameter.
- Prevents that outer DevExpress template parameter from colliding with nested `DxFormLayoutItem.ChildContent` template parameters.
- Keeps the 4.9.8 `DxAccordion`, `DxFormLayout`, `DxFormLayoutGroup`, responsive `DxFormLayoutItem`, secret masking/generation helpers and all MCP configuration fields intact.

## Preserved functionality
- Keeps the MCP transport/listener/protocol, client-admission, credential, DXFunction and data-domain controls introduced in 4.9.6-4.9.8.
- Keeps the persistent Chat microphone action, provider-neutral `SpeechRecognition` selection, Whisper-preferred managed-Python path, compatible Hugging Face/Transformers ASR choices, transcribe/translate and language selection.
- Keeps the built-in Speech to Text organic skill and conditional PublisherStudio handoff through advertised 1-Wire capabilities.
- Does not weaken or bypass architecture, service-resilience, text-ownership, iterator, system-variable or DevExpress build guards.

## Version
- Advances LocalGPT from 4.9.8 to 4.9.9.

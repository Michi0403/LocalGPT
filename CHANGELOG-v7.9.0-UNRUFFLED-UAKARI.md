# LocalGPT 7.9.0 — Unruffled Uakari

- Select the installed model for audio and speech synthesis from the runtime catalog. The generated WAV can be returned as a chat artifact.
- Install Hugging Face snapshots by repository and revision for use with configured runtime extensions. Bind an installed snapshot to a Python, JavaScript, or C# extension in `/install`. Changing the binding requires renewed approval.
- Show Python setup guidance, compatible PyTorch profiles, and model verification errors in `/install`.
- Verify model loading and run speech synthesis in a managed Python process. Use the saved model snapshot for offline inference.
- Seed editable media and speech Council presets with the built-in generators as preferences and discovery for compatible alternatives. The speech preset can recommend setting up Whisper when it fits the request and hardware.
- Leave model selection open in built-in presets and clear legacy model bindings on untouched default presets. Preserve their token budgets and user-edited settings.
- Handle normal 1-Wire approval worker shutdown and open running Council sessions from the Council editor in a separate Chat tab.

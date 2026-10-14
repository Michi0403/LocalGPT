# LocalGPT 4.9.3 — Catalog / Speech Compile Repair

## Fixed

- Added `LegacyPromptDefaults` to `IInitialDataCatalog`. `InitialDataCatalog` already implemented this collection, while `DatabaseInitializationService.SeedPromptsAsync` correctly depends on it to migrate only untouched legacy prompt defaults. The interface omission caused CS1061 during a real Windows build.
- Reworked `Chat.StartSpeechCouncilAsync` renderer dispatch. Blazor `ComponentBase.InvokeAsync` is non-generic; the previous expression lambda could bind to the `Action` overload, discarding `Task<bool>` and producing CS0029 when the awaited result was returned as `bool`. The renderer-dispatched callback now captures the `StartCouncilPromptAsync` result explicitly and returns it afterward.

## Preserved

- Microphone WAV persistence and chat attachment remain independent of Whisper transcription.
- Existing Council selection, model settings, approvals, runtime extensions, toolchain discovery, greenfield code generation, EF schema, and release orchestration are unchanged.
- Version identity advanced from 4.9.2 to 4.9.3 without crossing the configured one-digit minor/patch convention.

## Validation boundary

This source handoff was checked statically in the available environment. No .NET/MSBuild/NuGet build or restore was run; the user's local Windows/macOS builds remain authoritative.

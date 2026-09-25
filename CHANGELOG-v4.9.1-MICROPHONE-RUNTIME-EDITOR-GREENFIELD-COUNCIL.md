# LocalGPT 4.9.1 — microphone attachment, runtime editor, approval and greenfield Council repair

## Chat microphone

- Replaced the separate text microphone action with a compact DevExpress icon button positioned beside the native `DxAIChat` composer controls.
- Browser microphone capture now persists the WAV first and publishes it as a durable LocalGPT artifact before optional speech recognition.
- Whisper is optional: a recording is attached to the visible chat even when no Whisper model is installed or when transcription fails.
- When a selected Whisper model succeeds, the transcript continues through the normal selected Council/team/model/settings path instead of bypassing Chat configuration.
- Added localized recording, attachment, transcription and recovery states for all maintained UI languages.

## Runtime extensions

- Reworked the runtime-extension popup as a DevExpress-first editor using `DxPopup`, `DxFormLayout`, DevExpress editors and `DxHtmlEditor` for C#/JavaScript source.
- Source remains persisted as plain executable text; HTML-editor markup is encoded/decoded only at the UI boundary.
- Changed the HTML editor binding to delayed-input mode so Save and Save + Build/load cannot depend on a lost-focus race.
- Added a compact code-oriented toolbar and responsive popup/action layout.
- Runtime build/load now resolves a compatible, enabled, successfully validated configured compiler/runtime; invalid cross-language selections fail with setup guidance instead of attempting a misleading build.
- Lowered the floating Human Collaboration rail below modal popup layers so approval/Council launchers no longer cover the runtime-extension editor on narrow displays.

## Human Collaboration

- Approval/guidance text editors now bind on input before action buttons execute.
- Approval buttons enter a visible busy state immediately and reject duplicate clicks while the operation is running.
- Declining an approval no longer requires the user to invent a reason; an optional reason is preserved, otherwise LocalGPT records a neutral local-decline reason.
- Existing deferred approval execution remains centralized: approving a queued DXFunction still resumes the exact fingerprinted deferred invocation rather than creating an alternate execution path.

## Program Compiler & Repository Curator

- Updated the seeded Council workflow to classify **greenfield authoring** versus **existing-project maintenance before reading workspaces**.
- Explicit requests such as “create a .NET 10 C# program from scratch” no longer get redirected into unrelated uploaded/selected repositories merely because workspace context exists.
- Greenfield jobs now plan exact files and route them through `codegen.review.create` followed immediately by `codegen.review.execute`, so the user receives the normal approval card and the generated artifact instead of a prose-only plan.
- Requested .NET 10 projects use `net10.0`; requested generation/build/ZIP outcomes are carried through the code-generation workspace/build result and artifact URL.
- Large relevant source remains legitimate work and is handled through bounded/chunked reads; large unrelated uploads no longer become mandatory intake for a greenfield task.
- Bumped the repository-owned Council seed version so untouched seeded Program Compiler teams receive the corrected workflow while user-customized teams retain their customization protection.
- Updated the persisted code-generation review prompt only when the database still contains the exact previous shipped default; user-edited prompts are not overwritten.

## Toolchain discovery hygiene

- Discovery now filters obvious library/data files before validation (`.dll`, `.lib`, `.so`, `.dylib`, `.a`, etc.).
- On Windows, directly discovered candidates must be native executables or supported command wrappers (`.exe`, `.com`, `.cmd`, `.bat`); an extensionless candidate is accepted only when it has a PE `MZ` signature. This prevents Python runtime libraries and shell-style `npm` stubs from becoming compiler cards while keeping arbitrary manual toolchain configuration available.

## Validation

This handoff intentionally did not run .NET/MSBuild/NuGet/publish/signing/notarization. Source-level validation was performed instead:

- async continuation audit passed;
- application architecture audit passed;
- service resilience audit passed;
- code-generation/DXFunction wiring audit passed;
- cross-platform boundary audit passed;
- DevExpress Blazor control audit passed;
- SystemVariable initialization guard was source-emulated with zero new findings;
- XML-documentation finding set has no new unique finding versus 4.9.0;
- localization JSON parses and all newly added microphone keys are present in every maintained locale.

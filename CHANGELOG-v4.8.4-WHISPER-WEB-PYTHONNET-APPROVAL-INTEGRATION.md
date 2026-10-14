# LocalGPT 4.8.4 — Whisper, dynamic web, Python.NET and approval integration

## Changed

- Carries forward the reviewed .NET 10.0.401 / 10.0.12 and DevExpress 25.2.10 dependency updates from the personal 4.8.3 tree.
- Treats Python.NET 3.1.0 as a LocalGPT .NET dependency. The managed Python environment no longer tries to pip-install or recursively discover `Python.Runtime.dll`; only the selected CPython shared runtime and Python packages remain external toolchain concerns.
- Synchronizes the LocalGPT-managed virtual environment and maintained Whisper adapter into the generic database-backed toolchain installation/profile model while preserving user-edited profiles.
- Adds generic assembly-discovered DX functions for selecting a discovered Python runtime, creating the managed environment and installing configured Python package profiles. No per-function startup registration was added.
- Finishes upstream OpenAI Whisper transcription so installed reviewed weights are used directly and cannot silently trigger a second model download during transcription.
- Adds the preseeded Whisper Speech Team and Dynamic Web Research Team. Their prompts call registered functions themselves, queue exact approval cards and treat page/model output as evidence rather than instructions.
- Adds bounded browser microphone capture on `/chat`, Whisper transcription and handoff to the existing Council start path using the user's currently selected team/settings. Audio/transcript content is omitted from diagnostics.
- Adds bounded Playwright-based public web evidence extraction with lazy scrolling, details expansion, optional reveal selectors, private/local destination rejection, download/form restrictions and an approval-gated Chromium installer.
- Extends the approval registry so any descriptor that supports deferred approval can queue its exact invocation, not only calls marked as automatic. Persisted permission policy remains authoritative and approval is never inferred from prompt text.
- Moves the assistant operation policy into editable seeded prompt configuration so Council/single-model guidance consistently invokes available tools rather than asking the human to memorize function names.

## Regression protection

- 4.8.2 is the code baseline. Existing 4.8.0/4.8.1 generic toolchains, ASCII console, individual-model function access, Council history/repetition/rejoin fixes and approval architecture are retained rather than reimplemented.
- The 4.8.3 deletion of authored documentation/help sources is not carried forward.
- `Directory.Build.targets` remains the 4.8.2 baseline version, so the SystemVariable initialization guard is enabled. The older OneWire target remains in its pre-existing baseline state rather than being newly enabled without build-host verification.
- InteractiveServer render-mode ownership remains unchanged; nested child components inherit their routed page circuit.

## Validation boundary

Source-only validation is used for this handoff. No GitHub access, `dotnet`, NuGet restore, MSBuild, build, publish or installer execution is performed. Python/JSON/XML/source-policy checks and archive integrity are validated independently.

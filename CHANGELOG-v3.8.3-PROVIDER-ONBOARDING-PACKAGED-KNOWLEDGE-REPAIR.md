# LocalGPT 3.8.3 — provider onboarding and packaged-knowledge repair

## Scope

This is a bounded application-runtime repair. It does not add a LocalGPT-owned model host, bundled model, or new deployment architecture.

## Provider onboarding

- Added an OS-specific `ILmStudioPlatformService` boundary, mirroring the existing Ollama platform resolver.
- macOS Ollama discovery now recognizes the CLI inside `/Applications/Ollama.app/Contents/Resources/ollama` and the corresponding user Applications location, in addition to Homebrew/PATH locations.
- LM Studio/llmster discovery covers the documented user CLI location plus Windows user-cache and common Unix command locations.
- Provider bootstrap commands receive a bounded PATH override pointing at the resolved provider CLI directory. This keeps Finder/desktop-launched LocalGPT independent of an interactive shell's PATH.
- LM Studio guided model installation now runs both `lms get` and `lms load`, so the model is usable even when LM Studio JIT loading is disabled.
- `/install` keeps official vendor download links visible and explicitly directs users to the manual vendor path plus `Detect` when a guided action is unsuitable.
- The install-page security wording now accurately distinguishes manual snippets from explicit-confirmation guided actions.

## Packaged LocalGPT knowledge

- Every runtime knowledge file listed by LocalGPT's built-in `KnowledgeFiles` collection is copied to build and publish output.
- The provider-installation and toolchain-discovery articles are additionally embedded in the LocalGPT assembly. `InitialDataCatalog` uses those embedded copies only when the corresponding publish-layout file is absent.
- A clean installed LocalGPT can therefore seed provider/toolchain help without requiring the original source checkout.

## Chat/model seed truthfulness

- The built-in unverified Ollama and local OpenAI-compatible model-name defaults are now empty. Endpoints remain available for discovery.
- Chat no longer auto-applies the database default Council model preset merely because the page opened. Presets remain available as explicit templates; saved user preparation, requested presets, and real provider discovery still select models normally.
- `gpt-oss:20b` remains available in explicit model/preset recommendations; it is no longer presented as though it were connected before discovery.

## Preserved behavior

- `@rendermode InteractiveServer` architecture is unchanged.
- Existing Ollama multi-host/provider registry and Council routing remain additive.
- Existing 3.8.2 macOS per-artifact signing/notarization transaction flow is preserved, with one state-retention correction: submitted and final stapled SHA-256 values are tracked separately so expected stapling mutation does not invalidate completed state.

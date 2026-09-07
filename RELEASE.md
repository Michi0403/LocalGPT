# LocalGPT 3.9.1

LocalGPT 3.9.1 is the build-guard repair for the 3.9.0 local-runtime Setup completion.

The 3.9.0 source correctly added Ollama lifecycle control, provider-profile v2 parsing, first-model bootstrap, CanIRun JSON recommendations, and system/unified-memory flow, but two new Ollama profile-prefix checks were implemented directly in `InitialSetupAssistantPanel.razor`. The maintained text-service ownership build guard rejects that component-owned string policy before compilation.

3.9.1 keeps that guard unchanged and moves both Setup classification sites onto the already existing provider-owned `IAiProviderBootstrapService.IsOllamaProfile(...)` policy. The follow-up source review also found and removed the next constructor-initialization guard violation in the new CanIRun JSON request by moving the fixed JSON media type out of the `StringContent` constructor literal.

No 3.9.0 feature is rolled back. Provider bootstrap v2/fallback parsing, explicit Ollama Start / Stop / Restart / Refresh, manual first-model installation, CanIRun opt-in JSON recommendations, reviewed RAM/unified-memory flow, the eight hosted services, the 15 InteractiveServer boundaries, and the executable Council SQL seed are preserved.

See `CHANGELOG-v3.9.1-TEXT-SERVICE-OWNERSHIP-BUILD-REPAIR.md` and `VALIDATION-v3.9.1-source.md`. The full feature scope introduced in 3.9.0 remains documented in `CHANGELOG-v3.9.0-LOCAL-RUNTIME-SETUP-COMPLETION.md`.

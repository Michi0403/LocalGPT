# LocalGPT 3.9.7 — compile repair

## Windows Debug compiler failures

- Fixes `CS1673` in `ProviderModelIdentity.ResolveEquivalentCandidate`. `ProviderModelIdentity` remains a readonly struct, but LINQ predicates now capture local struct copies instead of implicitly capturing `this` from inside the struct.
- Fixes `CS0136` in `InitialSetupAssistantService.BuildHardwareListAsync` by separating the configured-profile CPU variable from the later locally detected CPU variable. Hardware behavior is unchanged; the variable names now match their distinct scopes.

## Preserved 3.9.6 behavior

- Keeps provider model inventory available independently of CanIRun.ai.
- Keeps CanIRun.ai as optional hardware-aware discovery that can surface additional models a user may not know about.
- Keeps CPU/SoC, RAM/unified memory, GPU facts, and separate reviewable CanIRun hardware identity.
- Keeps Ollama lifecycle controls, provider catalog/manual installation, and safe provider-specific model mapping.
- Keeps legacy Council/Chat/provider-model reconciliation limited to unique same-provider, normalized-host/port, same-concrete-model matches; model sizes and different hosts are not collapsed.
- Keeps Windows PowerShell 5.1, modern pwsh, macOS, and Linux release/documentation compatibility contracts from earlier releases.

## Validation boundary

- This environment performs source/static validation only. The user's Windows/macOS/Linux builds remain the authoritative compiler and packaged-runtime validation.

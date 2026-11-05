# LocalGPT 3.9.6 — provider model discovery and Council binding repair

## `/install` setup workflow

- Keeps one reopenable setup workflow for local hardware, optional CanIRun.ai discovery, provider/runtime setup, provider models, and the initial benchmark Council.
- Separates detected local hardware facts from the optional CanIRun.ai catalog identity. LocalGPT now carries CPU/SoC, system or unified RAM, and zero or more GPUs; a reviewed CanIRun hardware variant label does not overwrite the local accelerator identity.
- Adds cross-platform CPU/model probing behind `IHardwarePlatformProbeService`: Windows uses a read-only `Win32_Processor` query, macOS uses `sysctl`, and Linux reads `/proc/cpuinfo`.
- Moves optional NVIDIA probing entirely behind the platform-probe boundary. Missing `nvidia-smi` is treated as an unavailable optional probe rather than a reason to abort Setup.
- Keeps Ollama detection and explicit Start/Stop/Restart/Refresh controls in the setup form. Ollama is never silently started.

## Provider models independent from CanIRun.ai

- Step 4 is no longer gated by CanIRun.ai recommendations.
- Installed models from the selected provider and maintained provider model mappings are rendered even when CanIRun.ai is unavailable or the user does not opt in.
- Adds an explicit, user-triggered official provider catalog search path. It contacts only maintained provider-owned HTTPS hosts and sends no LocalGPT endpoint, hardware, or CanIRun state.
- Keeps manual provider model-ID installation as the reliable fallback when an external catalog cannot be searched or an external model name cannot be mapped safely.
- Ollama installed models may use the provider-safe pull/refresh operation to check or refresh model layers. LocalGPT does not invent a generic LM Studio update action where it lacks a trustworthy provider contract.

## CanIRun.ai as hardware-aware discovery

- CanIRun.ai remains optional and attributed, but its purpose is hardware-aware model discovery rather than gating provider inventory.
- The approved request can include reviewed CPU/SoC, RAM/unified memory, selected accelerator or CanIRun variant, and dedicated VRAM when known. LocalGPT host/endpoint identifiers are not sent.
- CanIRun recommendations are merged as additional hardware-fit discoveries into the selected provider's model view.
- Provider-specific Ollama or LM Studio IDs supplied by CanIRun are retained. Maintained aliases are used where available; unknown mappings stay visible for manual review instead of being guessed.
- Ordinary unmapped recommendations no longer throw `InvalidDataException` as control flow.

## Persisted provider-model compatibility

- Adds safe reconciliation for older persisted Council/Chat/provider-model selection keys.
- Reconciliation requires the same provider protocol, equivalent normalized host/port, and the same full concrete model token. `localhost` and `127.0.0.1` may reconcile, and an optional `:latest` suffix may reconcile.
- Model sizes and variants are never collapsed (`qwen3:4b` is not `qwen3:32b`), and LocalGPT does not silently move a binding to another host.
- Ambiguous matches stay unresolved so the user can select the exact route.
- Reconciliation is applied to Council Teams editing/runtime resolution, Council participant selection, Chat selections, Model Council, and provider benchmark selections. Editor reconciliation stays in detached editor state until the user saves.

## Preserved release/build contracts

- Preserves the packaged-runtime source-version fallback so installed applications do not require a source-tree `.csproj`.
- Preserves macOS durable runtime working-directory and file-logging behavior from the earlier packaged-runtime repair.
- Preserves release source fingerprints so same-version native/notarized artifacts cannot be silently reused for changed source.
- Preserves Windows PowerShell 5.1-compatible documentation scripting and the Debug-versus-Release PDF contract. Release still requires the embedded documentation PDF.
- Preserves existing `InteractiveServer` and hosted-service ownership boundaries.

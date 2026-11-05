# LocalGPT 3.9.0 — local runtime setup completion

- Repairs the empty Setup provider list by tagging all six packaged Ollama/LM Studio bootstrap profiles and adding `builtin.ai-provider-bootstrap-block-v2`, which captures the complete fenced JSON body including nested `modelAliases` objects.
- Keeps `builtin.ai-provider-bootstrap-block` as a fallback while preferentially parsing the new v2 block format. The v2 regex uses a new database key, so existing 3.8.9 installations receive it through normal additive initialization without deleting the user database.
- Reuses the existing `IOllamaProcessService` in Setup and shows the resolved Ollama executable plus installed/running state. Explicit Start, Stop, Restart, and Refresh actions are available in the provider card.
- Routes `IAiProviderBootstrapService.StartAsync` for Ollama through `IOllamaProcessService` instead of executing the long-running `ollama serve` command in LocalGPT's bounded foreground console. HTTP/DXFunction callers therefore get the same corrected startup behavior as the Setup UI.
- Automatically selects the current-platform Ollama bootstrap profile when Ollama is already installed locally and no other provider profile has been selected.
- Refreshes provider/model state after Ollama lifecycle changes. No provider is started silently.
- Adds an explicit first-model `Model ID` field in Setup. The value passes through `ResolveModelIdAsync` and the existing `InstallModelAsync` path; no hard-coded default model is introduced.
- Replaces Setup's fragile CanIRun.ai HTML device-page scraping with an explicit-opt-in JSON `POST /api/recommend` request using reviewed hardware facts.
- Changes the CanIRun.ai disclosure text so the user sees what leaves the machine: accelerator name, total system/unified RAM, and dedicated VRAM when known. LocalGPT endpoint/host identifiers are not included in the request payload.
- Extends the existing platform hardware-probe boundary with total physical-memory detection: Windows uses `Win32_ComputerSystem.TotalPhysicalMemory`, macOS uses `hw.memsize`, and Linux reads `MemTotal` from `/proc/meminfo`.
- Carries system/unified memory through Setup's editable hardware rows and durable configured-host persistence so Apple Silicon unified memory can be reviewed without conflating it with dedicated VRAM.
- Makes the 3.8.9 upgrade path useful immediately: when a confirmed local hardware profile predates the RAM field, Setup fills the missing RAM value from the read-only local probe for review, and an explicit local detection backfills only that missing field without replacing confirmed GPU/CPU/provenance facts.
- Retains the six existing provider bootstrap variants (Ollama and LM Studio on Windows/Linux/macOS), manual vendor paths, the 3.8.9 executable Council SQL seed repair, the original eight hosted services, and all 15 established `InteractiveServer` component boundaries.
- Bumps LocalGPT, installer console, webview wrapper, documentation metadata, browser asset cache version, and CanIRun user agent to 3.9.0 under the one-digit minor/patch versioning rule.

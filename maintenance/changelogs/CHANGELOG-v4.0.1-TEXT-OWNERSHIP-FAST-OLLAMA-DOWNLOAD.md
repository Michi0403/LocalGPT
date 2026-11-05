# LocalGPT 4.0.1 — build-guard repair and fast Ollama Windows download

## Why this release exists

The first local build of 4.0.0 exposed three source-level text-service ownership violations in `InitialSetupAssistantPanel.razor`. The runtime test also showed that Ollama's Windows updater was crawling at roughly one tenth of one percent every several seconds even though LocalGPTInstallerConsole already downloads the same official Ollama installer quickly with a streaming `HttpClient` path.

## Changes

### Text-service ownership build guard repaired

- The setup panel now injects the existing `CouncilTextService` for its case-insensitive Ollama key classification and old-runtime response matching.
- The three direct `.StartsWith(..., StringComparison)` / `.Contains(..., StringComparison)` operations reported by `Assert-TextServiceOwnership.ps1` are removed from the component.
- The render-time Ollama classifier remains local to the setup component and no longer calls `AiProviderBootstrapService.IsOllamaProfile`, preserving the 4.0.0 responsiveness fix while satisfying the repository's text ownership boundary.

### Windows Ollama guided install/update uses the fast streaming path

- The maintained `ollama-windows` knowledge profile no longer uses `irm https://ollama.com/install.ps1 | iex` for the guided install/update action.
- Guided Windows setup downloads the official `https://ollama.com/download/OllamaSetup.exe` URL directly through a streaming `HttpClient` request with automatic redirects, `ResponseHeadersRead`, a 4 MiB transfer buffer, and a 30-minute HTTP timeout.
- Download progress is emitted as clean bounded integer percentage lines (`>>> Ollama download 42%`) so the shared console stays visibly alive without depending on the vendor PowerShell script's large-file transfer/progress implementation.
- The downloaded vendor installer is executed with `/SILENT`, matching the existing guided-update intent, and the temporary installer is removed in a `finally` block.
- The manual official Ollama PowerShell command remains documented for users who prefer the vendor script directly.

### Long provider downloads receive a realistic command window

- Consequential provider install/update/model-download commands now request up to one hour from the shared console instead of ten minutes. The database-backed console maximum still clamps the effective timeout.
- Read-only provider detect/list commands remain bounded to 30 seconds.

## Preserved behavior

- Separate confirmation is still required for runtime installation/update and for model installation.
- Linux and macOS Ollama profiles keep their maintained vendor `install.sh` path; this release changes only the confirmed Windows bottleneck.
- Provider model links, CanIRun.ai scores, conservative model-ID resolution, progress cleanup, InteractiveServer ownership, Council self-cleanup, and the 4.0.0 reconnect/responsiveness changes remain intact.

## Validation boundary

This handoff is source/static validated only. No GitHub repository access and no `dotnet build`, `dotnet test`, `dotnet publish`, runtime launch, installer build, or documentation regeneration were performed in this environment.

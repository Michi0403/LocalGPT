# LocalGPT 4.0.1 source validation

## Validation boundary

This handoff was reviewed and validated from the supplied source tree only. No GitHub repository access and no `dotnet build`, `dotnet test`, `dotnet publish`, application/runtime launch, installer build, or documentation regeneration were performed in this environment. Runtime/compiler confirmation therefore remains a downstream local-build gate.

## Release-specific checks

`build/audit_release_4_0_1.py` verifies the following release contracts:

- Version `4.0.1` is present on the maintained LocalGPT, installer-console, and webview-wrapper project surfaces and preserves the one-digit minor/patch version policy.
- The three direct component string operations reported by the local 4.0.0 build are absent; setup classification/diagnostic matching is delegated to the injected `CouncilTextService`.
- The setup panel still avoids `AiProviderBootstrapService.IsOllamaProfile` on the render/progress hot path.
- The Windows Ollama profile uses the official `OllamaSetup.exe` endpoint with a streaming `HttpClient`, automatic redirect handling, `ResponseHeadersRead`, a 4 MiB transfer buffer, bounded integer percentage output, `/SILENT` installer execution, and `finally` cleanup.
- Linux/macOS Ollama profiles remain on the vendor shell installer path.
- Mutating provider commands request a one-hour timeout while read-only detect/list operations remain at 30 seconds.
- Existing 4.0.0 model-update detection, progress preservation, provider candidate reuse, circuit retention, conservative provider-model resolution, localization parity, and routed `InteractiveServer` ownership remain intact.
- Source-package hygiene checks reject generated build/cache directories and raw terminal ESC corruption in changed source.

## Maintained static audit results

- Application architecture policy: passed.
- Async continuation policy: passed for 259 source files (3,108 await tokens; 2,751 `ConfigureAwait(false)`; 135 renderer-affine `ConfigureAwait(true)`; 217 configured async disposals; 5 configured async streams).
- Service resilience: passed for 2,252 service methods; 29 yield methods and 3 direct Program/Startup methods remain explicitly skipped by the maintained audit.
- Cross-platform boundaries: 22 checks passed.
- C# XML documentation: passed for 10,420 direct declarations across 656 maintained source files.
- Razor XML documentation: passed for 45 component types and 802 direct `@code` declarations.
- Provider-qualified Council audit: 282 checks passed.
- Code-generation/DXFunction, configurable Council behavior policy, provider repetition policy, X-Round/heartbeat wiring, and 60-row executable Council SQL seed audits: passed.
- Text-service ownership guard: the PowerShell runtime is unavailable here, so the repository guard's regex/baseline logic was reproduced source-side against the same folders and baseline; it reports zero new operations. The exact three 4.0.0 failure expressions are no longer direct string operations in the component.
- `build/audit_release_4_0_1.py`: passed with 15 routed pages, 14 routed `InteractiveServer` boundaries, 6 localization catalogs, and 6 provider profiles.

## Runtime observation behind the download change

The reported 4.0.0 console showed the Windows Ollama update path advancing only about `0.1%` every several seconds. In 4.0.0 that action delegated the whole operation to `irm https://ollama.com/install.ps1 | iex`, while the repository's installer console already downloads `https://ollama.com/download/OllamaSetup.exe` directly with streaming `HttpClient`, `ResponseHeadersRead`, and a 4 MiB buffer. The 4.0.1 guided Windows profile removes that confirmed client-path difference by downloading the installer directly with the same transfer shape before launching it. This source-only comparison does not establish whether Ollama's CDN also contributed to the observed throughput.

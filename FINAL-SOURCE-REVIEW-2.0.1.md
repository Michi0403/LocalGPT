# LocalGPT 2.0.1 final source review

This source candidate closes the reported LocalGPT build, Chat UI, localization, startup, 1-Wire and local-provider regressions.

## Build and protocol

- `LocalGPT.WireProtocolVersion` remains the only authoritative protocol source and remains RID-neutral.
- Development builds compile the application in source-project mode and build the optional WinUI wrapper last through the package graph, preventing an x64/x86/ARM64 path from being imposed on the AnyCPU protocol output.
- The architecture guard parses MSBuild XML and rejects only real `RuntimeIdentifier` / `RuntimeIdentifiers` declarations.
- Namespace/compiler regressions reported during the 2.1 work have permanent source-contract tests.

## Chat and Council UI

- Chat actions use an adaptive DevExpress `DxToolbar`; no second page ribbon overlays the application drawer/menu.
- The AI kernel/model name is visually primary and its provider/endpoint is secondary.
- Chat text, upload and send controls use larger adaptive targets and the page prevents horizontal overflow.
- Former model thoughts are normalized before display so encoded or literal `pre`/`code` wrappers are not shown as text.
- Council controls are collapsible and do not permanently consume the full viewport.

## Localization

English and German catalogs have identical key sets. Statically identifiable UI labels, tooltips, placeholders and command captions are registered in the catalog, with additional German translations for common editor, provider, project, Council and security terminology. The runtime never performs a partial word translation: when a complete translation is unavailable, the complete original label remains intact rather than producing mixed-language text.

## LM Studio compatibility

Normal LocalGPT chat remains compatible with LM Studio through the OpenAI-compatible API. The default endpoint is `http://localhost:1234/v1`; LocalGPT probes `/v1/models`, selects a real advertised model identifier and adds the provider only when the endpoint is reachable.

The multi-model Council currently remains Ollama-specific because it uses Ollama lifecycle and hardware controls (`/api/tags`, `/api/ps`, `num_gpu`, `keep_alive`, unload behavior). Supporting LM Studio as a full Council scheduler requires a separate model-runtime adapter rather than pretending those APIs are interchangeable.

## Validation boundary

All included Python source/architecture tests pass. This delivery environment cannot execute the Windows .NET 10, WinUI, PowerShell or DevExpress runtime, so the maintainer's clean Windows build remains the definitive compilation and runtime proof.

# LocalGPT 3.4.1 — Ollama path comparer compile repair

## Fixed

- Restores the missing `ExecutablePathComparer` virtual member on `OllamaPlatformServiceBase`.
- Keeps Unix/macOS Ollama executable-path de-duplication case-sensitive with `StringComparer.Ordinal`.
- Keeps the Windows implementation case-insensitive through `WindowsOllamaPlatformService` overriding the comparer with `StringComparer.OrdinalIgnoreCase`.
- Resolves the `CS0115` release-build failure reported for `WindowsOllamaPlatformService.ExecutablePathComparer`.

## Regression guard

- Adds a 3.4.1 static release check that requires both the base virtual comparer and the Windows override so this inheritance contract cannot silently regress again.

No application feature, wire-protocol, InteractiveServer render-mode, GitHub Pages workflow, or documentation accessibility policy is changed by this patch.

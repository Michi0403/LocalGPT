# LocalGPT 3.6.6 — DocFX PDF cache/fallback repair

## Build repair

- Fixed the clean/retry documentation path where durable DocFX HTML was restored from cache but the DocFX command itself was intentionally not restored. If the cached PDF was absent or invalid and browser printing failed, the PDF fallback previously attempted to invoke a null command target and PowerShell stopped with: `The expression after '&' ... was not valid.`
- Added a lazy `Ensure-LocalGptDocfxToolForPdfFallback` resolver. It restores the repository-local DocFX tool only when the PDF plug-in is actually needed, retaining the fast HTML-cache path while guaranteeing a runnable fallback command.
- Preserved the isolated pinned DocFX 2.78.5 tool-path fallback if repository-local tool restore is unavailable.
- Reduced the single-browser print-book limit from 1500 to 1000 source pages. The current LocalGPT site has more than 1100 printable pages and Microsoft Edge failed that monolithic print job on macOS; large manuals now go directly to the proven DocFX PDF plug-in instead of wasting a browser-print attempt first.
- The shared LocalGPT/PublisherStudio PDF render lock, durable HTML/PDF cache, 30-minute per-navigation timeout, and final PDF validation remain unchanged.

## Version

- Version advanced from 3.6.5 to 3.6.6.
- Application, installer console, WebView wrapper, documentation metadata/PDF name, browser cache-buster, and outbound LocalGPT user-agent version were advanced together.

## Scope

- No LocalGPT runtime behavior, Blazor render-mode boundary, macOS launcher architecture repair, Future2 documentation, or licensing wording was changed in this patch.
- No PublisherStudio source was changed.

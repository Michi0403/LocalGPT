# LocalGPT 3.6.6 source validation

Static/source validation only; no .NET restore, build, publish, DocFX render, GitHub access, browser printing, or macOS native packaging tools were executed in this environment.

- Confirmed LocalGPT application, installer-console, and WebView wrapper versions are 3.6.6.
- Confirmed the supplied failure mode is addressed at its source: cached HTML may still skip eager DocFX restore, but the PDF plug-in path now calls `Ensure-LocalGptDocfxToolForPdfFallback` before `Invoke-LocalGptDocfx`.
- Confirmed the lazy resolver can select the repository-local manifest tool and retains the pinned isolated DocFX 2.78.5 fallback path.
- Confirmed the browser print-book threshold is 1000 pages, so a LocalGPT documentation set above 1100 printable pages uses the DocFX PDF plug-in directly.
- Confirmed the durable HTML/PDF cache, shared cross-product PDF lock, 30-minute DocFX navigation timeout, PDF validation/compression flow, and cleanup paths remain present.
- Confirmed the 3.6.5 macOS architecture/Rosetta repair and exact native-architecture manifest markers remain present.
- Confirmed the syntax-aware async-continuation audit passes for the LocalGPT source tree.
- Confirmed the maintained `@rendermode InteractiveServer` occurrence count remains 15.
- Confirmed version-bearing project XML and DocFX JSON parse successfully.
- Confirmed there are no repository-local `bin` or `obj` directories in the delivered source tree.

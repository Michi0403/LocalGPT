# LocalGPT 2.8.1 — Human-visible entity formatting repair

## Chat/history/Council presentation boundary

- Fixed HTML-encoded quote/apostrophe entities leaking into human-visible chat/history/Council text as strings such as `&quot;`.
- `ChatContentRenderer` now normalizes quote/apostrophe entities exactly once before structured-text/Markdown recognition so JSON containing encoded quotation marks can again be recognized and presented as human-readable structured content.
- The normalization is deliberately narrow: quote/apostrophe entities are decoded, while markup-significant `&lt;`, `&gt;`, and `&amp;` remain encoded on the MarkupString/Markdown path. This avoids turning stored encoded markup into executable HTML.
- Model Council final answers and visible member content now use the same maintained chat-content renderer as Chat instead of bypassing it with direct Markdown conversion.
- Plain-text Council prompt, thinking, spooler prompt, member-content and final-answer surfaces HTML-decode for display through normal Razor text encoding, so encoded punctuation is readable without creating an HTML execution boundary.

## Scope protection

- No Council execution policy, provider routing, hardware-performance preset, database schema, DXFunction contract, render-mode topology, or 1-Wire message shape changed.
- LocalGPT, WebView wrapper, and installer versions are **2.8.1**.
- Wire protocol remains **2.1.1**.
- No GitHub access and no dotnet/MSBuild build were used while preparing this source package.

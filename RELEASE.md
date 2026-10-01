# LocalGPT 5.2.5

LocalGPT 5.2.5 applies the InteractiveServer live-state ownership lesson from PublisherStudio to LocalGPT and makes it a permanent repository guard.

The runtime-extension `DxHtmlEditor` was the one analogous high-risk surface found in LocalGPT: delayed two-way `@bind-Markup` could feed a live vendor document back through the enclosing Install component on every delayed input notification. It now uses one-way Markup initialization, an observed `MarkupChanged` callback whose automatic render is suppressed, and an explicit editor-generation key for intentional source replacement. Save/Build still capture the current editor markup; ordinary typing no longer asks Blazor to redraw the editor that owns the caret and live document.

The new build-breaking transient-state audit also preserves LocalGPT's existing commit-on-change slider policy and rejects future RichEdit/HtmlEditor document/selection feedback loops and `DxRangeSelector` handle-move server feedback.

No .NET build was attempted in this environment. See `CHANGELOG-v5.2.5-TRANSIENT-UI-STATE-OWNERSHIP.md` and `VALIDATION-v5.2.5-source.md`.

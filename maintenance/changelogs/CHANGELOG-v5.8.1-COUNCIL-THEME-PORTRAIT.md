# LocalGPT 5.8.1 – Council inspection, readable overlays and mixed-orientation documentation

- On a completed Council lane inspection, resolve the full **available** server-owned participant snapshot on demand, rather than using the 8,000-character stream / 16,000-character final-answer recurrent display projection. Existing server-side activity retention policies are unchanged.
- Replace the one-column inspection flow with independently scrollable answer and provider-evidence panes, a fixed evidence header and close footer. The popup is DevExpress-owned; global styling is required for its portaled content.
- Fix overlapping Theme Fusion headings by removing sticky headers in the scrolling theme list, and preserve the glass effect with sufficiently opaque frosted surfaces.
- Print the separate documentation cover and TOC in A4 landscape, and HTML/API body chunks in A4 portrait; invalidate old cached landscape chunks to prevent mixed old/new outputs.
- Improve uploaded repository evidence classification: unknown extensions are sampled with a bounded content check, so text-like source/config files can use progressive file reads instead of binary preview. Model read completion is not inferred from upload indexing.
- No Python, GitHub or new third-party packages required; PublisherStudio PDF documentation receives the same orientation rule separately in 4.0.2.

## Validation limitations

Static code and archive checks only. .NET compilation, DevExpress interaction, PowerShell execution and full browser PDF output require Windows validation. Portrait pagination may increase or decrease the PDF page count depending on source layouts; no unmeasured reduction is claimed. Session-side stream retention limits and model file-reading behavior remain independent matters.

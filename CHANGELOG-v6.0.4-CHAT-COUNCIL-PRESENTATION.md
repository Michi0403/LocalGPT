# LocalGPT 6.0.4 — Chat Council transcript and evidence inspection layout

## Scope

This release addresses two presentation regressions evidenced by the 2026-10-18 `/chat` screenshots and saved HTML. It is based entirely on the 6.0.3 complete source archive; it does not modify PublisherStudio, the Council workflow, role/team policy, DXFunction security, or PDF/release packaging.

## Changes

1. **Ordered Council transcript isolation** (`ChatContentRenderer.cs`). The historical transcript remains authoritative and intact. Its model streams are now rendered as separate Markdown fragments using the stable, LocalGPT-owned `data-localgpt-stream-id` openings and matching completion comments. A missing code-fence terminator or a malformed reasoning block in one model stream can no longer make subsequent Council rounds descendants of that model's collapsed `<details>` element. Rendering closes only the disclosure elements still open in the rendered HTML; literal tags in fenced source examples cannot increase this count. This behavior includes incomplete live streams and corrective/final-answer recovery streams. Normal non-Council chat still uses the existing single-document Markdown renderer.
2. **Inspection dialog full-height content** (`Chat.razor`, `wwwroot/css/site.css`). The completed answer and provider stream both use the same inspection-owned scroll class instead of inheriting `.localgpt-live-participant-content`'s 22rem limit. Both panes grow to fill the available vertical grid track and scroll independently. Live-lane cards retain their existing bounded heights.
3. **Application version** advances from 6.0.3 to 6.0.4.

## What was observed

The supplied snapshot contains all the later Council phases in the DOM. However, the `qwen3.6:27b` Study `<details>` element contains subsequent, unrelated Study recovery, Verification, and Learning Synthesis panels as nested descendants. Those descendants disappear when its parent is collapsed, creating the false impression that only earlier rounds exist.

The inspection screenshot shows an empty vertical area beneath the right-side provider-stream content, despite the dialog's available height. The stream was styled with the 22rem live-card cap; the inspection view now has a dedicated viewport instead.

## Acceptance checks

- In a long Council run with an incomplete provider code fence or interrupted stream, every subsequent top-level round must be reachable independently in the traditional chat transcript, including Verification, Learning Synthesis and final consensus.
- The Council spooler and read-only evidence inspection must still reflect unchanged model and function output; no role work should be duplicated or suppressed.
- In the inspection modal, both Completed answer and Thinking/function/provider-stream columns fill the vertical region above the Close button and scroll independently at different content lengths.
- Check long messages, a running stream, completed/rejoined sessions, and a single-model non-Council chat.
- `pwsh ./Build-LocalDevelopment.ps1 -Configuration Debug` and `pwsh ./Build-Release.ps1` must continue to work without new manual prerequisites.

## Execution limits

The source ZIP has been statically validated, but .NET, PowerShell, a browser runtime, and the DevExpress UI were not run in this container. The live DOM and screenshot acceptance tests require running LocalGPT on the user's system.

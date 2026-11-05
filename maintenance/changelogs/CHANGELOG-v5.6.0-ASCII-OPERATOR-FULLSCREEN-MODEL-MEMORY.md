# LocalGPT 5.6.0 — console output, immersive ASCII layout and per-model memory planning

Source release following 5.5.9. Both minor and patch slots remain single digits: **5.5.9 → 5.6.0**. PublisherStudio is unchanged.

## Findings and corrections

1. **PowerShell `dir` missing from the console:** the redirected-stream pump consumed the `\r` of every Windows `\r\n` pair as a progress rewrite and discarded ordinary output before seeing `\n`. The pump now distinguishes complete CRLF line breaks (even when split across read buffers) from lone carriage-return progress updates. Captures, bounded events, progress coalescing, process cancellation and per-service diagnostics remain intact. Only explicitly confirmed, human-entered ASCII operator commands echo their actual input in the user's own console; background/provider command text remains masked in the UI and all command text remains omitted from application logs.
2. **Installed-shell dropdown:** the DevExpress ComboBox now binds to the stable `LocalConsoleShellKind` value using `ValueFieldName`, rather than trying to round-trip whole descriptor objects; choices continue to come from the host-specific shell-discovery service. Selecting PowerShell, Command Prompt, Bash or another detected adapter no longer relies on descriptor object identity.
3. **Empty Game Studio list inflating the toolbar:** the DevExpress revision `DxListBox` is retained, but displayed only when the user expands the compact `Games (N)` control and approved revisions actually exist. An empty list no longer creates a blank 110px editor in the console header. Explicit Refresh and Play remain available in the expanded selection area.
4. **Fullscreen not immersive:** browser fullscreen retains the popup-root ownership necessary for DevExpress dropdown portals; the header becomes a compact floating HUD with guide/fullscreen/close actions instead of a reserved layout row. The stage takes the remaining viewport, with smaller screen padding and no permanent empty scrollbar gutters. The existing game controls and authored scene/tick behavior remain unchanged.
5. **ASCII popup not filling its dialog:** dialog sizing is scoped to the presence of the existing chat-game content instead of waiting for a post-render JavaScript-applied class. Nested DevExpress FormLayout owners stretch to the popup's usable height, with no generic modal-body double scrolling; the unrelated dialog/editor scrolling policy remains intact.
6. **Per-model RAM/VRAM/unified memory after CanIRun.ai:** the host-scoped recommendation service now derives a clearly labelled memory plan from each source-reported quantization requirement and the reviewed hardware row. It shows estimated dedicated GPU VRAM, PC RAM/spill and unified/shared RAM in the setup cards and a concise summary in the DevExpress model grid. Shared memory is one physical pool, not additional RAM. Unknown source requirements or unknown GPU memory topology remain **unknown**, never fabricated. These are allocation-planning lower bounds without runtime/context/OS overhead, **not observed process consumption**.

## Preserved contracts

- No new dependencies, database migrations, external source access or copied model inventories.
- No removals of DevExpress controls, no form/modal architecture exceptions and no weakening of maintenance checks.
- Council approvals, game revisions, game-engine time/scene execution, renderer-affine async boundaries, shell user confirmations, logged exceptions and source-credit CanIRun.ai opt-in are retained.
- All six supplied screenshots and four relevant diagnostic captures were reviewed. Their Windows shell operation completed with exit code 0; the missing output is explained by the CRLF framing bug, not by a missing process launch.

## Verification limits

The new source-only `build/audit_ascii_console_memory_5_6_0.py` covers the terminal, picker, fullscreen, popup and memory propagation. Existing source-only DevExpress, render ownership, Game Studio, Data Management, async, architecture, cross-platform and text-ownership checks remain active. A Windows/.NET 10 compile, actual DevExpress popup interaction, shell execution and GPU-memory observation **still require the user's build/runtime environment**; no `.NET` build was attempted.

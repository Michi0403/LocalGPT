# LocalGPT 2.1.17 — Responsive workbench, customizable LearnBase, and ASCII layout

## Build correction

- Uses `relative.EndsWith("/", StringComparison.Ordinal)` in workspace access-policy evaluation.
- Preserves the explicit `entry => regex.IsMatch(entry)` predicate from 2.1.16.
- No workspace policy semantics or permission boundaries were relaxed.

## LearnBase source profiles

- Adds a database-backed catalog of selectable text/source endings.
- Adds editable additional endings, include and exclude regexes, per-file byte limits, and independent import switches for installer manifests, known documentation corpora, and project architecture summaries.
- Extends known text/source formats for .NET, Python, C/C++, Arduino/ESP32, PlatformIO-style build inputs, device trees, HDL, Fritzing text parts, KiCad, OpenSCAD, web assets, scripts and documentation.
- Rejects known binary containers from text parsing even when manually selected; Fritzing `.fzz` and `.fzpz` bundles are treated as binary containers.
- Applies the same policy through the LearnBase service and diagnostic controller contract.

## Responsive pages

- OneWire Security now uses the full route width with responsive card and inline-control grids.
- Human-guided Projects now keeps the project selector in a bounded side column and gives the editor the remaining viewport.
- Project Maintenance now uses responsive multi-column cards and expands wide maintenance sections across the available grid.
- Narrow displays collapse each page to a single readable column.

## Optional ASCII corridor

- The Matrix/ASCII console no longer occupies Chat unless the user explicitly opens it.
- The opened console receives a larger bounded share of the Chat viewport.
- Human controls and guide content are beside the frame on desktop and stack below it only on narrow displays.
- Fullscreen supports:
  - **Fit whole frame** — scales to both available width and height.
  - **Fit width + vertical scroll** — maximizes readable width while preserving vertical scrolling.
  - **Native size + scroll** — preserves the original monospace size without automatic scaling.
- Existing internal `ascii-doom` identifiers remain for stored-session and DXFunction compatibility; the visible game name remains **ASCII corridor**.

## Licensing boundary

- The ASCII corridor renderer and generated maps are project-owned LocalGPT code licensed with LocalGPT under Apache-2.0.
- No original DOOM engine runtime, WAD, level, artwork, sound or other commercial asset is included.
- Optional user-selected upstream repositories remain governed by their own licenses and notices.
- DevExpress remains a separate proprietary dependency and is not relicensed by LocalGPT's Apache-2.0 license.

## Version alignment

- LocalGPT application/runtime version: `2.1.17`.
- Organic-wire application advertisement: `2.1.17-organic-wire`.
- Seed history appends `seed-v2.1.17`; all prior release records remain intact.
- The separately versioned `LocalGPT.WireProtocolVersion` package is unchanged.

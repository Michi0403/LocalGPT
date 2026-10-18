# LocalGPT 6.0.4 — Source validation

Baseline: complete `LocalGPT-6.0.3-source.zip` (2,152 files).

## Static source checks

- Retain every baseline entry at the identical path; only the renderer, evidence Razor template, site CSS, and application version change.
- Keep Council stream boundary recognition restricted to LocalGPT-authored 32-character stream IDs and explicit completion comments, not arbitrary model text or role-name comparisons.
- Isolate Markdown parsing by Council participant and close unfinished rendered disclosures before the next participant's output. Preserve all text, function activity, and model reasoning; do not truncate transcript content.
- Preserve single-model/non-Council Markdown behavior, existing Council completion marker state, and live-activity data binding.
- Ensure both inspection columns have a full-height scroll class independent of the live participant 22rem content cap; leave running/board lane styles untouched.
- Verify that build and documentation scripts, architecture guards, third-party licenses, and source ZIP paths remain present and unchanged.
- Parse the changed `.csproj` as XML; audit source balancing and relevant selector references. Verify ZIP CRC and compare unchanged entries against the baseline by SHA-256.

## Live checks still required

No `pwsh`, `.NET` SDK, macOS release coordinator, or headless DevExpress browser is available in the packaging container. Actual compile, multi-model streaming replay, popup geometry and multi-platform release remain unverified here.

## Completed offline checks (2026-10-18)

- PASS: Original complete archive has 2,152 entries; proposed release has 2,154 (two new documents).
- PASS: Every original entry is retained; 2,148 original entries are byte-identical by SHA-256; exactly four original entries are intentionally changed.
- PASS: Archive CRC integrity and unique flat-root paths.
- PASS: Application `.csproj` parses as XML and declares `6.0.4`.
- PASS: Inspection Razor contains exactly two full-height evidence-scroll viewports; global site CSS defines their scroll ownership without modifying the live lane's 22rem cap.
- PASS: The Council rendering source separates stable stream openings and completion comments, then closes any leftover rendered `<details>` elements within that fragment before moving to the next stream.
- PASS (simulation, not a .NET runtime test): A malformed Council output containing an unterminated C# Markdown fence still yields two sibling provider panels and a readable final consensus when rendered through the same boundary/HTML-balancing algorithm using an offline Markdown implementation.
- PASS: Original release/Debug entrypoints, PowerShell checks, original third-party notice files and licenses remain unmodified and included.

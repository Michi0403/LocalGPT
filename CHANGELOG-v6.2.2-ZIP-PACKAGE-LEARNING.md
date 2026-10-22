# LocalGPT 6.2.2 — ZIP-package-aware ingestion and learning

## Added

- `IArchivePackageService` / `ArchivePackageService`: inspect actual ZIP signatures (rather than the `.zip` extension alone), identify Office Word, Excel, PowerPoint, OpenDocument text/spreadsheet/presentation, EPUB, and other ZIP containers from internal markers, and expose read-only, bounded semantic text pages.
- OOXML parsing of Word paragraphs, PowerPoint slide text, Excel worksheets including shared strings, inline strings, numeric cells and formulas. OpenDocument `content.xml` and EPUB XHTML text are included. Ordinary ZIP source archives yield bounded source-text parts.
- Chat uploads extract ZIP-compatible packages under their existing read-only quarantine boundary and generate a separate `readable/<package-name>.md` evidence source without disturbing full archive curation accounting.
- Quarantine inspection/promotion, classifier archive inventories, processor routing, safe document import, tracked-file learning pages, and LearnBase manifest signatures now use the shared package detector/reader.
- Tracked-file learning cursor supports successive package text pages anchored to the original ZIP's SHA-256, instead of treating compressed bytes as UTF-8 or falsely declaring the first bounded page complete.
- Remote knowledge ZIP extraction now rejects linked/special entries and unexpected overwrite, with the same entry safety checks.

## Existing guarantees retained

- ZIP entry count, uncompressed-byte, individual-entry size and workspace limits still come from the existing catalog/runtime policy.
- ZIP traversal paths, absolute entries and symbolic links are rejected or skipped. Extracted binaries are not executed. Initial uploads remain quarantined; project promotion and persisted document imports still require their existing approval gates.
- Learning, regex review and Council execution permissions remain unchanged. Existing PowerShell maintenance/build architecture scripts were not edited.

## Scope and known limits

- This is **ZIP-compatible container support**, not decoding for unrelated archive codecs such as RAR, 7z, TAR, or gzip. Those need their own explicitly installed, validated adapters.
- OOXML/ODF/EPUB text and structural package entries are covered, not full graphical layout, embedded-media comprehension, macros, or formula recalculation.
- Format handling is based on actual ZIP signature and known internal markers, but XML parts remain untrusted evidence, never instructions.
- `ReadSemanticTextAsync` yields a bounded initial excerpt. Tracked-file learning uses the separate resumable page API.
- No .NET build or live PowerShell gate execution was attempted; this archive is source-checked, not build-verified.

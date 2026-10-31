# LocalGPT 6.7.5 — Offline validation

This package is based on the complete 6.7.4 source archive, not on unrelated working-directory edits.

## Verified by offline inspection

- Source-level root-cause match: ingestion paths originate relative to `original/`, OCR previously required `original/` prefix, and an explicit UI action forwarded the unqualified path.
- OCR path construction keeps paths relative to the bounded workspace; traversal and symlink/reparse-point exclusions are retained/enhanced.
- Image-only recommendations no longer send an unobserved image's metadata to a text-only LLM and no longer qualify Council teams merely because capabilities mention `image`.
- Existing PowerShell Council source-contract guard extended, not disabled; no Python or Node validation files added.
- No database/migration changes and no JavaScript files changed.
- Existing platform RID definitions are unchanged.
- Complete ZIP comparison and integrity verification performed during packaging.

## Not verified

- Windows/.NET compilation or execution of PowerShell build guards (executables unavailable).
- Real quarantined image OCR through Ollama, correct text output or model compatibility.
- UI reconnect stability and all other previously open functionality.

Acceptance: Build on Windows; upload one PNG with no archive; inspect the recommendation (Solo or a team explicitly declaring vision/OCR), check OCR capability, execute OCR and verify result text and exact `original/<uploaded filename>` provenance; test a traversal path and symlink to confirm rejection. Check existing saved Council teams and Linux/macOS targets continue working.

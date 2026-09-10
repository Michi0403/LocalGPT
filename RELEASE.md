# LocalGPT 4.0.8

LocalGPT 4.0.8 fixes versioned documentation contamination during in-place source updates. A previous-version `LocalGPT-*.pdf` left under `docs/` could be copied by DocFX into the generated site during an HTML-only Debug build, then correctly rejected by the strict GitHub Pages snapshot validator after the expensive documentation pass.

The documentation generator now removes only non-current versioned LocalGPT PDFs from its generated `_site` tree before that tree is cached or published. Source PDFs are not deleted, the current-version PDF path remains intact, and the Pages validator still requires either exactly one current PDF or an explicit `pdfAvailable=false` HTML-only result.

The 4.0.7 installer compile-surface repair and early installer compilation preflight remain unchanged.

See `CHANGELOG-v4.0.8-DOCUMENTATION-PDF-VERSION-HYGIENE.md` and `VALIDATION-v4.0.8-source.md`.

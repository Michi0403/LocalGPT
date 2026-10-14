# LocalGPT 3.5.8 - macOS documentation PDF render recovery

## Fixed

- Hardened the complete documentation PDF path used by release builds after a real macOS timeout report on the large LocalGPT handbook render.
- The browser PDF helper now validates each generated candidate before accepting it. A non-empty but incomplete PDF no longer blocks later fallback attempts.
- Browser rendering now has two ordered profiles: the preferred tagged/outlined PDF first, then a compatibility renderer that omits the high-overhead tagging/outline switches while still printing the complete assembled HTML handbook.
- Compatibility output is recorded as `html-browser-print-compatibility` with `html-accessibility-fallback`; the generated HTML accessibility and local-link preflight remains mandatory.
- The DocFX Playwright PDF plug-in remains the last fallback, but its default timeout is bounded to five minutes on macOS instead of forcing the previous 30-minute floor. A positive operator-supplied `DOCFX_PDF_TIMEOUT` is honored exactly.
- Release validation accepts both browser PDF modes and still verifies that the complete API HTML page set participates in the browser-printed handbook.

## Preserved

- LocalGPT 3.5.7 optional WSL2 Linux build coordination and Linux-native developer lanes.
- macOS x64/ARM64 and Linux x64/ARM64 host-aware publishing behavior.
- LocalGPT.ReleasePackaging 1.0.1 ownership and package contract.
- Existing explicit `@rendermode InteractiveServer` boundaries.

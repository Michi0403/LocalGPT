# LocalGPT 4.0.8 — documentation PDF version hygiene

LocalGPT 4.0.8 repairs a documentation-build regression exposed by rebuilding an updated source tree in place.

- `Build-Documentation.ps1` now removes non-current `LocalGPT-*.pdf` files only from the generated DocFX `_site` tree before HTML is placed in the durable cache or copied to runtime/build output.
- A previous release PDF that remains in the source `docs/` directory is therefore no longer propagated into `bin/.../wwwroot/help-docs` during an HTML-only Debug build.
- The source PDF is deliberately left untouched. This preserves authored/source material while making the generated publication tree version-clean.
- `Update-GitHubPagesSnapshot.ps1` remains strict: it still rejects the wrong version, still requires exactly one current PDF when a PDF is present, and accepts a missing PDF only when `documentation-status.json` explicitly declares `pdfAvailable=false` under `-AllowMissingPdf`.
- The durable HTML cache is populated only after generated-output PDF cleanup, preventing a stale previous-version handbook from being persisted and replayed by the cache.
- The 4.0.7 installer logger import repair and early installer compile preflight are retained without modification.

No runtime UI, provider, Council, Ollama, rendezvous, installer deployment, render-mode, or async behavior was changed.

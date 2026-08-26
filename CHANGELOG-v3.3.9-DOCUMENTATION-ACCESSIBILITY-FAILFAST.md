# LocalGPT 3.3.9 — documentation accessibility fail-fast

- Repairs generated DocFX pages that omit the HTML language attribute by injecting `lang="en"` during the maintained LocalGPT theme post-processing pass.
- Reuses the GitHub Pages HTML parser as an HTML-only validation gate before PDF rendering, covering accessibility metadata and local links before the expensive 1,100+ page PDF job starts.
- Uses the temporary versioned PDF link stub only during the pre-PDF HTML validation and removes it before publishing or rendering, so no placeholder PDF can leak into the runtime documentation payload.
- Records `htmlPreflightValidated` in `documentation-status.json` and requires it in both release and local-development documentation guards.
- Checks Python 3 in `Initialize-BuildPrerequisites.ps1` before compilation/PDF work because the same validator is also required for the final GitHub Pages snapshot.
- Preserves the 3.3.7/3.3.8 DocFX dependency probe, cross-platform Node.js setup, live PDF diagnostics, and existing application dependency graph.

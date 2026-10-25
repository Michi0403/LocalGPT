# LocalGPT 6.3.7 offline validation

- Source baseline: user-supplied LocalGPT 6.3.6 full-source ZIP.
- LocalGPT source regressions: 63/63 passed (`python -m unittest discover -s tests/static -p 'test_*.py'`).
- `node --check docs/templates/localgpt/public/main.js`: passed.
- Chromium synthetic-page interaction via Playwright: one held-left-button drag updates actual CSS alpha mask, text is absent under claw regions, decorative SVG exists, mouse text selection is empty, hash navigation clears, reload clears, light/dark themes both exercised. No page errors in fixture.
- Full DocFX build, publisher PDF build, native Windows PowerShell checks, and .NET compilation: **not executed**. Installed documentation is not runtime verified.

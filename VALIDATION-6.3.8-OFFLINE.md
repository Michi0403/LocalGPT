# 6.3.8 offline validation

- Full source baseline: user-provided LocalGPT 6.3.7 archive.
- `python -m unittest discover -s tests/static -p 'test_*.py'`: 66/66 passed.
- `node --check docs/templates/localgpt/public/main.js`: passed.
- Synthetic Chromium dark/light theme: existing mask and three-claw drag retained; extra paper lips and fibers render; text remains in DOM but visibly cut; page remains readable outside cuts; hash navigation clears mask.
- Repeated two-drag test: 6 independent tear voids, 12 paper lip shapes; reset clears all.
- Not run: .NET compilation, DocFX generation, Windows PowerShell architecture gates, installed-app acceptance.

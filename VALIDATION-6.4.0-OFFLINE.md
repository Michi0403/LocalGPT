# LocalGPT 6.4.0 offline validation

- `python -m unittest discover -s tests/static -p 'test_*.py' -q`: 69 tests passed.
- `node --check docs/templates/localgpt/public/main.js`: passed.
- Headless Chromium fixture: SVG paw visible on pointer move, 54.39 px at 16px root font; dark filter uses cyan/blue-violet rim; light filter uses downward plum shadow; fur fill changes with theme; drag still applies the content CSS mask. No page JavaScript exceptions in the isolated fixture.
- The isolated fixture does not prove a full DocFX or installed Windows build. Build validation remains required on Windows.
- No guard baselines, DocFX config, or generated documentation snapshots modified.

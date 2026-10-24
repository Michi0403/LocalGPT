# Offline validation — LocalGPT 6.3.1

Input: customer source ZIP 6.3.0 (candidate). Changed source files and new static tests are recorded in the corresponding changelog.

`python -m pytest -q tests/static`: **29 passed**.  `node --check src/LocalGPT/wwwroot/js/localgpt-operational-feed.js`: passed.

These validate source contracts and JS syntax only. **No .NET SDK, compilation, Razor binding validation, Ollama runtime, GUI browser, Council multi-ZIP run or Windows installer execution**. No test has demonstrated that Stop, MFA, archive ingestion, or physical hardware detection now work end-to-end.

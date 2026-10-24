# LocalGPT 6.2.7 source validation

- Input: complete LocalGPT 6.2.6 source archive; database snapshot read-only; prior LocalGPT 6.0.12 source retained as historical reference.
- Tested: Python static regression suite (`tests/static/test_*.py`): 20 passed.
- Reviewed: fixed `content` shadow, prohibited static capability list, database-backed affinity, and C# render-state ownership.
- Export: all 52 ZIP JSON entries scanned for `compilestick` (only `ApplicationLogs.json` matched; 31 raw occurrences). No user database or logs included in source output.
- Unchanged: existing PowerShell architecture scripts, AGENTS.md and production permission contracts.
- Not performed here: dotnet compilation, Windows PowerShell architecture scripts, DevExpress runtime, SQLite migrations on live user DB, or real Council learning round. Passing source tests is not a substitute for those checks.

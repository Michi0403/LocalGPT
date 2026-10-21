# LocalGPT 6.1.6 — source-only validation

- Base: 6.1.5 supplied source ZIP.
- Python source-level regression command: `python -m unittest discover -s tests/static -p 'test_*.py' -v`.
- New checks assert actual inference integration, disabled planner tools, live team validation, atomic write-ahead journal, ordered resume skipping, approval gating, DI registration and existing cross-client call paths.
- Do **not** conflate these checks with Roslyn C# compilation, PowerShell gate validation, model-runtime inference or end-to-end recovery tests. The environment intentionally has no .NET or PowerShell runtime and neither was invoked.
- Original PowerShell architecture checks, build targets and packaging/maintenance scripts are unchanged. No bypasses or baseline edits.
- Manual host scenarios: single-model chat; semantic multi-subgoal planning; missing model fallback; team disabled between subgoals; process termination during Council call, inspect and approved resume; verify previously completed subgoals were not rerun; power loss during checkpoint write; model returns malformed JSON; no auto-repeat of risky calls.

- Inspected and corrected restart identification: an unlocked/recreated goal semaphore cannot authenticate ownership of a previously running checkpoint; ledger tracks successful in-process running writes instead.

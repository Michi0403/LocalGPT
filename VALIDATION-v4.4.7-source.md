# LocalGPT 4.4.7 source validation

Source-only validation was performed without `dotnet` and without GitHub/online repository access.

The supplied Windows build reached the maintained JavaScript diagnostics guard after all preceding localization, operational diagnostics, InteractiveServer, async-continuation, architecture, service-resilience, text-ownership, iterator, system-variable, and EF model/snapshot checks passed. It then failed because `src/LocalGPT/wwwroot/js/localgpt-game-console.js` had changed in 4.4.6 without the corresponding normalized SHA-256 entry in `build/javascript-diagnostics-files.sha256` being refreshed.

4.4.7 refreshes that exact manifest entry using the same LF-normalized UTF-8 SHA-256 algorithm implemented by `Assert-JavaScriptDiagnostics.ps1` / `Update-JavaScriptDiagnosticsManifest.Common.ps1`. The JavaScript source itself is not modified in this repair.

Static release and repository audits are rerun from both the working tree and the clean re-extracted source archive. A successful .NET compile is intentionally not claimed because this environment does not run `dotnet`.

## Executed source checks

- `build/audit_release_4_4_7.py` — passed.
- `build/audit_chat_ascii_console.py --root .` — 24 checks passed.
- `build/audit_ascii_doom_campaign.py --root .` — 32 checks passed.
- `build/audit_application_architecture.py --root . --product localgpt --mode all` — passed.
- `build/audit_service_resilience.py --root . --product localgpt` — passed for 2,483 guarded service methods; 29 yield methods and 3 direct Program/Startup methods skipped by policy.
- `build/audit_async_continuations.py --source-root src/LocalGPT` — passed for 270 files, 3,346 await tokens, 2,930 `ConfigureAwait(false)`, 193 renderer-affine `ConfigureAwait(true)`, 218 explicitly configured await-using disposals and 5 configured async streams.
- `build/audit_configurable_behavior_policy.py` — passed.
- `build/audit_xround_wiring.py` — passed.
- `build/audit_powershell_variable_interpolation.py` — passed.
- `build/audit_cross_platform_boundaries.py` — 22 checks passed.
- A Python equivalent of `Assert-JavaScriptDiagnostics.ps1` validated all 24 maintained LocalGPT browser JavaScript files, including normalized SHA-256 inventory membership, diagnostics marker, try/catch ownership and no empty catch blocks.

The Windows PowerShell build guard itself was not executed in this environment because PowerShell is not relied on here; the equivalent hash algorithm and complete maintained-JavaScript inventory contract were checked directly.

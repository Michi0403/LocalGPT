# LocalGPT 5.3.4 source validation

## Scope

Focused repair of the 5.3.3 configurable model-prompt/regex ownership regression and its Windows audit-reporting failure. Existing database, BusinessObject, Council, provider, DevExpress, safety, and execution architecture is retained.

No .NET build, restore, publish, PowerShell execution, GitHub repository access, or online repository access was performed.

## Runtime prompt/regex ownership

Executed directly with Python against the maintained LocalGPT source:

`python build/audit_runtime_text_ownership.py --root . --report <report>`

Observed result: **pass, 0 findings**.

The same audit was also executed with `PYTHONIOENCODING=cp1252` to emulate a legacy single-byte Windows console path. It completed successfully, confirming that console rendering no longer turns Unicode source examples into an opaque Python traceback.

The UTF-8 report path remains supported for complete finding text.

## Database-backed ownership checks

Static inspection verified:

- operational system/model prompts migrated by this repair resolve through database-backed runtime-policy values or persisted Council workflow-step prompt overrides;
- shipped defaults are supplied by `LocalGptRuntimePolicySeedDataService` and missing values are inserted by the existing database initialization flow without overwriting existing persisted values;
- direct operational regex patterns covered by the repaired guard resolve through `LocalGptRuntimePattern` and the existing database-backed regex store/compiler path;
- every runtime value, regex pattern, and collection referenced by maintained source has a declared enum key and seed entry; and
- no parallel prompt database, regex database, or replacement BusinessObject hierarchy was introduced.

## Source-only regression audits

Passed:

- application architecture;
- async continuation;
- async-only component/service architecture;
- provider-qualified Council (283 checks);
- Razor maintenance architecture;
- service resilience;
- transient UI-state ownership;
- CodeGeneration/DXFunction wiring;
- DevExpress Blazor controls; and
- cross-platform boundaries.

## Preserved dependency baseline

- LocalGPT version: 5.3.4
- DevExpress: 25.2.10
- .NET target: net10.0 / maintained 10.0.12 dependency baseline

No dependency downgrade was introduced.

## Build limitation

Per the maintenance constraint for this handoff, no `dotnet`, MSBuild, restore, publish, or compiler invocation was performed here. The next Windows build should therefore be treated as the compiler validation of this source package; the source-level ownership failure that stopped 5.3.3 has been removed rather than suppressed.

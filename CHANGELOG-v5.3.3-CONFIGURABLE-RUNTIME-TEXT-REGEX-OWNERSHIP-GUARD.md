# LocalGPT 5.3.3 — configurable runtime text/regex ownership guard

## Why this release exists

A Council role-synthesis path exposed a broader architecture regression: operational methods can still author system/model prompt prose, user/model-visible runtime text, and regular-expression patterns directly in source even though LocalGPT already owns database-backed prompt configuration, BusinessObject/seed boundaries, text services, and regex services.

The defect is broader than one Council class. Fixing only the reported prompt would leave the architecture open to the same regression elsewhere. This release therefore adds the permanent architecture rule first so the repository can report the complete migration set before those source findings are repaired.

## New baseline-free architecture rule

`build/Assert-RuntimeTextOwnership.ps1` and `build/audit_runtime_text_ownership.py` now scan maintained LocalGPT C# and Razor code and report every current finding without a grandfather/baseline file.

The audit emits three source error categories:

- `PROMPT001` — an operational method constructs prompt/instruction/briefing text instead of resolving a configurable prompt/template value;
- `TEXT001` — an operational method constructs authored runtime prose instead of obtaining it from an injected/configurable text owner; and
- `REGEX001` — regular-expression pattern text is embedded outside the database-backed regex seed/catalog/compiler ownership boundary.

Resettable seed/catalog owners remain the valid place for shipped defaults. Structured diagnostic logger templates and technical implementation identifiers are not treated as user behavior policy.

## Build integration

The LocalGPT build now runs `AssertLocalGptRuntimeTextOwnership` after the application-static architecture guard and before the older component/controller text-service guard. The audit prints all findings before failing and also writes the complete report to:

`src/LocalGPT/obj/runtime-text-ownership-findings.txt`

The same guard is part of `build/Invoke-RepositoryValidation.ps1`, and `Assert-ArchitectureTasks.ps1` protects the rule itself from being silently removed.

## Current migration inventory

The 5.3.3 source intentionally remains non-green under the new rule so the existing architecture debt can be surfaced rather than hidden:

- 148 `PROMPT001` findings;
- 1,626 `TEXT001` findings; and
- 92 `REGEX001` findings;
- 1,866 findings total.

The originally reported `MultiModelCouncilService.BuildConfiguredRoleSynthesisPrompt` hardcoded raw prompt is included as `PROMPT001`, and direct regex literals in services such as `RemoteControlPipelineService`, `UserDxAiFunctionService`, and project-maintenance fallbacks are included as `REGEX001`.

No application finding is baseline-suppressed in this release.

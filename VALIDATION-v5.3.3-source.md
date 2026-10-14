# LocalGPT 5.3.3 source validation

## Scope

Architecture-rule release only. This release intentionally installs a baseline-free guard before migrating the reported hardcoded prompt/text/regex findings so the complete source inventory can be captured from one build/report.

No .NET build, restore, publish, PowerShell execution, GitHub repository access, or online repository access was performed.

## Runtime-text ownership audit

Executed directly with Python against the maintained LocalGPT source:

`python build/audit_runtime_text_ownership.py --root .`

Expected result for this inventory release: non-zero, because the new rule is designed to expose existing debt rather than grandfather it.

Observed inventory:

- `PROMPT001`: 148
- `TEXT001`: 1,626
- `REGEX001`: 92
- total: 1,866

The audit output is approximately 627 KB and includes MSBuild-compatible `file(line,column): error CODE:` entries for every finding.

Verified representative findings include:

- `MultiModelCouncilService.RoleSynthesis.cs` / `BuildConfiguredRoleSynthesisPrompt` (`PROMPT001`);
- `ProjectMaintenanceService.cs` hardcoded solution regex fallback (`REGEX001`);
- `RemoteControlPipelineService.cs` hardcoded key regex (`REGEX001`); and
- `UserDxAiFunctionService.cs` hardcoded function-name regex (`REGEX001`).

## Guard integration

Static inspection verified:

- `Directory.Build.targets` invokes `AssertLocalGptRuntimeTextOwnership` after `AssertLocalGptApplicationStaticPolicy` and before `AssertLocalGptTextServiceOwnership`;
- normal LocalGPT builds write the complete findings report to `src/LocalGPT/obj/runtime-text-ownership-findings.txt`;
- `build/Invoke-RepositoryValidation.ps1` invokes the new guard;
- `build/Assert-ArchitectureTasks.ps1` requires both the PowerShell wrapper and Python audit plus the `PROMPT001`, `TEXT001`, and `REGEX001` contracts;
- `AGENTS.md` and `docs/architecture/project-data.md` document database-backed prompt/text ownership and regex-catalog ownership; and
- the audit has no baseline/grandfather file or suppression list for existing findings.

## Preserved dependency baseline

- LocalGPT version: 5.3.3
- DevExpress: 25.2.10
- .NET target: net10.0 / current 10.0.12 environment baseline from the prior release

No dependency downgrade was introduced.

## Packaging checks

- C# source was not modified to hide any finding.
- The reported hardcoded role-synthesis prompt remains present specifically so the new guard can prove it is caught before the subsequent migration repair.
- Python syntax compilation for the new audit passes.
- XML parsing of `Directory.Build.targets` passes.
- ZIP integrity is verified after packaging.

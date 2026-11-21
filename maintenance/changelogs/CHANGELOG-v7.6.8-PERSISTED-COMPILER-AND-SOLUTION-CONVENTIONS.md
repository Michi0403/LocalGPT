# LocalGPT 7.6.8 — Persisted Compiler & Solution Conventions

**Source-only candidate · owner-side PowerShell/.NET build and end-to-end execution pending.**

## Evidence and scope

The examined Git commit `443eac6632402015c8e47089562a1b2a3e6d2ac0` changes only four LocalGPT files: `LiveCouncilLaneBoard.razor`, `CodeGenerationWorkflowService.Draft.cs`, `CodeGenerationWorkflowService.DraftTargetValidation.cs`, and `LocalGptRuntimePolicySeedDataService.cs`. It adds `ValidateManifestLayout`, assuming `*.csproj` is the only project root, `App.razor` or `_Imports.razor` is universally required and `wwwroot` must sit under that hardcoded project root. Those constraints can reject valid applications and do not consult the user's persisted workspace/solution configuration. The incidental C# pattern-variable rename in the live lane UI is harmless. **This commit does not, by itself, remove a DbContext, database registration or DI wiring.** Do not attribute a database disconnection to a change it did not make.

The 7.6.7 source baseline also contained earlier fixed-file-name and fixed-extension assumptions in its source-closure, reference-validation, generated-workspace registration and artifact-building code. The source ZIP for this release is based on 7.6.7; it deliberately does *not* incorporate the four-file commit's new restrictive `ValidateManifestLayout` guard or its two new runtime policy notices. It retains the benign pattern-variable rename.

### Relevant persisted model: do not conflate fields

- `ProjectCompilerInstallation`: installed executable, enabled/validated state, version, language/toolchain profile, per-compiler environment. It **does not store preferred `.sln` filenames**.
- `ProjectWorkspaceRoot`: source root, `SolutionPattern`, optional `PreferredCompilerInstallationId`, workspace environment and selected project binding.
- Existing runtime-policy regex `runtime.project.solution-file-extension` provides the general editable solution-path convention; `SourceExtensions`, `TextExtensions` and `BinaryExtensions` are database-seeded and user-overridable extension collections.
- The authored and independently approved manifest is the authority for exact generated source-file paths; the compiler installation is not a source-file template.

## Source modifications

1. **Source-file classification and authoring** — `CodeGenerationWorkflowService.SourceClosure.cs`, `DraftTargetValidation.cs`, `DraftInspection.cs`, `Draft.cs`, `MultiModelCouncilService.FileWorkManifest.cs`: recognize solutions through the existing persisted runtime solution pattern, project declarations via the format suffix rather than one C#/F#/VB list, and reduce false-negative layout gates that demanded `Program.cs`, `App.razor` or fixed Twitch/SQLite folder names. The exact original requested .NET target and exclusion of DevExpress are still checked on generated XML. Do not interpret a successful lexical check as semantic correctness or compilation.
2. **Project references and output paths** — `CodeGenerationWorkflowService.SourceClosure.cs`, `Execution.cs`, `WorkspacePersistence.cs`: structural `.sln`/`.slnx` and project references use the manifest's actual paths, including relative resolution, duplicate GUID detection, and declaration/manifest equality. Generated revision registration prefers the **exact approved solution file** before fallback discovery; approved tracked workspace cloning and fallback solution discovery use the configured solution regex instead of hardcoding `.sln`/`.slnx` into the file search. The artifact exporter continues to materialize only approved bytes.
3. **Database-backed source catalog** — `ProjectMaintenanceService.TextAndValidation.cs`: text/source detection consults existing `TextExtensions`, `SourceExtensions` and solution regex rather than an extra private list embedded in the maintenance service. The Council's package/source proposal logic likewise uses the database-owned extension collections; externally proposed packages do not silently authorize restore.
4. **Actual build toolchain and environment** — `ArtifactBuildExecutor.cs`: preserves separate exact-human build authorization and configured build disablement; reads compiler installations and applicable workspace settings through `IProjectMaintenanceService` in a short-lived DI scope, because the executor is singleton and maintenance service is scoped. Uses the **registered enabled successfully validated .NET SDK executable**, an applicable workspace preferred compiler when set, the existing solution regex or selected workspace's `SolutionPattern`, and the saved compiler/workspace environment variables (workspace values override compiler values, matching project-maintenance build-review semantics). No PATH `dotnet` fallback; invalid workspace pattern, absent validated executable, or invalid environment fails closed with a clear result. No saved default, compiler record, hardware road, or process-wide environment is modified.
5. **Existing PowerShell source-contract guard** — extend `build/Assert-CouncilXRoundWiring.ps1` to reject the hardcoded layout gate and PATH executable and require the persisted-data/DI boundaries. 129 existing source-token assertion entries were checked offline without executing PowerShell.

## Security / functional boundaries retained

- No unapproved source materialization, no new writes to the user's DB, no project deletion, no changes to user-defined compiler defaults or preferred workspace pattern.
- Never classify `SourceOnly` ZIP/hash verification as a compiled, runnable application.
- Source-closure remains conservative: valid code can legitimately have nonstandard composition; exact semantic completeness, service wiring, dependency versions and framework compatibility require independent review and, when authorized, real compilation.
- Existing scaffolding for **explicit C# output kinds** still uses its C# template by design; this is distinct from generic reviewed-source draft mode. General language-specific template selection remains future work, not silently altered here.

## Limitations and follow-up work

- Existing source-closure human-decision parsing still contains a limited lexical mapping for common database providers and simple solution-name answers. Arbitrary free-text choices and unusual project systems require a typed contract/semantic review; they must not be guessed into another hardcoded template.
- The general solution regex is sourced from runtime policy in generic source-draft operations; a workspace-bound `SolutionPattern` is used in build execution when the target belongs to that workspace. Full propagation of each per-project naming/profile convention through the architect's draft-plan contract remains open.
- The build executor presently chooses the highest validated .NET SDK when no workspace preferred compiler is set. Exact target-framework-to-SDK compatibility and executable-specific environment/toolchain preparation should be independently verified in future owner runs.
- Structured validation of every declared assembly/service contract and complete semantic domain coverage requires compile/tests, not name guesses.

## Acceptance required on owner's Windows environment

- Execute unchanged `build/Assert-PowerShellCompatibility.ps1`, `build/Assert-CouncilXRoundWiring.ps1`, `build/Assert-ApplicationStaticPolicy.ps1`, `build/Assert-TextServiceOwnership.ps1` and full Debug/Release build. Editor did not run PowerShell/.NET.
- Positives: C# and F#/VB MSBuild project layouts and custom names, valid .sln/.slnx referenced paths, non-`Program.cs` startup work, alternative Blazor component root, persisted custom text extension; enabled validated SDK with non-default path and workspace environment.
- Negatives: invalid `.sln` GUID/project reference or missing manifest project, .NET target downgrade, prohibited DevExpress, preferred compiler unvalidated, invalid solution regex/env JSON, denied/outside-root build, no human approval.
- Repeat full Blazor .NET 10 Twitch + SQLite + selected solution name workflow, then verify code files, solution-project linkage, explicit decision provenance, independent inspections, source ZIP integrity, and **separate actual build receipt**.

PublisherStudio unchanged. No online push/commit or build undertaken. The commit was inspected read-only to establish its exact diff; this version was packaged from the provided 7.6.7 source archive.

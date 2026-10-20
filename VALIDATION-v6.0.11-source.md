# LocalGPT 6.0.11 source validation

Scope: full source release based on 6.0.10; only the Council source-review goal and receipt transport paths, model/editor/seed validation, version, and these notes changed.

## Static checks

- Workflow rules are persisted in `CouncilWorkflowStepDefinition` and edited using existing DevExpress controls, not team-key conditions or application statics.
- `CouncilTeamConfigurationService` validates impossible combinations and preserves strict DXFunction read requirements.
- `CouncilTeamConfigurationService` seed version increments to 47; user-modified teams retain their user-owned copy under the existing seed policy.
- Source evidence is counted by unique registered read path/cursor ranges, not by a file list or curation hash. A last page without coverage from offset zero is incomplete.
- Recovery must stop after a non-progressing attempt when the saved step enables progress-based continuation. A configured maximum bounds all retries.
- Both the provider-native truncation envelope and DXFunction gateway retain compact metadata produced by the actual registered file-reading function. A transport-truncated read does not count as a completely delivered source file; distinct attempts can trigger bounded smaller-page recovery. Caller-selected small read segments are no longer forced to 64,000 characters. No textual source contents are embedded in progress diagnostics.
- The `.csproj` version is `6.0.11`.

## Required runtime checks (not run here)

- Windows `dotnet build -c Debug` and native build-wired PowerShell/DevExpress/XML documentation guards.
- Learning Round against six archived source roots, including a large multi-page file and a limited model tool-turn budget.
- Verify that a duplicate read earns no incremental progress, a genuinely new read/page extends a bounded retry, a root still uncovered blocks completion, and progress cessation stops the repeat.
- Verify persistence of user-edited Council step goals and behavior across restart. Confirm existing user team overrides are not replaced by refreshed system seeds.

No end-to-end Council run, Windows debugger session, .NET build, or actual-source semantic learning is claimed as tested.

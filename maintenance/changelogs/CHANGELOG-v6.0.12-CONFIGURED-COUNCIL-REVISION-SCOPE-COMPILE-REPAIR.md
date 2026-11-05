# LocalGPT 6.0.12 — Council revision scope compile repair

## Reported failure

Windows Visual Studio reported CS0136 in `src/LocalGPT/Services/MultiModelCouncilService.ConfiguredWorkflowExecution.cs` at the source-review retry loop (`previousRevision`).

## Correction

The retry loop's local `out var previousRevision` was shadowing the existing variable declared in its enclosing non-loop workflow step. The inner declaration and its use are now named `previousSourceReviewRevision`. The value and the update to `workflowRevisions[firstStep.Key]` are unchanged. No Council configuration, recovery policy, evidence processing, persistence, or release guard was changed.

The main application package version was increased from 6.0.11 to 6.0.12; the shared `LocalGPT.ReleasePackaging` package version is unchanged.

## Scope

- One C# source file: lexical rename only.
- One application `.csproj`: version bump.
- This changelog and its validation notes.

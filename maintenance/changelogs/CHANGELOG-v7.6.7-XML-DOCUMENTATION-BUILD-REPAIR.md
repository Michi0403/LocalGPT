# LocalGPT 7.6.7 — XML Documentation Build Repair

**Baseline:** LocalGPT 7.6.6 source candidate. **Scope:** documentation/compiler correction; no runtime logic changes. **PublisherStudio:** unchanged.

## Owner build evidence (21 November 2026, 09:35:21)

After all existing PowerShell compatibility, architecture, text-ownership and Council source-contract gates passed, the Debug compiler reported four errors:

- `CS1572` at `src/LocalGPT/Interfaces/IHumanCollaborationService.cs:22`: orphaned `includeResolved` XML parameter on `GetSourcePlanningDecisionsAsync`.
- `CS1572` at the same file, line 23: orphaned `take` XML parameter on the same method.
- `CS1573` at line 29: `councilRunId` had no matching XML parameter on `GetSourcePlanningDecisionsAsync`.
- `CS1573` at `src/LocalGPT/Services/CodeGenerationWorkflowService.cs:76`: constructor injection parameter `humanCollaboration` had no matching XML parameter.

## Root cause and correction

During the 7.6.4 source-decision implementation, the new `GetSourcePlanningDecisionsAsync` interface method was inserted between the existing `GetSnapshotAsync` documentation and its declaration. A second summary was added, but the existing `<param>` tags remained attached to the new method. That causes a parameter-name mismatch when compiler XML documentation warnings are treated as errors. Independently, the newly injected `humanCollaboration` constructor parameter was not documented.

- Move the original snapshot comment as a complete unit so it immediately precedes `GetSnapshotAsync`.
- Provide the correct summary, `councilRunId`, `cancellationToken`, and return documentation for `GetSourcePlanningDecisionsAsync`.
- Document the existing `humanCollaboration` constructor parameter without changing the constructor signature or injected service behavior.
- Extend the existing `build/Assert-CouncilXRoundWiring.ps1` with PS 5.1-compatible source-contract assertions for both documentation-to-signature boundaries and all twelve constructor collaborators. No gate removed or loosened; no new test script or Python repository test introduced.

## Verification boundaries

The editor checked the exact source changes and ZIP integrity, but did **not** execute PowerShell or .NET compilation. The owner's next build must verify that the reported XML diagnostics are resolved and reveal any further compilation issues. Existing human decisions, manifest repair, independent approval, build-status handling and source artifact functionality remain unchanged. The previous cumulative tasks and the planned NUTS/competence work are retained in `TASKS.md`.

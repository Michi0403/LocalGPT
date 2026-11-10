# LocalGPT 7.0.6 — Council file/CodeDOM development lifecycle

Source revision based exclusively on LocalGPT 7.0.5. PublisherStudio remains 4.7.6; no PublisherStudio changes are included.

## Implemented

- Incremented LocalGPT application version to **7.0.6** and Council system-seed revision to **54**. Existing database-owned/user-modified Council teams remain preserved by the current seed reconciliation mechanism; the upgraded compiler team is a visible resettable default, not a runtime hardcoded team branch.
- Connected the existing `ICodeDomSourceService` to the same isolated Council artifact workspace as ordinary file edits, via read-only `council.artifact_workspace.codedom.preview` and separately approval-gated `council.artifact_workspace.codedom.write`. The existing typed `CodeDomTypeSpec` contract is reused rather than creating a second source generator. CodeDOM is explicitly optional and remains limited to its supported declaration shape; arbitrary C# refactoring still uses reviewed file edits.
- Added `council.artifact_workspace.sandbox.run`: reads the **current** approved workspace-relative file, validates it against the advertised Hyperlight guest language/extension and a mandatory inspection-time source SHA-256, and submits source with fresh approval to the existing isolated runner. `session.sandbox.runs` continues to supply actual status/exit/output evidence. The guest still does **not** mount the repository, and project dependencies or full-solution builds cannot be verified by this source-only route.
- Added `ICouncilDevelopmentWorkspaceService` and `CouncilDevelopmentWorkspaceService` with source SHA-256 baselines at approved repository staging. The baseline is stored **outside** the editable copy. `council.development.artifact.verify` requires either an executed, successful reviewed generation with a nonempty ZIP, **or** a staged source that actually changed and a refreshed complete ZIP whose byte hashes and file inventory match the current workspace. This closes the unconditional `codegen.review.create` completion mismatch between greenfield and existing-project tasks.
- Updated the compiler seed's generation, review, toolchain and verification steps to use the same staged workspace for CodeDOM and file-level edits, to preserve pending human approvals, and to use real output receipts rather than single-model prose. Peer review and configurable team roles remain. Completion recovery targets the configured verifier.
- Rejects reparse/symlink hops in artifact workspace text-file resolution to prevent an otherwise in-root relative path from following a link outside its workspace after staging.
- Extended the existing `build/Assert-CouncilRepositorySandboxAndTrafficFiles.ps1` PowerShell contract guard, removed its version-specific 7.0.5 assertion in favor of the repository's one-digit version policy, and wired it into standard LocalGPT builds. Added workflow documentation to the existing projects/workspaces and sandbox references.

## Unchanged boundaries and remaining limitations

- No change to PublisherStudio. No commits, GitHub access, publishing or merges into an original repository are performed. The original path remains untouched by Council staging; a ZIP is the handoff unless a future separately approved promotion operation is implemented.
- All consequential source writes, ZIP refreshes and guest runs still require their existing explicit approval. This release does not grant arbitrary host file access or bypass the application's configured DXFunction catalog and Council roles.
- CodeDOM generates only the shapes supported by the existing emitter. An isolated source snippet (whether from CodeDOM, C#, Python or PowerShell) is **not** a full-repository Hyperlight build. Full repository PowerShell checks still require the existing separately approved host console path; external dependency resolution, complete project compilation and IDE/runtime debugging require the target environment.
- `council.development.artifact.verify` verifies actual artifact evidence, **not** whether the resulting source is semantically correct or all tests passed. PowerShell guard and compiler receipts must be reported separately. A pending human decision remains pending, not a successful build.
- The DevExpress `DeviceInfo` / `DxComboBox` null-reference failure noted with 7.0.5 is not fixed by these development-workflow changes.

## Verification

- The source tree, XML project version, JSON DXFunction parameter schemas, diff scope and ZIP integrity can be checked without .NET. These are **not** compiler or guest execution evidence.
- No .NET SDK, Windows PowerShell/pwsh runtime, Hyperlight guest or Windows/Blazor runtime is available in the editing environment; **no PowerShell guard run, .NET build, Hyperlight execution or runtime regression test is claimed**.
- On the target Windows machine, run from the repository root:

```powershell
& ./build/Assert-CouncilRepositorySandboxAndTrafficFiles.ps1
& ./build/Invoke-RepositoryValidation.ps1
```

Only accept the revision after these and an interactive Council test complete. Suggested scenario: stage a real source archive; preview and write a CodeDOM helper; edit a real integration file; approve a source-file guest run; inspect the guest receipt; run repository guards; refresh ZIP; obtain `council.development.artifact.verify` success and compare the returned ZIP against the original. Repeat with explicit greenfield generation, and make sure an approval-pending or failed-build case cannot falsely complete.

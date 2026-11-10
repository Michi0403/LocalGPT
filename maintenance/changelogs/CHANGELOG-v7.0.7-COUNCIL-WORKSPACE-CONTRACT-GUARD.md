# LocalGPT 7.0.7 — Council artifact workspace contract guard correction

Source revision based on LocalGPT 7.0.6. No PublisherStudio or MeowReal changes.

## Corrected

- Fixed a build-blocking mismatch in `build/Assert-CouncilRepositorySandboxAndTrafficFiles.ps1` reported by the maintainer's Windows Debug build. The guard was checking `council.artifact_workspace.file.write`, which is not a registered function; the DI-owned handler and Council seed both use `council.artifact_workspace_file.write` (underscore before `file`).
- Guard now verifies the exact existing read and write function names against the Council seed **and** verifies their real descriptor names and invoke routes in `ArtifactWorkspaceDxAiFunctions.cs`. This preserves the architecture safety gate instead of inserting a fake string or changing the public DX function contract to satisfy a typo.
- Incremented the LocalGPT application version from `7.0.6` to `7.0.7` (single-digit minor and patch). No Council system-seed revision bump: the persisted configuration and runtime functions are unchanged.

## Why this matters

The maintainer's 7.0.6 build passed the earlier source/PowerShell architecture checks, then stopped at this guard with `Missing source contract council\.artifact_workspace\.file\.write`, producing MSB3073 before the C# application compilation could establish success or failure. The failure is real, not a harmless warning. This revision corrects that first reported blocker without weakening the gate.

## Verification

- Reviewed the 7.0.6 source and confirmed that `council.artifact_workspace_file.write` and `.read` are registered in `ArtifactWorkspaceDxAiFunctions.cs` and used consistently in the default Council seed.
- Source-contract patterns, exact descriptor routes, version format, and ZIP integrity can be checked statically without a .NET SDK.
- **Not executable in the editing environment:** Windows PowerShell / pwsh, .NET compilation, DevExpress runtime checks, and the full repository validation suite. These require a Windows/.NET machine; no build success is claimed.

Run from the repository root on Windows:

```powershell
& ./build/Assert-CouncilRepositorySandboxAndTrafficFiles.ps1
& ./build/Invoke-RepositoryValidation.ps1
```

Then build LocalGPT with the normal repository entrypoint and report any subsequent compiler errors verbatim. Do not accept this revision as built until it passes on the target machine.

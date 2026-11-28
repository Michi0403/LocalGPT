# LocalGPT 7.7.3 — configured compiler authority for artifact builds

Baseline: supplied LocalGPT 7.7.2 source candidate. PublisherStudio 4.9.1 is unchanged.

## Corrected

- `ArtifactBuildExecutor` no longer rejects every non-`.sln`/`.slnx`/MSBuild target or limits every approved build to a .NET installation. A persisted `ProjectWorkspaceRoot.SolutionPattern` is used as a **build target regex**, and a validated `PreferredCompilerInstallationId` selects the actual compiler. This uses existing project maintenance data rather than an invented language or PATH probe.
- Existing workspace `BuildArguments` now drives language-specific compiler calls; no flags are invented for unknown languages. Shared `ProjectBuildArgumentTemplate` supports `{target}`, `{targetDirectory}`, `{configuration}`, and `{outputDirectory}`. Placeholders are inserted as quoted arguments. A requested output directory must appear in the saved template so it cannot be silently ignored.
- `ProjectMaintenanceService.RunBuildVerificationAsync` uses the same expansion for persisted workspace arguments; when no explicit solution path is stored, it can resolve the selected workspace’s build target from already approved and hashed tracked files instead of treating the source directory as a universal .NET solution. Explicit request arguments and existing compiler-specific defaults remain intact.
- A project-associated generated review can pass its `ProjectId` to the artifact executor. This resolves the saved compiler/workspace policy when the generated work is under a separate, strictly bounded artifact root.
- For plain-file code-generation reviews with no existing MSBuild target, `BuildAfterGeneration` recognizes targets in *reviewed files only* via the project/workspace's saved regex. Non-CodeDOM languages need not masquerade as solutions.
- Retained the historical .NET solution/MSBuild convenience path **only for a validated .NET compiler**; `.sln`/`.slnx` are not universal language gates.
- Project Maintenance's workspace editor now describes the persisted `SolutionPattern` field as a build-target regex, keeping its database name unchanged for compatibility.
- The established compiler registry, toolchain knowledge/discovery and Council DX functions remain authoritative: `toolchain.installation.list`, `toolchain.profile.execute`, `project.revision.build.verify`, and `toolchain.discover` are not replaced by a private executor registry. Executing tools still requires human authorization.

## Safety / compatibility

- Still requires exact build confirmation, bounded target/output paths, enabled artifact-build policy, a successfully validated registered executable, bounded timeout, process-tree cleanup, saved environment order, and checked exit code.
- Ambiguous non-.NET compiler selection fails closed; assign the workspace's preferred compiler instead of guessing a language from a file extension. No shell is introduced for native executables; existing platform wrapper behavior is reused for registered wrapper executables.
- Missing arguments for an unknown compiler are reported as `BuildArgumentsMissing`. Invalid workspace patterns and missing/invalid compiler information are reported without launching a process.
- An existing customized workspace regex or build argument is not silently overwritten or migrated; persistence and schema names remain unchanged.
- DotNet-specific source-closure checks and DLL-only Council artifacts remain intentionally scoped to their existing .NET features, not imposed on generic build execution.

## Examples (saved workspace settings)

| Toolchain | Workspace build target regex | Saved workspace build arguments |
|---|---|---|
| Python (syntax validation) | `(?i)\.py$` | `-m py_compile {target}` |
| Cargo | `(?i)(^|/)Cargo\.toml$` | `build --manifest-path {target}` |
| Make | `(?i)(^|/)Makefile$` | `-f {target}` |
| Go | `(?i)\.go$` | `build {target}` |

Set an enabled, successfully validated compiler/runtime as the workspace's preferred installation. Examples are operator configuration, not hardcoded language defaults. An output-producing workflow must include `{outputDirectory}` when an explicit output directory is requested.

## Validation scope

Source-level review and ZIP integrity checks only. The regression guard `build/Assert-CompilerAgnosticArtifactBuild.ps1` is invoked from the existing `Assert-CouncilXRoundWiring.ps1` architecture guard. The editing environment has no PowerShell or .NET runtime; no compiler or PowerShell tests have been executed here. See `VALIDATION-v7.7.3-COMPILER-AGNOSTIC-BUILDS.md` for host acceptance cases.

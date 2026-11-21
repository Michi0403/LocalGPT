# LocalGPT 7.7.3 — compiler-agnostic build validation

## Source contract

Run in the repository's **PowerShell** environment:

```powershell
pwsh -NoProfile -File ./build/Assert-CompilerAgnosticArtifactBuild.ps1
pwsh -NoProfile -File ./build/Assert-CouncilXRoundWiring.ps1
```

Do not interpret source contract success as a compiled/runtime test. The checks guard against reintroduction of a .NET-only artifact gate and verify dynamic target/configuration wiring.

## Host/runtime acceptance

1. Register and validate a .NET SDK, retain a `.csproj` build without an assigned workspace; verify successful legacy behavior and existing configuration/output handling.
2. Register and validate a Python interpreter. Create a workspace with `(?i)\.py$`, a preferred Python installation, and `-m py_compile {target}`. Approve a build of a Python source **inside** the allowed artifact root. Verify the exact executable and exit code; no .NET requirement or generated `.sln`.
3. Configure a workspace for `Cargo.toml` or `Makefile` and saved arguments. Verify that the matching registered compiler/build-tool executable runs after approval; a nonmatching file is denied.
4. Configure two distinct language toolchains with no preferred compiler. Verify an ambiguous non-.NET selection returns `CompilerNotSelected` and launches nothing. Assign the preferred installation and retry.
5. Run `codegen.review.execute` with a registered project, `BuildAfterGeneration`, reviewed non-CodeDOM file(s) matching that project's saved pattern, and explicit human approval. Verify it builds the reviewed target under the approved artifact directory using the project's compiler; it must not write into the source project root.
6. Verify invalid workspace regexes, disabled/unvalidated executables, non-existent targets, path escapes, out-of-root output directories, and missing required `{outputDirectory}` are rejected without launching a compiler.
7. Validate `toolchain.profile.execute` and `project.revision.build.verify` still remain Council-discoverable approval-gated dynamic tools. Confirm no preference/environment values were mutated by an artifact build.
8. Verify Windows `.cmd`/`.bat` registered compiler wrappers and native Unix executables against the platform process adapter; confirm quoted paths with spaces and human approval requirements. Inspect output logs rather than assuming exit code zero proves the intended artifact was generated.

No .NET compilation, PowerShell execution, provider runtime invocation, or UI behavior was executed in the editing container.

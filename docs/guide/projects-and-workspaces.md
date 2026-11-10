# Projects and workspaces

## Durable project model

A LocalGPT project is language-neutral. It can describe a .NET solution, firmware workspace, game content, documentation set, or another technical/creative structure.

The durable model separates:

- project identity and purpose;
- versions and revisions;
- requirements and requirement links;
- named artifacts;
- topics and reviewed knowledge;
- tracked files and file-pattern rules;
- workspace roots and environment profiles;
- compiler installations and build evidence.

A filesystem path helps locate a checkout; it is not the identity of the project. Stable database identifiers survive moves and alternate workspaces.

## Revisions and tracked files

A revision records ancestry, status, structure metadata, and build evidence. A tracked file uses a normalized relative path and stable identity. Hashes provide evidence that the file being reviewed is the file that was assessed; they do not grant permission to overwrite it.

The typical flow is:

1. register or select a project;
2. resolve an allowed workspace root;
3. scan expected files using maintained patterns;
4. create or select a revision;
5. inspect and propose changes;
6. save a review record;
7. approve the exact revision for testing;
8. run bounded verification through the configured toolchain.

## Workspace permission assessment

A workspace contains rules for allowed paths, expected structure, compiler profiles, and environment variables. Findings are grouped as:

- **Approved** — the required operation is inside the allowed boundary;
- **Warning** — the operation may proceed only after visible review;
- **Danger** — the operation is blocked until the boundary is corrected.

Compiler execution requires a fresh assessment with proven access and no unresolved danger finding. A prior successful build does not automatically approve a later command against changed files or another path.

## Toolchains

Compiler installations are records, not guessed commands. A toolchain can describe executable path, version, environment, supported project types, and validation state. Native execution goes through the command runner and approval policy rather than direct `cmd.exe` composition.

## Collaboration and knowledge

Projects can link reviewed Council knowledge to topics and versions. Knowledge entries keep provenance, review state, archival state, and optional expiration. Automatic context should use only entries that remain current and explicitly eligible.

## Artifacts

Generated source, documents, firmware, reports, and other outputs belong to reviewable artifact workspaces. Saving an artifact is separate from compiling, flashing, publishing, or replacing a project file.

Continue with [Project and data architecture](../architecture/project-data.md) for the service and persistence boundaries.

## Building a game with the Project system

Set **Project type** to `Game` to make a project a LocalGPT game-authoring project. The existing project capabilities still apply: revisions describe the project structure, requirements capture intended behavior, workspace roots/files provide source context, and project artifacts hold reviewed build/configuration data.

The Projects page adds a **Game project build** section for this project type. Choose a runtime profile and the bounded runtime defaults, then build the project. LocalGPT persists the resulting `ProjectGameDefinition` as a project-owned `GameBuild` artifact. The build requires explicit confirmation because it writes durable project metadata.

A built game can be launched without leaving `/Chat`:

```text
:game project <project name or id>
```

AI/Council workflows can use `project.game.get`, `project.game.build`, and `project.game.start`. `project.game.build` uses the normal one-use approval gate; `project.game.start` only coordinates an already-approved build into the shared game runtime.

The ownership direction is deliberate: **Project -> built game definition -> GameDirector runtime -> controllers/renderers**. The runtime is a consumer of the build, not the owner of the game project.

## Council coding workflow: staged files, CodeDOM and isolated source tests (LocalGPT 7.0.6)

The seeded **Program Compiler & Repository Curator** team is an editable default, not an unchangeable runtime coding policy. An existing repository uploaded as ZIP, a registered project, or a separately approved absolute source path is copied through `council.repository.workspace.stage` to a new isolated Council artifact workspace. The original repository is not overwritten and no Git commit is made. File edits use `council.artifact_workspace_file.write` with a fresh approval.

**CodeDOM is an alternative source-generation helper inside the same workspace.** `council.artifact_workspace.codedom.preview` invokes `ICodeDomSourceService` without mutations. `council.artifact_workspace.codedom.write` writes an approved typed C# declaration to a workspace-relative `.cs` path through the same emitter. The existing CodeDOM declaration format is intentionally limited; existing classes and arbitrary modern C# refactors should still be handled as reviewed file edits. Neither operation silently executes generated code.

To test an executable *single source file*, `council.artifact_workspace.sandbox.run` reads that exact workspace path, checks `expectedSourceSha256` against the current source hash returned by `council.artifact_workspace_file.read`, and submits the matched contents to a configured matching Hyperlight guest language after a separate approval. The returned SHA-256 and run ID are correlated with `session.sandbox.runs`. A C# service file containing project dependencies is **not** a standalone compilation test; a Hyperlight guest does not mount the repository. Whole-repository PowerShell guards run through approved host `localgpt.console.execute` and their independent receipts; no guest results are reported as full-solution build results.

After approved edits, `council.artifact_workspace_zip` creates a complete source archive. `council.development.artifact.verify` checks that the staged workspace differs from its approved original-byte baseline and that the ZIP contains the *same current bytes and file inventory*. For greenfield codegen it checks the approved executed review's ZIP and build outcome. This shared completion receipt replaces the former unconditional `codegen.review.create` requirement. It does not certify compiler success or PowerShell guards; those outcomes must be reported separately. If human approval is pending, the task is pending rather than falsely completed.

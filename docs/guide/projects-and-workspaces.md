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

# LocalGPT project maintenance, workspaces, toolchains, and revision evidence

## Purpose

LocalGPT can maintain full local solutions without using an absolute path as the identity of a project or file. Database identities remain stable while path fields help the user and Council locate the current checkout, solution, project file, and tracked file.

## Path and identity model

A project, revision, and tracked file each has a stable GUID. A tracked file also has a stable hash key derived from the project, revision, and normalized relative path. The database stores:

- the current absolute file path;
- project-relative and workspace-relative paths;
- the absolute solution path and nearest project-file path;
- file name, extension, size, last-write time, content type, encoding hint, and SHA-256 content hash;
- an approved file role plus optional structure and content-format regular expressions.

Paths are helper metadata. Moving a checkout requires registering or scanning the new revision workspace; it does not change the project/revision identity.

## Workspace resolution

Workspace roots are selected in this order:

1. exact selected project;
2. project-type regular expression;
3. default global workspace;
4. another enabled global workspace;
5. the per-user LocalAppData fallback.

Source checkouts are never used as snapshot output directories. Generated review workspaces and ready-for-test archives live below the resolved workspace root.

## Compiler inventory

The setup page discovers and stores multiple versions of:

- .NET SDK (`dotnet`);
- JDK/JRE (`javac`, `java`);
- Python;
- PowerShell 7 and Windows PowerShell;
- MSVC, GNU C++, and Clang C++.

Discovery checks PATH, common Windows/Linux/macOS locations, and user-entered roots. Every installation has its own executable, home directory, version probe, architecture, enabled/default flags, and optional environment JSON such as `DOTNET_ROOT`, `JAVA_HOME`, or a private toolchain PATH. A compiler must pass a fresh bounded validation probe before it can build a revision.

## Exact-source revision workflow

1. Select a project and revision.
2. Register the revision source root and solution path.
3. Scan the revision. LocalGPT records each approved file path, regex metadata, SHA-256 hash, and exact size.
4. A CodeDOM/Council change review clones every approved file byte-for-byte into an isolated workspace. The clone is rejected if a source hash changed or a copied byte differs.
5. Reviewed changes are applied only in that isolated workspace.
6. Register and rescan that generated workspace as the revision source.
7. Run the selected compiler and optional tests after one-use human approval.
8. LocalGPT hashes every tracked file before and after build/test. Any source mutation invalidates the evidence.
9. Council members review the bounded log and manifest and record whether compile errors are absent.
10. A fresh human approval re-hashes the files. Approval fails if the state differs from the successful build evidence.
11. LocalGPT creates a lossless ZIP of the exact approved files plus a manifest and marks the revision ready for human testing.

The source checkout is never overwritten by the final approval operation. Tests can be mandatory per approval request; when required, “not run” is not accepted as success.

## DXFunctions and controller boundary

The Council can read project metadata, request workspace registration, scan files, save file regex metadata, request a build verification, submit a Council evidence review, and request final ready-for-test approval. Every file read, metadata write, compiler execution, and final approval operation has a current human-approval boundary. Controller routes expose the same operations for the LocalGPT UI and trusted integrations.

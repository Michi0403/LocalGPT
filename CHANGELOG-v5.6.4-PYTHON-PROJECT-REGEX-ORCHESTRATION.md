# LocalGPT 5.6.4 — Python, project and regex orchestration

This version is based directly on the full LocalGPT 5.6.3 source repository. PublisherStudio was not changed.

## Executing Python safely

- The single embedded Python.NET interpreter and its single-reader GIL job queue remain the authoritative embedded worker. No concurrent GIL access was added.
- A singleton Python task supervisor lists bounded live/recent job status (up to 256 in memory), supports cancellation and delegates embedded work unchanged. Approved Python runtime-plugin scripts execute in separate CPython processes, through validated compiler profiles, with per-device admission lanes and an operator-configured maximum of parallel subprocesses. Process logs omit scripts, prompts and media.
- Redirected stdout and stderr are consumed concurrently before writing the subprocess input to avoid a pipe deadlock. Executions have cancellation and time limits, and terminate child process trees on failure where supported.
- The configured process capacity is read through the persisted Local AI toolchain policy at process admission; the embedded GIL queue capacity still requires a restart to change.
- `/install`, `api/local-ai/runtime/jobs` and approved DXAiFunctions expose process status and cancellation without creating a second executor.
- Existing Diffusers image/video pipelines now support explicitly selected native-device placement, model CPU offload, sequential CPU offload, or balanced component placement only where supported within a compatible multi-device backend. Audio/Whisper continues to use the existing Python.NET runtime. No AMD+NVIDIA unified VRAM pool is asserted.

## Project, file, regex, knowledge, compiler versions

- Project IDs, revision IDs and tracked project files are the source of truth for script authoring. A new `IProjectSourceNavigationService` composes existing project, project-revision, tracked-file, regex-curator, bounded regex compiler, safe-text and plugin services — no direct DbContext access.
- AI and UI can find related files by approved curated regex matches against file metadata and bounded verified readable text, with explicit project/revision IDs, source hashes, completed learning status, curator names, review provenance and independently counted approvals.
- Compiler evidence is attached only where project workspace metadata explicitly associates a validated toolchain. It is not automatically treated as target-framework compatibility; upgrades require separately approved revisions.
- With explicit user approval and a current completed source-hash review, one tracked C#/JavaScript/Python project file can be published as a nonexecuted runtime-plugin snapshot; a project artifact records its source file, hash and revision. Build/load and execution remain separate approved operations.
- New HTTP controller and DI DXAiFunctions `project.source.navigate` (read) and `project.source.publish-plugin` (approval required) use the same project-owned service as the DevExpress `/install` workflow.
- AI cannot bypass that project/source ownership using `runtime.plugins.save` for source scripts. Existing manually maintained plugin definitions and UI actions remain functional.

## Editor and Council

- Preserved the existing DevExpress HTML editor, with its live markup ownership and explicit conversion back to source text on save/build. No fallback to a simplistic native or memo editor.
- Completed Council participant answer/thinking/provider evidence opens an independent DevExpress popup outside the DxAIChat message-template cache; this avoids a post-run expandable-template state desynchronization.

## Validation and limitations

- Source-only maintenance guards and Python AST validation were executed; no .NET compilation, Visual Studio Razor generation, Godot runtime or Python hardware/GPU execution was performed. These still require Windows/device acceptance tests.
- Regex matching is bounded and source-backed; PDF/audio/video binaries are cataloged through their project file metadata, not read as regex text.
- Recent Python job history is in memory (not database durable across application restarts); jobs can be refreshed in the frontend. Parallel media jobs still run through the single embedded Python.NET interpreter; separate-process parallelism currently applies to approved Python plugins.
- Balanced Diffusers placement requires multiple compatible devices exposed by the same Python environment and explicit support by the selected pipeline. It does not merge heterogeneous AMD and NVIDIA device memory.

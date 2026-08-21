# LocalGPT 3.2.5 — Repository Maintenance, DX Workbench & GitHub Refresh

## Project self-maintenance

- Learning Round repository ingestion now treats source identity as the project boundary: LocalGPT source maintains `LocalGPT Core`, PublisherStudio/BlazorPublisher source maintains `PublisherStudio`, and another identifiable repository maintains its own project tied to the chat upload workspace and repository root.
- The complete source-backed project state remains in the existing database model: version, revision, SDK/target frameworks, workspace root, full tracked-file inventory, relative/absolute paths, hashes, sizes and roles.
- A Learning Round no longer creates a generic per-run `council-run` project when its own repository-aware maintenance path is responsible for source projects.
- LocalGPT Core release maintenance is source-driven. The running repository version comes from `src/LocalGPT/LocalGPT.csproj`, historical releases are discovered from maintained changelogs, and exactly one matching version/revision is promoted current. This repairs the stale 0.1.7/2.3.x current-marker drift without deleting history.
- A canonical PublisherStudio project is seeded independently so LocalGPT can maintain PublisherStudio/BlazorPublisher knowledge when that source is supplied.

## Canonical repository knowledge

- Canonical public repositories supplied by the user are recorded as source knowledge:
  - `https://github.com/Michi0403/LocalGPT`
  - `https://github.com/Michi0403/BlazorPublisher`
- `localgpt.repository.knowledge.refresh` performs a bounded, read-only remote source import and then feeds the resulting repository snapshot through the same project-maintenance path. It never writes to GitHub.
- Two enabled manual example pipelines are seeded: `repository-refresh.localgpt` and `repository-refresh.publisherstudio`. They run only when explicitly invoked by the user/policy.
- Learning Council instructions tell members these canonical repositories may be inspected for current read-only facts and distinguish inspection from persisted refresh.

## DX Functions and Remote Control UX

- `/dx-functions` now follows the established configuration-workbench navigation pattern with Catalog, User AI Functions and X Functions sections.
- InteractiveServer attachment/loading state is explicit; creation/policy controls stay disabled while the 1,500+ entry catalog is attaching or refreshing instead of accepting misleading clicks.
- `UserDxFunctionEditor` initializes only when its input identity/mode changes, preventing parent-catalog rerenders from resetting an open editor.
- `/remote-control` now uses the same tabbed/workbench navigation architecture as `/install`: Connectors, Action pipelines, Execution history and Template language are separate responsive sections rather than two permanently stretched columns.
- Responsive form/list sizing prevents the Remote Control editor from overflowing or becoming excessively wide on large or narrow displays.

## Preserved boundaries

- Existing InteractiveServer render-mode directives are retained.
- Existing advanced Remote Control pipelines, JSON/OData user functions, X-Round automation, chat behavior, and 1-Wire protocol remain intact.
- No EF migration was added.
- Application version is 3.2.5; LocalGPT, InstallerConsole and WebView wrapper remain aligned.

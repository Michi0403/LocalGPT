# LocalGPT 2.3.11 source validation

Static/source-side validation performed without claiming a .NET compile in this environment:

- MSBuild XML parses after adding the post-documentation Pages snapshot target.
- The snapshot target is limited to non-design-time Windows Debug/Release LocalGPT builds and can be disabled with `SeedLocalGptGitHubPagesSnapshotOnBuild=false`.
- Snapshot creation receives the exact current build output root instead of probing a stale configuration first.
- Manual snapshot selection requires a version match in `documentation-status.json`.
- The tracked 2.3.7 Kawaii snapshot still validates as a complete artifact (884 HTML pages / 855 API pages); the first real 2.3.11 build is expected to replace it automatically.

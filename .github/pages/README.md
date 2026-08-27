# LocalGPT GitHub Pages payload

`localgpt-kawaii-docs.zip` is the single tracked **HTML** publishing snapshot for GitHub Pages. The complete versioned PDF remains a release artifact and is deliberately excluded from the tracked Pages ZIP so multi-gigabyte handbooks do not exceed repository/Pages artifact budgets.

The authored `docs/` tree and generated `docs/_site/` output are not branch-deployment mirrors. GitHub Actions validates and extracts this ZIP, adds `.nojekyll`, and deploys the resulting static artifact directly.

A successful Windows Debug or Release build now validates the documentation produced by that exact build and refreshes this ZIP automatically. The MSBuild target passes the current build output explicitly, so stale documentation from another configuration is never selected accidentally.

For diagnostics or an explicit refresh, `Update-GitHubPagesSnapshot.cmd` remains available. Automatic seeding can be disabled for a special build with `-p:SeedLocalGptGitHubPagesSnapshotOnBuild=false`.

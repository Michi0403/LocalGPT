# GitHub Pages publishing correction R4

The previous workflow tried to publish
`LocalGPTWebviewWrapper/LocalGPT/wwwroot/help-docs` directly. That directory is
listed in `.gitignore`, so it exists after a local documentation build but does not
exist in a clean GitHub Actions checkout.

R4 publishes `.github/pages/localgpt-kawaii-docs.zip`, a tracked snapshot of the
same generated site. The workflow performs path-safety checks, validates the Kawaii
theme, persistent theme selector, API pages and cat-paw favicon, then uploads the
extracted tree as the Pages artifact.

The deployment job references the `github-pages` environment. If that environment
was deleted, GitHub creates it again when the default-branch deployment runs. The
workflow no longer deploys from release tags, so the tag protection that rejected
`v2.3.2` is not involved.

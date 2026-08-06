# LocalGPT GitHub Pages payload

`localgpt-kawaii-docs.zip` is the single tracked publishing snapshot for GitHub Pages.

The authored `docs/` tree and generated `docs/_site/` output are not branch-deployment mirrors. GitHub Actions validates and extracts this ZIP, adds `.nojekyll`, and deploys the resulting static artifact directly.

When the generated Kawaii documentation changes, replace this ZIP with the complete contents of `src/LocalGPT/wwwroot/help-docs/` and commit the changed archive.

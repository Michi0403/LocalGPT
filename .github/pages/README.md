# LocalGPT GitHub Pages payload

`localgpt-kawaii-docs.zip` is the tracked publishing snapshot for GitHub Pages.

The generated directories `docs/_site/` and
`LocalGPTWebviewWrapper/LocalGPT/wwwroot/help-docs/` are deliberately ignored by
Git, so a clean GitHub Actions checkout cannot publish either directory directly.
The workflow validates and extracts this ZIP instead.

When the generated Kawaii documentation changes, replace this ZIP with the contents
of the app's `wwwroot/help-docs` directory and commit the changed archive.

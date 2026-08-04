# LocalGPT 2.2.5

## GitHub Pages Kawaii deployment reliability

- Publishes the exact generated `wwwroot/help-docs` tree shipped in the selected release ZIP instead of serving the repository's Markdown source folder.
- Runs automatically when a release is published and when the Pages workflow or extraction helper changes on the default branch.
- Allows manual republishing with an optional release tag; an empty tag selects the latest release.
- Rejects documentation candidates that do not contain the active Kawaii CSS, JavaScript, HTML activation markers, light/dark theme rules, paw cursor and click-scratch behavior.
- Selects the newest matching themed candidate when a release contains several ZIP packages.
- Writes SHA-256 hashes and source-archive details into `github-pages-deployment.json` for live-site diagnosis.
- Blocks deployment with a precise repository-setting instruction when Pages still uses a legacy `/docs` branch source, so source Markdown is never mistaken for the generated site.
- Updates the Pages artifact action while retaining `.nojekyll`, the complete API tree and the shipped PDF.

No frontend feature or documentation animation was removed. The installed application documentation remains the source of truth for the public site.

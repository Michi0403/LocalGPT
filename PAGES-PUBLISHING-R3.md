# GitHub Pages publishing correction (R3)

## What was wrong

The previous workflow always downloaded ZIP assets from the selected GitHub release. The reworked documentation could therefore work perfectly inside the app while GitHub Pages continued to publish the older documentation embedded in the already-created release packages.

## What this package changes

- Publishes the exact checked-in app help tree from `src/LocalGPT/wwwroot/help-docs`.
- Triggers on changes to that help tree, the workflow, or its validator.
- Removes release/tag deployment triggers and the optional release-tag input.
- Separates artifact creation from deployment.
- Keeps the standard `github-pages` environment only on the deploy job.
- Validates the Kawaii theme, persistent selector, API output, cat-paw favicon, and required files before upload.
- Adds `.nojekyll` to the Pages artifact.
- Adds content hashes to favicon URLs so the old DocFX `D` is not kept by browser favicon caches.

## First deployment after deleting the environment

1. Commit this package to the repository's default branch.
2. Keep **Settings → Pages → Source** set to **GitHub Actions**.
3. The push starts **Publish LocalGPT Kawaii documentation**.
4. The workflow reference recreates the missing `github-pages` environment automatically.
5. If the browser tab still temporarily shows the old icon, close the tab and open the site again; the versioned favicon URL normally makes a hard refresh unnecessary.

No release ZIP upload is required for this Pages update. Release packages remain relevant for distributing the application, but they are no longer the source of the public documentation deployment.

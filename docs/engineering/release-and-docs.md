# Release and documentation pipeline

## Release discipline

Releases use a clean, reviewable lane. The version must be greater than prior public releases, repository state must be understood, and required build/validation steps must finish before assets are published.

The release package matrix includes desktop/WebView and backend packages for supported Windows, Linux, macOS, and architecture targets. Setup packages remain separate from portable packages.

## Documentation build

The build restores the repository-local DocFX tool, extracts metadata from `LocalGPT.dll` and `LocalGPT.xml`, builds conceptual and API pages, installs the Kawaii assets, produces the versioned PDF, writes `documentation-status.json`, and copies the complete tree into `wwwroot/help-docs`.

The build script injects:

- the `localgpt-kawaii-docs` HTML class;
- cache-busted Kawaii CSS/JS links;
- an early theme bootstrap;
- paw/cat favicon and brand assets.

## GitHub Pages

The repository keeps one pinned `.github/pages/localgpt-kawaii-docs.zip` snapshot. A successful Windows Debug or Release build now validates the exact `wwwroot/help-docs` tree produced by that build and refreshes the pinned ZIP automatically. The snapshot validator requires Python 3 and checks version agreement, theme markers, hashes, `index.html`, `api/index.html`, status metadata, relative links, accessibility, page counts, and the tagged PDF.

The GitHub Pages workflow does not rebuild LocalGPT. It validates and extracts the committed, version-matched ZIP, verifies the root and API index again, and deploys that no-Jekyll static artifact. `Update-GitHubPagesSnapshot.cmd` remains available for an explicit refresh and selects only generated documentation whose `documentation-status.json` matches the current project version.

## Node warnings

GitHub's official Pages and artifact actions may emit Node or dependency deprecation warnings. These warnings do not mean the static site runs Node. The deployed site is HTML, CSS, browser JavaScript, and assets. The workflow result and Pages deployment status determine success.

## PDF

The preferred PDF route assembles the generated HTML pages and prints them with an installed Edge/Chrome/Chromium browser. A secondary DocFX PDF route may require Node. Both routes must produce a real, sufficiently sized document; a one-page fallback shell is rejected.

## Stop conditions

Stop and report clearly when:

- the working tree or version is unsuitable;
- required licensed dependencies are unavailable;
- compilation or validation fails;
- the API graph is empty;
- the PDF is incomplete;
- release assets do not contain the complete docs tree;
- publishing credentials/permissions are unavailable.

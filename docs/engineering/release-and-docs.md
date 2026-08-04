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

The Pages workflow resolves a release tag, downloads release ZIPs, inspects each ZIP safely, and scores complete documentation candidates. It validates theme markers, hashes, index/API pages, status metadata, relative assets, and page counts before uploading one Pages artifact.

Windows ZIP backslashes and UTF-8 BOMs are accepted. Setup archives that do not contain the full site are ignored.

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

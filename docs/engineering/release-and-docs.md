# Release and documentation pipeline

## Release discipline

Releases use a clean, reviewable lane. The version must be greater than prior public releases, repository state must be understood, and required build/validation steps must finish before assets are published.

The release package matrix is host-aware by default. **macOS is the broad coordinator:** `-Runtime all` builds macOS x64/ARM64, Linux x64/ARM64, and Windows x64/x86/ARM64 application/setup payloads in one run. LocalGPT's optional WinUI/WebView wrapper is the deliberate exception because its native Windows toolchain remains Windows-host-only; macOS warns and skips only that wrapper. Windows always builds the maintained Windows outputs and, when a release-ready WSL distro is installed, the default `-WslLinux Auto` mode additionally delegates Linux x64/ARM64 Full/Light builds to WSL. Missing/unready WSL remains non-fatal. Native Linux remains a first-class Linux lane.

WSL provisioning is explicit through `Setup-WslLinuxBuild.ps1 -Provision` or the opt-in `-ProvisionWslBuildTools` switch. The WSL bridge mirrors source to the Linux filesystem, reuses parent-prepared documentation, and normally terminates only a distro it had to start. Linux TAR.GZ/DEB are mandatory. RPM is created natively on Linux and on macOS when `rpmbuild` is installed (Homebrew `rpm` is supported; `-ProvisionNativePackagingTools` is the explicit install opt-in). AppImage remains Linux/WSL-native unless the explicit container fallback is requested. macOS Full/Light outputs include TAR.GZ, DMG, and PKG when the built-in `pkgbuild` is available. `Build-Release.ps1 -Runtime all-rids` remains the explicit all-RID request on any host.

## Documentation build

The build restores the repository-local DocFX tool, extracts metadata from `LocalGPT.dll` and `LocalGPT.xml`, builds conceptual and API pages, installs the Kawaii assets, produces the versioned PDF, writes `documentation-status.json`, and copies the complete tree into `wwwroot/help-docs`.

The build script injects:

- the `localgpt-kawaii-docs` HTML class;
- cache-busted Kawaii CSS/JS links;
- an early theme bootstrap;
- paw/cat favicon and brand assets.

## GitHub Pages

The repository keeps one pinned `.github/pages/localgpt-kawaii-docs.zip` snapshot. A successful Windows Debug or Release build now validates the exact `wwwroot/help-docs` tree produced by that build and refreshes the pinned ZIP automatically. The snapshot validator requires Python 3 and checks version agreement, theme markers, hashes, `index.html`, `api/index.html`, status metadata, relative links, accessibility, page counts, and the release PDF before producing an HTML-only tracked Pages snapshot. The release PDF is not duplicated into the Pages ZIP; Pages links to the latest release instead.

The GitHub Pages workflow does not rebuild LocalGPT. It validates and extracts the committed, version-matched ZIP, verifies the root and API index again, and deploys that no-Jekyll static artifact. `Update-GitHubPagesSnapshot.cmd` remains available for an explicit refresh and selects only generated documentation whose `documentation-status.json` matches the current project version.

## Node warnings

GitHub's official Pages and artifact actions may emit Node or dependency deprecation warnings. These warnings do not mean the static site runs Node. The deployed site is HTML, CSS, browser JavaScript, and assets. The workflow result and Pages deployment status determine success.

## PDF

The preferred PDF route prints generated HTML with an installed Edge/Chrome/Chromium-family browser in small durable 8-12 page parts and merges them with the repository-owned `LocalGPT.ReleasePackaging` build helper. Windows discovery covers normal machine/user installs and App Paths; macOS application-bundle discovery remains supported. A secondary DocFX PDF route may require Node, but chunked/low-memory builds refuse an unsafe monolithic fallback unless the existing explicit operator override is set. Every accepted route must produce a real, sufficiently sized document; a one-page fallback shell is rejected.

## Stop conditions

Stop and report clearly when:

- the working tree or version is unsuitable;
- required licensed dependencies are unavailable;
- compilation or validation fails;
- the API graph is empty;
- the PDF is incomplete;
- release assets do not contain the complete docs tree;
- publishing credentials/permissions are unavailable.

### Reusing a previously printed handbook

A rebuild with the **same LocalGPT version** first prefers the validated content-addressed DocFX/PDF cache. This exact cache uses hashes of the compiled assembly, XML comments, documentation sources, and documentation script; rebuilding the assembly can therefore change the key even when the handbook version is unchanged.

If the exact cache misses, `Build-Documentation.ps1` can recover the last **published PDF for that same version** from `src/LocalGPT/wwwroot/help-docs` or the supplied output web root **before replacing the published HTML**. Recovery requires a matching `documentation-status.json` declaring that exact version/PDF name, a successful DocFX publication, and the same current count of printable HTML source pages. The PDF must pass both complete-file checks and `LocalGPT.ReleasePackaging pdf-verify`. Unverified, stale-version, or different-page-count artifacts are not reused. The existing HTML can still be regenerated when its source-input key changes; reusing the PDF does not mean the HTML is reused.

The **version is the publication identity** for this secondary recovery route. If documentation text or API semantics changed without a version change *but the printable page count remained identical*, the recovered PDF might be older than the new HTML. Therefore increment the project version when changing published content, or explicitly set `FUTURE2_DOCUMENTATION_FORCE_PDF_REBUILD=1` for a fresh PDF in the same version. This override bypasses the complete-PDF cache, same-version recovery, and existing browser chunks; it does not change the repository-local storage architecture. An initial build of a **new version** still has to print a new PDF once. Interrupted runs continue to reuse valid chunks normally when this override is not set.

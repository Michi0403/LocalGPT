# LocalGPT Release Process

This file documents the release process for maintainers. It is not permission for an automated tool to publish, push, tag, or modify a local machine.
Do not publish a LocalGPT release by improvising around it.

## Non-Negotiable Rules

- Work from `main` for releases unless the user explicitly asks otherwise.
- The working tree must be clean before the release publish command starts.
- Every public release must use a semantic prefix greater than every existing
  public release tag, for example `0.3.0-alpha.20260603` after `0.2.0-*`.
- Never reuse a tag. Never publish a lower semantic version and call it latest.
- Run the physical source-format guard before packaging:

  ```powershell
  .\build\Assert-SourceFormatting.ps1
  ```

- If the source-format guard fails, stop. Do not publish anyway.
- Windows wrapper releases must include the Blazor and DevExpress static assets
  inside the actual `.msix`, not just in loose build folders.
- A release with unstyled HTML, missing DevExpress CSS, missing scoped CSS, or a
  broken WebView2 frontend is invalid.
- Commit documentation, script, or code changes before creating the GitHub
  release. The release tag must point at the committed state being shipped.

## Required Version Shape

Use:

```text
major.minor.patch-topic.yyyymmdd
```

Examples:

```text
0.3.0-alpha.20260603
0.3.1-release-fix.20260603
0.4.0-ai-workbench.20260604
```

The `major.minor.patch` prefix controls ordering. The topic and date make the
release readable, but they do not replace a higher semantic prefix.

## Required Publish Command

Use the repository script instead of hand-made zips:

```powershell
.\LocalGPTWebviewWrapper\build\Publish-LocalGptRelease.ps1 `
  -Version "0.3.0-alpha.20260603" `
  -Configuration Release `
  -Platforms x64,x86,arm64 `
  -BackendRuntimeIdentifiers win-x64,linux-x64,osx-x64,osx-arm64 `
  -CreateGitHubRelease
```

The script must:

- run `build\Assert-SourceFormatting.ps1`
- refuse a release version that is not higher than existing GitHub releases
- stamp the MSIX package identity version temporarily
- build Windows WebView2/MSIX wrapper packages
- verify required Blazor/DevExpress assets inside the generated MSIX
- publish backend-only zips for Windows, Linux, and macOS runtime identifiers
- write `release-manifest.txt` with SHA256 hashes
- write `release-notes.md`
- create a GitHub release with the new tag and mark it latest
- restore the checked-in package manifest version afterward

## Required Asset Contract

Every public GitHub release must include this full payload unless the user
explicitly asks for a partial diagnostic release:

```text
LocalGPT-WebView2-<version>-windows-x64.zip
LocalGPT-WebView2-<version>-windows-x86.zip
LocalGPT-WebView2-<version>-windows-arm64.zip
LocalGPT-Backend-<version>-win-x64.zip
LocalGPT-Backend-<version>-linux-x64.zip
LocalGPT-Backend-<version>-osx-x64.zip
LocalGPT-Backend-<version>-osx-arm64.zip
release-manifest.txt
release-notes.md
```

This is the known-good payload shape used by
`v0.1.5-alpha.20260603` and later full releases. A release with only
`windows-x64` and `win-x64` assets is a partial diagnostic release, not the
normal public download set.

Every Windows WebView2 release zip must contain an `.msix` that includes:

```text
LocalGPTWebviewWrapper/wwwroot/_framework/blazor.web.js
LocalGPTWebviewWrapper/wwwroot/_content/DevExpress.Blazor/dx-blazor.svg
LocalGPTWebviewWrapper/wwwroot/_content/DevExpress.Blazor.Themes/office-white.bs5.min.css
LocalGPTWebviewWrapper/wwwroot/LocalGPT.styles.css
```

If any of these are missing, fix packaging before publishing.

## After Publishing

After the GitHub release is created:

1. Confirm the new release appears above older releases.
2. Confirm it is marked latest when it is the intended public download.
3. Confirm `release-manifest.txt` and `release-notes.md` are attached.
4. Push `main`.
5. Do not delete or overwrite old releases unless the user explicitly asks.

## Stop Conditions

Stop and report honestly if:

- source formatting fails
- build or publish fails
- `gh release list` cannot prove the new version is higher
- generated packages lack required static assets
- GitHub release creation fails
- the working tree becomes dirty unexpectedly during release

Do not continue with a partial or lower-version release.

## Git Safety

Git is a revision ledger for source history, not a license to destroy local
work. Agents must not run destructive cleanup against uncommitted changes.

Forbidden unless the user explicitly asks for that exact destructive action:

- `git reset --hard`
- `git checkout -- <path>`
- `git restore <path>`
- `git clean`
- `git revert` when it is used to remove unreviewed work instead of creating a
  deliberate reviewed reverse commit
- deleting, overwriting, or regenerating files to erase another worker's
  uncommitted changes

If the tree is dirty, inspect and explain it. Commit, stash, copy, or discard
only when the user explicitly approves the chosen handling. Never silently lose
features, fixes, generated knowledge, or release work.

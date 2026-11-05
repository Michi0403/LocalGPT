# LocalGPT 5.6.1 — PDF release-helper version and capability handshake

Source release based on the user-supplied **5.6.0** repository, without reverting that release's ASCII console, DevExpress controls, Game Studio, Data Management or setup-memory features. Version progression: **5.6.0 → 5.6.1**.

## Root cause confirmed by the macOS publish log

The documentation process had **195 durable PDF chunks** available and tried to merge them with `pdf-merge --input-list <manifest>`. The source implementation supported that compact UTF-8 input manifest, but `Build-Release.ps1` still requested `LocalGPT.ReleasePackaging 1.0.2`. The repository-owned package preparation reused the old existing `.nupkg`, so the actual installed helper understood only repeated `--input` flags. It failed with `At least one --input PDF is required.` **before PDF compression could run**. This was not evidence of exhausted disk space, a file-path change or a hard PDF-size limit.

## Changes

1. **Unified packaging-helper version:** source project, release entrypoint, ensure/install and publish defaults now agree on `1.0.4`. The new version avoids silently recycling the stale `1.0.2` package.
2. **Early compatibility check:** the installed tool advertises `pdf-merge-input-list` through a read-only `capabilities` command. Release installation checks it before running documentation; direct/normal-build documentation also checks it before DocFX starts when a PDF-required packaging tool is supplied. Incompatible installations fail immediately with an actionable message.
3. **Version drift detection:** the package ensure and publish scripts compare the requested package version to the `.csproj` before reusing a cache or overriding the pack version.
4. **Preserved PDF flow:** all durable chunks remain reusable; the merge continues to use its short UTF-8 manifest to avoid large Windows command lines. The existing 128 MiB optimization trigger, 256 MiB size ceiling, Ghostscript/qpdf fallback policy and oversized-output refusal remain intact. No multi-gigabyte, uncompressed fallback has been introduced.
5. **Source validation and release identity:** application version, browser resource cache tokens and exact-version acceptance scripts advance to 5.6.1; a standalone source-only packaging regression audit and per-file release checklist are included.

## PublisherStudio impact review

PublisherStudio 4.1.8 currently assembles its PDF using repeated `--input` arguments, a syntax supported by its pinned `LocalGPT.ReleasePackaging 1.0.2`. It does **not** call the new manifest argument. No PublisherStudio source or package was changed for this defect.

## Verification limits

Source-only Python audits and archive structural checks can be performed without .NET or GitHub. Building/running the actual .NET 10 tool, generating the compressed final PDF and testing full publish on macOS/Windows/Linux require the maintainer's .NET environment. See `RELEASE-CHECKLIST-v5.6.1.md`.

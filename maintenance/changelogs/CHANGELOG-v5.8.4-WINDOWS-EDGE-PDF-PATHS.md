# LocalGPT 5.8.4 — Windows Edge documentation PDF path repair

## Failure reproduced from the supplied Windows build log

- LocalGPT 5.8.3, all supporting projects and DocFX HTML built successfully. The HTML accessibility check passed for 1,603 HTML files.
- Microsoft Edge timed out twice while writing the first `front-matter.pdf`; its diagnostics reported Windows error `0x3` (path not found) and cache-directory creation failures.
- The browser output path recorded in the log was 269 characters, and one of Edge's internal cache paths was 253 characters **before** additional files. These native browser paths were longer than or dangerously close to classic Windows path limits.
- This is not a PDFsharp merge failure, nor evidence of a macOS PowerShell incompatibility. PowerShell remained in charge of the documentation build.

## Changes

- Shorten the durable, hashed PDF cache child directory from `browser-pdf-chunks/portrait-body-v2-pages-<n>-total-<n>` to `p/v2-p<n>-n<n>`, reducing the failing `front-matter.pdf` output example from 269 to 228 characters.
- Shorten isolated Chromium profiles and volatile cache names under the **existing repository-owned documentation cache**: `browser-profiles/browser-profile-<guid>/cache` to `bp/p<guid>/c`. The logged internal cache example shortens from 253 to 220 characters.
- Check on Windows that the PDF output's parent directory exists and that Edge output/profile paths leave headroom below the conventional Win32 path-length limit. If not, fail immediately with actionable guidance to shorten the build root or set `FUTURE2_BUILD_STORAGE_ROOT` / `FUTURE2_DOCUMENTATION_CACHE_ROOT` instead of waiting for repeated 8-minute renderer timeouts.
- Update the existing PowerShell architecture validator to retain the repository-local cache ownership guarantee and assert the short PDF paths remain wired in.
- Preserve HTML cache paths, PowerShell build entrypoints, DocFX, separate landscape front matter/portrait body, durable PDF chunk checks, managed PDF merge and validation, and optional compression. No Python dependency is added.

## Version and scope

- LocalGPT: **5.8.3 → 5.8.4**.
- LocalGPT.ReleasePackaging and the vendored MIT PDFsharp source: unchanged.
- PublisherStudio: unchanged.

Actual Windows Edge printing and macOS build behavior have not been run here; the ZIP is statically validated only.

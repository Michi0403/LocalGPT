# LocalGPT 2.3.11 — version-matched Pages snapshot auto-seed

- Debug and Release builds now validate and regenerate `.github/pages/localgpt-kawaii-docs.zip` directly from the documentation tree produced by that exact build.
- The automatic build target passes the current `TargetDir` documentation root explicitly, so an older Release output can no longer override a fresh Debug build (or the reverse).
- `Update-GitHubPagesSnapshot.cmd` now selects only a generated Debug/Release documentation tree whose `documentation-status.json` version matches the current `LocalGPT.csproj` version; stale outputs are reported instead of being packaged.
- Interrupted documentation builds no longer leave the tiny DocFX PDF link-validation placeholder behind in `docs/`; only the known marker-bearing placeholder is removed, never a real authored PDF.
- The previous Human Collaboration render-mode fix and LocalPathExplorer text-service ownership fix remain unchanged.

The checked-in snapshot inside a clean source archive may still describe the last owner-generated documentation until the first successful 2.3.11 Debug/Release build. That build now refreshes the tracked snapshot automatically and is the intended source of truth before commit/deployment.

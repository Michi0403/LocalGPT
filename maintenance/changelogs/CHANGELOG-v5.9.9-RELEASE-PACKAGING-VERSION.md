# LocalGPT 5.9.9 — Release packaging helper version ownership

## Fixed: macOS release version mismatch

`Build-Release.ps1` still requested `LocalGPT.ReleasePackaging` package `1.0.7`, even though the helper project (`src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj`) and its publishing/installation scripts already declared `1.1.0`. This caused `build/Ensure-ReleasePackagingPackage.ps1` to abort before package construction with a version contract mismatch.

The release coordinator now reads the helper's declared `<Version>` from the helper project and uses that **one source of truth** consistently for its repository-local package filename, `Ensure-ReleasePackagingPackage.ps1` invocation, validated package, copy and release-bundle inventory. Missing project/version is an explicit error. The existing package version/notice/capability checks remain untouched.

The helper source and project version remain **1.1.0**; only LocalGPT's application version increments to **5.9.9**. The 1-Wire protocol version remains **2.1.1**. There is no runtime or PublisherStudio change.

## Previously validated steps in the supplied macOS log

The user's 5.9.8 release reached and passed notices, PowerShell compatibility, documentation/SQL/DevExpress/Node preflights, cross-platform boundary audit, JavaScript inventory, Apple signing/notarization credential preflight, and RID-neutral wire-protocol package preparation. The next step blocked on this helper version mismatch. The unrelated .NET workload advertising update is not required by this fix.

## Usage

Extract the complete 5.9.9 source or copy the corrected `Build-Release.ps1` into the existing 5.9.8 checkout to retry the current version without requiring a new PDF. The helper version is now resolved dynamically. Re-run `pwsh ./Build-Release.ps1` on macOS to verify the complete publish; no publish was executed offline.

# Release process

1. Start from a clean clone at the intended release commit and confirm there is exactly one `.git` directory.
2. Confirm runtime databases, WAL/SHM files, logs, `.vs`, `.cr`, `bin`, `obj`, generated repository snapshots, user publish files, credentials, and generated license files are untracked. When applying the supplemental source patch to an old clone, run `tools/Remove-TrackedRuntimeArtifacts.ps1 -WhatIf` first, review the exact four paths, then rerun without `-WhatIf` and stage only the approved deletions.
3. Use the required .NET 10 SDK, configured DevExpress 25.2 NuGet feed, valid DevExpress license, and required Windows/WebView2 workloads on the licensed build machine.
4. Run `.\build\Assert-SourceFormatting.ps1`. It must reject any `using static System.Net.WebRequestMethods;` import before compilation.
5. Restore and build `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.sln` in Debug and Release.
6. Manually start the loopback host and verify the migration/initial-feed completion log appears once. AI coding tools must not start or probe localhost services.
7. Verify prompt/variable edits survive restart and maintainer-created knowledge is not overwritten by seed refreshes.
8. Test normal chat, streaming chat, provider switching, council mode, knowledge views, diagnostics, and desktop-wrapper startup.
9. Test one plain stream, one `<think>` stream, one Harmony stream, markers split across chunks, cancellation, timeout, and two concurrent single-model streams. Confirm thinking text updates continuously, remains expanded while active, and collapses after final output starts.
10. Test a multi-member AI Council stream. Confirm member panels update without the old two-second bursts, do not cross/interleave their HTML containers, lose the live indicator after completion, and end with the consensus output.
11. Keep `NativeCommands:Enabled=false` for the normal safety test. In an isolated test workspace only, the human maintainer may explicitly enable it and verify allowlist, traversal rejection, timeout, process-tree cancellation, audit logging, secret redaction, and the separate PowerShell opt-in.
12. Verify local-provider auto-start does not execute an unrestricted shell command.
13. Keep `ArtifactBuilds:Enabled=false` for the normal safety test. In an isolated artifact directory only, the human maintainer may explicitly enable it and verify root/target/output rejection, timeout, cancellation, and process-tree termination.
14. Generate any required DevExtreme runtime-license script only on the licensed build machine and place it in release staging, never in Git. Follow `docs/DEVEXPRESS_ASSETS.md`.
15. Review `THIRD-PARTY-NOTICES.md`, `LICENSE.MD`, this release changelog, and `VALIDATION.md`.
16. Package only from a clean staging tree. Exclude `.git`, databases, logs, credentials, private feeds, generated license material, user files, and unlicensed assets.

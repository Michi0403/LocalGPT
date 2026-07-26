# Release process

1. Start from a clean clone at the intended release commit and confirm there is exactly one `.git` directory.
2. Confirm runtime databases, WAL/SHM files, logs, `.vs`, `.cr`, `bin`, `obj`, generated repository snapshots, user publish files, credentials, key/certificate files, private-feed configuration, generated license files, and unlicensed font assets are untracked.
3. Use the required .NET 10 SDK, configured DevExpress 25.2 NuGet feed, valid DevExpress license, and required Windows/WebView2 workloads on the licensed build machine.
4. Run the source and security guards:

   ```powershell
   ./build/Assert-ProtectedRepositoryFiles.ps1
   ./build/Assert-SourceFormatting.ps1
   ./build/Assert-SecurityPolicy.ps1
   ```

5. Restore and audit direct and transitive dependencies. Do not suppress or ignore advisories merely to obtain a green build:

   ```powershell
   dotnet restore ./LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.sln
   ./build/Audit-Dependencies.ps1
   dotnet package list ./LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.sln --include-transitive --vulnerable --format json
   ```

6. Build `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.sln` in Debug and Release. Review every compiler warning, especially nullability, async/cancellation, disposal/lifetime, EF migration, path handling, obsolete API, package-audit, streaming, and persistence warnings.
7. Manually start the loopback host and verify the migration/initial-feed completion log appears once. AI coding tools must not start or probe localhost services.
8. Verify only the reviewed repository-knowledge allowlist is seeded. Confirm model/council suggestions remain unapproved until a human reviews them and that prompt/variable edits survive restart.
9. Test normal chat, streaming chat, provider switching, knowledge views, diagnostics, and desktop-wrapper startup.
10. Test one plain stream, one `<think>` stream, one Harmony stream, and explicit/automatic DeepSeek, Gemma, and Apple/OpenELM/MLX profiles; include markers split across chunks, cancellation, timeout, and two concurrent single-model streams. Confirm thinking text updates continuously, remains expanded while active, and collapses after final output starts.
11. Test a multi-member AI Council stream. Confirm member panels update without polling bursts, do not cross/interleave their HTML containers, lose the live indicator after completion, and end with the consensus output.
12. Test Projects: create/edit a project, topic, and version; link a chat to the project and exact version; restart and confirm persistence; rate, comment on, clear, and reload assistant feedback; verify a recorded path is not accessed merely by loading/selecting the project; verify Git is only recommended; and verify save confirmations reset after success and failure.
13. Test council project context and linking. Confirm project selection supplies bounded context, artifact generation defaults off, URL parameters cannot enable it, project-topic linking requires its own fresh confirmation, and both confirmations are consumed after one run even on failure.
14. Keep `NativeCommands:Enabled=false` and `ArtifactBuilds:Enabled=false` for the normal safety test. In an isolated disposable workspace only, the human maintainer may explicitly enable each feature and verify allowlists, traversal rejection, target containment, timeout, cancellation, process-tree termination, audit logging, secret redaction, and the separate PowerShell opt-in.
15. Verify local-provider auto-start does not execute an unrestricted shell command and read-only diagnostics cannot launch builds or commands.
16. Test the installer in a disposable Windows VM: no arguments must show help; asset mismatch must fail; download/extraction failures must return nonzero; traversal/symlink archives must be rejected; unsafe delete targets must be rejected; and forced uninstall must preserve the learning base.
17. Generate any required DevExtreme runtime-license script only on the licensed build machine and place it in release staging, never in Git. Follow `docs/DEVEXPRESS_ASSETS.md`.
18. Review `THIRD-PARTY-NOTICES.md`, `LICENSE.MD`, the v0.1.1, v0.1.2, v0.1.3, and v0.1.4 changelogs plus `MISSING_FEATURE_REVIEW-v0.1.4.md`, `SECURITY.md`, and `VALIDATION.md`.
19. Package only from a clean staging tree. Exclude `.git`, `.vs`, `.cr`, `bin`, `obj`, databases, logs, credentials, private feeds, keys/certificates, generated license material, user files, and unlicensed font binaries.

## Current debug iteration

The active implementation ledger is `CHANGELOG-v0.1.4-ef-snapshot-runtime-debug.md`; unresolved architecture work is mirrored in `docs/OPEN_TASKS.md`. A source package must not describe an item as complete when it remains open there.

Migration-bearing candidates must pass `build/Assert-EfSnapshotArchitecture.ps1` and an owner startup against a disposable or backed-up SQLite database. A successful compile does not prove that the executable EF snapshot can construct its model.

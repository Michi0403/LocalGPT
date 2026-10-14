# LocalGPT 5.5.6 — Cross-platform validation at build time

## Fixed: a publish-only architectural error

- The supplied failure report ends at `Assert-CrossPlatformBoundaries.ps1`: `LearningRoundService.Checklist.cs` directly selects Windows versus Unix string-comparison rules while validating learning-project file paths. A successful compile did not run this existing audit, so it surfaced only at the later release preflight.
- `LearningRoundService` now receives the existing, DI-registered `IPlatformRuntimeService`. Its checklist uses `platform.PathsEqual` for ancestor traversal and `platform.IsSameOrDescendantPath` in addition to the existing path-root and relative traversal checks. Hash verification, symlink/junction rejection, blocking failed reads, and per-file learning progress remain intact.
- Auditing the newer 5.5.5 source also exposed a second direct host-OS branch in `GameGodotWorkspaceService.CheckedFullPath`. It now uses the same injected platform service, keeps the **descendant-only** restriction (rejecting the root itself), and retains symlink/junction checks, explicit user approval, export conflict protection and revisioned Godot editing.

## Earlier, mandatory cross-platform gate

- `Directory.Build.targets` now runs the *existing* `build/Assert-CrossPlatformBoundaries.ps1` for normal LocalGPT builds (`BeforeBuild`) and for Publish, regardless of target RID. Missing scripts and nonzero audit exits fail the build. Design-time IDE evaluation is excluded, as with the repository's other maintained build guards; there is **no optional bypass**.
- The underlying `build/audit_cross_platform_boundaries.py` still rejects host-OS branches outside platform/composition services. It now additionally checks its own MSBuild registration, scheduling, no-bypass condition and invocation. No exception-list entries were added.
- Release compilation, including `Build-Release.ps1 -CompileOnly`, inherits the new early check via the normal build targets. Release preflight continues to run it separately before package creation.

## Version and retained features

- Advanced LocalGPT **5.5.5 → 5.5.6** and the local script cache keys. All version slots remain single digit. PublisherStudio is unchanged.
- Retained Game Studio, Godot source revisions, AI curation, DevExpress editors, Council approvals, project/file learning checklist and Data Management without replacing or weakening their service architecture.
- Updated the retained 5.5.5 source-only acceptance scripts to expect version 5.5.6 while preserving their substantive checks and original script names.

## Verification and limits

- Source-only cross-platform audit: **passed**, including both corrected services and MSBuild wiring.
- Retained Game Studio, learning checklist and Data Management source contracts: **passed**.
- Additional source-only architecture, Razor maintenance, DevExpress controls, UI-state ownership, async continuation/boundaries, service resilience and configurable prompt/regex ownership audits: **passed**.
- Additional repository-maintenance audits and exact results are recorded in `RELEASE-CHECKLIST-v5.5.6.md`.
- **No .NET compilation, package publish, Godot runtime, or GitHub access was performed**. The owner should run the normal Visual Studio build first, then the release/publish path on the target build host. If a compiler or device-specific failure occurs, the diagnostic should be handled as a separate targeted defect.


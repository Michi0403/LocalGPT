# LocalGPT 7.6.8 — enable approved builds and restore live-join button

## Changed
- `appsettings.json` / `appsettings.Development.json`: `ArtifactBuilds.Enabled` and `NativeCommands.Enabled` now `true`.
  Every reviewed TwitchManager/StreamScheduler run since 16 Nov recorded `*.sln:Disabled` because the
  ArtifactBuildExecutor refused all builds by configuration. The executor still requires a fresh human
  confirmation for each exact build, and native commands remain behind their approval gate.
- `Chat.razor` (Running session tools): a "Join live Council <id>" button is shown whenever a live run exists
  that this tab is not attached to. Previously the only join button depended on the *selected* run still running,
  so a stopped run left selected hid the join action entirely.

- Foreign-workspace guard (P0-762-03, enforcement part): project-bound automatic calls
  - `localgpt.path.browse` returns `Denied` for paths inside the CouncilArtifacts root unless the path contains
    a review id owned by the bound project (the root itself is not owned). Text: runtime policy
    `CouncilForeignArtifactBrowseDenied` (new seed string).
  - `council.artifact_workspaces` lists only workspaces of the bound project's own reviews.
  Cause: in run cace3936 qwen3.5:2b correctly classified the Twitch request as greenfield, found the new
  project's workspace empty, then used `localgpt.path.browse` (broker path, not limited by the step's
  preference list) to open `CouncilArtifacts\TwitchManager` from 20 Nov and reuse the broken project.
  Manual/UI browsing and calls without a bound project are unchanged.

## Observed in the 21 Nov frontend test (open, not fixed here)
- Seed team `csharp-modern-host-development` disables organic functions in Planning, Implementation, Curation
  and Release and requires no completed functions, so it can only produce prose and never a draft/files.
  The General Council routed the Twitch one-liner to it.
- gpt-oss:20b led Preparation/Planning only because it is the first/`configured=True` member of the preset.
- Models (deepseek) emit Microsoft.AspNetCore.*/Microsoft.Extensions.* 6.0 PackageReferences in net10.0
  Web-SDK projects; DraftTargetValidation should reject framework-provided package pins.
- Build status `Disabled` is reported as review status `BuildFailed` in older rows (see P0-762-06).

- Still open: give a greenfield run with no ChatUpload workspace its own fresh run-owned working
  directory (owner request) instead of only the DB-backed source draft.

## Verification
- Not compiled in the editing environment; owner Debug build/run in Visual Studio required.

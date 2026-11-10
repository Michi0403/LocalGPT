# LocalGPT 7.0.5 — Council coding access, repository staging, 1-Wire file journal

Source release based on LocalGPT 7.0.4. Companion PublisherStudio source revision: 4.7.6.

## Changes

- Advanced application/project version from 7.0.4 to 7.0.5; advanced system Council team seed revision to 53 so the updated **Program Compiler & Repository Curator** seed can be applied through the existing seed/migration mechanism.
- Enabled the preseeded compiler-intake role's organic function access. Updated compiler team capabilities, automatic read-only tools, and workflow instructions to classify a request for the *next version of an uploaded repository* as existing-project maintenance, not greenfield. Added explicit guidance for exact file paths, staged revisions, version rollover, review-backed edits, receipts, PowerShell guards, and real source ZIP handoff.
- Added `council.repository.workspace.stage`: copies a selected, safely extracted uploaded ZIP root, registered project, or expressly approved absolute local source folder into a **new isolated, editable Council artifact workspace**. Requires fresh human approval, checks selected archive roots, rejects links/oversized snapshots, and leaves original folders untouched. It does not merge back to the user's repository or commit/publish changes. Existing artifact-workspace read/write/ZIP functions remain the editing and packaging interfaces and retain their own confirmation policies.
- Added `session.sandbox.run` and `session.sandbox.runs` DX AI functions for approved C#/Python/PowerShell Hyperlight guest **source-snippet** execution and subsequent status/stdout/stderr/exit-code receipts. Run is explicitly approval gated; receipt listing is read-only. Existing Hyperlight provider decides which languages are provisioned; there is no host-execution fallback. The guest does **not** mount the full repository filesystem, so these are snippet checks rather than a complete .NET project build.
- Updated Council compiler workflow instructions to request existing `localgpt.console.execute` separately for running the repository's own PowerShell guards on the host, with human approval, without falsely calling that a guest-VM run. Council file access and guest execution are kept separate by architecture and safety boundary.
- Added independently persisted JSON-lines `OneWire-traffic.jsonl` packet journal with enabled-by-default `OneWire:Traffic:FileLoggingEnabled`, customizable `FilePath`, and bounded file rotation `MaximumFileBytes` (default 64 MiB, one `.previous` file). Per-user default is `%LOCALAPPDATA%\LocalGPT\OneWire-traffic.jsonl` on Windows. Existing in-memory views and general ILogger lines are retained; a file I/O error only warns and does not interrupt a packet or the live view. Payload/raw-frame persistence follows existing traffic payload and frame options.
- Recognized expected `OperationCanceledException` during `Chat.LoadCouncilTeamsAsync` when its token is canceled or the page is disposed, logging at Debug rather than treating the teardown as an application failure. This addresses the cancellation noise seen in the supplied trace, **not** the DevExpress null reference.
- Added repository-owned PowerShell source-contract checks (`build/Assert-CouncilRepositorySandboxAndTrafficFiles.ps1`) to both repository and release validation pipelines.

## Verification and outstanding limitations

- No .NET SDK, compiler, PowerShell executable, Windows runtime, or Hyperlight runtime was available in the editing environment. **No .NET compilation, PowerShell guard execution, Hyperlight guest receipt, or end-to-end Council test is claimed.** Run the repository's existing PowerShell validation on the target machine before merging; source ZIP is **not** a compiler-verified release.
- Uploaded debug logs include `TaskCanceledException` during circuit disconnect and repeated `DevExpress.Blazor.Internal.DeviceInfo..ctor(GetDeviceInfoResult)` `NullReferenceException` from `DxComboBox.OnAfterRenderAsync`. These are **not declared fixed** by the Council/workspace/1-Wire changes; reproduce and verify separately on the intended DevExpress/Blazor runtime.
- Actual disk-path merging after staging remains a separately approved action; this source revision does **not** silently overwrite user repositories. Hyperlight guest checks and repository-wide PowerShell guards are deliberately not conflated.
- The default `Full` traffic payload mode and `IncludeRawFrame=true` can persist sensitive content; select `Redacted`/`None` and `IncludeRawFrame=false` where appropriate, and restrict filesystem access to the log directory.

## Suggested Windows checks

Run these from the LocalGPT source root (PowerShell):

```powershell
& ./build/Assert-CouncilRepositorySandboxAndTrafficFiles.ps1
& ./build/Invoke-RepositoryValidation.ps1
```

The second command uses the repository's normal dotnet restore/build pipeline and should only be run by the operator on the intended Windows/.NET machine.

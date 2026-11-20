# LocalGPT 7.6.0 — technical evidence and project-delivery trace

Source candidate, based on 7.5.9. No .NET builds, network requests or PowerShell execution were performed in the editing environment. PublisherStudio 4.9.1 is unchanged. No live database was changed, no historical draft was migrated, and no new EF database schema is required. JSON spool/checkpoint additions are optional for historical records. The current source-candidate version follows the owner's one-digit patch rollover: 7.5.9 -> 7.6.0.

## Audit evidence (7.5.8, supplied 20 November 2026)

Sources: supplied JSON database export `20261120DbasJsonLocalgpt.zip`, `RelevantLogAndAppFiles.zip` (GoalExecution, CouncilLogs and CouncilSpooler), earlier 7.5.8 transcript, and 7.5.9 source. The database export is a snapshot, not a live writable database. The project/version names and IDs below identify historical records only. The full exports, logs, sensitive configuration and generated Twitch source are not bundled in this repository.

### Separate runs; no filesystem materialization

| Record | 20 Nov failed planning | 19 Nov unfinished draft |
|---|---|---|
| Council run | `a8ddfcff-4c5b-4bbe-b398-33b18f357f6d` | `91a00673-f153-409a-b1bb-f39abc6dacc3` |
| `LocalGptProjects.Id` | `542fb6aa-ebb0-4615-b29c-099c7137a78d` | `8289705b-893c-4614-aafe-2ee2ca6e5eb0` |
| `LocalGptProjectRevisions.Id` | `1c8f8279-0db3-4877-acfb-fc39a99a9d9c` | `82e77972-66c5-4177-8b49-d047c1e8ba49` |
| `RevisionName` | `council-a8ddfcff4c5b4bbeb39833b18f357f6d` | `council-91a00673f153409ab1bbf39abc6dacc3` |
| `CodeGenerationChangeReviews` | No row for run/project | SourceDraft, same ID as Council run |
| SourceRootPath, SolutionPath, snapshot path/hash | Empty | Empty |
| Tracked files / versions / requirements / artifacts / build checks | 0 | 0 |
| Approval consumed | No source review exists | 0; no review hash |

`GoalExecution/a8ddfcff4c5b4bbeb39833b18f357f6d.json`: goal state NeedsApproval (6), failed step state Failed (3), possible external effects flag true; root project/revision IDs are null. Spool stream status Completed with incomplete final answer, 130 steps and 73 warnings; **not completed delivery**. Project revision's IsUserApproved is a placeholder/revision status, not an approval of source or a build. Run lasted approximately 09:41–10:49 UTC. No generated ZIP, reviewed workspace or toolchain invocation is evidenced for this run. `ChatMemoryConversations` has no project-linked row for either project; conversation `62dc55ed-ff60-405c-906a-6425d57411ed` created at the end of the later run has null project and version IDs (time correlation alone is not authoritative identity). Do not attach the older draft to the newer project by title or timestamp.

19 Nov draft payload: 12 reserved paths, 10 candidate files, 27 assignments, 19 inspections. Stored source is not approved materialized source. All paths below are relative draft paths, not a real filesystem root:

| Path | State |
|---|---|
| `TwitchDashboard.sln` | stored |
| `TwitchDashboard/TwitchDashboard.csproj` | stored, HTML-encoded XML; net8.0 contradicts original request |
| `TwitchDashboard/Program.cs` | stored |
| `TwitchDashboard/Services/TwitchService.cs` | missing |
| `TwitchDashboard/Models/StreamEvent.cs` | stored |
| `TwitchDashboard/Models/TwitchUser.cs` | stored |
| `TwitchDashboard/Pages/Index.razor` | stored, encoded markup |
| `TwitchDashboard/Pages/Timeline.razor` | stored, encoded markup |
| `TwitchDashboard/Pages/TimelineConfigurator.razor` | stored, encoded markup |
| `TwitchDashboard/Shared/NavMenu.razor` | stored, encoded markup |
| `TwitchDashboard/wwwroot/css/site.css` | missing |
| `TwitchDashboard/appsettings.json` | stored, encoded quotes |

The historical goal includes previous assistant consensus, not just the user goal. The older project work order explicitly calls .NET 10 unsupported and requests `Change TargetFramework to 'net8.0'`. Output metadata nevertheless says net10.0 for ClassLibrary and blank for Solution. These facts show that validating manifest output metadata alone is insufficient. Do not repair these stored historical bytes automatically or synthesize new approvals. Existing 7.5.9 provenance-specific HTML decoding and immutable architect work orders are relevant to fresh runs, but they cannot retroactively fix the old payload.

### Tool audit and errors

15 `SystemVariables` invocation receipts for the later Council run:
- 8 successful `project.maintenance.get` (all eight returned 19,169 characters and were truncated at the configured 16,000-character transport ceiling).
- 1 successful `localgpt.path.browse`, 1 successful `dxai.tools.discover`.
- 1 `project.workspace.files.list` MissingWorkspace. No existing workspace is expected for greenfield source creation.
- 2 invalid `human.collaboration.request` and 2 invalid `chat.upload_workspace_files` calls. Schema rejections are not executed actions. No source approval; a host exclusion request was declined. No research, reviewed source creation, source file write, compiler or build receipt in the 15 records.

75 warning/error ApplicationLogs rows in the affected time window: ten context preflight rejections recorded at three logging layers (16K, 32K and 2K roads could not accommodate the supplied packets), seven thinking-only responses, eight large maintenance result truncations, four invalid tool calls, startup delays/unreachable routes, bounded tool loop termination, repetition watchdog and failed keyaccount review. The previous source candidate's plan correction addresses the failed manifest transition, not these other categories. Host startup delays and context-fit rejections are infrastructure/harness conditions, not proof of model incompetence. No inference success rate is inferred from this one run.

### Technical facts in local database

- `ProjectCompilerInstallations`: `622157cf-b899-4593-9a74-21430bf2676b`, `C:\Program Files\dotnet\dotnet.exe`, profile `dotnet-sdk`, enabled and last validated successfully on 14 Nov at version 10.0.402, not default. `cc5725d7-b689-4c91-9956-819381d78877`, `C:\Program Files (x86)\dotnet\dotnet.exe`, enabled default, empty version, validation unsuccessful (no SDK). This is a separate configuration problem, not a proven cause of the failed planning run, which never invoked the build tool. Do not silently change the default or claim a fresh validation. Owner should select and revalidate the correct SDK and verify PATH/global.json/build target.
- `CouncilKnowledgeEntries` already contains approved/current SourceBacked references including Microsoft .NET docs corpus, ASP.NET/Blazor source map, modern .NET architecture and .NET 8–10 Blazor architecture guidance. Model-suggested unverified `dotnet version` is not authority. The appfiles include runtime path layout for `LearningBase`, but not the complete actual LearningBase source corpus. The existing remote inspect and import functions, public search (approval gated), and learning diagnostics are the right infrastructure; no parallel learning database is created.

## Implemented in 7.6.0

1. A coordinator-owned evidence packet before coordinated manifest planning for a single explicit .NET target. Read observed SDK records (including bad default) and approved/current source-backed reference excerpts from the existing database services. Use the existing DXFunction registry and resolved planning-role policy to inspect configured official .NET release-channel JSON and .NET docs reference URLs. Validate dispatch/read-only receipt, URL, cache containment, size, bytes and matching channel/latest SDK. Preserve the retrieved bytes under `LearningBase/RemoteSources/TechnicalEvidence/<SHA256>.bin` using create-without-overwrite and hash verification; keep the existing remote-source cache and approval-gated import. The docs reference is from main, not asserted to be an exact version-pinned documentation corpus. The source URLs and rules are runtime-policy seeds; the editing environment did not call them. A missing/disabled/failed lookup stays explicitly unverified. Never infer a downgrade from missing evidence.
2. Architect packets and the server-wrapped persisted shared contract retain coordinator evidence separately from model architecture proposals. File author/reviewer packets already consume the persisted shared contract. A separate evidence step records the snapshot in the spool. Approvals, host settings and role assignments are unchanged. The original user requirement remains distinct from source-reference data and all reviewer opinions. This is a run/draft-owned evidence snapshot, not a guarantee against administrator filesystem edits or all possible untrusted model behavior.
3. Preserve the goal for project provisioning rather than prior consensus where GoalText is available. A blank Solution output-framework field inherits the single explicitly requested target; actual project file is still required and explicit incompatible metadata is rejected.
4. Project source validation now applies to C#/F#/VB projects and MSBuild props/targets; checks XML, explicit target literals even alongside dynamic MSBuild expressions, and explicit DevExpress package/reference declarations including central package versions. Revalidate persisted files before submission and legacy sequential completion. Reject a corrective inspection that explicitly proposes a conflicting framework when the candidate has no detected target defect. An invalid vote remains invalid; source is not automatically certified. Dynamic MSBuild imports, implicit dependency resolution, runtime API compatibility and independently verified package versions still require actual owner-side verification.
5. Compact `project.maintenance.get` automatic result for unmaterialized projects; return actual project/revision identity, paths, counts, compact SDK facts and explicit detail request. Additional optional `summaryOnly` parameter enables summaries for other requests; existing full response remains available, including explicit `summaryOnly=false`. No change to the global transport limit or saved context/host configuration.
6. Durable step receipt and Council spool now retain project/revision IDs from actual run results. Spool also retains coordinated-source and artifact verification flags independently of stream completion. Existing history without fields remains readable; root goal identity stays separate so new attempts do not silently reuse another run's project.
7. Extend existing PowerShell source guard with wiring assertions and exact downgrade regex examples. No Python repository tests or .NET build attempted.

## Open wires and owner acceptance

- Run all existing PowerShell gates and normal build on the owner machine; the editing environment has no PowerShell executable. Confirm the new runtime keys/regexes are seeded into the real upgraded database without overwriting owner customizations.
- Repeat the Twitch request using the same saved roles and hardware roads. Verify research receipts occur before planning; correct local SDK record and invalid default are displayed as observations; genuine upstream response has matching channel and immutable content hash. Blocked/failed/declined network policy must not fabricate results or cause version fallback. Check cached bytes and persisted draft contract; no approval bypass. Verify latest-channel JSON URL availability in owner environment; docs main reference is not version-matched proof.
- Run sources with invalid `net6.0`, `net7.0`, `net8.0`, mixed `net8.0;$(OtherTarget)`, malformed/encoded XML and DevExpress references; ensure they do not pass. A reviewer requesting net8.0 must not remove a valid net10.0 candidate. Another independent valid vote remains required. Test legitimate negative reviews, multiple targets, inherited targets, normal source-only and approved successful/failed builds. Inspect fresh Razor/JSON bytes for HTML-encoding regressions; do not modify historical payload on startup.
- Compare actual draft/project/revision IDs, exact manifest paths, completed file hashes, one-use approval, reviewed workspace, generated ZIP and source-only vs verified outcomes. Verify `summaryOnly=false` old details and default compact result on new empty projects. Review 19 Nov draft only under its own identity; explicit remediation/restart remains necessary.
- Remaining: initial Intake/Structure context fitting, safe exact function-level replay classification (read-only receipts vs unknown side effects), malformed-call specialist/learning attribution, host recovery/health, thinking-only response routing, durable task graph across process restart, ChatMemory project association, verification/benchmark and incremental compilation. Existing saved approvals and host exclusion outcomes remain untouched. No full all-history audit or successful runtime fix is claimed.
- Remaining research generalization: automatic evidence for implicit/best-suited framework choice and all other languages, version-specific API/dependency docs, approved DuckDuckGo discovery/import integration, learning/diagnostics feedback with curated evidence, and strict GitHub branch/tag pin handling in the general repository importer (existing generic branch fallback remains). The new targeted route uses exact raw source URLs and rejects mismatched release channels; do not regard all remote data as authoritative or automatically train on it.

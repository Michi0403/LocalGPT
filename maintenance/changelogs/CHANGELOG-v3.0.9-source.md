# LocalGPT 3.0.9 — five-profile all-model benchmark lanes and build-contract repair

## Benchmark behavior repaired

- The maintained initial hardware calibration now attempts five named performance points for every benchmark-capable provider-qualified subject: **Low, Normal, High, Expert and Max**.
- The five points are repeated parameter measurements of the same frozen model set. They are not model packs, quartets, representative groups, separate benchmark slides, or replacement membership sets.
- The deterministic measurement service receives the exact distinct provider-qualified Council selection and keeps the existing coverage invariant: every benchmark-capable frozen identity must be represented in the returned report.
- The maintained initial calibration disables failure-based and improvement-based profile early-stop behavior so every configured profile point is attempted. The generic benchmark service keeps the failure-stop setting configurable for other callers.
- Context/output bounds are taken from caller configuration and the database-backed LocalGPT catalog policy instead of one developer machine. The maintained workflow can therefore reach the configured runtime maximum context instead of stopping at the previous narrower benchmark ceiling.
- Independent physical/provider hosts may benchmark concurrently while models sharing one host remain sequential. This keeps multi-machine benchmarking useful without manufacturing same-host VRAM contention.

## Real provider execution provenance

- Benchmark scores remain based on real provider calls through `IProviderModelRuntimeService`; CanIRun.ai or other hardware guidance is explicitly advisory setup evidence and is never substituted for benchmark timing, throughput or answer quality.
- Every task result now records how many provider-call attempts were actually made, including the one bounded same-role corrective retry after a generic capability refusal.
- Aggregate benchmark evidence exposes provider-call counts, contract-compliant calls, successful profile points, measured timing/throughput/quality and explicit failure evidence.
- A provider/model failure is isolated to that Benchmark Subject. Its failure remains in the coverage matrix and the rest of that host queue continues instead of losing the entire all-model calibration.
- If every provider target fails, LocalGPT retains the failed coverage matrix and does not fabricate or persist hardware routes.

## Rich live Benchmark Subject lanes restored

- The deterministic measurement phase now creates one normal Council live participant lane for every frozen Benchmark Subject.
- Each lane shows its provider-qualified endpoint/model, queued/running state, streamed per-profile measurement progress, actual provider-call provenance and final measured/failure summary.
- These lanes reuse the existing live Council participant rendering and spooler ownership instead of replacing the chat with a separate benchmark-only UI.
- The ordered final transcript receives a bounded aggregate matrix after measurement; detailed per-subject streams stay in their live lanes so a large all-model run does not flood every later role prompt.

## Five exact persisted calibration tiers

- Benchmark profile persistence now recognizes **Low, Normal, High, Expert and Max**.
- A stored tier contains a model route only when that model completed that exact named measured point successfully.
- A lower successful point is never silently promoted into a missing higher tier. If no subject completed an exact tier, that tier is omitted and logged instead of invented.
- Applying any resulting performance profile still changes only matching provider-qualified hardware/token roads and never changes Council membership.

## Human collaboration longevity

- Blocking human collaboration boundaries remain indefinite: there is no inactivity timeout that auto-kills a Council because the user is away.
- While blocked, the live Council session is touched on every recheck and explicitly reports that it remains alive until the questions are answered or the run is explicitly stopped.
- The 30-second delay in the wait loop is only a status/recheck interval, not an approval deadline.

## 3.0.7 benchmark evidence reviewed

- The supplied 3.0.7 evidence shows that the deterministic calibration really did invoke provider models; provider request/accept stream messages were present.
- That run attempted 95/95 distinct provider-qualified benchmark subjects, 86 produced recommendations, and only four stored profile tiers were produced in that version.
- The observed 1800-second timeout occurred later in the social **Quality review** role for a Council participant, not in the deterministic benchmark measurement or a blocking human approval.
- The later human collaboration request in the supplied log was `GateMode None`; the same log records the Council run as completed. This release therefore keeps human waits indefinite while hardening benchmark measurement/failure isolation rather than adding an approval timeout.

## Build-contract repair

- `App.razor` intentionally preserves the prerendered DOM with `ssr: { disableDomPreservation: false }` for the browser/reconnect hardening introduced in 3.0.8.
- `Assert-OperationalDiagnostics.ps1` and `Assert-InteractiveServerRenderModes.ps1` still required the old destructive `true` contract and therefore rejected the source before C# compilation. Both guards now validate `false`, matching the actual reviewed application contract.
- The explicit `@rendermode` boundary set remains unchanged from 3.0.8.

## XML documentation cleanup

- The explicit constructors of `MinecraftDatapackService` and `MinecraftProjectService` now own their parameter documentation directly.
- This retains the 3.0.8 removal of invalid class-level `<param>` tags while satisfying the maintained declaration-documentation policy, avoiding the earlier CS1572 drift.

## Preserved

- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper are version **3.0.9**.
- LocalGPT 1-Wire protocol/package version remains **2.1.1**.
- Existing controllers, DXFunctions, provider routing, initial-setup hardware/provider/model workflow, hardware roads, Council team configuration, reconnect behavior and 20 explicit render-mode boundaries are retained.

## Source-only note

This repository was edited and statically validated without invoking `dotnet` restore/build/publish/pack and without GitHub/repository network access.

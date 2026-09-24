# LocalGPT 4.8.0 source validation

LocalGPT 4.8.0 was validated as a source package only, respecting the owner requirement not to run `dotnet`, MSBuild, NuGet, publish, installer execution, or GitHub access in this environment.

## Static source checks

- Version references for the LocalGPT app, installer console, webview wrapper, documentation metadata and browser asset cache keys resolve to `4.8.0`; historical release files are left historical.
- `appsettings.json`, `appsettings.Development.json` and maintained documentation JSON parse successfully.
- Versioned `.csproj` files parse as XML and expose `4.8.0`.
- `Runtime/Python/localgpt_runtime_bridge.py` parses with Python AST without importing or executing project code.
- No `__pycache__`, `.pyc` or `.pyo` files are included in the source tree.
- The LocalGPT Razor page set still has 15 `@rendermode InteractiveServer` files; `Error.razor` remains the only `@page` Razor file without that mode, matching 4.7.9.

## Requested behavior checks

- The shared `OperationalActivityFeed` is a `<pre>` ASCII terminal surface and no longer renders `DxMemo`.
- The known local-AI catalog contains OpenAI Whisper with direct GitHub source acquisition and a Python runtime adapter; Hugging Face remains in a separately named optional package profile.
- Runtime policy has user-facing and AI-facing surfaces for device, Whisper model/task/language/prompt, queue, media limits and caching.
- Toolchains exposes Python discovery, reviewed manufacturer/GitHub downloads, and redacted environment scope inspection/editing through service/controller/DXFunction layers.
- `project.ingestion.promote` declares deferred approval support and does not accept a `userConfirmed` parameter; promotion confirmation is supplied by the common DXFunction approval gate.
- Generated artifact ZIP refresh remains deferred-approval capable.
- Automatic AI calls now reach the common database policy + Human Collaboration approval gate instead of being rejected before an approval can be shown; user-saved preauthorization remains authoritative.
- Generic toolchain execution profiles and environment overrides have BusinessObject contracts, database persistence, services, controller routes, DXFunctions, and Toolchains UI; Python links reuse the existing generic compiler/runtime installation records.
- The benchmark path explicitly enables the existing repetition watchdog, covering repeated-output cases even when the general runtime watchdog setting is disabled.
- Rejoined live-Council messages synchronize the selected Council session with the canonical cache before DevExpress reloads; direct human sends update both stores.
- Council prompt reconstruction keeps 24 recent user turns and four cleaned assistant consensuses, still bounded by the configured maximum prompt characters.
- Popup-specific ASCII stage sizing no longer forces the middle grid row to the full popup height, preserving lower/footer controls.
- Human Collaboration refresh captures its lifetime token before awaited work and treats circuit-disposal cancellation/disposal as normal shutdown rather than an application error.

## Package checks

The final source ZIP is checked for CRC integrity, path traversal entries and compiled Python cache files, then extracted to a temporary directory and byte-compared with the staged source tree.

## Runtime boundary

No claim is made that this environment compiled or ran the .NET application. The first authoritative .NET compiler/runtime pass remains the owner's normal Visual Studio/build environment.

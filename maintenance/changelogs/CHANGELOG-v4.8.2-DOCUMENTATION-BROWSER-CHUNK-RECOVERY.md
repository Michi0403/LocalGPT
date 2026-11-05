# LocalGPT 4.8.2 — documentation browser/chunk recovery

LocalGPT 4.8.2 repairs the documentation PDF path reported from a normal Windows Visual Studio build of 4.8.1 and keeps the existing macOS browser-print path intact. The application/runtime feature set from 4.8.0/4.8.1 is unchanged.

## Documentation PDF regression repaired

- Normal LocalGPT builds now build the repository-owned `LocalGPT.ReleasePackaging` project as a **build-only dependency** (`ReferenceOutputAssembly="false"`) whenever documentation generation is enabled. The helper is therefore available to the documentation target without becoming an application/runtime dependency.
- `BuildLocalGptDocumentation` passes that helper explicitly to `Build-Documentation.ps1`. This restores the bounded browser-chunk path during ordinary IDE builds instead of only release flows that already supplied `-PackagingTool`.
- The chunking decision no longer depends on whether a merge helper happened to be supplied. If the handbook requires chunking and the helper is missing, the script reports that exact condition rather than incorrectly describing the browser as unavailable and silently dropping into a giant monolithic renderer.
- The fallback console message now says that the bounded browser path failed to produce a complete document, rather than claiming browser printing was unavailable when the actual reason could be chunk-merger wiring or a render failure.

## Cross-platform browser discovery hardened

- Windows browser discovery keeps Program Files, Program Files (x86), LocalAppData and `PATH` probes, and additionally probes `ProgramW6432` plus the Windows `App Paths` registry for Edge, Chrome and Brave. This covers 32-bit shell/IDE hosting and managed/per-user browser installations.
- macOS application-bundle discovery remains intact for Chrome, Edge and Chromium and now also recognizes Brave in both `/Applications` and `~/Applications`.
- Linux/PATH discovery remains intact and now includes Brave command names as another Chromium-family option.
- The selected browser path/name is printed before rendering, making future diagnostics unambiguous.

## Smaller, durable PDF parts

- The default browser PDF part size is now deliberately small on all machines: 8 pages on <=16 GiB hosts, 10 pages on <=32 GiB hosts, and 12 pages on larger hosts.
- Larger machines still receive larger Node/Chromium heaps and higher DocFX parallelism; RAM is no longer used as a reason to inflate one browser print part to 50/75/100/120 pages.
- `FUTURE2_DOCUMENTATION_BROWSER_PDF_CHUNK_PAGES` remains the explicit operator override (5-250 pages), so specialized build machines can still choose a different value deliberately.
- Durable chunk reuse/retry behavior, isolated browser profiles, HTML accessibility preflight, complete-PDF checks and the existing protection against unsafe monolithic fallback for chunked/low-memory builds remain in force.

## Maintenance/security policy

No maintenance guard was disabled or relaxed. The application architecture, method diagnostics, text-service ownership, iterator, system-variable, EF snapshot, JavaScript/static asset and DevExpress control validations remain enabled in their existing build order.

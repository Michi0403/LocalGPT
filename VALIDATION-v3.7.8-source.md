# LocalGPT 3.7.8 source validation

This source package was prepared without GitHub access and without running a .NET build, matching the supplied-source workflow.

## Static checks performed

- Version references were checked for LocalGPT 3.7.8 and the one-digit minor/patch rule.
- The notarization path was compared across 3.7.5, 3.7.6, and 3.7.7 before changing orchestration.
- Transient Apple/network failures are retried without terminating the release; genuine terminal Apple statuses remain fatal.
- Recoverable Keychain/profile conditions now route through a wait/retry path; fresh uploads persist a submission sidecar before status polling; Apple `In Progress` no longer fails on the local checkpoint.
- Durable PDF reuse rejects incomplete files without an `%%EOF` marker.
- The adaptive browser PDF path remains `html-browser-chunked`; complete chunks are cached durably and validated before reuse.
- Routed Razor pages remain covered by the existing InteractiveServer guard; no component render modes were changed in this release.
- Final source audit: passed.
- Application architecture audit: passed.
- Cross-platform boundary audit: passed (22 checks).
- Service resilience audit: passed (2185 service methods checked; documented iterator/Program exclusions retained).
- Async continuation audit: passed (259 source files, 2982 await tokens).

## Not performed

- No `dotnet build`, `dotnet test`, notarization submission, code signing, Homebrew installation, or browser PDF render was executed in this environment.

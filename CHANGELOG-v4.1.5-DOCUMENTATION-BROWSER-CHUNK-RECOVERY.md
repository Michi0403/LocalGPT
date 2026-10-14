# LocalGPT 4.1.5 — Documentation Browser Chunk Recovery

LocalGPT 4.1.5 hardens the documentation PDF renderer after an 8 GiB macOS release build showed a single 8-page Edge chunk stalling for more than eleven minutes and the recovery path then launching a monolithic 1,172-page DocFX/Playwright PDF render. The application runtime and provider-management behavior are unchanged.

## Changes

- Keeps adaptive memory sizing from 4.1.2: an 8–10 GiB host still uses 8-page chunks, a 768 MiB Chromium JavaScript heap, a 1 GiB Node heap, and DocFX parallelism 1.
- Uses a 90-second browser-render timeout by default in low-memory mode instead of allowing one unhealthy chunk to consume the normal eight-minute timeout. `FUTURE2_DOCUMENTATION_BROWSER_PDF_TIMEOUT` remains authoritative when explicitly set.
- Caps the post-render stability wait at 15 seconds on low-memory hosts instead of allowing an additional three-minute tail after a timed-out browser.
- Keeps every browser render in a unique user-data directory and also isolates Chromium disk cache and crash dumps inside that profile.
- Cleans any surviving browser child whose command line contains the unique build profile path before deleting the profile. Normal user browser sessions are not targeted.
- Uses only the modern headless engine on low-memory hosts; high-memory hosts retain the compatibility headless retry.
- Retries a failed cover/index or page chunk once by default with a completely fresh browser profile. `FUTURE2_DOCUMENTATION_BROWSER_PDF_CHUNK_RETRIES` can set 0–5 retries.
- Preserves already completed durable PDF chunks, so a failure at a later chunk resumes from the first missing chunk.
- Refuses the monolithic DocFX/Playwright PDF fallback whenever the documentation requires chunking, and also refuses it for any low-memory documentation build. This prevents recovery from undoing the bounded-memory design.
- Provides an explicit emergency override, `FUTURE2_DOCUMENTATION_ALLOW_MONOLITHIC_PDF_FALLBACK=1`, for an operator who intentionally accepts the memory risk.
- Records browser timeout, post-render stability window, retry count, and monolithic-fallback policy in `documentation-status.json`.

## Preserved contracts

The 4.1.4 `ConfigurationRoot` qualification repair, 4.1.3 system-variable guard repair, 4.1.2 adaptive documentation memory policy, 4.1.1 provider/model management workbench, 4.1.0 Matrix operator controls, macOS entitlements/signing preparation, PDF version hygiene, renderer-affine continuation policy, localization, persistence, and installer semantics remain intact.

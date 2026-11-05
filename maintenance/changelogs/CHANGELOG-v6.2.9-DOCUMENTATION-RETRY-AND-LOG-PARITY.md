# LocalGPT 6.2.9 — documentation recovery and console parity

Base: LocalGPT 6.2.8. No architecture guards or approval rules were modified.

- Aligned the existing browser-PDF retry helper with PublisherStudio: retries remove only the failed candidate; thrown renderer failures are observed and retried within the configured bound, with active output rechecked before success.
- Cached invalid PDF cover/index and chunk candidates are called out explicitly and replaced without discarding validated prior chunks.
- Added the same DocFX chunk `queue` and per-part `progress` messages as PublisherStudio to make failures and resumptions comparable in both release-build consoles.
- Added matching release entrypoint stage messages (preflight, compile, documentation). Both projects intentionally keep different platform/tool and architecture checks; matching logs do not mean bypassing either project's gates.
- Added `tests/static/Assert-DocumentationChunkRecovery.ps1` and adjusted two source regression version expectations.
- Bumped project version to 6.2.9.

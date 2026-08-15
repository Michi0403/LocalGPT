# LocalGPT 2.9.1 live Council transcript-status repair

LocalGPT 2.9.1 removes the transient orange/animated Council waiting box from generated chat content and renders the same live progress as a normal inline transcript paragraph. This avoids repeated layout/scrollbar jumps while preserving the Council progress information.

Council execution, 2.8.8 optional role coordination, 2.8.9/2.9.0 rejoin recovery, reasoning/function traces and provider routing are intentionally unchanged. Razor component helper statics were also converted to instance members and are now covered by the architecture audit.

## Versions

- LocalGPT: 2.9.1
- LocalGPTWebviewWrapper: 2.9.1
- LocalGPTInstallerConsole: 2.9.1
- LocalGPT Wire Protocol: 2.1.1 (unchanged)

See `CHANGELOG-v2.9.1-LIVE-COUNCIL-TRANSCRIPT-STATUS.md` and `VALIDATION-v2.9.1-source.md`.

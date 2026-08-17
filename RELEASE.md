# LocalGPT 3.0.0 EF migration/startup repair

LocalGPT 3.0.0 repairs the release-blocking startup regression in 2.9.8/2.9.9 where the persisted Council-team model changed without a matching EF Core migration and model snapshot update.

The repair adds the six missing Council-team policy columns with backward-compatible defaults, preserves existing team rows, updates the EF snapshot, and adds a build-time source guard for missing persisted scalar properties.

The Council live-lane synchronization work from 2.9.9 is otherwise unchanged.

## Versions

- LocalGPT: 3.0.0
- LocalGPTWebviewWrapper: 3.0.0
- LocalGPTInstallerConsole: 3.0.0
- LocalGPT Wire Protocol: 2.1.1 (unchanged)

See `CHANGELOG-v3.0.0-source.md` and `VALIDATION-v3.0.0-source.md`.

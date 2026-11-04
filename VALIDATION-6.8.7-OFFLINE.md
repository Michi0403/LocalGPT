# LocalGPT 6.8.7 — offline validation

- Source basis: exact LocalGPT 6.8.6 ZIP. Only four existing files modified; these two reports added.
- Streaming and nonstreaming null-request guards appear before dereferences; 6.8.6 fails corresponding negative controls.
- Selected-run change invokes automatic exact-run history restoration; 6.8.6 fails negative control.
- Active run attachment accepts live session without requiring a persisted spool entry.
- Completed run no longer raises an inaccurate running-only rejoin error.
- Added preflight contracts use null-safe string-position inspection and retain old architecture validations.
- Version increment follows x.y.z under 10 rule: 6.8.6 -> 6.8.7.
- Zip integrity tested via ZipFile.testzip; no .NET or PowerShell run.

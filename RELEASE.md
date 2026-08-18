# LocalGPT 3.1.3

LocalGPT 3.1.3 is the Council resilience successor to 3.1.2. It preserves durable benchmark evidence and the benchmark coverage truth guard while making configured Social Team rounds recover required member work after provider/model failure, treating explicit stop as normal cancellation, and removing the live user-message shadow-DOM flicker path.

## Toolchain state

- .NET SDK policy: `10.0.400`
- Target framework: `net10.0`
- DevExpress: existing `25.2.*` package lane retained
- 1-Wire protocol: `2.1.1`

## Council resilience state

- workflow steps persist user-editable member failure recovery mode and bounded recovery turns;
- the existing same-member safe transport fallback remains first-line recovery;
- eligible alternate recovery members come only from the saved Social Team role pool and prefer another AI-host road when available;
- failed attempts remain auditable and recovery attempts are separate Council steps;
- participant infrastructure faults no longer strand a host queue;
- phase infrastructure faults can no longer be logged and silently discarded;
- explicit user stop is cancellation, not a failed Council result;
- live user messages are rendered from authoritative DxAIChat state rather than heartbeat-recreated JavaScript rows.

## Compatibility

No database migration or benchmark evidence archive schema migration is introduced. Existing 3.1.1/3.1.2 evidence remains compatible, existing Social Teams load with safe defaults for the newly persisted workflow recovery fields, and the 3.1.2 machine-derived benchmark coverage truth remains authoritative.

See `CHANGELOG-v3.1.3-COUNCIL-ROUND-RECOVERY-CANCELLATION-UI-STABILITY.md` and `VALIDATION-v3.1.3-source.md`.

# LocalGPT 3.1.5

LocalGPT 3.1.5 is the provider-stream repetition-watchdog successor to 3.1.4. It preserves the repository-wide XML documentation completeness work and all earlier benchmark/Council evidence and recovery behavior while preventing an actively streaming model from monopolizing a benchmark host road or Council member slot by repeating the same short text cycle indefinitely.

## Toolchain state

- .NET SDK policy: `10.0.400`
- Target framework: `net10.0`
- DevExpress: existing `25.2.*` package lane retained
- 1-Wire protocol: `2.1.1`

## Repetition watchdog

- starts timing on the first substantive generated provider fragment;
- analyzes only provider-generated stream text, not LocalGPT status messages or provider-trace metadata;
- uses a bounded 12,288-character rolling tail;
- requires substantial output, exact-ish short-cycle periodicity, six or more cycles and 97% agreement;
- requires four suspicious samples over at least six seconds before stopping a request;
- cancels only the current provider request and leaves caller/Council cancellation tokens untouched;
- preserves the partial stream as evidence and omits provider content from exception logs.

## Recovery semantics

During deterministic all-model benchmark calibration, repetition retries use the existing configured Social Team member-recovery attempt count. The same provider-qualified subject is retried because substituting another model would invalidate the benchmark identity. When retries are exhausted the failed task is retained and the host queue continues.

During ordinary Council work, repetition failure feeds into the existing same-member safe recovery and configured round-member recovery introduced in 3.1.3. Social Team recovery mode, retry count and eligible role-member pool remain authoritative; no second scheduler was added.

## Compatibility

No database migration, benchmark evidence schema migration, 1-Wire protocol change, or removal of 3.1.4 XML documentation coverage is introduced by 3.1.5.

See `CHANGELOG-v3.1.5-STREAM-REPETITION-WATCHDOG.md` and `VALIDATION-v3.1.5-source.md`.

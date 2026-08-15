# LocalGPT 2.9.3 source validation

No .NET build was executed in the packaging environment. The Windows development build remains authoritative.

Source-only checks completed:

- `build/audit_release_2_9_3.py`
- previous 2.9.2, 2.8.8 and 2.8.6 release regressions
- provider-qualified Council audit (280 checks)
- Council X-Round / heartbeat audit
- benchmark/rejoin repair audit
- code-generation / DXFunction wiring audit
- human-visible entity formatting audit
- architecture policy audit
- service resilience audit
- async continuation audit
- XML documentation coverage and quality
- documentation / 1-Wire contract audit
- exact `@rendermode` comparison against LocalGPT 2.9.2
- Wire Protocol 2.1.1 byte-preservation check
- source-only ZIP integrity and forbidden-artifact scan

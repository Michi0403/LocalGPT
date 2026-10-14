# LocalGPT 5.4.5 source validation

The following checks are run against supplied source without `dotnet`, MSBuild, NuGet restore/publish or GitHub:

- application architecture / static ownership
- async continuation and async-only architecture
- service method resilience / diagnostic boundaries
- provider-qualified Council regression
- newly added version/revision source contracts including deterministic local knowledge/regex preflight
- full source ZIP integrity and SHA-256

The user's local .NET 10 build and runtime acceptance are still required. In particular, test a Learning Round containing LocalGPT's `net10.0` projects plus an unrelated older-framework sample. The judge should first retrieve exact local Knowledge/regex and SDK pins, never suggest a downgrade of the same project revision, and request approval for missing matching-version documentation. Existing user-edited Council presets must remain preserved as user copies.

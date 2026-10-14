# LocalGPT 3.2.9 source validation

This archive is **source-only and not compiled** in the preparation environment. No `dotnet`, MSBuild, restore, publish, EF command or runtime launch was performed. No GitHub/online repository access was used. The user's Windows .NET build remains authoritative for compilation and runtime validation.

## Database and migration review

- The supplied SQL export was imported into a temporary SQLite database for read-only analysis during this release review.
- SQLite integrity check returned `ok` and declared foreign-key check returned zero violations.
- The additive knowledge↔RegEx table was validated separately as a SQLite schema shape with restrictive foreign keys and the intended indexes.
- The EF context/snapshot scalar-property consistency was reviewed source-side for maintained DbSet entity types.
- The EF snapshot architecture guard was updated from global navigation-name counts to entity-specific relationship contracts because legitimate restored reverse navigations now share names such as `Artifacts` and `BuildVerifications`.

## Final source-only validation gate

The final 3.2.9 worktree completed these source checks successfully before packaging:

- Async continuation validation: **258 source files**, **2,973 await tokens**, **2,618 `ConfigureAwait(false)`**, **135 renderer-affine `ConfigureAwait(true)`**, **215 configured async disposals**, and **5 configured async streams**.
- Application architecture policy: passed.
- Service resilience: **2,155 service methods** own diagnostics boundaries; 29 iterator/yield methods and 3 direct Program/Startup methods remain intentionally handled by their separate policies.
- C# XML documentation: **10,101 declarations across 639 maintained source files** passed coverage and quality checks.
- Razor XML documentation: **45 components and 776 direct `@code` declarations** passed coverage and quality checks.
- Code-generation / DXFunction wiring audit: passed.
- Provider-qualified Council feature audit: **282 checks passed**.
- Provider stream-repetition policy audit: passed.
- Chat ASCII-console lifecycle audit: **17 checks passed**.
- Release-specific LocalGPT 3.2.9 audit: **56 checks passed**, covering versions, DevExpress retention, Database workbench boundaries, semantic selectors, knowledge↔RegEx migration/service wiring, restored navigation contracts, drawer rerender behavior, teardown hardening, and release documentation.

## Environment limitation

PowerShell/Roslyn validation and the repository's .NET-backed syntax/build gates cannot run in this preparation environment because the requested workflow intentionally does not use a .NET SDK. They were not claimed as executed. The archive therefore still needs the normal Windows build on the user's machine, exactly as with prior source-only handoffs.

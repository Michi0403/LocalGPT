# LocalGPT 2.8.5 source-only validation

Validation performed without GitHub access and without invoking dotnet, MSBuild, or a .NET compiler:

- LocalGPT 2.8.5 multilingual source regression audit passed, including version alignment, dynamic localization discovery, six-culture key parity, translated-value coverage, and render-mode count.
- Strict async/Council Teams responsiveness regression audit passed: 158 source files, 2,344 await tokens, 2,135 `ConfigureAwait(false)`, 31 renderer-affine `ConfigureAwait(true)`, 175 configured async disposals, and 3 configured async streams.
- Human-visible entity formatting, benchmark/rejoin/build-guard, architecture, and service-resilience audits passed.
- All six LocalGPT localization JSON files parse and have identical 1,862-key sets.
- The 19 `@rendermode` directives are unchanged from the supplied 2.8.4 source ZIP.

The source package is intentionally not compiled. The user's Windows .NET 10 build remains authoritative for compile/runtime confirmation.

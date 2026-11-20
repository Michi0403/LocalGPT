# LocalGPT 7.6.3 — PowerShell, text ownership and cancellation build repair

Source candidate against the supplied 7.6.2 archive; owner build evidence 2026-11-20 18:46–18:50. No .NET compilation, online GitHub access or PowerShell runtime execution in the editing environment.

## Confirmed failures and corrections

- 18:47:34 LGPS5101: `Assert-CouncilXRoundWiring.ps1` line 678 wrapped `ConvertFrom-Json` directly in `@(...)`. 18:47:56: the subsequent run reported false exact-enum drift. The registry declares `begin,next,complete,submit,repair` in that order. Assign the parsed JSON to a variable, then explicitly enumerate the parsed array using `foreach`, preserving the exact case-sensitive comparison and invalid-alias checks. No registry values were changed.
- 18:49:13: direct `ActivityKey.StartsWith` in the Razor lane board violated text-service ownership. Move the execution filter into the existing DI-owned live session display projection (service/interface), apply it before rendering/copying activity text, preserve ordinal matching and the colon-delimited execution GUID prefix. Existing one-argument calls remain unfiltered through an optional parameter; no new DI registration or UI architecture.
- 18:50:50 CS0136: the cancellation handler redeclared `interrupted` in the enclosing method's scope. Rename the inner snapshot to `interruptedSnapshot` and keep all receipt identity, artifact-expectation and cancellation persistence operations intact.
- Update the existing Council wiring assertions for the corrected variable and the service-owned filter. Compatibility gate, text-service ownership gate and build entrypoints are unchanged. Version increased to 7.6.3; all outstanding tasks retained.

## Validation boundary

The original source, registry schema, interface/service/call sites and the three reported gate/compiler failure sites were inspected. Source archive comparison and integrity check performed. PowerShell and .NET were not run. Owner-side local changes between build attempts are unknown; this archive repairs the supplied original 7.6.2 source. Run the repository's PowerShell gates, full build and runtime acceptance from TASKS.md. PublisherStudio unchanged.

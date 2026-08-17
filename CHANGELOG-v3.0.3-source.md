# LocalGPT 3.0.3 source changelog

## Windows C# compile repair

- Fixed `LocalGptCatalogService.PromptCatalog.cs` after the 3.0.1/3.0.2 partial split: `GetSuggestion()` now logs its cancellation/failure path through the maintained `_logger` field instead of the out-of-scope constructor parameter name `logger`. This directly addresses both reported `CS0103` errors.
- Hardened `MinecraftDatapackService` against DI-field/parameter shadowing by renaming its `CouncilTextService` collaborator field from `text` to `_text`.
- `BuildMinecraftDatapackArtifactIdentity(...)` now routes Pascal-identifier formatting through `_text.ToPascalIdentifier(...)`, rather than accidentally binding the call to its `string text` method parameter. This directly addresses the reported `CS1061` error while preserving the rule that string normalization remains service-owned.
- Cleaned the duplicated nested XML `<summary>` around `LocalGptCatalogService.GetSuggestion()` that had been introduced by an earlier documentation rewrite.

## Regression protection

- Added `build/audit_release_3_0_3.py` covering the two partial-logger failures, the Minecraft service-field shadowing failure, maintained 3.0.2 Windows guard repairs, Minecraft DXFunction wiring, Wire Protocol 2.1.1, and all 19 explicit InteractiveServer render-mode declarations.
- The 3.0.2 source package used as the baseline was verified byte-for-byte against the exact ZIP previously returned to the user before applying 3.0.3.
- No Windows PowerShell guard was weakened or changed in this release. The user's latest Windows output already showed the operational diagnostics, InteractiveServer, async, architecture, service resilience, text ownership, iterator, system-variable, EF, JavaScript, and static-web-asset guards passing before the C# compiler reached these three errors.

## Version

- LocalGPT: 3.0.3
- LocalGPTWebviewWrapper: 3.0.3
- LocalGPTInstallerConsole: 3.0.3
- Wire Protocol: 2.1.1 (unchanged)
- Council seed version: 25 (unchanged)

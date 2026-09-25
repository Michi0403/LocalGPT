# LocalGPT 4.9.3

LocalGPT 4.9.3 is a narrow compile-repair release on top of 4.9.2.

It exposes `LegacyPromptDefaults` through `IInitialDataCatalog`, matching the existing `InitialDataCatalog` implementation used by deterministic prompt-seed upgrades, and fixes the microphone-to-Council renderer dispatch so the boolean result of `StartCouncilPromptAsync` is captured explicitly instead of being lost through Blazor's non-generic `InvokeAsync` overload.

No runtime-plugin, microphone recording, approval, Council orchestration, toolchain, persistence-schema, or packaging architecture was otherwise changed.

This is a source-only handoff. No .NET/MSBuild/NuGet build, restore, publish, native packaging, signing, notarization or installer execution was performed in the handoff environment. See `CHANGELOG-v4.9.3-CATALOG-SPEECH-COMPILE-REPAIR.md` and `VALIDATION-v4.9.3-source.md`.

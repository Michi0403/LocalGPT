# LocalGPT 2.7.1 — 1-Wire live capability sync and compile repair

## Version

- Rolled LocalGPT, InstallerConsole and WebView wrapper from **2.7.0** to **2.7.1**.
- The independent `LocalGPT.WireProtocolVersion` package remains **2.1.1** because the required capability/skill response message contracts already exist there.

## Fixes

- Fixed the C# regex literal for the PowerShell `.ps1` detector in `InitialDataCatalog`; the former `\.`/`\b` source escaping produced CS1009 in the generated C# string literal.
- Fixed the `Path.GetRelativePath(...).Replace('\\', '/')` character literal in `CodeGenerationWorkflowService`; the malformed backslash character literal caused the reported CS1012/CS1010 parse failures.
- LocalGPT now accepts authenticated `CapabilityResponse`, `SkillResponse`, and `SkillStateUpdate` messages from an already-linked 1-Wire peer and atomically refreshes that peer's capabilities, skills, UI features and hardware while retaining the approved connection state.
- The peer advertisement reports the actual LocalGPT assembly version rather than the stale historic `2.3.6-organic-wire` text.
- The 1-Wire architecture audit now checks that live peer-directory refresh handling remains present.

## Source-only validation

No `dotnet`, MSBuild, restore, build, publish, DocFX, or GitHub access was used. Source audits passed for architecture policy, code-generation/DXFunction wiring, 1-Wire/documentation contracts, service resilience, XML documentation and async continuation policy.

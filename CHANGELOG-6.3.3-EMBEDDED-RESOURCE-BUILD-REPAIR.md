# LocalGPT 6.3.3 — NETSDK1022 seed resource build repair

Base: LocalGPT 6.3.2 default-seed integrity source.

## Fix

- Fixed the confirmed `NETSDK1022` build failure caused by embedding the same three mandatory reference documents twice under different logical names:
  `docs/reference/toolchain-discovery.md`, `docs/reference/ai-provider-installation.md`, and `docs/reference/ascii-game-authoring.md`.
- Kept their `LocalGPT.SeedDocument.docs.reference.*` resources because `InitialDataFeedService.TryReadEmbeddedAsync()` reads those names on installed distributions. Removed only the redundant legacy `LocalGPT.Knowledge.*` declarations for those three documents; no seed data, required policy document, or packaged content file was removed.
- Retained the standalone `LocalGPT.Knowledge.core-defaults.json` manifest and all 18 mandatory seeded documents.
- Incremented the application version to `6.3.3`, and synchronized offline release assertions.
- Added a static guard that rejects duplicate physical `EmbeddedResource` paths even if they have different `LogicalName`s, and verifies the mandatory resource names.

## Verification and limits

- XML parsing, no-duplicate resource checks, offline Python tests, and ZIP CRC integrity verification; no .NET build, GitHub, installation, or live seed run performed here.
- User's build log: LocalGPT stops at `NETSDK1022`. PublisherStudio.Web compiled its DLL despite an earlier NuGet restore warning; its documentation generation status is not proven by the supplied excerpt. PublisherStudio source unchanged.
- On the owner's .NET 10 SDK, rebuild to confirm the compiler/resource pipeline and installation behavior.

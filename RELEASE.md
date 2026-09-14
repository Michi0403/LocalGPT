# LocalGPT 4.2.7

LocalGPT 4.2.7 is a compile-repair follow-up to 4.2.6. It retains the multi-host provider setup, per-drive Ollama storage/deletion evidence, bidirectional Council/ASCII terminal surface, Crazy ASCII continuity and macOS chunked-documentation repair from 4.2.6, while correcting a C# `CS1628` error in the local Ollama manifest digest reader.

`ProviderRuntimeManagementService.TryReadManifestDigests` now collects SHA-256 references in an ordinary local set captured by its recursive JSON reader, then assigns the completed set to the method's `out` parameter after parsing. This preserves the conservative deletion behavior while avoiding illegal capture of an `out` parameter by a local function.

No `dotnet` build, release publish, signing/notarization run, or GitHub operation was performed for this source handoff. See `CHANGELOG-v4.2.7-PROVIDER-MANIFEST-COMPILE-REPAIR.md` and `VALIDATION-v4.2.7-source.md`.

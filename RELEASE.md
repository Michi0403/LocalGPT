# LocalGPT 3.0.3 partial compile and service-shadowing repair

LocalGPT 3.0.3 is a narrow compiler repair over 3.0.2. The user's Windows build passed the maintained architecture/build guards and then exposed three C# errors created by the earlier partial/service extraction work.

Key changes:

- `LocalGptCatalogService.GetSuggestion()` now uses its maintained `_logger` field in both exception paths, fixing two `CS0103` failures.
- `MinecraftDatapackService` stores its injected `CouncilTextService` as `_text`, preventing methods with a `string text` parameter from shadowing the service collaborator.
- Datapack artifact identity generation routes `ToPascalIdentifier` through that injected text service, fixing the reported `CS1061` without bypassing service scope/ownership.
- The 3.0.2 Windows guard, namespace, structure, lightweight rejoin, Minecraft controller, and seeded DXFunction repairs remain intact.
- No new EF migration, browser JavaScript change, render-mode change, or Wire Protocol change is introduced.

Versions:

- LocalGPT: 3.0.3
- LocalGPTWebviewWrapper: 3.0.3
- LocalGPTInstallerConsole: 3.0.3
- LocalGPT Wire Protocol: 2.1.1

See `CHANGELOG-v3.0.3-source.md` and `VALIDATION-v3.0.3-source.md`.

This source package was not compiled in the repair environment. No GitHub or .NET/MSBuild invocation was used.

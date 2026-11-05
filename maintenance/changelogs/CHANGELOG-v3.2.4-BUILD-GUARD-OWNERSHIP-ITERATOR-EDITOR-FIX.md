# LocalGPT 3.2.4 changelog

## Build-guard repair after 3.2.3

- Fixed the five `Assert-TextServiceOwnership.ps1` failures introduced by the JSON/OData user-function editor. Generated `user-source.*` key classification and deterministic key creation now belong to `IUserDxAiFunctionService` / `UserDxAiFunctionService`, while the Razor editor only calls the injected service. No ownership baseline or exemption was added.
- Fixed `Assert-IteratorExceptionPolicy.ps1` for `LearningProjectWorkspaceSyncService`. Repository file discovery is now materialized into an `IReadOnlyList<string>` with logged inaccessible-directory handling instead of using a `yield` iterator that contained `catch`. No iterator-policy baseline was changed.
- Restored the missing `_userEditorInitialMode` backing field in `DxFunctionCatalog.razor`, resolving the five reported `CS0103` compiler errors while retaining the simple Source and advanced Pipeline entry points.

## 3.2.3 functionality retained

- Simple JSON/OData user AI Functions remain backed by the existing Remote Control connector/pipeline architecture.
- X Functions & automation remain exposed through the existing Council X-Round workflow controls.
- Learning Rounds retain source-backed project synchronization: exact project/version/SDK/framework metadata, chat upload workspace root, revision hash, and complete tracked repository structure are persisted through the existing project database model.
- Project briefing remains source-grounded and must not invent .NET 7/8 requirements when the repository declares .NET 10.
- The `xRoundCause ?? string.Empty` recovery warning repair remains present.
- Existing InteractiveServer render-mode boundaries, `Chat.razor`, `Chat.razor.css`, EF schema/migrations, and 1-Wire protocol version are unchanged.

## Validation boundary

- The user's Windows .NET 10 build of 3.2.3 exposed the ownership, iterator, and missing-field failures fixed here.
- This 3.2.4 archive is source-only in the preparation environment: no `dotnet`, MSBuild, NuGet restore/build/publish/pack, EF command, or GitHub access was used while preparing it.

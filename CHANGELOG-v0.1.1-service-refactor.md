# LocalGPT v0.1.1 service-boundary refactor

## Architecture comparison

- Rechecked the current `Michi0403/BlazorPublisher` PublisherStudio foundation and retained the profitable modular-monolith conventions: one composition root, thin Blazor/controller boundaries, explicit service ownership, deterministic source packaging, and business state outside UI components.
- Kept LocalGPT-specific safety boundaries and did not copy source code from the reference repository.

## Static retirement

- Removed the former `Extensions/PlainStatics` runtime container.
- Replaced `CouncilChatStaticsGeneral` and its broad call surface with DI-owned services, including `CouncilRuntimeService`, `CouncilTextService`, `DevExpressChatService`, `SqliteUtilityService`, `SqliteGridPresentationService`, `NavigationUrlService`, `AiDiscoveryService`, `LoggingConfigurationService`, and `LocalGptCatalogService`.
- Moved diagnostic endpoint behavior into controllers.
- Updated Blazor pages and application services to consume injected services rather than global helper state.
- Preserved only valid static forms: application entry points, pure extension methods, generated regex accessors, immutable constants/catalog values, logger null scopes, and security guards.

## Streaming

- Preserved incremental Council thinking and answer updates while routing streaming status creation through injected services.
- Kept response formatter state scoped per response so parallel or consecutive streams cannot share mutable buffers.

## Validation boundary

- No .NET/DevExpress compilation was performed in the cloud workspace because the required SDK and licensed package feed are unavailable.
- Structural source checks, DI registration checks, forbidden-static scans, JSON/XML parsing, archive extraction, and hash verification are performed before packaging.

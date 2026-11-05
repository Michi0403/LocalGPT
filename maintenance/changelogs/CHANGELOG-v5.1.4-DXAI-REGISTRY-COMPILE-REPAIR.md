# LocalGPT 5.1.4 — DXAI registry compile repair

- Removed the stale `serviceProvider` reference that caused CS0103 in `DxAiFunctionRegistry`.
- Preserved the no-captured-provider registry architecture instead of reintroducing deferred scoped-service resolution.
- Reads current persisted DXAIFunction catalog policy through `IDbContextFactory<LocalGptMemoryDbContext>` after database initialization, avoiding the registry ↔ catalog-service dependency cycle.
- Uses `nameof(DxAiFunctionCatalogEntry)` for the persisted catalog data type instead of introducing another raw system-variable string key, and keeps descriptor safety defaults as the bounded fallback when persisted policy cannot be read.
- Advanced LocalGPT source/cache metadata from 5.1.3 to 5.1.4.

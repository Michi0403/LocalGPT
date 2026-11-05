# LocalGPT 6.6.7 — Council model inventory and role assignments

- Fix model picker showing an empty list despite detected reachable Ollama models. `IsConfigured` is not a prerequisite for selecting reachable models; exact provider/endpoint/model identity is retained.
- Add Refresh provider models to Council Teams and make searchable per-provider eligible-model editor accessible for all AI roles, including AllSelected roles. Do not modify AllSelected behavior implicitly.
- Add model-to-role assignment dialog under ProviderModelPanel Properties: selects from database-owned teams/roles, searchable virtualized DevExpress multiple selection, explicit separate switch to exact-model selection policy, confirmed save using the existing CouncilTeamConfigurationService. Supplied team templates are preserved as user-owned copies on edit.
- Introduce indexed EF tables `CouncilRoleIndexes` and `CouncilRoleModelBindings`, with migration/snapshot and backfill from existing stored `RolesJson`. Team JSON remains authoritative for runtime and is synchronized to these normalized lookup tables during confirmed saves.
- Add business objects, interface, services, controller with approval guard, dependency injection and build-wired PowerShell contract checks. No independent role policy source and no same-name cross-host substitution.
- Existing Windows/Linux/macOS runtime targets, Council backend observer lifetime, native presentation and Hyperlight sandboxes unchanged.

## Verification limitation
Archive and offline source checks completed. There is no .NET SDK or PowerShell installed in this execution environment; no successful dotnet build or live Ollama/Razor/EF migration integration has been established. Build and migration require host testing before treating the release as production verified.

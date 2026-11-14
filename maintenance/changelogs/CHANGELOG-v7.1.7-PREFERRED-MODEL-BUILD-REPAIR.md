# LocalGPT 7.1.7 — Preferred-model editor compile repair

- Fixed the remaining `CS0104` in `src/LocalGPT/Components/Pages/DataManagement.PreferredModels.razor.cs`: `new ConfigurationRoot` now explicitly constructs `LocalGPT.BusinessObjects.ConfigurationRoot`. The corresponding goal-execution constructor was already qualified in 7.1.6; this second call site had been missed.
- Extended the existing interactive forms preflight to detect a reintroduced unqualified constructor at this call site, before compilation.
- The PowerShell `LGPS5101` errors shown on November 14 were triggered by the previously provided standalone cleanup script copied into the LocalGPT source root. That script is not included in either the 7.1.6 or 7.1.7 source ZIP; after running it, it should be kept outside the repository or removed. No repository-wide PowerShell guard was relaxed.
- No changes to Council selection behavior, saved preferences, 1-Wire/MCP/SignalR wire formats, OAuth/authentication, native installers, inference scheduling or user data.
- LocalGPT application version 7.1.7; setup/installer version remains 5.0.3.

This change is based on the user-supplied Windows Debug build logs dated November 14, 2026. The full .NET build was not run in the preparation environment, where `dotnet` and `pwsh` are unavailable.

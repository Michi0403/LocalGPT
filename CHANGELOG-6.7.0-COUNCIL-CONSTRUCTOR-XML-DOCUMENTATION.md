# LocalGPT 6.7.0 — Council constructor XML documentation build repair

- Fixed CS1573 in `CouncilTeamConfigurationService`: the injected `CouncilRoleAssignmentIndexService roleIndex` constructor parameter now has a matching XML `<param>` documentation tag.
- Extended the existing, mandatory `build/Assert-CouncilXRoundWiring.ps1` contract to compare every documented and declared argument of this Council constructor, detecting omitted and stale parameter tags. No compiler diagnostics or policies were suppressed.
- Updated the LocalGPT application version to 6.7.0. All application behavior, Council role assignments, EF Core schema/migrations, Model/Chat UI, Hyperlight provisioner, Windows/Linux/macOS RIDs, and unrelated projects remain as in 6.6.9.
- Source and archive checks are offline only; no full .NET compile or PowerShell execution was possible in this environment.
- NuGet's separate `LocalGPTWebviewWrapper` project-load/restore warning from Visual Studio is not diagnosed or fixed by this documentation patch.

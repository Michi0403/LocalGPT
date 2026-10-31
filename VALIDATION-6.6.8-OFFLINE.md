# LocalGPT 6.6.8 — offline source/package verification

## Reported Windows build errors

- `ProviderModelRoleEditor.razor(21,82) CS1503`: DevExpress `DxComboBox` change handler was a bare method group and the generated component could not infer `EventCallback<string>`.
- Two generated `CS1662` diagnostics in the same Razor component were cascading type-inference failures.

## Source corrections

- Explicitly specify `DxComboBox TData="TeamOption" TValue="string"` and a `ValueChanged` lambda typed as `string`.
- Keep the role `DxListBox TData="RoleOption" TValue="string"` and use an explicit `IEnumerable<string>` lambda for `ValuesChanged`.
- Add the two signatures to the pre-existing, build-wired version-independent `Assert-CouncilXRoundWiring.ps1`. No PowerShell policy is disabled.
- Increase `LocalGPT.csproj` version to `6.6.8`; no execution, persistence, role migration, or provider inventory code changes.

## Offline checks

- All 2,332 files from the exact 6.6.7 source archive retained, with no deletion.
- Exactly three original files changed: `ProviderModelRoleEditor.razor`, `Assert-CouncilXRoundWiring.ps1`, `LocalGPT.csproj`.
- Razor callback syntax reviewed against DevExpress Blazor 25.2 `DxComboBox.ValueChanged` (`EventCallback<TValue>`) and `DxListBox.ValuesChanged` (`EventCallback<IEnumerable<TValue>>`) documentation and against the working typed list callback in `CouncilTeams.razor`.
- `Directory.Build.targets` and `LocalGPT.csproj` parsed as XML; Council guard is still build-wired.
- All 28 JavaScript diagnostics SHA-256 hashes matched after newline normalization.
- All seven runtime identifiers retained: win-x64, win-x86, win-arm64, linux-x64, linux-arm64, osx-x64, osx-arm64.
- Source ZIP member inventory, SHA-256 comparison to the 6.6.7 baseline, and ZIP integrity verified.

## Limitations

- This environment has **no `dotnet`, Windows PowerShell, or `pwsh` executable**. The Windows Visual Studio/Razor build, PowerShell guard execution, EF migrations, and live DevExpress UI interactions are **not verified**.
- A successful compile is therefore a required downstream acceptance test, not a claimed outcome of this offline repair.

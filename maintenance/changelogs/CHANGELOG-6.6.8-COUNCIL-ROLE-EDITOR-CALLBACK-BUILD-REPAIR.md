# LocalGPT 6.6.8 — Council role editor callback build repair

## Corrected

- `ProviderModelRoleEditor.razor`: set explicit `TData="TeamOption"` and `TValue="string"` on the DevExpress ComboBox and provide a typed `EventCallback<string>` lambda for `ValueChanged`. This resolves the Razor-generated method-group/`EventCallback` mismatch reported on line 21 in 6.6.7.
- `ProviderModelRoleEditor.razor`: make the existing multi-select `DxListBox` `ValuesChanged` lambda explicitly `IEnumerable<string>` to match the DevExpress 25.2 callback contract, consistent with `CouncilTeams.razor`.
- Extend the existing build-wired, version-independent `Assert-CouncilXRoundWiring.ps1` source guard to preserve typed callbacks in the role editor.

## Preserved

- Model role assignment flows, indexed EF role tables and migration, saved-team execution semantics, provider-qualified model identities, Ollama inventory fixes, confirmation and protected-team copying.
- Council, CodeDOM, Hyperlight, PowerShell validation, and seven Windows/Linux/macOS runtime identifiers.
- No Python or JavaScript test suites added; no existing validation disabled.

## Verification scope

Offline static source-equivalence checks, XML parsing, JavaScript SHA-256 manifest verification, version checks, and ZIP integrity can run in this packaging environment. **PowerShell, .NET 10/Razor compilation, database migration execution, and DevExpress UI runtime behavior have not been executed here.** Build acceptance requires a Windows/Linux/macOS development machine with the project toolchain.

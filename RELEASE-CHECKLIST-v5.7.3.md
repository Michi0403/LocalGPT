# LocalGPT 5.7.3 — Release checklist

## Changed existing files (7)

- `build/Assert-AsyncContinuationPolicy.ps1`
- `build/Assert-ThemeArchitecture.ps1`
- `build/Invoke-ArchitectureAudit.ps1`
- `src/LocalGPT/Components/Layout/ThemeSwitcher.razor`
- `src/LocalGPT/LocalGPT.csproj`
- `src/LocalGPT/wwwroot/css/localgpt-theme-contract.css`
- `src/LocalGPT/wwwroot/switcher-resources/css/theme-switcher.css`

## Added files (3)

- `CHANGELOG-v5.7.3-BUILD-GATES-THEME-REPAIR.md`
- `RELEASE-CHECKLIST-v5.7.3.md`
- `VALIDATION-v5.7.3-source.md`

## Deleted files

None. Every file from 5.7.2 is still present.

## Windows acceptance checklist

- [ ] Run Debug build under Windows PowerShell 5.1: async retry exception accepted; all other async rules enforced.
- [ ] Run the runtime text/Regex audit: `RegexEngineService` accepted, other unapproved `new Regex` rejected.
- [ ] Verify `LocalGPT.dll` is generated; then build `LocalGPTWebviewWrapper` without cascade CS0006/WMC1006.
- [ ] Hard-reload Office White and confirm shell nav is light, text dark, icons/hover readable.
- [ ] Switch to Fluent Dark and another light theme, confirm drawer tracks the shell and DevExpress components track their separately chosen theme.
- [ ] Open Theme Fusion, review all three existing columns on desktop and mobile widths; close and verify popup is inert/hidden.
- [ ] Run Release/DocFX/Pages only after Debug verification; inspect generated output and PowerShell 5.1 scripts.

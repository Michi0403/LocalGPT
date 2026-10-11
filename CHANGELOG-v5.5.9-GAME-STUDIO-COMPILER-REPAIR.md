# LocalGPT 5.5.9 — Game Studio C# compiler repair

## Fixed from the owner's 22:26 Visual Studio build

1. **CS1061 (`GameStudio.razor.cs:56`):** Added `using Microsoft.JSInterop;` to the Game Studio code-behind so the framework extension `IJSRuntime.InvokeVoidAsync` resolves. The existing interactive `OnAfterRenderAsync` render boundary and guarded diagnostics stay unchanged.
2. **CS1503 (`GameStudio.razor:31`):** The `DxComboBox` project selector now uses the same typed `ValueChanged` callback pattern as maintained Chat and Data Management selectors: `@((Guid value) => SelectProjectAsync(value))`. It retains the `Guid` value, DevExpress editor, async service lookup and current project selection behavior.
3. **CS1662 (generated `GameStudio_razor.g.cs`):** The compiler's generated lambda/delegate diagnostic accompanies the project selector's incompatible method-group callback. The typed adapter addresses that source-side mismatch; no generated compiler files are edited.

## Retention and regression controls

- No Game Studio functionality, Godot revision features, business objects, data-service behavior or DevExpress controls were removed.
- Extended the maintained Game Studio source-only regression audit with explicit checks for the JavaScript extension namespace and the typed DevExpress value callback. Other Game Studio, Chat revision, learning, Data Management and architecture checks remain intact.
- Advanced the LocalGPT source version **5.5.8 → 5.5.9** and existing frontend cache identifiers. The next version must roll over to 5.6.0, not 5.5.10.
- PublisherStudio was not changed.

## Verification boundary

Source-only checks and ZIP integrity are recorded in `RELEASE-CHECKLIST-v5.5.9.md`. No .NET/Godot compiler or GitHub access was used. These two source fixes target the errors in the supplied Windows build log; the owner's next Visual Studio build is the authoritative compiler result.

# LocalGPT 6.3.6 — architecture-compliant build repair

Source-level correction of the Visual Studio failures reported against LocalGPT 6.3.5. No architecture, XML-documentation, runtime-policy, DevExpress, or diagnostics guards were relaxed or disabled.

- Replaced Chat Keyaccount HTML checkbox/numeric inputs with typed DevExpress `DxCheckBox` and `DxSpinEdit` controls with the existing limits preserved.
- Restored the owned FormLayout-native-neutral structure for the physically grouped Ollama setup host: CSS style and host grouping remain, but its container is a `<div>` rather than an interactive-page `<section>`.
- Preserved post-initialization `runtimePolicy.Reload()` and corrected the fallback diagnostic to explicitly state that the built-in seed remains active; no error was swallowed newly.
- Converted the new static Keyaccount JSON extraction helper into an instance helper to satisfy the application static policy.
- Moved the UI-generated missing-goal-requirement string join to the already-injected `CouncilTextService`, shared by Model Council and Minecraft Mod Builder.
- Reviewed the operational-feed scroll JS and added error diagnostics with guarded try/catch. Registered its normalized SHA-256 in the existing diagnostics manifest; no audit bypasses.
- Added the two missing XML `<param>` descriptions to `AiProviderBootstrapService` for `databaseFactory` and `databaseInitializer`, and removed the unused legacy Knowledge constructor parameter causing CS9113; preserved provider discovery and DB startup behavior.
- Added offline tests covering the same architecture regressions, updated existing source-version assertions to 6.3.6 and bumped the main application version from 6.3.5 to 6.3.6.

## Validation

- Offline Python source tests and source-contract checks; full .NET 10 / licensed DevExpress Windows compilation is not available in this environment.
- Requires Visual Studio build verification; source test success must not be interpreted as runtime or compiler proof.

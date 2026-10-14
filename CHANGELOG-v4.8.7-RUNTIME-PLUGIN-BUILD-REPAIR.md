# LocalGPT 4.8.7 — runtime plugin build repair

## Fixed

- Fixed three `await using` policy violations in `RuntimePluginExecutionHost.LoadCompiledPackageAsync`. Each asynchronous stream disposal is now explicitly configured with `ConfigureAwait(false)`.
- Fixed `CS0236` in `Install.RuntimePlugins.razor.cs` by removing the instance-method call from the field initializer and creating the default runtime-plugin draft during `Install.OnInitialized()`.

## Preserved

- Dynamic `DxAiFunctionRegistry` runtime-extension registration and the existing approval/deferred-execution path.
- Persisted `RuntimePluginDefinition` BusinessObjects and runtime-plugin service ownership.
- Generic/custom compiler configuration and common toolchain autodiscovery.
- Existing ESP32/Arduino/PlatformIO support and runtime-extension UI.

## Validation

- The repository Python async-continuation audit is run source-side for this handoff.
- Active release identity is synchronized to 4.8.7.
- No .NET build is claimed; the user's Windows build remains authoritative.
